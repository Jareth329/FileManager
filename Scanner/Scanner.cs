﻿using FileManager.Core;
using FileManager.Core.Enums;
using FileManager.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Security;

namespace FileManager.Scanner
{
    internal static class FileFilter
    {
        internal static long MinSize { get; set; } = 0;
        internal static long MaxSize { get; set; } = 0;

        internal static bool IsDefault()
        {
            if (MinSize > 0) return false;
            if (MaxSize > 0) return false;
            return true;
        }
    }

    internal static class Scanner
    {
        internal static readonly HashSet<string> BlacklistedFolderPaths = JsonUtil.LoadHashSetFromFile(Path.Combine(Settings.GetMetadataPath(), "blacklisted_folder_paths.json")) ?? new HashSet<string>();
        internal static readonly HashSet<string> BlacklistedFolderNames = JsonUtil.LoadHashSetFromFile(Path.Combine(Settings.GetMetadataPath(), "blacklisted_folder_names.json")) ?? new HashSet<string>();
        internal static readonly HashSet<string> BlacklistedFilePaths = new();
        internal static readonly HashSet<string> BlacklistedFileNames = new();

        //private static readonly Dictionary<FileCategory, string> extensions = new();
        private static readonly HashSet<string> extensions = new();

        // folders in chosenFolders should have their default case, but replace \ with /
        private static readonly Dictionary<string, sbyte> chosenFolders = new();
        private static readonly Dictionary<string, ScanAction> scannedFolders = new();
        private static readonly HashSet<string> tempFolders = new();
        private static readonly HashSet<string> tempFiles = new();
        private static readonly HashSet<string> scannedFiles = new();

        internal static sbyte RecursionDepth { get; set; } = -1;
        internal static string CurrentFolder { get; set; } = string.Empty;

        // FileAttributes.Encrypted | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary
        private const FileAttributes skippedFolderAttributes = (FileAttributes)16646;
        private static bool cancelling = false;
        private static ulong scanId = 0;

        // -------------------------------------------------------------------------------------------------- //
        //                                              Prescan                                               //
        // -------------------------------------------------------------------------------------------------- //
        internal static void SelectFiles(string[] files)
        {
            foreach (string file in files)
            {
                tempFiles.Add(file);
            }
        }

        internal static void SelectFolders(string[] folders)
        {
            foreach (string folder in folders)
            {
                // TryAdd() to avoid overwriting manual changes by user when importing duplicate folders
                chosenFolders.TryAdd(folder.Replace('\\', '/'), -2);
            }
        }

        internal static void SetRecursionDepthOverride(string folder, sbyte recurDepth) => chosenFolders[folder.Replace('\\', '/')] = recurDepth;

        internal static void CancelPrescan()
        {
            cancelling = true;
            chosenFolders.Clear();
            scannedFolders.Clear();
            tempFolders.Clear();
            tempFiles.Clear();
        }

        internal static void Prescan()
        {
            cancelling = false;
            scannedFolders.Clear();
            foreach (var folder in chosenFolders)
            {
                if (cancelling) return;
                PrescanFolder(folder.Key, folder.Value);
            }

            if (Settings.SkipPreviouslyImportedFolders)
            {
                // construct sqlite query from tempFolders
                foreach (string folder in tempFolders)
                {
                    //
                }
            }
            tempFolders.Clear();
        }

        private static void PrescanFolder(string folder, sbyte recurDepthOverride)
        {
            CurrentFolder = folder;
            AddScannedFolder(folder);

            // return if override is set to 0, or if override is default and recursionDepth is set to not recurse (0)
            if (recurDepthOverride == 0 || (recurDepthOverride == -2 && RecursionDepth == 0)) return;

            sbyte maxDepth = (recurDepthOverride > -2) ? recurDepthOverride : RecursionDepth;
            foreach (string subfolder in EnumerateFolders(folder, maxDepth))
            {
                if (cancelling) return;
                CurrentFolder = subfolder;
                AddScannedFolder(subfolder);
            }
        }

        private static bool IsBlacklisted(string folder)
        {
            if (BlacklistedFolderPaths.Contains(folder.ToUpperInvariant())) return true;
            if (BlacklistedFolderNames.Contains(new DirectoryInfo(folder).Name.ToUpperInvariant()))
            {
                BlacklistedFolderPaths.Add(folder);
                return true;
            }
            return false;
        }

        private static void AddScannedFolder(string folder)
        {
            if (IsBlacklisted(folder)) scannedFolders[folder] = ScanAction.Blacklist;
            // add to TempFolders to be filtered by sql query later
            else if (Settings.SkipPreviouslyImportedFolders) tempFolders.Add(folder);
            else scannedFolders[folder] = ScanAction.Import;
        }

        private static IEnumerable<string> EnumerateFolders(string folder, sbyte maxDepth)
        {
            try
            {
                return new FileSystemEnumerable<string>(
                    folder,
                    (ref FileSystemEntry entry) => entry.ToFullPath(),
                    new EnumerationOptions()
                    {
                        AttributesToSkip = skippedFolderAttributes,
                        IgnoreInaccessible = true,
                        MaxRecursionDepth = maxDepth,
                        RecurseSubdirectories = true,
                        ReturnSpecialDirectories = false
                    })
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.IsDirectory,
                    ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                    {
                        var name = new Span<char>();
                        entry.FileName.ToUpperInvariant(name);
                        if (BlacklistedFolderNames.Contains(name.ToString())) return false;
                        if (BlacklistedFolderPaths.Contains(entry.ToFullPath().ToUpperInvariant().Replace('\\', '/'))) return false;
                        return true;
                    }
                };
            }
            catch (IOException) { return Array.Empty<string>(); }
            catch (SecurityException) { return Array.Empty<string>(); }
            catch (UnauthorizedAccessException) { return Array.Empty<string>(); }
        }

        // -------------------------------------------------------------------------------------------------- //
        //                                               Scan                                                 //
        // -------------------------------------------------------------------------------------------------- //
        internal static void CancelScan()
        {
            CancelPrescan();
            scannedFiles.Clear();
            ScannerDatabase.DeletePaths(scanId);
        }

        internal static void Scan()
        {
            scanId = Extensions.GetRandomUInt64();

            IterateFiles(tempFiles);
            foreach (var folderKV in scannedFolders)
            {
                if (folderKV.Value == ScanAction.Import)
                {
                    IterateFiles(Directory.GetFiles(folderKV.Key));
                }
            }

            if (scannedFiles.Count > 0)
            {
                ScannerDatabase.Insert(scanId, scannedFiles);
            }

            CancelScan();
        }

        private static void IterateFiles(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (cancelling) return;
                if (!extensions.Contains(Path.GetExtension(file).ToUpperInvariant())) continue;
                // only create FileInfo objects if filter is not set to default values
                if (!FileFilter.IsDefault() && !IsWithinConstraints(file)) continue;
                scannedFiles.Add(file);

                // insert into database if count equal to max insert size
                if (scannedFiles.Count == Settings.MaxDatabaseInsertSize)
                {
                    ScannerDatabase.Insert(scanId, scannedFiles);
                    scannedFiles.Clear();
                }
            }
        }

        private static bool IsWithinConstraints(string file)
        {
            var fileInfo = new FileInfo(file);

            if (FileFilter.MinSize > 0 && fileInfo.Length < FileFilter.MinSize) return false;
            if (FileFilter.MaxSize > 0 && fileInfo.Length > FileFilter.MaxSize) return false;

            return true;
        }
    }
}
