namespace FileManager.Core.Types
{
    internal sealed class ImportInfo
    {
        internal ulong Id { get; set; }
        internal string Name { get; set; } = string.Empty;
        internal string Description { get; set; } = string.Empty;
        internal uint Color { get; set; }

        internal int Processed { get; set; }
        internal int Total { get; set; }
        internal int Success { get; set; }
        internal int Failure { get; set; }

        internal bool Complete { get; set; }
        internal long Started { get; set; }
        internal long Finished { get; set; }
    }
}
