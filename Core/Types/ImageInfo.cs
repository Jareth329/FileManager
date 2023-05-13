using FileManager.Core.Enums;

namespace FileManager.Core.Types
{
    internal sealed class ImageInfo
    {
        internal string Hash { get; set; } = string.Empty;
        internal ImageType Type { get; set; } = ImageType.Error;

        internal ulong AverageHash { get; set; }
        internal ulong ColorHash { get; set; }
        internal ulong DifferenceHash { get; set; }
        internal ulong PerceptualHash { get; set; }
        internal ulong WaveletHash { get; set; }

        internal byte Red { get; set; }
        internal byte Green { get; set; }
        internal byte Blue { get; set; }
        internal byte Yellow { get; set; }
        internal byte Cyan { get; set; }
        internal byte Fuchsia { get; set; }

        internal byte Vivid { get; set; }
        internal byte Neutral { get; set; }
        internal byte Dull { get; set; }

        internal byte Light { get; set; }
        internal byte Medium { get; set; }
        internal byte Dark { get; set; }
        internal byte Alpha { get; set; }
    }
}
