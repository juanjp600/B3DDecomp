using AsmResolver;
using AsmResolver.PE;
using AsmResolver.PE.Win32Resources;
using SharpDisasm.Udis86;
using System.Diagnostics;
using System.Text;
using B3DDecompUtils;

namespace Blitz3DDecomp;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No input given, closing");
            return;
        }

        string exePath = args[0].CleanupPath();
        if (!exePath.EndsWith(".exe"))
        {
            Console.WriteLine("Input isn't an exe, closing");
            return;
        }

        var outputPath = Directory.GetCurrentDirectory().CleanupPath();

        var exeName = Path.GetFileName(exePath);
        var peImage = PEImage.FromFile(exePath);
        var resources = peImage.Resources;
        if (resources?.Entries is not IEnumerable<IResourceEntry> entries)
        {
            throw new Exception($"");
        }

        static IEnumerable<IResourceEntry> flattener(IResourceEntry entry)
        {
            if (entry is not IResourceDirectory dir) { return new[] { entry }; }

            return dir.Type is ResourceType.RcData or (ResourceType)1111
                ? dir.Entries
                : Enumerable.Empty<IResourceEntry>();
        }

        var flatten = entries
            .SelectMany(flattener)
            .SelectMany(flattener)
            .SelectMany(flattener)
            .SelectMany(flattener);
        var data = flatten
            .OfType<IResourceData>()
            .Select(d => d.Contents)
            .OfType<DataSegment>()
            .First()
            .Data;

        var symbols = new List<Symbol>();
        var symbolByName = new Dictionary<string, Symbol>();
        var symbolByAddress = new Dictionary<int, Symbol>();
        void addSymbol(string symbolName, int symbolAddress)
        {
            symbols.Add(new Symbol(name: symbolName) { Address = symbolAddress });
            symbolByName.Add(symbolName, symbols.Last());
            symbolByAddress.TryAdd(symbolAddress, symbols.Last());
        }
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        var codeLen = reader.ReadInt32();
        var codeBytesForDecomp = reader.ReadBytes(codeLen);
        var codeBytesForReading = codeBytesForDecomp.ToArray();

        var symbolCount = reader.ReadInt32();
        for (int i = 0; i < symbolCount; i++)
        {
            var symbolName = reader.ReadCStr();
            var symbolAddress = reader.ReadInt32();
            addSymbol(symbolName, symbolAddress);
        }

        var compiler = symbolByName.ContainsKey("__CSTRS")
            ? Compiler.BlitzPlus
            : Compiler.Blitz3d;

        const int magicXorNumber = 0x004a_7000;

        var relocs = new List<Reloc>();

        void writeReloc(string symbolName, int offsetFromSymbol, int relocAddress, int originalAdder)
        {
            int symbolAddress = getSymbolAddress(symbolName);
            int originalValue
                = (codeBytesForDecomp[relocAddress + 0] <<  0)
                | (codeBytesForDecomp[relocAddress + 1] <<  8)
                | (codeBytesForDecomp[relocAddress + 2] << 16)
                | (codeBytesForDecomp[relocAddress + 3] << 24);

            int newValueForDecomp =
                (symbolAddress ^ magicXorNumber)
                + offsetFromSymbol + (originalValue + originalAdder);
            int newValueForHumanReading =
                symbolAddress
                + offsetFromSymbol + (originalValue + originalAdder);

            codeBytesForDecomp[relocAddress + 0] = (byte)((newValueForDecomp >> 0) & 0xff);
            codeBytesForDecomp[relocAddress + 1] = (byte)((newValueForDecomp >> 8) & 0xff);
            codeBytesForDecomp[relocAddress + 2] = (byte)((newValueForDecomp >> 16) & 0xff);
            codeBytesForDecomp[relocAddress + 3] = (byte)((newValueForDecomp >> 24) & 0xff);

            codeBytesForReading[relocAddress + 0] = (byte)((newValueForHumanReading >> 0) & 0xff);
            codeBytesForReading[relocAddress + 1] = (byte)((newValueForHumanReading >> 8) & 0xff);
            codeBytesForReading[relocAddress + 2] = (byte)((newValueForHumanReading >> 16) & 0xff);
            codeBytesForReading[relocAddress + 3] = (byte)((newValueForHumanReading >> 24) & 0xff);

            relocs.Add(new Reloc { SymbolName = symbolName, RelocAddress = relocAddress, OffsetFromSymbol = offsetFromSymbol, OriginalValue = originalValue + originalAdder });
        }

        int nextMadeUpAddress = 0x5eff_ffff;
        int getSymbolAddress(string symbolName)
        {
            if (!symbolByName.TryGetValue(symbolName, out var symbol))
            {
                addSymbol(symbolName, nextMadeUpAddress);
                nextMadeUpAddress--;
                var newSymbol = symbols.Last();
                newSymbol.ForceSetInferredType(SymbolType.BuiltIn, "BuiltIn");
                newSymbol.NewName = $"_builtIn{newSymbol.Name}";
                return newSymbol.Address;
            }
            return symbol.Address;
        }

        var relativeRelocsCount = reader.ReadInt32();
        for (int i = 0; i < relativeRelocsCount; i++)
        {
            var symbolName = reader.ReadCStr();
            var relocAddress = reader.ReadInt32();
            var symbolAddress = getSymbolAddress(symbolName);
            writeReloc(symbolName, -relocAddress - 4, relocAddress, 4);
        }

        var absoluteRelocsCount = reader.ReadInt32();
        for (int i = 0; i < absoluteRelocsCount; i++)
        {
            var symbolName = reader.ReadCStr();
            var relocAddress = reader.ReadInt32();
            var symbolAddress = getSymbolAddress(symbolName);
            writeReloc(symbolName, 0, relocAddress, 0);
        }

        symbols = symbols.OrderBy(s => s.Address).ToList();
        relocs = relocs.OrderBy(r => r.RelocAddress).ToList();

        for (int i=0; i<relocs.Count - 1; i++)
        {
            if (relocs[i].RelocAddress == relocs[i+1].RelocAddress)
            {
                Debugger.Break();
            }
        }

        var libFunctions = new List<LibFunction>();
        if (symbolByName.TryGetValue("__LIBS", out var libsSymbol))
        {
            byte[] libsBytes = codeBytesForReading[libsSymbol.Address..];
            using var libStream = new MemoryStream(libsBytes);
            using var libReader = new BinaryReader(libStream);
            while (true)
            {
                string libName = libReader.ReadCStr();
                if (string.IsNullOrEmpty(libName)) { break; }
                while (true)
                {
                    string functionName = libReader.ReadCStr();
                    if (string.IsNullOrEmpty(functionName)) { break; }
                    int lookupAddress = libReader.ReadInt32();
                    libFunctions.Add(new LibFunction { LibName = libName, FunctionName = functionName, LookupAddress = lookupAddress });
                    var symbol = symbols.FirstOrDefault(s => s.Address == lookupAddress);
                    if (symbol != null)
                    {
                        symbol.ForceSetInferredType(SymbolType.Libs, "__LIBS");
                        symbol.NewName = $"{symbol.Name}__LIBS";
                    }
                    Logger.WriteLine($"Lib function: {libName}.{functionName}@{lookupAddress:X8}");
                }
            }
        }

        var dataMembers = new List<DataMember>();
        if (symbolByName.TryGetValue("__DATA", out var dataSymbol))
        {
            byte[] dataBytes = codeBytesForReading[dataSymbol.Address..];
            using var dataStream = new MemoryStream(dataBytes);
            using var dataReader = new BinaryReader(dataStream);
            while (true)
            {
                int dataType = dataReader.ReadInt32();
                if (dataType == 0) { break; }
                uint value = dataReader.ReadUInt32();
                dataMembers.Add(new DataMember { Type = dataType, Value = value });
            }
        }

        //File.WriteAllBytes(outputPath + exeName.Replace(".exe", "_code.dat"), codeBytesForReading);
        var relocsBackup = relocs.ToList();
        bool inferenceDone = false;
        while (!inferenceDone)
        {
            Logger.WriteLine("Inferring...");
            inferenceDone = true;
            for (int i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                var symbolEnd = i < symbols.Count - 1 ? symbols[i + 1].Address : codeLen;
                if (symbolEnd > codeLen) { symbolEnd = codeLen; }
                if (symbol.Address >= codeLen) { break; }
                for (int j = i+1; j < symbols.Count; j++)
                {
                    if (symbols[j].Address == symbols[i].Address)
                    {
                        if (symbols[j].Type == SymbolType.Other)
                        {
                            symbols[j].TrySetInferredType(symbols[i].Type, symbols[i].OwnerName);
                            symbols[j].NewName = $"{symbols[j].Name}{symbols[i].OwnerName}";
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                if (symbols[i].Name.StartsWith("_t") && i < symbols.Count - 3)
                {
                    symbols[i].ForceSetInferredType(SymbolType.Type, symbols[i].Name);
                    symbols[i + 1].ForceSetInferredType(SymbolType.Type, symbols[i].OwnerName);
                    symbols[i + 1].NewName = $"{symbols[i + 1].Name}{symbols[i].Name}";
                    symbols[i + 2].ForceSetInferredType(SymbolType.Type, symbols[i].OwnerName);
                    symbols[i + 2].NewName = $"{symbols[i + 2].Name}{symbols[i].Name}";
                }

                while (relocs.Count > 0 && relocs[0].RelocAddress < symbol.Address)
                {
                    relocs.RemoveAt(0);
                }

                var symbolType = symbol.Type;
                if (symbolType == SymbolType.Code)
                {
                    var functionCode = codeBytesForDecomp[symbol.Address..symbolEnd];
                    var disasm = new SharpDisasm.Disassembler(
                            functionCode,
                            SharpDisasm.ArchitectureMode.x86_32,
                            address: (uint)symbol.Address,
                            copyBinaryToInstruction: true,
                            vendor: SharpDisasm.Vendor.Intel);

                    void ProcessInstruction(SharpDisasm.Instruction instr)
                    {
                        var instrStr = instr.ToString();

                        bool isJump = (int)instr.Mnemonic
                            is (>= (int)ud_mnemonic_code.UD_Ija and <= (int)ud_mnemonic_code.UD_Ijz)
                            or (int)ud_mnemonic_code.UD_Icall;
                        int instructionStart = (int)instr.Offset;
                        int instructionEnd = instructionStart + instr.Bytes.Length;

                        while (relocs.Count > 0 && relocs[0].RelocAddress >= instructionStart && relocs[0].RelocAddress < instructionEnd)
                        {
                            var relocToDecode = relocs[0];
                            if (symbolByName.TryGetValue(relocToDecode.SymbolName, out var relocSymbol))
                            {
                                if (isJump)
                                {
                                    if (relocSymbol.Type == SymbolType.Other && relocSymbol.Address < codeLen)
                                    {
                                        inferenceDone = false;
                                        relocSymbol.TrySetInferredType(SymbolType.Code, symbols[i].OwnerName);
                                        relocSymbol.NewName = $"{relocSymbol.Name}{symbols[i].OwnerName}";
                                    }
                                }
                            }
                            else
                            {
                                Logger.WriteLine($"Oh no! {relocToDecode.SymbolName} {relocToDecode.RelocAddress}");
                            }
                            relocs.RemoveAt(0);
                        }
                    }

                    var instructions = disasm.Disassemble().ToArray();
                    if (instructions.Length > 0)
                    {
                        foreach (var instruction in instructions)
                        {
                            ProcessInstruction(instruction);
                        }
                        var lastInstructionOpcode = instructions.Last().Mnemonic;
                        if (lastInstructionOpcode is not (ud_mnemonic_code.UD_Iret or ud_mnemonic_code.UD_Iretf or ud_mnemonic_code.UD_Inop))
                        {
                            if (i < symbols.Count && symbols[i+1].Type == SymbolType.Other)
                            {
                                inferenceDone = false;
                                symbols[i + 1].TrySetInferredType(symbols[i].Type, symbols[i].OwnerName);
                                symbols[i + 1].NewName = $"{symbols[i + 1].Name}{symbols[i].OwnerName}";
                            }
                        }
                    }
                }
                else if (symbolType == SymbolType.Other)
                {
                    if ((symbolEnd - symbol.Address) == (3 * 4))
                    {
                        int firstInt = BitConverter.ToInt32(codeBytesForReading[symbol.Address..(symbol.Address + 4)]);
                        if (firstInt == 0x6)
                        {
                            int vectorSize = BitConverter.ToInt32(codeBytesForReading[(symbol.Address + 4)..(symbol.Address + 8)]);
                            int vectorTypeAddress = BitConverter.ToInt32(codeBytesForReading[(symbol.Address + 8)..(symbol.Address + 12)]);
                            var vectorTypeSymbol = symbolByAddress[vectorTypeAddress];
                            var newName = $"Vector{symbol.Name}{vectorTypeSymbol.NameToPrint}_sz{vectorSize}";
                            symbol.TrySetInferredType(SymbolType.Vector, newName);
                            symbol.NewName = newName;
                        }
                    }
                }
            }
            relocs = relocsBackup.ToList();
        }

        for (int i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            var symbolEnd = i < symbols.Count - 1 ? symbols[i + 1].Address : codeLen;
            if (symbolEnd > codeLen) { symbolEnd = codeLen; }
            if (symbol.Address >= codeLen) { break; }

            if (symbol.Type != SymbolType.DimArray) { continue; }

            var newName = symbol.Name;
            int elementType = BitConverter.ToInt32(codeBytesForReading.AsSpan()[(symbol.Address + 4)..][..4]);
            int dimensionCount = BitConverter.ToInt32(codeBytesForReading.AsSpan()[(symbol.Address + 8)..][..4]);
            newName += elementType switch
            {
                1 => "_int",
                2 => "_float",
                3 => "_string",
                5 => "_obj",
                _ => throw new Exception($"Unexpected dim element type {elementType}")
            };
            newName += $"_{dimensionCount}dim";
            symbol.ForceSetInferredType(SymbolType.DimArray, newName);
            symbol.NewName = newName;
        }

        var disasmPath = outputPath.AppendToPath(exeName.Replace(".exe", "_disasm"));
        
        DirectoryUtils.RecreateDirectory(disasmPath);
        
        File.WriteAllText(disasmPath + "/Compiler.txt", compiler.ToString());

        var builtInSymbols = BuiltInSymbolExtractor.FromFile(exePath);
        File.WriteAllLines(disasmPath + "/BuiltInSymbols.txt", builtInSymbols);

        for (int i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            if (symbol.Type != SymbolType.Other || symbol.Address >= codeLen) { continue; }

            var symbolEnd = i < symbols.Count - 1 ? symbols[i + 1].Address : codeLen;
            if (symbolEnd > codeLen) { symbolEnd = codeLen; }

            var symbolData = codeBytesForReading[symbol.Address..symbolEnd];
            if (symbolData.Length == 0) { Debugger.Break(); }

            bool hasFourLeadingZeroes = false;
            if (compiler == Compiler.BlitzPlus)
            {
                if (symbolData.Length > 4)
                {
                    hasFourLeadingZeroes = BitConverter.ToUInt32(symbolData[..4]) == 0;
                }
            }
            if (symbol.Name == "__CSTRS") { continue; }

            int indexOfNul = -1;

            for (int j = hasFourLeadingZeroes ? 4 : 0; j < symbolData.Length; j++)
            {
                if (symbolData[j] == '\0')
                {
                    indexOfNul = j;
                    break;
                }
            }
            if (hasFourLeadingZeroes)
            {
                symbolData = symbolData[4..];
                indexOfNul -= 4;
            }

            string strValue = Encoding.ASCII.GetString(symbolData[..indexOfNul]);
            string leftoverBytes = string.Join(" ", symbolData[indexOfNul..].Select(b => $"{b:X2}"));
            string newName = $"StringConstant{symbol.Name}_{string.Join("", strValue.Where(c => char.IsLetterOrDigit(c) || c is '_'))}";
            symbol.NewName = newName;

            string textFilePath = disasmPath + "/Strings.txt";
            File.AppendAllText(textFilePath, $"@{symbol.Address:X8}: {symbol.NameToPrint}\n");
            File.AppendAllText(textFilePath, $"    \"{strValue}\" {leftoverBytes}\n");
        }

        for (int i=0;i<symbols.Count;i++)
        {
            var symbol = symbols[i];
            if (symbol.Name == "__LIBS"
                || symbol.Type == SymbolType.Libs
                || symbol.Type == SymbolType.Other) { continue; }

            Directory.CreateDirectory(disasmPath+$"/{symbol.Type}");
            string textFilePath = disasmPath + $"/{symbol.Type}/{symbol.OwnerName ?? "NoOwner"}.txt";
            var symbolEnd = i < symbols.Count - 1 ? symbols[i + 1].Address : codeLen;
            if (symbolEnd > codeLen) { symbolEnd = codeLen; }
            File.AppendAllText(textFilePath, $"@{symbol.Address:X8}: {symbol.NameToPrint}\n");
            if (symbol.Address >= codeLen) { continue; }

            while (relocs.Count > 0 && relocs[0].RelocAddress < symbol.Address)
            {
                relocs.RemoveAt(0);
            }

            var symbolType = symbol.Type;
            if (symbolType == SymbolType.Code)
            {
                var functionCode = codeBytesForDecomp[symbol.Address..symbolEnd];
                var disasm = new SharpDisasm.Disassembler(
                        functionCode,
                        SharpDisasm.ArchitectureMode.x86_32,
                        address: (uint)symbol.Address,
                        copyBinaryToInstruction: true,
                        vendor: SharpDisasm.Vendor.Intel);

                string InstructionToLine(SharpDisasm.Instruction instr)
                {
                    var instructionBytes = codeBytesForReading[(int)instr.Offset..((int)instr.Offset + instr.Length)];
                    var bytesStringRepresentation = string.Join(" ", instructionBytes.Select(b => Convert.ToHexString(new[] { b })));
                    var instrStr = instr.ToString();
                    var padding = new string(' ', Math.Max(40 - bytesStringRepresentation.Length, 1));

                    int instructionStart = (int)instr.Offset;
                    string retVal = $"    @{instructionStart:X8}: {bytesStringRepresentation}{padding}{instr}";
                    int instructionEnd = instructionStart + instructionBytes.Length;

                    while (relocs.Count > 0 && relocs[0].RelocAddress >= instructionStart && relocs[0].RelocAddress < instructionEnd)
                    {
                        var relocToDecode = relocs[0];
                        if (symbolByName.TryGetValue(relocToDecode.SymbolName, out var relocSymbol))
                        {
                            var operandValue = (relocSymbol.Address ^ magicXorNumber) + relocToDecode.OriginalValue;
                            var relocAddressStr = $"0x{operandValue:x2}";
                            var oldVal = retVal;
                            var replacementValue = $"@{relocSymbol.NameToPrint}";
                            if (relocToDecode.OriginalValue < 0)
                            {
                                replacementValue += relocToDecode.ToString();
                            }
                            else if (relocToDecode.OriginalValue > 0)
                            {
                                replacementValue += "+" + relocToDecode.ToString();
                            }
                            retVal = retVal.Replace(relocAddressStr, replacementValue);
                            if (oldVal == retVal)
                            {
                                Logger.WriteLine($"Replacement jank in {symbol.Name} ({instr}): can't find {relocAddressStr} ({relocToDecode.SymbolName}, {relocToDecode.OffsetFromSymbol}, {relocToDecode.OriginalValue})");
                            }
                        }
                        else
                        {
                            Logger.WriteLine($"Oh no! {relocToDecode.SymbolName} {relocToDecode.RelocAddress}");
                        }
                        relocs.RemoveAt(0);
                    }

                    return retVal;
                }

                var lines = disasm.Disassemble().Select(InstructionToLine).ToArray();
                File.AppendAllLines(textFilePath, lines);
                File.AppendAllText(textFilePath, "\n");
            }
            else if (symbol.Type == SymbolType.Type && i < symbols.Count - 2)
            {
                i += 2;
                symbol = symbols[i];
                int readBytes(int offset)
                {
                    int absOffset = symbol.Address + (5 * 4) + offset;
                    return BitConverter.ToInt32(codeBytesForReading[absOffset..(absOffset + 4)]);
                }
                int fieldCount = readBytes(0);
                for (int j = 0; j < fieldCount; j++)
                {
                    int fieldTypeAddress = readBytes((j+1) * 4);
                    var fieldTypeSymbol = symbolByAddress[fieldTypeAddress];
                    File.AppendAllText(textFilePath, $"    Field {j}: {fieldTypeSymbol.NameToPrint}\n");
                }
                File.AppendAllText(textFilePath, "\n");
            }
            else if (symbolType == SymbolType.Data)
            {
                foreach (var dataMember in dataMembers)
                {
                    switch (dataMember.Type)
                    {
                        case 1:
                            File.AppendAllText(textFilePath, $"    INT:{dataMember.Value:X8}\n");
                            break;
                        case 2:
                            File.AppendAllText(textFilePath, $"    FLT:{dataMember.Value:X8}\n");
                            break;
                        case 4:
                            var stringSymbol = symbolByAddress[(int)dataMember.Value];
                            File.AppendAllText(textFilePath, $"    STR:@{stringSymbol.NameToPrint}\n");
                            break;
                        default:
                            File.AppendAllText(textFilePath, $"    {dataMember.Type}:{dataMember.Value}");
                            break;
                    }
                }
            }
            else
            {
                var symbolData = codeBytesForReading[symbol.Address..symbolEnd];
                for (int j=0;j<symbolData.Length;j+=4)
                {
                    var subset = symbolData[j..Math.Min(j + 4, symbolData.Length)];
                    var bytesStringRepresentation = string.Join(" ", subset.Select(b => Convert.ToHexString(new[] { b })));
                    string line = $"    @{(symbol.Address + j):X8}: {bytesStringRepresentation}";
                    File.AppendAllText(textFilePath, line + "\n");
                }
                File.AppendAllText(textFilePath, "\n");
            }
        }

        if (libFunctions.Any())
        {
            Directory.CreateDirectory(disasmPath+"/Libs");
            foreach (var grouping in libFunctions.GroupBy(f => f.LibName.ToLowerInvariant()))
            {
                var libName = grouping.Key;
                var functions = grouping.ToArray();
                var filePath = disasmPath + "/Libs/" + Path.GetFileNameWithoutExtension(grouping.Key) + ".txt";
                File.AppendAllText(filePath, $"{libName}\n");
                foreach (var function in functions)
                {
                    var symbol = symbolByAddress[function.LookupAddress];
                    File.AppendAllText(filePath, $"    {symbol.Name}: {function.FunctionName}\n");
                }
            }
        }

        File.WriteAllLines(disasmPath + "/symbols.txt", symbols.Select(s => $"@{s.Address:X8}: {s.NameToPrint} {s.Type}"));
        Logger.WriteLine(":tada:");
    }
}