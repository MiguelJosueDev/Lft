# Módulo: Lft.Analyzer.Core (Application Layer)

## Visión General
`Lft.Analyzer.Core` es el motor de análisis estático de arquitectura. Su propósito es validar que el código del proyecto cumpla con las reglas arquitectónicas definidas (ej. Clean Architecture), independientemente del lenguaje de programación subyacente.

Este módulo define el modelo abstracto del proyecto (`ArchNode`), las reglas (`IRule`) y el motor de ejecución (`AnalyzerEngine`).

## Componentes Principales

### 1. Modelo de Arquitectura (`ArchNode`)
Representa una unidad de código (clase, interfaz, módulo) de forma agnóstica al lenguaje.
-   **Propiedades**:
    -   `Id`: Identificador único (ej. nombre completo de la clase).
    -   `Layer`: Capa arquitectónica a la que pertenece (Domain, Application, Infrastructure, Presentation).
    -   `DependsOnIds`: Lista de dependencias hacia otros nodos.
    -   `Metadata`: Información extra (ej. atributos, modificadores).

### 2. Motor de Análisis (`AnalyzerEngine`)
Orquesta el proceso de validación:
1.  **Carga**: Utiliza `IProjectModel` (implementado por `Lft.Ast.*`) para cargar todos los archivos del proyecto y convertirlos en una lista de `ArchNode`.
2.  **Evaluación**: Itera sobre una colección de `IRule` configuradas.
3.  **Reporte**: Genera un `AnalysisReport` con todas las violaciones encontradas.

### 3. Reglas (`IRule`)
Define una restricción arquitectónica que debe cumplirse.
-   **Contrato**: `EvaluateAsync(IEnumerable<ArchNode> nodes) -> IEnumerable<Violation>`.
-   **Ejemplos de Reglas**:
    -   `LayerDependencyRule`: Verifica que las dependencias fluyan en la dirección correcta (ej. Domain no debe depender de Infrastructure).
    -   `NamingConventionRule`: Verifica que las clases sigan convenciones de nombres (ej. Interfaces empiezan con 'I').

### 4. Reporte (`AnalysisReport`)
Contiene el resultado del análisis:
-   `Violations`: Lista de problemas encontrados, con severidad y ubicación.
-   `Score`: Puntuación de salud arquitectónica (opcional).

## Interacción con otras Capas
-   **Usa**: `Lft.Ast.CSharp` (o JS) para poblar el modelo de nodos.
-   **Es usado por**: `Lft.Cli` (comando `lft analyze`) y `Lft.Migrate` (para evaluar el estado antes de migrar).

## Extensibilidad
Para agregar una nueva regla:
1.  Implementar la interfaz `IRule`.
2.  Definir la lógica de evaluación sobre los `ArchNode`.
3.  Registrar la regla en la configuración del `AnalyzerEngine`.

## Ejemplo de Flujo
1.  Usuario ejecuta `lft analyze`.
2.  `AnalyzerEngine` llama a `CSharpCodebaseLoader` (de `Lft.Ast.CSharp`).
3.  El loader parsea los `.cs` y devuelve una lista de `ArchNode` con sus dependencias resueltas.
4.  `AnalyzerEngine` ejecuta `LayerDependencyRule`.
5.  La regla detecta que `UserEntity` (Domain) depende de `SqlDataReader` (Infrastructure).
6.  Se genera una `Violation` y se muestra en el reporte final.
