﻿using System.Collections.Immutable;

namespace Blitz3DDecomp;

static class Registers
{
    public static readonly ImmutableArray<string> Names
        = new[] { "eax", "ebx", "ecx", "edx", "edi", "esi", "esp", "ebp" }.ToImmutableArray();

    public static bool IsRegister(this string s)
        => Names.Contains(s);

    public static bool ContainsRegister(this string s)
        => Names.Any(n => s.StartsWith(n) || s.StartsWith($"[{n}"));
}