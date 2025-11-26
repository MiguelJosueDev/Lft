# LFT Intelligent Path Discovery System

## Problem Statement

El proyecto Artemis tiene múltiples aplicaciones con variaciones en:
- Nombres de namespace (`LiveFree.Accounts`, `LiveFree.Artemis.Ticketing`, `LiveFree.Shell`)
- Nombres de archivos de extensión (`AccountsServicesExtensions` vs `CellularServicesExtension`)
- Estructura de carpetas (algunas apps tienen `Host`, otras no)
- Métodos de registro (`AddAccountsServices` vs `AddCellularServices`)

## Solution Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      LFT Generation Pipeline                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────┐ │
│  │ ProjectAnalyzer │───▶│ ProjectManifest  │───▶│ StepExecutor│ │
│  └─────────────────┘    └──────────────────┘    └─────────────┘ │
│          │                       │                      │        │
│          ▼                       ▼                      ▼        │
│  ┌─────────────────┐    ┌──────────────────┐    ┌─────────────┐ │
│  │NamespaceResolver│    │InjectionLocator  │    │ CodeInjector│ │
│  └─────────────────┘    └──────────────────┘    └─────────────┘ │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. ProjectAnalyzer

Analiza la estructura del proyecto y genera un `ProjectManifest`.

```csharp
public interface IProjectAnalyzer
{
    Task<ProjectManifest> AnalyzeAsync(string profileRoot, CancellationToken ct = default);
}
```

**Responsabilidades:**
- Detectar tipo de proyecto (API, Functions, Hybrid)
- Encontrar carpetas de cada capa (Api, Services, Repositories, Models)
- Detectar convenciones de nombres
- Identificar archivos de extensión existentes

### 2. ProjectManifest

Contiene toda la información descubierta del proyecto.

```csharp
public sealed record ProjectManifest
{
    // Identity
    public string AppName { get; init; }              // "Accounts", "Transactions"
    public string BaseNamespace { get; init; }        // "LiveFree.Accounts"

    // Layers (discovered paths)
    public LayerInfo Api { get; init; }
    public LayerInfo Services { get; init; }
    public LayerInfo Repositories { get; init; }
    public LayerInfo Models { get; init; }
    public LayerInfo? Functions { get; init; }
    public LayerInfo? Host { get; init; }

    // Conventions (auto-detected)
    public NamingConventions Conventions { get; init; }

    // Injection Points (discovered)
    public IReadOnlyList<InjectionPoint> InjectionPoints { get; init; }
}

public sealed record LayerInfo
{
    public string Path { get; init; }                 // Full path to layer folder
    public string Namespace { get; init; }            // Detected namespace
    public string ProjectName { get; init; }          // .csproj name

    // Key folders within layer
    public string? ExtensionsPath { get; init; }
    public string? EntitiesPath { get; init; }
    public string? MappersPath { get; init; }
    public string? EndpointsPath { get; init; }
    public string? RoutesPath { get; init; }
}

public sealed record NamingConventions
{
    public string ServiceExtensionClass { get; init; }    // "AccountsServicesExtensions"
    public string ServiceExtensionMethod { get; init; }   // "AddAccountsServices"
    public string RoutesExtensionClass { get; init; }     // "AccountsRoutesExtensions"
    public string RoutesExtensionMethod { get; init; }    // "AddAccountsRoutes"
    public string RepoExtensionMethod { get; init; }      // "AddAccountsRepositories"
    public string MappingProfileClass { get; init; }      // "AccountsMappingProfile"
    public bool UsesSingularExtension { get; init; }      // false = "Extensions", true = "Extension"
}

public sealed record InjectionPoint
{
    public InjectionTarget Target { get; init; }
    public string FilePath { get; init; }
    public string ClassName { get; init; }
    public string MethodName { get; init; }
    public string? TokenMarker { get; init; }             // "LFT-TOKEN - Services -"
    public InjectionPosition DefaultPosition { get; init; }
}

public enum InjectionTarget
{
    ServiceRegistration,      // Services/Extensions/ServiceRegistrationExtensions.cs
    EndpointRegistration,     // Api/Extensions/[App]ServicesExtensions.cs
    RouteRegistration,        // Api/Extensions/[App]RoutesExtensions.cs
    RepositoryRegistration,   // Repositories/Extensions/ServiceRegistrationExtensions.cs
    MappingProfile,           // Repositories/Mappers/[App]MappingProfile.cs
    MqlQuery                  // Repositories/Extensions/ServiceRegistrationExtensions.cs
}
```

