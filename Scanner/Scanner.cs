using FileManager.Core;
using FileManager.Core.Enums;
using FileManager.Core.Extensions;
using FileManager.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
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
        // for these and others, I am not sure if I should just store them in the database or in json; json might be slightly smaller and would be faster since I just
        // need to load them all once at the start of the program; but it also adds more things I need to keep track of (especially with path management); biggest 
        // benefit of leaving them as json is that it is easy for users to modify them manually, but idk how frequently users will want to do that
        internal static readonly HashSet<string> BlacklistedFolderPaths = new(); // JsonUtil.LoadHashSetFromFile(Path.Combine(Settings.GetMetadataPath(), "blacklisted_folder_paths.json")) ?? new HashSet<string>();
        internal static readonly HashSet<string> BlacklistedFolderNames = new(); // JsonUtil.LoadHashSetFromFile(Path.Combine(Settings.GetMetadataPath(), "blacklisted_folder_names.json")) ?? new HashSet<string>();
        internal static readonly HashSet<string> BlacklistedFilePaths = new();
        internal static readonly HashSet<string> BlacklistedFileNames = new();

        // folders in chosenFolders should have their default case, but replace \ with /
        private static readonly Dictionary<string, sbyte> chosenFolders = new();
        private static readonly Dictionary<string, ScanAction> scannedFolders = new();
        private static readonly HashSet<string> tempFolders = new();
        private static readonly HashSet<string> tempFiles = new();
        private static readonly HashSet<string> scannedFiles = new();

        // needs to be loaded from database (see Core.Collections (also probably rename that class))
        // this will just be a temporary set dictating which file extensions should be considered in the scan (based on user settings)
        // as a result; I might allow the user to choose which categories of files should be scanned (i.e. only IMAGE and AUDIO)
        // then I can query the list of file extensions belonging to those categories from the relevant table in the database and use it to 
        // construct this hashset (it might also be best to not have it be static since I would need to call Clear() every time)
        private static readonly HashSet<string> extensions = new()
        {
            // temporarily populate this with values (need to load from somewhere eventually)
            ".PNG", ".JPEG", ".JPG", ".JFIF", ".GIF", ".APNG", ".WEBP", ".BMP", ".SVG", ".TGA"
        };

        internal static sbyte RecursionDepth { get; set; } = -1;
        internal static string CurrentFolder { get; set; } = string.Empty;

        internal static uint ImportColor { get; set; } = 0;
        internal static string ImportName { get; set; } = string.Empty;
        internal static string ImportDesc { get; set; } = string.Empty;

        // FileAttributes.Encrypted | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary
        private const FileAttributes skippedFolderAttributes = (FileAttributes)16646;
        private static bool cancelling = false;
        private static ulong importId = 0;
        private static int fileCount = 0;

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
                // TryAdd() to avoid overwriting manual changes by user when scanning duplicate folders
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
                var results = ScannerDatabase.QueryPreviouslyImportedPaths(tempFolders);    // folders that have been previously imported (IGNORE)
                tempFolders.ExceptWith(results);                                            // folders that have NOT been previously imported (SCAN)

                foreach (string importedFolder in results) scannedFolders[importedFolder] = ScanAction.Ignore;
                foreach (string newFolder in tempFolders) scannedFolders[newFolder] = ScanAction.Scan;
            }
            tempFolders.Clear();

            if (Settings.AutostartScanOnPrescanCompletion)
            {
                Scan();
            }
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
            else scannedFolders[folder] = ScanAction.Scan;
        }

        // pretty sure that docs said any negative number for MaxRecursionDepth would be interpreted as int.MaxValue, but that seems to be false
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
                        MaxRecursionDepth = (maxDepth < 0) ? int.MaxValue : maxDepth,
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
            catch (Exception ex) when (ex is IOException || ex is SecurityException || ex is UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        // -------------------------------------------------------------------------------------------------- //
        //                                               Scan                                                 //
        // -------------------------------------------------------------------------------------------------- //
        internal static void CancelScan()
        {
            fileCount = 0;
            CancelPrescan();
            scannedFiles.Clear();
            ScannerDatabase.DeletePaths(importId);
        }

        internal static void Scan()
        {
            importId = Id.GetRandomUInt64();

            IterateFiles(tempFiles);
            foreach (var folderKV in scannedFolders)
            {
                if (folderKV.Value == ScanAction.Scan)
                {
                    IterateFiles(Directory.GetFiles(folderKV.Key));
                }
            }

            CreateImportInfo();

            if (scannedFiles.Count > 0)
            {
                ScannerDatabase.Insert(importId, scannedFiles.ToArray());
            }

            CancelScan();

            if (Settings.AutostartImportOnScanCompletion)
            {
                // emit signal to start import process
            }
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
                fileCount++;

                // insert into database if count equal to max insert size
                if (scannedFiles.Count == Settings.MaxDatabaseInsertSize)
                {
                    ScannerDatabase.Insert(importId, scannedFiles.ToArray());
                    scannedFiles.Clear();
                }
            }
        }

        private static void CreateImportInfo()
        {
            Lists.Imports[importId] = new ImportInfo()
            {
                Id = importId,
                Name = ImportName,
                Description = ImportDesc,
                Color = ImportColor,
                Total = fileCount
            };
            // insert into database (or do that with a method call on Lists (which would also handle adding to dictionary))
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
