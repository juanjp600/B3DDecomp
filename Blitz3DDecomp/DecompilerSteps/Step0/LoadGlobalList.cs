using B3DDecompUtils;

namespace Blitz3DDecomp;

static class LoadGlobalList
{
    public static void FromDir(string inputDir)
    {
        inputDir = inputDir.AppendToPath("Variable");
        if (!Directory.Exists(inputDir)) { return; }
        foreach (var filePath in Directory.GetFiles(inputDir))
        {
            _ = new GlobalVariable(Path.GetFileNameWithoutExtension(filePath)[2..]);
        }
    }
}