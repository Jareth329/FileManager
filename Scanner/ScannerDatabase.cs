﻿using FileManager.Core;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace FileManager.Scanner
{
    internal static class ScannerDatabase
    {
        internal static void Insert(ulong importId, string[] paths)
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
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle);
            }
        }

        internal static void DeletePaths(ulong importId)
        {
            //
        }

        // will likely change argument/return types
        internal static IEnumerable<string> QueryPreviouslyImportedPaths(HashSet<string> folders)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "metadata.db")}");
                var cmd = connection.CreateCommand();

                // should result in something like: WHERE folder=f0 OR folder=f1 OR folder=f2 ... with each one being a proper paramter
                var builder = new StringBuilder("SELECT folder FROM paths WHERE folder=f0");
                int count = 0;
                foreach (var folder in folders)
                {
                    if (count == 0) cmd.Parameters.AddWithValue("f0", folder);
                    else
                    {
                        string fnum = $"f{count}";
                        builder.Append(" OR folder=").Append(fnum);
                        cmd.Parameters.AddWithValue(fnum, folder);
                    }
                    count++;
                }
                builder.Append(';');
                cmd.CommandText = builder.ToString();

                // execute reader, construct (List<string>?) from results
                // call tempFolders.ExceptWith(list) to get list of folders that HAVE NOT been imported before
                // list will contain the folders that HAVE been imported before

                return Array.Empty<string>(); // tmp
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle);
                return Array.Empty<string>();
            }
        }
    }
}
