﻿using FileManager.Core;
using FileManager.Core.Types;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security;

namespace FileManager.Importer
{
    internal static class Importer
    {
        // this is the generic importer that ALL files (stored by Scanner) will go through
        // it will handle checking if the file still exists and is accessible
        // it will handle storing information common to all file types (hash, hash/path relationship, file times, file size, etc) (also hash/importId relationship)
        // it will also handle determining the file's category and handing the files off to their dedicated importers (if one exists for that category)

        // this will query a collection of file paths to process from the paths table in the database, based on their associated importId
        // I might also store a (processed INT) boolean on each row to determine if that path has already been processed; this would then be 
        // updated to be true right before this method exits (though this does create an issue with multiple threads, so I would need to either 
        // have an additional state for 'processing' to indicate another thread is handling it (and then update the batch when it is queried), or
        // have this method handle the threading itself (so it can keep track of the current offset in the table and prevent duplicate importing)

        // this might be declared in another class; might also have a Dictionary<byte, string[]> for easily getting a list of types for a category
        private static readonly Dictionary<string, byte> categoryLookup = new(); // png:0 for example

        internal static void Import(ulong importId)
        {
            try
            {
                int offset = 0, limit = Settings.MaxImportBatchSize;
                string[] paths = new string[limit];

                using var connection = new SQLiteConnection(Database.GetConnectionString("imports.db"));
                SQLiteCommand cmd = connection.CreateCommand(), delete = connection.CreateCommand();
                SQLiteParameter offsetParam = cmd.CreateParameter(), endpointParam = cmd.CreateParameter(), deleteEndpointParam = delete.CreateParameter();

                // could maybe remove the endpoint and just use LIMIT instead (though I am unsure if that affects performance)
                cmd.CommandText =
                @"
                    SELECT path FROM paths
                    WHERE id = $id
                    AND idx >= $offset
                    AND idx < $endpoint;
                ";
                delete.CommandText = "DELETE FROM paths WHERE idx < $endpoint"; // everything before endpoint should already be processed

                cmd.Parameters.AddWithValue("$id", importId);
                offsetParam.ParameterName = "$offset";
                endpointParam.ParameterName = "$endpoint";
                deleteEndpointParam.ParameterName = "$endpoint";

                Lists.Imports[importId].Started = DateTime.UtcNow.Ticks;

                while (true)
                {
                    int count = 0;
                    offsetParam.Value = offset;
                    endpointParam.Value = offset + limit;

                    connection.Open();
                    // delete previous batch (if there was one)
                    if (offset > 0)
                    {
                        deleteEndpointParam.Value = offset;
                        delete.ExecuteNonQuery();
                    }

                    // read paths for this batch
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            paths[count] = reader.GetString(0);
                            count++;
                        }
                    }
                    connection.Close();

                    // import this batch
                    if (count > 0)
                    {
                        ProcessBatch(importId, new ReadOnlySpan<string>(paths, 0, count));
                    }

                    // delete final batch (if there was one) and exit if done
                    if (count < Settings.MaxImportBatchSize)
                    {
                        if (count > 0)
                        {
                            connection.Open(); // closing handled implicitly by using statement (hopefully)
                            deleteEndpointParam.Value = offset + limit;
                            delete.ExecuteNonQuery();
                        }
                        break;
                    }

                    // update offset
                    offset += count;
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine($"Importer.Import() : {sqle}");
            }
        }

