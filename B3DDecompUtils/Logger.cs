//#define LOGGER_ENABLED
using System.Text;

namespace B3DDecompUtils;

public static class Logger
{
#if LOGGER_ENABLED
    private static FileStream? stream;

    public static void WriteLine(string line)
    {
        stream ??= File.Create("log.txt");

        Console.WriteLine(line);
        stream.Write(Encoding.UTF8.GetBytes(line+"\n"));
    }

    public static void End()
    {
        stream?.Flush();
        stream?.Dispose();
    }
#else
    public static void WriteLine(string _) { }
    public static void End() { }
#endif
}