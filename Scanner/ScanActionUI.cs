using Godot;

namespace FileManager.Scanner
{
    public partial class ScanActionUI : MarginContainer
    {
        private static readonly Color lightGrey = new Color("595959");
        private static readonly Color darkGrey = new Color("333333");
        private static int index;

        public override void _Ready()
        {
            var bgColor = GetNode<ColorRect>("bg_color");
            if (index++ % 2 == 0) bgColor.Color = lightGrey;
            else bgColor.Color = darkGrey;
        }
    }
}