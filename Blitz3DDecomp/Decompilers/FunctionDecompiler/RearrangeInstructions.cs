namespace Blitz3DDecomp;

static partial class FunctionDecompiler
{
    public static class RearrangeInstructions
    {
        public static void Process(Function function)
        {
            foreach (var instructions in function.AssemblySections.Values)
            {
                for (int i = 0; i < instructions.Length; i++)
                {
                    var instruction = instructions[i];
                    var leftArg = instruction.LeftArg;
                    //var rightArg = instruction.RightArg;
                    if (leftArg.Contains("eax")) { continue; }

                    int nextFoundReference = -1;
                    for (int j = i + 1; j < instructions.Length; j++)
                    {
                        var nextInstruction = instructions[j];
                        if (nextInstruction.LeftArg.Contains(leftArg) || nextInstruction.RightArg.Contains(leftArg))
                        {
                            nextFoundReference = j;
                            break;
                        }
                    }
                    
                    if (nextFoundReference < i+2) { continue; }
                    var potentialSifter = instructions[nextFoundReference];
                    for (int j = nextFoundReference; j > i; j--)
                    {
                        var potentialBlocker = instructions[j];
                        if (potentialBlocker.LeftArg.Contains(potentialSifter.LeftArg)
                            || potentialBlocker.LeftArg.Contains(potentialSifter.RightArg)
                            || potentialBlocker.RightArg.Contains(potentialSifter.LeftArg)
                            || potentialBlocker.RightArg.Contains(potentialSifter.RightArg)
                            || potentialSifter.LeftArg.Contains(potentialBlocker.LeftArg)
                            || potentialSifter.LeftArg.Contains(potentialBlocker.RightArg)
                            || potentialSifter.RightArg.Contains(potentialBlocker.LeftArg)
                            || potentialSifter.RightArg.Contains(potentialBlocker.RightArg))
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}