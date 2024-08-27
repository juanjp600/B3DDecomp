namespace B3DDecompUtils;
public static class PathUtils
{
    public static string CleanupPath(this string path)
        => path.Replace("\\", "/");
    
    public static string AppendToPath(this string path, string fsEntry)
    {
        path = path.CleanupPath();
        if (!path.EndsWith("/")) { path += "/"; }
        return path + fsEntry;
    }

    public static bool IsDisasmPath(this string path)
        => path.EndsWith("_disasm/", StringComparison.OrdinalIgnoreCase)
           || path.EndsWith("_disasm", StringComparison.OrdinalIgnoreCase);
}
