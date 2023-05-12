using FileManager.Core;
using FileManager.Core.Enums;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace FileManager.Scanner
{
    internal static class ScannerDatabase
    {
        internal static Error Insert(ulong importId, string[] paths)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "imports.db")}"))
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO paths (id, path, idx) VALUES ($id, $path, $index)";
                    cmd.Parameters.AddWithValue("$id", importId);

                    var path = cmd.CreateParameter();
                    path.ParameterName = "$path";
                    cmd.Parameters.Add(path);

                    var index = cmd.CreateParameter();
                    index.ParameterName = "$index";
                    cmd.Parameters.Add(index);

                    connection.Open();
                    using var transaction = connection.BeginTransaction();

                    var pathsSpan = new ReadOnlySpan<string>(paths);
                    int count = pathsSpan.Length;
                    for (int i = 0; i < count; i++)
                    {
                        path.Value = pathsSpan[i];
                        index.Value = i;
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                return Error.OK;
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle);
                return Error.Database;
            }
        }

        internal static Error DeletePaths(ulong importId)
        {
            return Error.OK;
        }

        internal static IEnumerable<string> QueryPreviouslyImportedPaths(IEnumerable<string> paths)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "imports.db")}"))
                {
                    return Array.Empty<string>(); // tmp
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle);
                return Array.Empty<string>();
            }
        }
    }
}
