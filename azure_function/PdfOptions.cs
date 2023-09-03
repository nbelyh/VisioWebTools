using System.Drawing;

namespace AddTooltips
{
    public class PdfOptions
    {
        public int VerticalLocation { get; set; } = 0;
        public int HorizontalLocation { get; set; } = 0;
        public string Icon { get; set; } = "Note";
        public Color Color { get; set; } = Color.LightYellow;
    }
}
