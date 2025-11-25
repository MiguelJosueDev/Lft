# LFT – Arquitectura General

## Visión general
LFT es una CLI modular diseñada para la generación de código (CRUDs, APIs, UI) basada en templates. Su objetivo es automatizar tareas repetitivas de desarrollo manteniendo una arquitectura limpia y extensible.

## Capas de la arquitectura

La solución sigue una arquitectura en capas estricta:

1.  **Dominio / Contratos**: El núcleo estable. Define *qué* se hace, no *cómo*.
2.  **Aplicación / Casos de uso**: La lógica de orquestación (Pipelines). Coordina los motores de generación, integración y análisis.
3.  **Infraestructura**: Implementaciones concretas de acceso a disco, parsing de lenguajes (AST), etc.
4.  **Presentación / Entrada**: Puntos de entrada para el usuario (CLI, y en el futuro GUI).

## Proyectos por capa

| Capa | Proyecto | Descripción |
| :--- | :--- | :--- |
| **Dominio** | `Lft.Domain` | Modelos puros (GenerationRequest, FileChangePlan). |
| **Dominio** | `Lft.Abstractions` | Interfaces **estrictamente compartidas** (ICodeGenerationEngine, IFileWriter). |
| **Aplicación** | `Lft.App` (Conceptual) | Pipelines y Workflows (ej. `GenPipeline`). |
| **Aplicación** | `Lft.Engine` | Motor de generación con templates (Liquid). |
| **Aplicación** | `Lft.Integration` | Inserción de código en archivos existentes (Anchors). |
| **Aplicación** | `Lft.Diff` | Cálculo de diferencias (Old vs New). |
| **Aplicación** | `Lft.Analyzer.Core` | Reglas de arquitectura agnósticas. |
| **Aplicación** | `Lft.Migrate` | Orquestador de migraciones (Engine + AST + LLM). |
| **Infraestructura** | `Lft.Infrastructure` | I/O, Logging, HTTP clients. |
| **Infraestructura** | `Lft.Ast.CSharp` | Parsing y manipulación de C# (Roslyn). |
| **Infraestructura** | `Lft.Ast.JavaScript` | Parsing y manipulación de JS/TS. |
| **Presentación** | `Lft.Cli` | Comandos de consola (Thin Client). |

## Dependencias permitidas

- **Presentación** -> **Aplicación**, **Dominio**, **Infraestructura** (solo para DI).
- **Aplicación** -> **Dominio**, **Abstractions**.
- **Infraestructura** -> **Abstractions**, **Dominio**.
- **Dominio** -> *Ninguna*.

> **Regla de Oro**: `Lft.Domain` y `Lft.Abstractions` no deben depender de nada externo ni de otras capas.
> **Regla de Abstractions**: Solo entran en `Lft.Abstractions` contratos que se usan en múltiples módulos y que deben ser muy estables. Todas las demás interfaces viven dentro del módulo que las usa.

## Flujo de datos (Data Flow)

El siguiente diagrama ilustra cómo fluyen los datos a través de los módulos durante un comando típico `gen`:

```mermaid
flowchart LR
    CLI[CLI / App Pipeline] -->|GenerationRequest| Engine
    Engine -->|GenerationResult| Integration
    Integration -->|FileChangePlan[]| Diff
    Diff -->|FileDiff[]| CLI
    CLI -->|Aceptar / rechazar| Writer[Infrastructure Writer]
    Writer --> Filesystem
```

1.  **CLI/Pipeline**: Construye el `GenerationRequest`.
2.  **Engine**: Genera archivos en memoria (`GenerationResult`).
3.  **Integration**: Calcula cómo aplicar esos cambios (`FileChangePlan`).
4.  **Diff**: Compara el plan con el disco (`FileDiff`).
5.  **Writer**: Si el usuario aprueba, persiste los cambios.

## Responsabilidad de I/O

**Importante**:
- `Engine`, `Integration`, `Diff`, `Analyzer` y `Migrate` **NUNCA** escriben a disco. Son funciones puras o de solo lectura.
- La escritura es responsabilidad exclusiva de `Lft.Infrastructure` (ej. `IFileWriter`), invocada únicamente por la capa de Aplicación (Pipelines) o la CLI al final del flujo.

## Comandos principales

- `lft gen`: Genera código nuevo a partir de templates.
- `lft analyze`: Valida reglas de arquitectura en el código existente.
- `lft diff`: Muestra diferencias entre versiones o cambios propuestos.
- `lft migrate`: Asiste en la migración de código de una versión de template a otra.

## Principios de diseño

- **Single Responsibility**: Cada módulo hace una sola cosa bien.
- **Filosofía Unix**: Herramientas pequeñas que se pueden componer.
- **Agnosticismo**: El Core no debe saber de CLI ni de GUI.
- **Extensibilidad**: Nuevos lenguajes o motores se agregan como nuevos proyectos de Infraestructura o Aplicación.

## Evolución futura (TODO)

- [ ] `Lft.Gui`: Interfaz gráfica para configuración visual.
- [ ] `Lft.Stats`: Módulo de estadísticas de uso y generación.
- [ ] Soporte para plugins externos.
