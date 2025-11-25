# Módulo: CLI

## Responsabilidad principal
- Punto de entrada para el usuario (`Program.cs`).
- Parseo de argumentos y opciones.
- Configuración de DI (Dependency Injection).
- Salida a consola (colores, spinners, tablas).
- **Delegación**: Invoca Pipelines de la capa de Aplicación (ej. `GenPipeline`).

## Qué NO debe hacer
- **Contener lógica de negocio o flujos complejos**. La CLI no decide "si dry-run entonces diff". Eso es lógica del Pipeline.
- Manipular strings de código directamente.
- ❌ **Anti-ejemplo**: Tener un `if/switch` gigante en el comando que decida paso a paso qué servicios llamar.

## Proyectos relacionados
- `Lft.Cli`

## Interfaces y contratos públicos
- N/A (es el consumidor final).

## Dependencias permitidas
- Todas las capas de Aplicación e Infraestructura.

## Patrones internos
- **Command Pattern**: Cada verbo (`gen`, `diff`) es un comando.
- **Thin Client**: La CLI es solo una interfaz tonta sobre la lógica de aplicación.

## Flujos típicos

### Ejecución de Comando
1. Parsear `args`.
2. Configurar `IServiceProvider`.
3. Resolver el Pipeline correspondiente (ej. `var pipeline = provider.GetRequiredService<GenPipeline>();`).
4. Ejecutar `pipeline.RunAsync(request)`.
5. Manejar excepciones y retornar Exit Code.

## Open questions / TODOs
- [ ] Elegir librería de CLI (System.CommandLine, Spectre.Console, etc.).
- [ ] Modo interactivo (prompts).
