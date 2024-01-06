namespace Blitz3DDecomp;

sealed class Instruction
{
    public string Name;
    public string DestArg;
    public string SrcArg1;
    public string SrcArg2;

    public int[]? CallParameterAssignmentIndices;
    public Function.DecompGeneratedTempVariable? ReturnOutputVar = null;

    public Instruction(string name, string destArg = "", string srcArg1 = "", string srcArg2 = "")
    {
        Name = name;
        DestArg = destArg;
        SrcArg1 = srcArg1;
        SrcArg2 = srcArg2;
    }

    public bool IsJumpOrCall
        => Name is
            "call" or "jmp" or "je" or "jz"
            or "jne" or "jnz" or "jg" or "jge"
            or "jl" or "jle";

    public override string ToString()
    {
        var retVal = Name;

        if (string.IsNullOrWhiteSpace(DestArg)) { return retVal; }
        retVal += " " + DestArg;

        if (string.IsNullOrWhiteSpace(SrcArg1)) { return retVal; }
        retVal += ", " + SrcArg1;

        if (string.IsNullOrWhiteSpace(SrcArg2)) { return retVal; }
        retVal += ", " + SrcArg2;

        return retVal;
    }
}