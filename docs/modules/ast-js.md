# Módulo: Ast.JavaScript (Infrastructure Layer)

## Responsabilidad principal
- Parsing y manipulación de código JavaScript/TypeScript.
- **Code Injection**: Insertar importaciones, rutas o configuraciones en archivos JS/TS existentes.
- **Syntax Validation**: Validar que el código generado sea sintácticamente correcto (usando herramientas como Esprima o TypeScript Compiler API).

## Estado Actual
🚧 **En Construcción / Planificado**

Este módulo está diseñado para ser el equivalente de `Lft.Ast.CSharp` pero para el ecosistema frontend.

## Componentes Planificados

### 1. JavaScriptInjectionService (`IJavaScriptInjectionService`)
- Capacidad para inyectar `imports` al inicio del archivo.
- Inserción de rutas en arrays de configuración (ej. `routes.js`).
- Modificación de objetos exportados.

### 2. TypeScript Support
- Soporte específico para tipos e interfaces de TypeScript.
- Uso de `ts-morph` o librerías similares para manipulación segura del AST.

## Dependencias
- Posiblemente requiera invocar herramientas de Node.js o usar librerías de .NET que envuelvan motores de JS (como Jint o ClearScript) si se necesita ejecución, aunque para AST puro se buscarán parsers nativos o wrappers.

## Uso típico (Futuro)
```csharp
// Inyección de una ruta en un archivo de rutas de React/Vue
var request = new JsInjectionRequest(
    FilePath: "src/router/index.js",
    TargetArray: "routes",
    Snippet: "{ path: '/users', component: UsersList }"
);

await _jsInjectionService.InjectIntoArrayAsync(request);
```
