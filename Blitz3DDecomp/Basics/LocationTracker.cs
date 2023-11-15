using System.Diagnostics;

namespace Blitz3DDecomp;

ref struct LocationTracker
{
    public readonly int TrackDirection;
    public readonly bool PreserveDeref;

    private string location;
    public string Location
    {
        get => location;
        set => location = PreserveDeref ? value : value.StripDeref();
    }

    public LocationTracker(int trackDirection, string initialLocation, bool preserveDeref = false)
    {
        TrackDirection = trackDirection;
        location = preserveDeref ? initialLocation : initialLocation.StripDeref();
        PreserveDeref = preserveDeref;
    }

    public bool ProcessInstruction(Function.Instruction instruction)
    {
        var (dest, src) = (instruction.DestArg, instruction.SrcArg1);
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

        if (instruction.Name == "lea" && PreserveDeref)
        {
            var srcStripped = src.StripDeref();
            if (srcStripped == src) { return false; }
            src = srcStripped;
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
        if (instruction.Name == "lea")
        {
            Location = dest;
            return true;
        }

        return false;
    }
}