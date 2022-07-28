using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XwaHooksSetup
{
    static class StreamExtensions
    {
        public static void WriteText(this Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static Dictionary<string, string> ReadDictionary(this Stream stream)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string section = string.Empty;
            var sb = new StringBuilder();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line.StartsWith("*** ", StringComparison.OrdinalIgnoreCase))
                    {
                        dictionary[section] = sb.ToString();

                        section = line;
                        sb.Clear();
                        continue;
                    }

                    sb.AppendLine(line);
                }

                dictionary[section] = sb.ToString();
            }

            return dictionary;
        }
    }
}
