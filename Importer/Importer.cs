using FileManager.Core;
using System;
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

        internal sealed class CommonMetadata
        {
            internal string Hash { get; set; }
            internal string Folder { get; set; }
            internal string File { get; set; }
            internal string Type { get; set; }

            internal long Size { get; set; }
            internal long CreationTime { get; set; }
            internal long LastWriteTime { get; set; }

            internal CommonMetadata(string hash, FileInfo? info)
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

        internal static void Import(ulong importId)
        {
            try
            {
                int offset = 0, limit = Settings.MaxImportBatchSize;
                string[] paths = new string[limit];

                using var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "paths.db")}");
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
                Console.WriteLine(sqle);
            }
        }

        private static void ProcessBatch(ulong importId, ReadOnlySpan<string> paths)
        {
            int count = paths.Length, index = 0;
            var metadata = new CommonMetadata[count];

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
                        var meta = new CommonMetadata(hash, fileInfo);
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

            InsertMetadata(new ReadOnlySpan<CommonMetadata>(metadata, 0, index));
            UpdateImportSuccessCount(importId); // could move this to the end of Import(), but it might actually be better to update this after every batch
        }

        private static void InsertMetadata(ReadOnlySpan<CommonMetadata> _metadata)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "metadata.db")}");
                SQLiteCommand pathCmd = connection.CreateCommand(), commonCmd = connection.CreateCommand();
                SQLiteParameter pathHashParam = pathCmd.CreateParameter(), folderParam = pathCmd.CreateParameter(), fileParam = pathCmd.CreateParameter(),
                    hashParam = commonCmd.CreateParameter(), typeParam = commonCmd.CreateParameter(), sizeParam = commonCmd.CreateParameter(),
                    creationParam = commonCmd.CreateParameter(), lastWriteParam = commonCmd.CreateParameter(), uploadParam = commonCmd.CreateParameter();

                pathCmd.CommandText = "INSERT OR IGNORE INTO paths VALUES ($hash, $folder, $file);";
                pathCmd.Parameters.Add(pathHashParam);
                pathCmd.Parameters.Add(folderParam);
                pathCmd.Parameters.Add(fileParam);

                commonCmd.CommandText = "INSERT OR IGNORE INTO common VALUES ($hash, $type, $size, $creation, $lastWrite, $upload);";
                commonCmd.Parameters.Add(hashParam);
                pathCmd.Parameters.Add(typeParam);
                commonCmd.Parameters.Add(sizeParam);
                commonCmd.Parameters.Add(creationParam);
                commonCmd.Parameters.Add(lastWriteParam);
                commonCmd.Parameters.Add(uploadParam);

                connection.Open();
                using var transaction = connection.BeginTransaction();
                foreach (var item in _metadata)
                {
                    pathHashParam.Value = item.Hash;
                    folderParam.Value = item.Folder;
                    fileParam.Value = item.File;
                    pathCmd.ExecuteNonQuery();

                    hashParam.Value = item.Hash;
                    typeParam.Value = item.Type;
                    sizeParam.Value = item.Size;
                    creationParam.Value = item.CreationTime;
                    lastWriteParam.Value = item.LastWriteTime;
                    uploadParam.Value = DateTime.UtcNow.Ticks;
                    commonCmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle);
            }
        }

        // ui will only show: processed/total (failed)
        // dictionary will also have a separate success count which will only be shown in ui once import has finished
        // this success count will be determined with COUNT(SELECT hash FROM imports WHERE id = $id)
        private static void UpdateDictionaryAndUI(ulong importId, bool failed)
        {
            // update ui and dictionary of importInfo
        }

        // this method will handle the COUNT() query and update the value for the importInfo in the UI, dictionary, and database
        private static void UpdateImportSuccessCount(ulong importId)
        {
            // 
        }
    }
}
