# Cómo probar el módulo de diff (`Lft.Diff`)

Este módulo implementa el servicio `IFileDiffService` (basado en LCS) y está cubierto
por el proyecto de pruebas `tests/Lft.Diff.Tests`. Sigue estos pasos para
preparar el entorno y ejecutar las pruebas.

## 1. Instalar el SDK de .NET 10 localmente (si no lo tienes)

La imagen base no trae `dotnet`. Puedes instalar el SDK 10 en tu `$HOME` sin
privilegios de administrador:

```bash
# Descarga el instalador oficial
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh

# Instala el SDK 10.0 en ~/.dotnet (puedes cambiar la ruta si lo prefieres)
./dotnet-install.sh --channel 10.0 --quality ga --install-dir "$HOME/.dotnet"

# Exporta el binario a tu PATH para esta sesión (opcional: añade a tu shell rc)
export PATH="$HOME/.dotnet:$PATH"
```

> Nota: si ya tienes `dotnet` en tu PATH, puedes omitir este paso.

## 2. Restaurar y ejecutar únicamente las pruebas de diff

Desde la raíz del repositorio:

```bash
$HOME/.dotnet/dotnet test tests/Lft.Diff.Tests/Lft.Diff.Tests.csproj
```

## 3. Comandos útiles

- Ejecutar un caso específico (usa el nombre completo del test):
  ```bash
  $HOME/.dotnet/dotnet test tests/Lft.Diff.Tests/Lft.Diff.Tests.csproj \
    --filter FullyQualifiedName~Lft.Diff.Tests.LcsFileDiffServiceTests.Compute_ShouldDetectAddedLines
  ```

- Mostrar salida detallada en caso de fallos:
  ```bash
  $HOME/.dotnet/dotnet test tests/Lft.Diff.Tests/Lft.Diff.Tests.csproj -v n
  ```

Con esto podrás validar localmente el módulo de diffs antes de integrarlo en la CLI.
