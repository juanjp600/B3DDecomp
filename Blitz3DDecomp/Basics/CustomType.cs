namespace Blitz3DDecomp;

sealed class CustomType
{
    public sealed class Field : Variable
    {
        public Field(string name) : base(name) { }
        public override string ToInstructionArg()
        {
            return $"\\{Name}";
        }
    }
    
    public static readonly List<CustomType> AllTypes = new List<CustomType>();
    
    public readonly string Name;
    public readonly List<Field> Fields = new List<Field>();

    public CustomType(string name)
    {
        Name = name;
        AllTypes.Add(this);
    }
}
