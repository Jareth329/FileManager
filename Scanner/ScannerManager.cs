using FileManager.Core;
using Godot;
using System.Threading.Tasks;

namespace FileManager.Scanner
{
	public sealed partial class ScannerManager : Node
	{
        // -------------------------------------------------------------------------------------------------- //
        //                                              General                                               //
        // -------------------------------------------------------------------------------------------------- //

        // called whenever a new import/scan process is starting
        public static void Setup()
        {
            // reset local recursion depth to global settings value
            Scanner.RecursionDepth = Settings.MaxScanRecursionDepth;
        }

        // called whenever the currently processing path should be updated
        public static string GetCurrentFolder() => Scanner.CurrentFolder;

        // connect to the signal emitted by the changing of Settings.MaxScanRecursionDepth
        private static void SignalEXAMPLE(sbyte maxDepth) => SetRecursionDepth(maxDepth);

        // these might be moved to Importer
        public static void SetImportColor(uint color) => Scanner.ImportColor = color;
        public static void SetImportName(string name) => Scanner.ImportName = name;
        public static void SetImportDesc(string desc) => Scanner.ImportDesc = desc;

        // -------------------------------------------------------------------------------------------------- //
        //                                              Prescan                                               //
        // -------------------------------------------------------------------------------------------------- //

        // called whenever user selects new folders with the buttons, or by dropping them onto program
        public static void SelectFolders(string[] folders)
		{
            // add the newly selected paths to the internal dictionary, and assign the default recursionDepth
            Scanner.SelectFolders(folders);

            // update the UI to reflect the new paths
        }

        // called whenever user selects new files with the buttons, or by dropping them onto program
        public static void SelectFiles(string[] files)
		{
            // add the newly selected paths to the internal dictionary, and assign the default recursionDepth
            Scanner.SelectFiles(files);

            // update the UI to reflect the new paths
        }

        // called whenever user changes local recursion depth
        public static void SetRecursionDepth(sbyte recurDepth) => Scanner.RecursionDepth = recurDepth;

        // called whenever user changes a recursion depth override
        public static void SetRecursionDepthOverride(string folder, sbyte recurDepth) => Scanner.SetRecursionDepthOverride(folder, recurDepth);

        // called if user wants to cancel the prescan and clear memory
        public static void CancelPrescan() => Task.Run(Scanner.CancelPrescan);

        // called whenever user has finished setting the recursion settings for selected paths and is ready to begin prescan
        public static void StartPrescan() => Scanner.Prescan();// Task.Run(Scanner.Prescan);

        // -------------------------------------------------------------------------------------------------- //
        //                                               Scan                                                 //
        // -------------------------------------------------------------------------------------------------- //

        // called whenever user wants to cancel the scan, clear memory, and remove scanned paths from database (if relevant)
        public static void CancelScan() => Task.Run(Scanner.CancelScan);

        // called whenever user has finished adjusting scan settings and is ready to begin scan
        public static void StartScan() => Scanner.Scan();// Task.Run(Scanner.Scan);
    }
}