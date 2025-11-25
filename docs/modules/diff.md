# Módulo: Diff

## Responsabilidad principal
- Comparar dos cadenas de texto (Original vs Nuevo).
- Generar un modelo de diferencias (`FileDiff`, `DiffHunk`, `DiffLine`).
- Formatear el diff para visualización (colores, formato git-like).

## Qué NO debe hacer
- Decidir qué archivos cambiar.
- Escribir a disco.
- ❌ **Anti-ejemplo**: Preguntar al usuario si quiere aplicar el cambio (eso es UI/CLI).

## Proyectos relacionados
- `Lft.Diff`

## Interfaces y contratos públicos

### `IFileDiffService`
```csharp
public interface IFileDiffService
{
    FileDiff Compute(string filePath, string oldText, string newText);
}
```

## Dependencias permitidas
- `Lft.Domain` (si `FileDiff` está ahí, o definirlo internamente).

## Patrones internos
- **Algoritmo LCS (Longest Common Subsequence)**: Para calcular el diff mínimo.

## Flujos típicos

### Calcular Diff
1. Recibe `oldText` y `newText`.
2. Ejecuta algoritmo de comparación.
3. Retorna objeto `FileDiff` con la lista de cambios (líneas agregadas, eliminadas, mantenidas).

## Open questions / TODOs
- [ ] Soportar diffs de lado a lado (side-by-side) en el futuro?
- [ ] Optimización para archivos muy grandes.
