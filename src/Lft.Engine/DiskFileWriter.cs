namespace Lft.Engine;

public sealed class DiskFileWriter : IFileWriter
{
    public async Task WriteFileAsync(string path, string content, bool overwrite = false)
    {
        if (File.Exists(path) && !overwrite)
        {
            Console.WriteLine($"[WARN] File already exists: {path}. Skipping.");
            return;
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content);
        Console.WriteLine($"[INFO] Wrote: {path}");
    }
}
