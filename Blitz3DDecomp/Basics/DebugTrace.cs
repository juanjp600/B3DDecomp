using System.Collections.Immutable;
using System.Diagnostics;
using B3DDecompUtils;

namespace Blitz3DDecomp;

public readonly struct DebugTrace
{
    private readonly ImmutableArray<string>? messages;
    public ImmutableArray<string> Messages => messages ?? ImmutableArray<string>.Empty;

    private DebugTrace(ImmutableArray<string> messages)
    {
        this.messages = messages;
    }

    public DebugTrace Append(string msg)
    {
        Logger.WriteLine(msg);
        return new DebugTrace(messages: Messages.Insert(0, msg));
    }
}
