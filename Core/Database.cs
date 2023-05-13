using System;
using System.Data.SQLite;
using System.IO;

namespace FileManager.Core
{
    internal static class Database
    {
        internal static void Create()
        {
            try
            {
                using (var connMetadata = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "metadata.db")}"))
                {
                    var cmd = connMetadata.CreateCommand();
                    connMetadata.Open();
                    using var transaction = connMetadata.BeginTransaction();

                    // general note: still unsure if sha should be a FK in every table except common or not; easier to answer for tables
                    // where it is not unique (like paths) and instead I can just make a composite PK of it and another column, but it 
                    // is not so easy to answer for tables where it is unique (like image)

                    // general note2: need to decide if I will have multiple databases or not (depends mainly on whether there are 
                    // performance concerns one way or the other, as I should not need to query between multiple concepts at once
                    // (ie imports and metadata))

                    // common for all file types; sha256 hash, fileSize (bytes), fileCreationTime, fileLastWriteTime, fileFirstUploadTime, fileCategory
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS common(hash TEXT PK, size INT, create INT, lastw INT, fstup INT, cat INT);";
                    cmd.ExecuteNonQuery();

                    // for storing folder relationships (parents/etc) I think it is better to have those relationships be stored in a separate table
                    // with the full path as the PK; that way they can be looked up and will only be stored once (instead of having to duplicate folder
                    // parents across all rows in paths) ;; note that I could use the same approach for the folder column and change it to an INT folderId
                    // which would save some space in the database
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS paths(hash TEXT FK, folder TEXT, file TEXT, PK (hash, folder, file))";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE INDEX IF NOT EXISTS _folder ON paths(folder)";
                    cmd.ExecuteNonQuery();

                    // need to decide if all tags will be stored in the same table, or if each main category will have its own (which would require that
                    // I ensure tagIds are unique across all types (maybe just use a Guid) (might be fine to just use cryptorandom ulong though)
                    // if I use separate tables AND I make each category something the user can modify AND allow the user to add new tag categories, then
                    // I will also need to ensure I create tables for those. Currently though I am leaning towards either having subcategories that are 
                    // mainly for grouping purposes in the UI AND also adding all possible columns to database for each tag type and allowing user to 
                    // choose which ones are enabled (disabled ones would not have their content removed from database unless user specifically chooses
                    // to do so) (but this would depend on how much additional space is required to store the empty columns)
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS tags(hash TEXT FK, tagId INT FK, PK (hash, tagId))";
                    cmd.ExecuteNonQuery();

                    // metadata specific to images; sha256 hash, type (actual type of image, png/jpeg/etc) see Core.Enums.ImageType,
                    //  (perceptual hashes): averageHash, colorHash, differenceHash, perceptualHash, waveletHash,
                    //  (colors): red, green, blue, yellow, cyan, fuchsia/magenta, vivid, neutral, dull, light, medium, dark, alpha
                    // might add other tertiary colors like orange/brown/purple depending on the estimated database size increase
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS image(hash TEXT PK, type INT, avg INT, colr INT, dif INT, perc INT, wav INT, r INT, g INT, b INT, y INT, c INT, f INT, v INT, n INT, d INT, l INT, m INT, INT k, INT, a INT)";
                    cmd.ExecuteNonQuery();

                    // id comes first in PK because that will be what I query by
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS imports(id INT, hash TEXT, PK (id, hash));";
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                }

                using (var connImports = new SQLiteConnection($"Data Source={Path.Combine(Settings.GetMetadataPath(), "imports.db")}"))
                {
                    var cmd = connImports.CreateCommand();
                    connImports.Open();
                    using var transaction = connImports.BeginTransaction();

                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS paths(id INT, path TEXT, idx INT, PK (id, path));";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS imports(id INT PK, name TEXT, desc TEXT, color INT, proc INT, total INT, succ INT, fail INT, done INT, start INT, finish INT);";
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
            catch (SQLiteException sqle)
            {
                Console.WriteLine(sqle);
            }
        }
    }
}
