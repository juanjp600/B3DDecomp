using System.Diagnostics;
using System.Text;

namespace B3DDecompUtils;

public static class Logger
{
    private static FileStream? stream;
    
    public static void WriteLine(string line)
    {
        stream ??= File.Create("log.txt");

        if (line.Count(c => c == '\\') > 2)
        {
            //Debugger.Break();
        }

        if (line.Contains("fillroom: arg0 is float"))
        {
            Debugger.Break();
        }
        Console.WriteLine(line);
        stream.Write(Encoding.UTF8.GetBytes(line+"\n"));
    }

    public static void End()
    {
        stream?.Flush();
        stream?.Dispose();
    }
    
}