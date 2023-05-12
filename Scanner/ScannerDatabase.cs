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
        internal static Error Insert(ulong importId, IEnumerable<string> paths)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "imports.db")}"))
                {
                    var cmd = connection.CreateCommand();
                    // actual table will be (path, id, index) ;; change foreach to for () and use i for index
                    cmd.CommandText = "INSERT INTO paths (id, path) VALUES ($id, $path)";
                    cmd.Parameters.AddWithValue("$id", importId);

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
