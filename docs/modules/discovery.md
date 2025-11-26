# Módulo: Lft.Discovery (Application Layer)

## Visión General
`Lft.Discovery` es un módulo especializado en la **introspección y análisis** de la estructura del proyecto existente. Su objetivo es proporcionar una "imagen" detallada del código base para que otros módulos (como `App` o `Integration`) puedan tomar decisiones inteligentes.

A diferencia de `Lft.Analyzer` (que busca violaciones de reglas), `Lft.Discovery` busca **información útil** para la generación e inyección de código.

## Componentes Principales

### 1. Project Analyzer (`IProjectAnalyzer`)
- **Responsabilidad**: Escanear el directorio del proyecto y construir un `ProjectManifest`.
- **Funciones**:
    - Identificar capas arquitectónicas (Domain, Application, Infrastructure, API).
    - Detectar tecnologías usadas (Entity Framework, MediatR, etc.).
    - Mapear la estructura de carpetas a conceptos del dominio.

### 2. Namespace Resolver (`INamespaceResolver`)
- **Responsabilidad**: Determinar el namespace correcto para un archivo nuevo o existente.
- **Lógica**:
    - Analiza archivos vecinos para inferir el namespace.
    - Soporta convenciones de nombres y estructuras de carpetas anidadas.

### 3. Injection Point Locator (`IInjectionPointLocator`)
- **Responsabilidad**: Encontrar lugares precisos donde inyectar código.
- **Conceptos**:
    - `InjectionTarget`: El archivo y la clase destino.
    - `InjectionPoint`: La ubicación exacta (ej. "dentro del método `ConfigureServices`, antes de `app.Run()`).
- **Uso**: Es utilizado por las estrategias de inyección de `Lft.App` para no depender de rutas hardcodeadas.

## Interacción
- **Usa**: `Lft.Ast.CSharp` (para análisis sintáctico preciso).
- **Es usado por**: `Lft.App` (para configurar el contexto de generación y las estrategias de inyección).

## Ejemplo de Uso
```csharp
var manifest = await _projectAnalyzer.AnalyzeAsync(rootPath);
var servicesLayer = manifest.Layers.FirstOrDefault(l => l.Type == LayerType.Application);

var injectionPoint = await _locator.FindServiceRegistrationPointAsync(manifest);
// Retorna: "src/MyProject.Api/Program.cs", Method: "RegisterServices", Line: 45
```
