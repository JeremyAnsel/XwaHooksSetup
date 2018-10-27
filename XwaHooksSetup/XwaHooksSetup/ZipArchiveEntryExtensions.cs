using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XwaHooksSetup
{
    static class ZipArchiveEntryExtensions
    {
        public static void CopyTo(this ZipArchiveEntry entry, string path)
        {
            using (Stream stream = entry.Open())
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(file);
            }

            File.SetLastWriteTimeUtc(path, entry.LastWriteTime.UtcDateTime);
        }
    }
}
