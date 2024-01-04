namespace Blitz3DDecomp.DecompilerSteps.Step3_Obsolete;

static class DimArrayAccessRewrite
{
    private static void ProcessSection(Function.AssemblySection section)
    {
        for (int i = 0; i < section.Instructions.Count; i++)
        {
            var instruction = section.Instructions[i];
            if (instruction.Name != "add"
                || !instruction.DestArg.IsRegister()
                || !instruction.SrcArg1.StartsWith("[@_a", StringComparison.Ordinal))
            {
                continue;
            }

            var dimArray = DimArray.TryFindByName(instruction.SrcArg1.StripDeref())
                ?? throw new Exception($"Could not find dim array matching instruction arg {instruction.SrcArg1}");
            instruction.Name = "mov";
            instruction.SrcArg1 =  $"{dimArray.Name}[{instruction.DestArg}>>2]";
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