        private static void ProcessBatch(ulong importId, ReadOnlySpan<string> paths)
        {
            int count = paths.Length, index = 0;
            var metadata = new CommonInfo[count];

            for (int i = 0; i < count; i++)
            {
                bool failed = false;
                try
                {
                    string path = paths[i];
                    if (!File.Exists(path)) failed = true;
                    else
                    {
                        // could use another method to avoid reliance on Godot here, but this is the fastest I have tested
                        string hash = Godot.FileAccess.GetSha256(path);
                        var fileInfo = new FileInfo(path);
                        var meta = new CommonInfo(hash, fileInfo);
                        if (meta.Size < 0) failed = true;
                        else metadata[index] = meta;
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is SecurityException || ex is UnauthorizedAccessException)
                {
                    failed = true;
                }

                // only increment if result was actually added to metadata
                if (!failed) index++;
                UpdateDictionaryAndUI(importId, failed);
            }

            var metaSpan = new ReadOnlySpan<CommonInfo>(metadata, 0, index);
            Insert(importId, metaSpan);
            UpdateSuccessCount(importId); // could move this to the end of Import(), but it might actually be better to update this after every batch
            ImportAdditionalMetadata(metaSpan);
        }

        private static void ImportAdditionalMetadata(ReadOnlySpan<CommonInfo> metadata)
        {
            foreach (var meta in metadata)
            {
                string path = Path.Combine(meta.Folder, meta.File);
                byte category = categoryLookup[meta.Type];
                // unsure if animations will be stored in same table as images, but with extra data null for images, or extra in separate table, or if
                // animations in general will be in a separate table
                if (category == Category.Image) ImageImporter.Import(meta.Hash, path);
                else if (category == Category.Animation) AnimationImporter.Import(meta.Hash, path);
                // handle others here once their importers are created, files with OTHER category will only be processed by the common importer
            }
        }

        //  - need to either add support for scanning all files, or allow user to choose new file types to at least add common importer support to
        //      (if user just wants to use tagging/grouping/searching capabilities) (category already defaults to Other if type not recognized)

        private static void Insert(ulong importId, ReadOnlySpan<CommonInfo> _metadata)
        {
            try
            {
                using var connection = new SQLiteConnection(Database.GetConnectionString("metadata.db"));
                SQLiteCommand pathCmd = connection.CreateCommand(), commonCmd = connection.CreateCommand(), importCmd = connection.CreateCommand();
                SQLiteParameter pathHashParam = pathCmd.CreateParameter(), folderParam = pathCmd.CreateParameter(), fileParam = pathCmd.CreateParameter(),
                    hashParam = commonCmd.CreateParameter(), sizeParam = commonCmd.CreateParameter(), creationParam = commonCmd.CreateParameter(),
                    lastWriteParam = commonCmd.CreateParameter(), uploadParam = commonCmd.CreateParameter(), categoryParam = commonCmd.CreateParameter(),
                    importHashParam = importCmd.CreateParameter();

                pathCmd.CommandText = "INSERT OR IGNORE INTO paths (hash, folder, file) VALUES ($hash, $folder, $file);";
                pathCmd.Parameters.Add(pathHashParam);
                pathCmd.Parameters.Add(folderParam);
                pathCmd.Parameters.Add(fileParam);

                commonCmd.CommandText = "INSERT OR IGNORE INTO common (hash, size, creat, lastw, fstup, cat) VALUES ($hash, $size, $creation, $lastWrite, $upload, $category);";
                commonCmd.Parameters.Add(hashParam);
                commonCmd.Parameters.Add(sizeParam);
                commonCmd.Parameters.Add(creationParam);
                commonCmd.Parameters.Add(lastWriteParam);
                commonCmd.Parameters.Add(uploadParam);
                commonCmd.Parameters.Add(categoryParam);

                importCmd.CommandText = "INSERT OR IGNORE INTO imports (id, hash) VALUES ($importId, $hash);";
                importCmd.Parameters.AddWithValue("$importId", importId);
                importCmd.Parameters.Add(importHashParam);

                connection.Open();
                using var transaction = connection.BeginTransaction();
                foreach (var item in _metadata)
                {
                    pathHashParam.Value = item.Hash;
                    folderParam.Value = item.Folder;
                    fileParam.Value = item.File;
                    pathCmd.ExecuteNonQuery();

                    bool found = categoryLookup.TryGetValue(item.Type, out byte category);
                    hashParam.Value = item.Hash;
                    sizeParam.Value = item.Size;
                    creationParam.Value = item.CreationTime;
                    lastWriteParam.Value = item.LastWriteTime;
                    uploadParam.Value = DateTime.UtcNow.Ticks;
                    categoryParam.Value = (found) ? category : Category.Other;
                    commonCmd.ExecuteNonQuery();

                    importHashParam.Value = item.Hash;
                    importCmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine($"Importer.Insert() : {sqle}");
            }
        }

        // ui will only show: processed/total (failed)
        // dictionary will also have a separate success count which will only be shown in ui once import has finished
        // this success count will be determined with COUNT(SELECT hash FROM imports WHERE id = $id)
        private static void UpdateDictionaryAndUI(ulong importId, bool failed)
        {
            // update ui and dictionary of importInfo
            var temp = Lists.Imports[importId];
            temp.Processed++;
            if (failed) temp.Failure++;
            Lists.Imports[importId] = temp;

            // update ui (the reason temp exists, since otherwise I need to access imports[importId] 2x anyways and it would be pointless)
        }

        // this method will handle the COUNT() query and update the Success value for the importInfo in the UI, dictionary, and database
        private static void UpdateSuccessCount(ulong importId)
        {
            try
            {
                using var connection = new SQLiteConnection(Database.GetConnectionString("metadata.db"));
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM imports WHERE id = $importId;";
                cmd.Parameters.AddWithValue("$importId", importId);
                using var reader = cmd.ExecuteReader();
                int count = (reader.Read()) ? reader.GetInt32(0) : 0;

                Lists.Imports[importId].Success = count;
                // update UI
                // update database
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine($"Importer.UpdateSuccessCount() : {sqle}");
            }
        }
    }
}
