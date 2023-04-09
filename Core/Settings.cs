using System;

namespace FileManager.Core
{
    internal static class Settings
    {
        internal static string DefaultMetadataPath { get; set; } = string.Empty;
        internal static string MetadataPath { get; set; } = string.Empty;

        internal static sbyte MaxScanRecursionDepth { get; set; } = -1;

        internal static bool SkipPreviouslyImportedFolders { get; set; } = true;
        internal static bool SkipSubfoldersOfBlacklistedFolders { get; set; } = true;

        internal static string GetMetadataPath()
        {
            return MetadataPath;
        }
    }
}
