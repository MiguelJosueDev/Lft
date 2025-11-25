# LFT – Convenciones y Estándares

## Convenciones de nombres de entidades y módulos

- **Entidades**: PascalCase (ej. `FundingType`, `User`).
- **Plurales**: Se usa inglés estándar (ej. `FundingTypes`, `Users`).
- **Clases generadas**: `{Entity}{Role}` (ej. `FundingTypeRepository`, `UserController`).

## Convenciones de namespaces

- Base: `[Company].[Project].[Module]`
- Los namespaces se derivan de la configuración del proyecto o argumentos de CLI.

### Ejemplo de Mapeo
Si `BaseNamespaceName` es `Lft.Generated`:

| Proyecto | Namespace |
| :--- | :--- |
| **Domain** | `Lft.Generated.Domain` |
| **Api** | `Lft.Generated.Api` |
| **Infrastructure** | `Lft.Generated.Infrastructure` |

## Convenciones de archivos generados

| Tipo | Ubicación sugerida | Patrón de nombre |
| :--- | :--- | :--- |
| **Model** | `src/[Project].Domain/Models` | `{Entity}.cs` |
| **Repository Interface** | `src/[Project].Domain/Interfaces` | `I{Entity}Repository.cs` |
| **Repository Impl** | `src/[Project].Infrastructure/Repositories` | `{Entity}Repository.cs` |
| **Controller** | `src/[Project].Api/Controllers` | `{Entity}Controller.cs` |

## Convenciones de DI y logging

- **Inyección de Dependencias**: Constructor injection siempre.
- **Logging**: Usar `ILogger<T>` como primera dependencia en el constructor si es necesario.
- **Registro**: Los servicios se registran en `ServiceCollectionExtensions` dentro de cada capa.

## Convenciones para templates

Variables reservadas en Liquid:

- `_EntityName`: Nombre original de la entidad.
- `_EntityPascal`: PascalCase.
- `_EntityCamel`: camelCase.
- `_EntityPlural`: Pluralizado.
- `_Namespace`: Namespace raíz calculado.
- `_ModelName`: Nombre de la clase de modelo.

## Estilo de código

- **Lenguaje**: C# (versión LTS más reciente).
- **Nullable**: `enable` en todos los proyectos.
- **Implicit Usings**: `enable`.
- **Formato**: Estándar `dotnet format`.

## TODOs

- [ ] Definir convenciones para tests unitarios generados.
- [ ] Estandarizar manejo de excepciones en capas.
