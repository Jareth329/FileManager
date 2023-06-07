using FileManager.Core;
using Godot;

namespace FileManager.Globals
{
    public partial class SettingsAccess : Node
    {
        // godot does not like to interact with static C# objects/methods
        public int GetDefaultScanDepth() => Settings.DefaultScanRecursionDepth;
        public void SetDefaultScanDepth(int depth) => Settings.DefaultScanRecursionDepth = (sbyte)depth;


    }
}
