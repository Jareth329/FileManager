using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FileManager.Core.Extensions
{
    internal static class JsonUtil
    {
        internal static HashSet<string>? LoadHashSetFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<HashSet<string>>(stream);
        }
    }
}
