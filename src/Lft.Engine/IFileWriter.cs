namespace Lft.Engine;

public interface IFileWriter
{
    Task WriteFileAsync(string path, string content, bool overwrite = false);
}
