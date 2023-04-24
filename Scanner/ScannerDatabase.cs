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
        internal static Error Insert(ulong scanId, IEnumerable<string> paths)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "paths.db")}"))
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "INSERT INTO paths (path, id) VALUES ($path, $id)";
                    cmd.Parameters.AddWithValue("$id", scanId);

                    var path = cmd.CreateParameter();
                    path.ParameterName = "$path";
                    cmd.Parameters.Add(path);

                    connection.Open();
                    using var transaction = connection.BeginTransaction();

                    foreach (string p in paths)
                    {
                        path.Value = p;
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

        internal static Error DeletePaths(ulong scanId)
        {
            return Error.OK;
        }

        internal static IEnumerable<string> QueryPreviouslyImportedPaths(IEnumerable<string> paths)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "imports.db")}"))
                {
                    //
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
