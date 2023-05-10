using System.Collections.Generic;

namespace FileManager.Core
{
    internal static class Collections
    {
        // load from .json file; will contain 
        // not sure if will use actually; I know I will have a table that contains rows of (ext TEXT PK, cat TEXT) which will be used
        // to determine the category of each file extension (i.e. png will be in the IMAGE category) (categories might be INT instead of TEXT)
        internal static readonly Dictionary<string, List<string>> FileExtensions = new();
    }
}
