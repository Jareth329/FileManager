using Godot;

namespace FileManager.Core.Extensions
{
    internal static class ColorConverter
    {
        internal static int ConvertToInt32(Color color)
        {
            uint tmp = (byte)color.R8;
            tmp <<= 8;
            tmp |= (byte)color.G8;
            tmp <<= 8;
            tmp |= (byte)color.B8;
            tmp <<= 8;
            tmp |= (byte)color.A8;
            return (int)tmp;
        }

        internal static Color ConvertToColor(int color)
        {
            uint tmp = (uint)color;
            byte r = 0, g = 0, b = 0, a = 0;
            a = (byte)(tmp & 0xff);
            tmp >>= 8;
            b = (byte)(tmp & 0xff);
            tmp >>= 8;
            g = (byte)(tmp & 0xff);
            tmp >>= 8;
            r = (byte)(tmp & 0xff);
            return new Color(r, g, b, a);
        }
    }
}
