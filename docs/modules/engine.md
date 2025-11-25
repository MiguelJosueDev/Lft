# Módulo: Engine

## Responsabilidad principal
- Cargar y parsear "Template Packs" (conjuntos de templates).
- Resolver variables de contexto (CLI args, convenciones, config).
- Renderizar templates (usando Liquid u otro motor).
- Orquestar la ejecución de pasos de generación (`create`, `group`).

## Qué NO debe hacer
- Escribir directamente a disco (usa `IFileWriter` o devuelve `GenerationResult`).
- Modificar archivos existentes (eso es responsabilidad de `Integration`).
- Interactuar con el usuario (eso es `Cli`).
- ❌ **Anti-ejemplo**: Leer directamente `Program.cs` del disco para decidir si aplica cambios.

## Proyectos relacionados
- `Lft.Engine` (Implementación principal)
- `Lft.Domain` (Modelos de entrada/salida)

## Interfaces y contratos públicos

### `ICodeGenerationEngine`
```csharp
public interface ICodeGenerationEngine
{
    Task<GenerationResult> GenerateAsync(
        GenerationRequest request, 
        CancellationToken cancellationToken = default);
}
```

### `ITemplateRenderer`
```csharp
public interface ITemplateRenderer
{
    string Render(string templateContent, IReadOnlyDictionary<string, object?> variables);
}
```

## Dependencias permitidas
- `Lft.Domain`
- `Lft.Abstractions`
- `Lft.Infrastructure` (para lectura de templates si es necesario, aunque idealmente se inyecta).

## Patrones internos
- **Strategy**: Para diferentes motores de renderizado.
- **Chain of Responsibility / Pipeline**: Para la resolución de variables.

## Flujos típicos

### Generación de código
1. `GenerateAsync(request)` recibe la solicitud.
2. `TemplatePackLoader` carga el pack especificado.
3. `VariableResolver` construye el contexto de variables.
4. `StepExecutor` itera sobre los pasos del pack, renderizando contenido y rutas.
5. Retorna `GenerationResult` con la lista de archivos virtuales.

## Open questions / TODOs
- [ ] Definir soporte para "partials" o includes en templates.
- [ ] Definir cómo manejar templates binarios (imágenes, etc.).
