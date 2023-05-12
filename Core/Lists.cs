using FileManager.Core.Types;
using System.Collections.Generic;

namespace FileManager.Core
{
    // using individual byte declarations for now to save ram (probably)
    // internal enum Category { Image, Video, Audio, Text, Model, AI, Other }
    internal static class Category
    {
        internal const byte Image = 0;
        internal const byte Animation = 1;
        internal const byte Video = 2;
        internal const byte Audio = 3;
        internal const byte Model = 4;
        internal const byte Text = 5;
        internal const byte AI = 6;
        internal const byte Other = 11;
    }

    internal static class Lists
    {
        // load from .json file; will contain 
        // not sure if will use actually; I know I will have a table that contains rows of (ext TEXT PK, cat TEXT) which will be used
        // to determine the category of each file extension (i.e. png will be in the IMAGE category) (categories might be INT instead of TEXT)
        internal static readonly Dictionary<string, List<string>> FileExtensions = new();

        internal static readonly Dictionary<ulong, ImportInfo> Imports = new();
    }
}
