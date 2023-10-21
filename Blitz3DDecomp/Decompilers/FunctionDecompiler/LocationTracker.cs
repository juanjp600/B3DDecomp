namespace Blitz3DDecomp;

ref struct LocationTracker
{
    public readonly int TrackDirection;
    public readonly bool PreserveDeref;

    public string Location;

    public LocationTracker(int trackDirection, string initialLocation, bool preserveDeref = false)
    {
        TrackDirection = trackDirection;
        Location = initialLocation;
        PreserveDeref = preserveDeref;
    }

    public bool ProcessInstruction(Function.Instruction instruction)
    {
        var (dest, src) = (instruction.LeftArg, instruction.RightArg);
        if (!PreserveDeref)
        {
            (dest, src) = (dest.StripDeref(), src.StripDeref());
        }

        if (instruction.Name == "xchg")
        {
            if (dest != Location && src != Location) { return false; }

            Location = dest == Location ? src : dest;
            return true;
        }

        if (TrackDirection < 0)
        {
            (dest, src) = (src, dest);
        }

        if (src != Location) { return false; }

        if (instruction.Name == "mov")
        {
            Location = dest;
            return true;
        }
        if (instruction.Name == "lea" && !PreserveDeref)
        {
            Location = dest;
            return true;
        }

        return false;
    }
}