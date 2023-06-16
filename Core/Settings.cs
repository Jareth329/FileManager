using FileManager.Globals;
using System;
using System.IO;

namespace FileManager.Core
{
    internal static class Settings
    {
        internal static string DefaultMetadataPath { get; set; } = "metadata";
        internal static string MetadataPath { get; set; } = string.Empty;
        internal static bool UseDefaultMetadataPath { get; set; } = true;

        internal static string DefaultThumbnailPath { get; set; } = "thumbnails";
        internal static string ThumbnailPath { get; set; } = string.Empty;
        internal static bool UseDefaultThumbnailPath { get; set; } = true;

        internal static sbyte MaxScanRecursionDepth { get; set; } = -1;
        internal static sbyte DefaultScanRecursionDepth { get; set; } = -1;

        internal static bool SkipPreviouslyImportedFolders { get; set; } = true;

        internal static ushort MaxDatabaseInsertSize { get; set; } = 1024;
        internal static short MaxImportBatchSize { get; set; } = 1024; // need to ensure this is min 1 in settings ui spinbox

        internal static bool AutostartScanOnPrescanCompletion { get; set; } = true;
        internal static bool AutostartImportOnScanCompletion { get; set; } = false;

        internal static bool SqliteUseSynchronousOff { get; set; } = true;

        internal static bool EnableFullscreenBackground { get; set; } = true;
        internal static int FullscreenBackgroundColor { get; set; } = 0;

        internal static int HsplitOffset { get; set; } = -240;
        internal static int VsplitOffsetLeft { get; set; } = 0;
        internal static int VsplitOffsetRight { get; set; } = 320;

        internal static string GetMetadataPath()
        {
            if (UseDefaultMetadataPath || string.IsNullOrWhiteSpace(MetadataPath))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), DefaultMetadataPath);
            }
            else return MetadataPath;
        }

        internal static string GetThumbnailPath()
        {
            if (UseDefaultThumbnailPath || string.IsNullOrWhiteSpace(ThumbnailPath))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), DefaultThumbnailPath);
            }
            else return ThumbnailPath;
        }

        internal static string GetSqliteSynchronous() => (SqliteUseSynchronousOff) ? "OFF" : "NORMAL";
    }
}
