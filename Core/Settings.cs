using System;

namespace FileManager.Core
{
    internal static class Settings
    {
        internal static string DefaultMetadataPath { get; set; } = "metadata";
        internal static string MetadataPath { get; set; } = string.Empty;
        internal static bool UseDefaultMetadataPath { get; set; } = true;

        internal static sbyte MaxScanRecursionDepth { get; set; } = -1;

        internal static bool SkipPreviouslyImportedFolders { get; set; } = true;

        internal static ushort MaxDatabaseInsertSize { get; set; } = 1024;
        internal static short MaxImportBatchSize { get; set; } = 1024; // need to ensure this is min 1 in settings ui spinbox

        internal static bool AutostartScanOnPrescanCompletion { get; set; } = false;
        internal static bool AutostartImportOnScanCompletion { get; set; } = false;

        internal static bool SqliteUseSynchronousOff { get; set; } = false;

        internal static string GetMetadataPath() => (UseDefaultMetadataPath) ? DefaultMetadataPath : MetadataPath;

        internal static string GetSqliteSynchronous() => (SqliteUseSynchronousOff) ? "OFF" : "NORMAL";
    }
}
