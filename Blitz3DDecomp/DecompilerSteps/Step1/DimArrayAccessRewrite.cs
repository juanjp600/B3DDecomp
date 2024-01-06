namespace Blitz3DDecomp.DecompilerSteps.Step1;

static class DimArrayAccessRewrite
{
    private static void ProcessSection(Function.AssemblySection section)
    {
        for (int i = 0; i < section.Instructions.Count; i++)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name != "add"
                || !instruction.SrcArg1[..3].IsRegister()
                || !instruction.SrcArg2.StartsWith("[@_a", StringComparison.Ordinal))
            {
                continue;
            }

            var dimArray = DimArray.TryFindByName(instruction.SrcArg2.StripDeref())
                ?? throw new Exception($"Could not find dim array matching instruction arg {instruction.SrcArg2}");
            instruction.Name = "mov";
            instruction.SrcArg1 =  $"{dimArray.Name}[{instruction.SrcArg1}>>2]";
            instruction.SrcArg2 = "";
        }
    }
    
    public static void Process(Function function)
    {
        foreach (var section in function.AssemblySections.Values)
        {
            ProcessSection(section);
        }
    }
}