# Módulo: Migrate

## Responsabilidad principal
- **Orquestar** la actualización de código generado.
- Combinar servicios de Engine, AST e Integration.
- Preservar lógica custom del usuario durante la migración.

## Qué NO debe hacer
- **No implementa** parsing de bajo nivel (usa `Lft.Ast`).
- **No implementa** renderizado (usa `Lft.Engine`).
- Ejecutarse sin supervisión (siempre requiere confirmación/diff).
- ❌ **Anti-ejemplo**: Tener lógica duplicada de cómo renderizar un template.

## Proyectos relacionados
- `Lft.Migrate`

## Interfaces y contratos públicos

### `IMigrationService`
```csharp
public interface IMigrationService
{
    Task<MigrationProposal> ProposeMigrationAsync(
        string filePath, 
        string targetVersion, 
        CancellationToken ct);
}
```

## Dependencias permitidas
- `Lft.Engine` (para generar la versión "v2" limpia).
- `Lft.Ast.*` (para leer el código "v1" sucio).
- `Lft.Integration` (para mezclar).

## Patrones internos
- **Pipeline**: Leer V1 -> Generar V2 -> Extraer Custom V1 -> Inyectar en V2.

## Flujos típicos

### Migración de Archivo
1. Leer archivo actual (V1).
2. Generar versión nueva base (V2) usando `Engine`.
3. Identificar bloques de código custom en V1 (métodos no generados, lógica en anchors).
4. Inyectar bloques custom en V2.
5. (Opcional) Usar LLM para adaptar lógica si las firmas cambiaron.
6. Retornar propuesta de migración.

## Open questions / TODOs
- [ ] Definir prompt engineering para el LLM.
- [ ] Costo y latencia de llamadas a LLM.
