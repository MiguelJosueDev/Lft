# Módulo: Integration

## Responsabilidad principal
- Analizar archivos existentes para encontrar puntos de inserción (Anchors).
- Generar planes de modificación (`FileChangePlan`) para archivos existentes.
- Insertar código nuevo en posiciones específicas (Before/After anchor).

## Qué NO debe hacer
- Generar el código nuevo (eso lo hace `Engine`).
- Escribir a disco (devuelve el plan, `Cli` o `Infrastructure` escriben).
- Parsear AST complejo (delegar a `Lft.Ast.*`).
- ❌ **Anti-ejemplo**: Recibir un string de código y escribirlo directamente al archivo sin pasar por un Plan.

## Proyectos relacionados
- `Lft.Integration`

## Interfaces y contratos públicos

### `IFileIntegrationService`
```csharp
public interface IFileIntegrationService
{
    Task<FileChangePlan> IntegrateAsync(
        string filePath, 
        string newFragment, 
        IntegrationOptions options);
}
```

## Dependencias permitidas
- `Lft.Domain`
- `Lft.Abstractions`
- `Lft.Ast.CSharp` (opcional, si usa AST para encontrar puntos).

## Patrones internos
- **Strategy**: Diferentes estrategias de inserción (Anchor, Regex, AST-based).

## Flujos típicos

### Inserción por Anchor
1. Recibe `GenerationResult` y ruta del archivo destino.
2. Lee el contenido actual del archivo.
3. Busca el token de anchor (ej. `// LFT-ANCHOR: METHODS`).
4. Genera el nuevo contenido insertando el bloque generado.
5. Retorna `FileChangePlan` con `OldContent` y `NewContent`.

## Open questions / TODOs
- [ ] Definir estrategia de "Idempotencia" (no insertar si ya existe).
- [ ] Manejo de conflictos si el anchor no existe.
