using System.Text;

namespace B3DDecompUtils;

public static class BinaryReaderExtensions
{
    public static string ReadCStr(this BinaryReader reader)
    {
        var bytes = new List<byte>();
        while (true)
        {
            byte b = reader.ReadByte();
            if (b == '\0') { break; }
            bytes.Add(b);
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}