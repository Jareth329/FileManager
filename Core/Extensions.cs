using System;
using System.Collections.Generic;
using System.Linq;

namespace FileManager.Core.Extensions
{
    internal static class Extensions
    {
        internal static bool ContainsOrdinalIgnore(this HashSet<string> set, string value)
        {
            if (set is null) return false;
            if (set.Count == 0) return false;
            return set.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