### 3. NamespaceResolver

Detecta namespaces analizando archivos existentes.

```csharp
public interface INamespaceResolver
{
    string? ResolveFromFile(string csFilePath);
    string? ResolveFromDirectory(string directory);
    string InferNamespace(string basePath, string appName, string layer);
}
```

**Algoritmo:**
1. Buscar archivos `.cs` en el directorio
2. Parsear `namespace X.Y.Z;` con regex
3. Si no hay archivos, inferir basándose en estructura de carpetas

### 4. InjectionPointLocator

Encuentra puntos de inyección en archivos existentes.

```csharp
public interface IInjectionPointLocator
{
    Task<IReadOnlyList<InjectionPoint>> LocateAsync(
        string searchRoot,
        InjectionTarget target,
        CancellationToken ct = default);
}
```

**Estrategias de búsqueda:**

| Target | Search Pattern | File Pattern |
|--------|---------------|--------------|
| ServiceRegistration | `**/Services/**/Extensions/ServiceRegistrationExtensions.cs` | Method: `Add*Services` |
| EndpointRegistration | `**/Api/**/Extensions/*ServicesExtension*.cs` | Method: `Add*Services` |
| RouteRegistration | `**/Api/**/Extensions/*RoutesExtension*.cs` | Method: `Add*Routes` |
| RepositoryRegistration | `**/Repositories*/**/Extensions/ServiceRegistrationExtensions.cs` | Method: `Add*Repositories` |
| MappingProfile | `**/Repositories*/**/Mappers/*MappingProfile.cs` | Constructor |
| MqlQuery | `**/Repositories*/**/Extensions/ServiceRegistrationExtensions.cs` | `AddMqlQueries` lambda |

## Discovery Algorithm

### Phase 1: Structure Detection

```
1. Start at profileRoot
2. Detect api/ and app/ subdirectories
3. For each layer in api/:
   a. Find projects by .csproj files
   b. Match project name to layer type:
      - *.Api → API layer
      - *.Services → Services layer
      - *.Repositories.* → Repository layer
      - *.Models → Models layer
      - *.Functions → Functions layer
      - *.Host → Host layer
   c. Record paths and project names
```

### Phase 2: Namespace Detection

```
1. For each discovered layer:
   a. Find any .cs file in Extensions/ folder
   b. Extract namespace using regex: `namespace\s+([\w.]+)`
   c. Store base namespace (without .Extensions suffix)
2. Fallback: Infer from project name
   - "LiveFree.Accounts.Api" → namespace "LiveFree.Accounts.Api"
```

### Phase 3: Convention Detection

```
1. Find *ServicesExtension*.cs in Api/Extensions/
2. Parse class name → ServiceExtensionClass
3. Find public static method Add*Services → ServiceExtensionMethod
4. Determine singular vs plural by checking file name suffix
5. Repeat for Routes, Repositories
6. Find *MappingProfile.cs in Repositories/Mappers/
```

### Phase 4: Injection Point Location

```
For each target type:
1. Use glob pattern to find candidate files
2. Parse each file with Roslyn
3. Find matching class and method
4. Check for LFT-TOKEN comments
5. Record InjectionPoint with:
   - File path
   - Class name
   - Method name
   - Token marker (if found)
   - Default position (beginning/end based on target type)
```

## Template Variable Mapping

El `ProjectManifest` se convierte en variables para los templates:

```yaml
# Auto-discovered variables (prefixed with _)
_AppName: "Accounts"
_BaseNamespace: "LiveFree.Accounts"

# Layer paths
_ApiPath: "/path/to/LiveFree.Accounts.Api"
_ServicesPath: "/path/to/LiveFree.Accounts.Services"
_RepositoriesPath: "/path/to/LiveFree.Accounts.Repositories.SqlServer"
_ModelsPath: "/path/to/LiveFree.Accounts.Models"

# Layer namespaces
_ApiNamespace: "LiveFree.Accounts.Api"
_ServicesNamespace: "LiveFree.Accounts.Services"
_RepositoriesNamespace: "LiveFree.Accounts.Repositories.SqlServer"
_ModelsNamespace: "LiveFree.Accounts.Models"

# Conventions
_ServiceExtensionClass: "AccountsServicesExtensions"
_ServiceExtensionMethod: "AddAccountsServices"
_RoutesExtensionClass: "AccountsRoutesExtensions"
_RoutesExtensionMethod: "AddAccountsRoutes"
_RepoExtensionMethod: "AddAccountsRepositories"
_MappingProfileClass: "AccountsMappingProfile"

# Injection targets (resolved paths)
_InjectServiceTarget: "/path/to/ServiceRegistrationExtensions.cs"
_InjectEndpointTarget: "/path/to/AccountsServicesExtensions.cs"
_InjectRouteTarget: "/path/to/AccountsRoutesExtensions.cs"
_InjectRepoTarget: "/path/to/ServiceRegistrationExtensions.cs"
_InjectMappingTarget: "/path/to/AccountsMappingProfile.cs"
```

