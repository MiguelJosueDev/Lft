# Módulo: Lft.App (Application Layer)

## Visión General
`Lft.App` es el corazón de la lógica de negocio de la CLI. Actúa como la capa de orquestación que coordina los distintos motores (`Engine`, `Integration`, `Ast`) para cumplir con los casos de uso del usuario (Generación, Análisis, Migración).

Su responsabilidad principal es definir y ejecutar **Pipelines**. Un Pipeline es una secuencia ordenada de pasos que transforman una solicitud (`GenerationRequest`) en un resultado tangible (archivos creados o modificados).

## Componentes Principales

### 1. Pipelines (`GenPipeline`)
El `GenPipeline` es el flujo principal para el comando `lft gen`.
**Flujo de Ejecución:**
1.  **Context Setup**: Prepara el entorno, carga variables desde configuración, argumentos CLI y convenciones.
2.  **Smart Context Resolution**: Invoca a `SmartContextVariableProvider` para enriquecer el contexto con información del proyecto existente (namespaces, rutas).
3.  **Generation**: Delega a `Lft.Engine` la renderización de templates Liquid.
4.  **Path Resolution**: Usa `ISmartPathResolver` para determinar la ubicación física final de cada archivo generado.
5.  **Smart Injection**: Ejecuta `SmartInjectionStep` para modificar archivos existentes (ej. registrar en DI).
6.  **Syntax Validation**: Verifica que el código generado sea válido antes de persistir.
7.  **Persistencia**: Escribe los cambios en disco (si no es `dry-run`).

### 2. Smart Path Resolution (`ISmartPathResolver`)
Este componente permite que la CLI sea "consciente" de la estructura del proyecto.
-   **Implementación**: `SuffixBasedPathResolver`.
-   **Lógica**:
    -   Recibe un sufijo de archivo (ej. `Repository.cs`) y un "hint" opcional (Perfil).
    -   Escanea el directorio raíz buscando archivos que coincidan.
    -   **Profile Support**: Si se especifica un perfil (ej. `transactions-app`), filtra los resultados para incluir *solo* archivos dentro de directorios que coincidan con ese nombre.
    -   **Strict Mode**: Si hay perfil pero no se encuentran coincidencias dentro de él, retorna `null` (fallback a default) para evitar colocar archivos en el proyecto equivocado.

### 3. Smart Context (`SmartContextVariableProvider`)
Conecta el código generado con el código existente.
-   **Función**: Escanea el proyecto antes de la generación para encontrar namespaces reales.
-   **Variables Inyectadas**:
    -   `models_namespace`: Namespace donde viven los modelos.
    -   `repositories_namespace`: Namespace de los repositorios.
    -   Etc.
-   **Beneficio**: Los templates no necesitan "adivinar" el namespace basándose en la carpeta; usan el que realmente existe en el proyecto.

### 4. Smart Injection (`SmartInjectionStep`)
Orquesta la modificación de archivos existentes para integrar el nuevo código.
-   Utiliza el patrón **Strategy** para manejar diferentes tipos de inyección.
-   **Estrategias (`IInjectionStrategy`)**:
    -   `RepositoryInjectionStrategy`: Busca la clase de configuración de DI (ej. `Program.cs` o extensiones) e inyecta `services.AddScoped<IRepo, Repo>();`.
    -   `MapperInjectionStrategy`: Busca perfiles de AutoMapper (`MappingProfile.cs`) e inyecta configuraciones de mapeo.
    -   `ServiceInjectionStrategy`: Registra servicios de dominio.
    -   `RouteInjectionStep`: (Específico para APIs) Registra nuevas rutas en los endpoints.

## Interacción con otras Capas
-   **Usa**: `Lft.Domain` (Modelos), `Lft.Abstractions` (Interfaces), `Lft.Engine` (Renderizado), `Lft.Ast.*` (Análisis de código).
-   **Es usado por**: `Lft.Cli` (Presentación).

## Extensibilidad
Para agregar una nueva capacidad de generación (ej. soportar GraphQL):
1.  Crear nuevos Templates en `Lft.Engine`.
2.  Implementar una nueva `IInjectionStrategy` en `Lft.App` si se requiere registrar algo en el startup.
3.  Registrar la estrategia en el `GenPipeline`.

## Manejo de Errores
-   Las excepciones en los pasos del pipeline son capturadas y logueadas, pero pueden detener el proceso para evitar dejar el proyecto en un estado inconsistente.
-   En `dry-run`, los errores de I/O se simulan o ignoran.
