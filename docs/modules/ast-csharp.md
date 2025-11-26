# Módulo: Lft.Ast.CSharp (Infrastructure Layer)

## Responsabilidad principal
- Parsing y manipulación de código C# utilizando **Roslyn** (Microsoft.CodeAnalysis).
- **Code Injection**: Insertar métodos, propiedades o sentencias en clases existentes de forma segura y respetando la sintaxis.
- **Syntax Validation**: Validar que el código generado sea compilable.
- **Namespace Extraction**: Leer archivos para extraer su declaración de namespace.

## Componentes Clave

### 1. CSharpInjectionService (`ICSharpInjectionService`)
- Permite inyectar código en métodos, constructores o clases.
- **Preservación de Formato**:
    - Utiliza `NormalizeWhitespace()` de forma quirúrgica (solo en el nodo modificado) para evitar reformatear archivos completos y generar diffs innecesarios.
- **Idempotencia**: Verifica si el código a inyectar ya existe (análisis semántico o textual simple) para evitar duplicados.

### 2. CSharpSyntaxValidator (`ICSharpSyntaxValidator`)
- Parsea el código generado en memoria.
- Verifica diagnósticos de nivel `Error`.
- Previene que la CLI escriba código roto en el disco.

### 3. CSharpCodebaseLoader
- Carga un proyecto completo y construye un grafo de `ArchNode` para el módulo `Analyzer`.
- Resuelve dependencias entre clases y capas.

## Dependencias
- `Microsoft.CodeAnalysis.CSharp`
- `Microsoft.CodeAnalysis.Workspaces.MSBuild` (opcional, para carga de proyectos completos).

## Uso típico
```csharp
// Inyección de una línea en un método
var request = new CodeInjectionRequest(
    FilePath: "path/to/Program.cs",
    ClassNameSuffix: "Program", 
    MethodName: "ConfigureServices",
    Snippet: "services.AddScoped<IUserService, UserService>();",
    Position: CodeInjectionPosition.End
);

await _injectionService.InjectIntoMethodAsync(request);
```
