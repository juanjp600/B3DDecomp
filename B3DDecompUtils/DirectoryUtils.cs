namespace B3DDecompUtils;

public static class DirectoryUtils
{
    public static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path)) { Directory.Delete(path, recursive: true); }
        Directory.CreateDirectory(path);
    }
}