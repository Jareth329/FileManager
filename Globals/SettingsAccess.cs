using FileManager.Core;
using FileManager.Core.Extensions;
using Godot;
using System.IO;

namespace FileManager.Globals
{
    public partial class SettingsAccess : Node
    {
        // godot does not like to interact with static C# objects/methods
        public int GetDefaultScanDepth() => Settings.DefaultScanRecursionDepth;
        public void SetDefaultScanDepth(int depth) => Settings.DefaultScanRecursionDepth = (sbyte)depth;

        public bool GetEnableFullscreenBackground() => Settings.EnableFullscreenBackground;
        public void SetEnableFullscreenBackground(bool enabled) => Settings.EnableFullscreenBackground = enabled;

        public Color GetFullscreenBackgroundColor() => ColorConverter.ConvertToColor(Settings.FullscreenBackgroundColor);
        public void SetFullscreenBackgroundColor(Color color) => Settings.FullscreenBackgroundColor = ColorConverter.ConvertToInt32(color);

        public int GetHsplitOffset() => Settings.HsplitOffset;
        public void SetHsplitOffset(int offset) => Settings.HsplitOffset = offset;
        public int GetVsplitOffsetLeft() => Settings.VsplitOffsetLeft;
        public void SetVsplitOffsetLeft(int offset) => Settings.VsplitOffsetLeft = offset;
        public int GetVsplitOffsetRight() => Settings.VsplitOffsetRight;
        public void SetVsplitOffsetRight(int offset) => Settings.VsplitOffsetRight = offset;

        public string GetMetadataPath() => Settings.GetMetadataPath();
        public string GetThumbnailPath() => Settings.GetThumbnailPath();

    }
}
