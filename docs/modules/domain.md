# Módulo: Lft.Domain (Domain Layer)

## Visión General
`Lft.Domain` es el núcleo de la solución. Contiene los modelos de datos puros, excepciones de dominio y lógica de negocio fundamental que no depende de ninguna tecnología de infraestructura o aplicación específica.

**Regla de Oro**: Este proyecto **NO** debe tener dependencias externas (salvo `Lft.Abstractions` si es estrictamente necesario, o librerías base de .NET).

## Componentes Principales

### 1. Modelos de Generación
- `GenerationRequest`: Encapsula toda la información necesaria para iniciar un proceso de generación (Entidad, Lenguaje, Perfil, Directorio de Salida).
- `GenerationResult`: Representa el resultado de la generación en memoria (lista de `GeneratedFile`).
- `GeneratedFile`: Representa un archivo virtual con su ruta relativa y contenido.

### 2. Modelos de Integración
- `FileChangePlan`: Describe una modificación propuesta a un archivo existente.
- `CodeInjectionRequest`: Solicitud agnóstica para inyectar un fragmento de código.
- `CodeInjectionPosition`: Enum (Beginning, End, Before, After).

### 3. Modelos de Análisis
- `ArchNode`: Representación abstracta de una unidad de código (Clase, Interfaz) para análisis arquitectónico.
- `Layer`: Definición de una capa arquitectónica.

## Responsabilidad
- Definir el "Lenguaje Ubicuo" de la herramienta LFT.
- Garantizar que los contratos de datos sean consistentes entre módulos.

## Interacción
- **Es usado por**: **TODOS** los demás módulos (`App`, `Engine`, `Cli`, `Infrastructure`, etc.).