## Updated _index.yml Format

```yaml
name: Main
entryPoints:
  - name: Crud
    commandName: crud
    action: group
    steps:
      # CREATE steps use discovery for output paths
      - name: CreateModel
        action: create
        source: resources/api/models/model.liquid
        output: "{{ _ModelsPath }}/{{ _ModelName }}Model.cs"
        # OR use layer hint for auto-discovery
        layer: models
        fileName: "{{ _ModelName }}Model.cs"

      # INJECT steps use discovered injection points
      - name: InjectService
        action: inject
        target: ServiceRegistration  # Uses discovered injection point
        template: "services.AddScoped<I{{ _ModuleName }}Service, {{ _ModuleName }}Service>();"

      - name: InjectEndpoint
        action: inject
        target: EndpointRegistration
        template: "services.AddScoped<I{{ _ModuleName }}Endpoint, {{ _ModuleName }}Endpoint>();"

      - name: InjectRoute
        action: inject
        target: RouteRegistration
        template: "app.Map{{ _ModuleName }}Routes(basePrefix: basePrefix, prefix: \"{{ _routeModuleName }}\");"
        position: beginning

      - name: InjectMapping
        action: inject
        target: MappingProfile
        template: "CreateMap<{{ _ModelName }}Model, {{ _ModelName }}Entity>().ReverseMap();"
```

## Fallback Strategy

Si el descubrimiento automático falla:

1. **Profile config override**: Usar valores de `lft.config.json` si están definidos
2. **Convention-based inference**: Inferir basándose en el nombre del app
3. **Interactive prompt**: Preguntar al usuario (solo en modo interactivo)
4. **Error with guidance**: Mostrar error con instrucciones de configuración

```json
// lft.config.json - Fallback/Override configuration
{
  "profile": "accounts-app",
  "discovery": {
    "enabled": true,
    "overrides": {
      "baseNamespace": "LiveFree.Accounts",
      "mappingProfileClass": "AccountMappingProfile"
    }
  }
}
```

## Implementation Phases

### Phase 1: Core Discovery (MVP)
- [ ] `ProjectAnalyzer` basic implementation
- [ ] `NamespaceResolver` with regex parsing
- [ ] `LayerInfo` detection for Api, Services, Repositories, Models
- [ ] Integration with `VariableContext`

### Phase 2: Injection Point Location
- [ ] `InjectionPointLocator` implementation
- [ ] Roslyn-based method/class detection
- [ ] LFT-TOKEN marker support
- [ ] Update `StepExecutor` to use discovered points

### Phase 3: Convention Detection
- [ ] Auto-detect naming conventions from existing files
- [ ] Support for singular/plural variations
- [ ] Support for app-specific prefixes (Artemis.Ticketing)

### Phase 4: Robustness
- [ ] Caching of discovered manifests
- [ ] Fallback configuration
- [ ] Validation and error messages
- [ ] Unit tests for edge cases

## File Structure

```
src/Lft.Discovery/
├── Lft.Discovery.csproj
├── IProjectAnalyzer.cs
├── ProjectAnalyzer.cs
├── ProjectManifest.cs
├── LayerInfo.cs
├── NamingConventions.cs
├── InjectionPoint.cs
├── INamespaceResolver.cs
├── NamespaceResolver.cs
├── IInjectionPointLocator.cs
├── InjectionPointLocator.cs
└── Extensions/
    └── ProjectManifestExtensions.cs  # Convert to VariableContext
```

## Summary

Este sistema permite que LFT:
1. **Descubra automáticamente** la estructura de cualquier proyecto Artemis
2. **Detecte convenciones** analizando archivos existentes
3. **Encuentre puntos de inyección** sin configuración manual
4. **Genere código** con namespaces y paths correctos
5. **Sea extensible** para soportar nuevos patrones en el futuro
