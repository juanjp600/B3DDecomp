using B3DDecompUtils;

namespace Blitz3DDecomp;

static class LoadDimArrays
{
    public static void FromDir(string inputDir)
    {
        inputDir = inputDir.AppendToPath("DimArray");
        foreach (var filePath in Directory.GetFiles(inputDir))
        {
            _ = DimArray.TryCreateFromSymbolName(Path.GetFileNameWithoutExtension(filePath));
        }
    }
}