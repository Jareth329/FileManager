using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Security;
using FileManager.Core;
using FileManager.Core.Extensions;

namespace FileManager.Scanner
{
    internal enum ScanAction { Import, Skip, Blacklist }

    internal static class Prescanner
    {
        internal static readonly HashSet<string> BlacklistedFolderPaths = JsonUtil.LoadHashSetFromFile(Path.Combine(Settings.GetMetadataPath(), "blacklisted_folder_paths.json")) ?? new HashSet<string>();
        internal static readonly HashSet<string> BlacklistedFolderNames = JsonUtil.LoadHashSetFromFile(Path.Combine(Settings.GetMetadataPath(), "blacklisted_folder_names.json")) ?? new HashSet<string>();

        internal static sbyte RecursionDepthOverride { get; set; } = -2;
        internal static readonly Dictionary<string, sbyte> ChosenFolders = new();

        private static readonly Dictionary<string, ScanAction> scannedFolders = new();
        private static readonly HashSet<string> tempFolders = new();

        internal static void Start()
        {
            scannedFolders.Clear();
            foreach (var folder in ChosenFolders)
                Prescan(folder.Key, folder.Value);

            if (Settings.SkipPreviouslyImportedFolders)
            {
                // construct sqlite query from tempFolders
                foreach (string folder in tempFolders)
                {
                    //
                }
            }

            // construct tree or list from scannedFolders

            tempFolders.Clear();
        }

        private static sbyte GetMaxRecursionDepth(sbyte recurDepthOverride)
        {
            if (recurDepthOverride > -2) return recurDepthOverride;
            if (RecursionDepthOverride > -2) return RecursionDepthOverride;
            return Settings.MaxScanRecursionDepth;
        }

        private static bool IsBlacklisted(string folder)
        {
            if (BlacklistedFolderPaths.ContainsOrdinalIgnore(folder)) return true;
            if (BlacklistedFolderNames.ContainsOrdinalIgnore(new DirectoryInfo(folder).Name))
            {
                BlacklistedFolderPaths.Add(folder);
                return true;
            }
            return false;
        }

        private static void AddFolder(string folder, bool blacklisted, bool parentWasBlacklisted)
        {
            if (blacklisted) scannedFolders[folder] = ScanAction.Blacklist;
            else if (parentWasBlacklisted && Settings.SkipSubfoldersOfBlacklistedFolders) scannedFolders[folder] = ScanAction.Skip;
            else if (Settings.SkipPreviouslyImportedFolders) tempFolders.Add(folder);
            else scannedFolders[folder] = ScanAction.Import;
        }

        private static void Prescan(string folder, sbyte recurDepthOverride)
        {
            bool blacklisted = IsBlacklisted(folder);
            AddFolder(folder, blacklisted, false);

            if (recurDepthOverride == 0) return;
            if (recurDepthOverride == -2 && RecursionDepthOverride == 0) return;
            if (recurDepthOverride == -2 && RecursionDepthOverride == -2 && Settings.MaxScanRecursionDepth == 0) return;

            sbyte maxDepth = GetMaxRecursionDepth(recurDepthOverride);
            foreach (string subfolder in EnumerateFolders(folder, maxDepth))
                AddFolder(subfolder, IsBlacklisted(subfolder), blacklisted);
        }

        private static IEnumerable<string> EnumerateFolders(string folder, sbyte recurDepth)
        {
            try
            {
                return new FileSystemEnumerable<string>(
                    folder,
                    (ref FileSystemEntry entry) => entry.ToFullPath(),
                    new EnumerationOptions()
                    {
                        // FileAttributes.Encrypted | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary
                        AttributesToSkip = (FileAttributes)16646,
                        IgnoreInaccessible = true,
                        MaxRecursionDepth = recurDepth,
                        RecurseSubdirectories = true,
                        ReturnSpecialDirectories = false
                    })
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory
                };
            }
            catch (IOException) { return Array.Empty<string>(); }
            catch (SecurityException) { return Array.Empty<string>(); }
            catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
        }
    }
}
