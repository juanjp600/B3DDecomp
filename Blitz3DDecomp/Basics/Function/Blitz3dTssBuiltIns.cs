namespace Blitz3DDecomp;

static class Blitz3dTssBuiltIns
{
    public static void Init()
    {
        Blitz3dBuiltIns.Init();
        var loadFont = Function.GetFunctionByName("_builtIn__floadfont");
        loadFont.Parameters.RemoveRange(2, 3);
    }
}