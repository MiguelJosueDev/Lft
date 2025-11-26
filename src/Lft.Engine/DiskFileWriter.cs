using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lft.Engine;

public sealed class DiskFileWriter : IFileWriter
{
    private readonly ILogger<DiskFileWriter> _logger;

    public DiskFileWriter(ILogger<DiskFileWriter>? logger = null)
    {
        _logger = logger ?? NullLogger<DiskFileWriter>.Instance;
    }

    public async Task WriteFileAsync(string path, string content, bool overwrite = false)
    {
        if (File.Exists(path) && !overwrite)
        {
            _logger.LogWarning("File already exists: {Path}. Skipping.", path);
            return;
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content);
        _logger.LogInformation("Wrote file: {Path}", path);
    }
}
