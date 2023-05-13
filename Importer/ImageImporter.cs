namespace FileManager.Importer
{
    internal static class ImageImporter
    {
        private struct PerceptualHashes
        {
            internal ulong Average = 0;
            internal ulong Difference = 0;
            internal ulong Perceptual = 0;
            internal ulong Wavelet = 0;

            internal PerceptualHashes(string hashes)
            {
                string[] _hashes = hashes.Split('?');
                if (_hashes.Length == 4)
                {
                    _ = ulong.TryParse(_hashes[0], out Average);
                    _ = ulong.TryParse(_hashes[1], out Difference);
                    _ = ulong.TryParse(_hashes[2], out Perceptual);
                    _ = ulong.TryParse(_hashes[3], out Wavelet);
                }
            }
        }

        private static ushort[] GetBuckets(PerceptualHashes phashes)
        {
            ulong hash = phashes.Difference;
            ushort[] result = new ushort[4];
            result[0] = (ushort)(hash & 0xffff);
            hash >>= 16;
            result[1] = (ushort)(hash & 0xffff);
            hash >>= 16;
            result[2] = (ushort)(hash & 0xffff);
            hash >>= 16;
            result[3] = (ushort)(hash & 0xffff);
            return result;
        }

        private struct Colors
        {
            // magenta=fuchsia, orange?, violet?, brown?
            // array ??
            internal byte R;    // red
            internal byte G;    // green
            internal byte B;    // blue
            internal byte Y;    // yellow
            internal byte C;    // cyan
            internal byte F;    // fuchsia/magenta
            internal byte V;    // vivid
            internal byte N;    // neutral
            internal byte D;    // dull
            internal byte L;    // light
            internal byte M;    // medium
            internal byte K;    // dark
            internal byte A;    // alpha
        }

        private static ulong GetColorHash(Colors colors)
        {
            static void AddByte(byte color, ref ulong hash)
            {
                if (color > 196) hash |= 255;       // 1111 1111
                else if (color > 160) hash |= 127;  // 0111 1111
                else if (color > 132) hash |= 63;   // 0011 1111
                else if (color > 96) hash |= 31;    // 0001 1111
                else if (color > 64) hash |= 15;    // 0000 1111
                else if (color > 32) hash |= 7;     // 0000 0111
                else if (color > 16) hash |= 3;     // 0000 0011
                else if (color > 0) hash |= 1;      // 0000 0001
                hash <<= 8;
            }
            static void AddNibble(byte color, ref ulong hash, bool last=false)
            {
                // implicit else is 0000
                if (color > 192) hash |= 15;        // 1111
                else if (color > 128) hash |= 7;    // 0111
                else if (color > 64) hash |= 3;     // 0011
                else if (color > 0) hash |= 1;      // 0001
                if (!last) hash <<= 4;
            }

            ulong hash = 0ul;

            // 48 bits : color/hue
            AddByte(colors.R, ref hash);
            AddByte(colors.G, ref hash);
            AddByte(colors.B, ref hash);
            AddByte(colors.Y, ref hash);
            AddByte(colors.C, ref hash);
            AddByte(colors.F, ref hash);

            // 12 bits : saturation
            AddNibble(colors.V, ref hash);
            AddNibble(colors.N, ref hash);
            AddNibble(colors.D, ref hash);

            // 3 bits : brightness/value
            hash <<= 1;
            if (colors.L > 84) hash |= 1;
            hash <<= 1;
            if (colors.M > 84) hash |= 1;
            hash <<= 1;
            if (colors.K > 84) hash |= 1;

            // 1 bit : alpha
            hash <<= 1;
            if (colors.A < 255) hash |= 1;

            return hash;
        }

        internal static void Import(string hash, string path)
        {
            // get thumbnail path
            // create & save thumbnail, return phashes and colors (ALL PYTHON)
            // get magickInfo to determine dimensions and (more importantly) the actual image type
            // insert metadata into metadata/image table
        }
    }
}
