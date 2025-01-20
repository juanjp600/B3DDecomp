namespace Blitz3DDecomp;

abstract class VariableWithOwnType : Variable
{
    private DeclType declType = DeclType.Unknown;
    public sealed override DeclType DeclType
    {
        get => declType;
        set
        {
            declType = value;
            fields = null;
        }
    }

    protected VariableWithOwnType(string name) : base(name) { }
}