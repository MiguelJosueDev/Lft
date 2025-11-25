# Módulo: Analyzer

## Responsabilidad principal
- Analizar la estructura del proyecto existente.
- Validar reglas de arquitectura (ej. "Infra no depende de UI").
- Detectar violaciones de convenciones de nombres.

## Qué NO debe hacer
- Modificar código.
- Generar código.
- ❌ **Anti-ejemplo**: Intentar arreglar automáticamente una violación (eso sería un "Fixer" o "Migrate").

## Proyectos relacionados
- `Lft.Analyzer.Core`
- `Lft.Analyzer.CSharp` (Implementación específica).

## Interfaces y contratos públicos

### `IArchitectureAnalyzer`
```csharp
public interface IArchitectureAnalyzer
{
    Task<AnalysisReport> AnalyzeAsync(string projectPath, AnalysisOptions options);
}
```

## Dependencias permitidas
- `Lft.Domain`
- `Lft.Ast.CSharp` (para entender el código).

## Patrones internos
- **Visitor**: Para recorrer el AST y verificar reglas.
- **Rule Engine**: Motor para ejecutar lista de reglas configuradas.

## Flujos típicos

### Ejecutar Análisis
1. Cargar configuración de reglas (`lft.arch.yml`).
2. Parsear solución/proyecto para construir grafo de dependencias.
3. Evaluar cada regla contra el grafo.
4. Generar reporte de violaciones.

## Open questions / TODOs
- [ ] Definir formato de `lft.arch.yml`.
- [ ] Soporte para "suppressions" (ignorar reglas específicas).
