using System.IO;

namespace FileManager.Core.Types
{
    internal sealed class CommonInfo
    {
        internal string Hash { get; set; }
        internal string Folder { get; set; }
        internal string File { get; set; }
        internal string Type { get; set; }

        internal long Size { get; set; }
        internal long CreationTime { get; set; }
        internal long LastWriteTime { get; set; }

        internal CommonInfo(string hash, FileInfo? info)
        {
            Hash = hash;

            if (info is not null)
            {
                Folder = info.DirectoryName ?? string.Empty;
                File = info.Name;
                // this is fine for initial type calculation; will need to be updated by category importer if extension does not match actual type
                Type = info.Extension.ToUpperInvariant();
                Size = info.Length;
                CreationTime = info.CreationTimeUtc.Ticks;
                LastWriteTime = info.LastWriteTimeUtc.Ticks;
            }
            else
            {
                Folder = string.Empty;
                File = string.Empty;
                Type = string.Empty;
                Size = -1;
            }
        }
    }
}
