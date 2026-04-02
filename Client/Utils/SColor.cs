

using UnityEngine;

namespace MiyakoCarryService.Client.Utils
{
    public class SColor
    {
        public Color Rgb;
        public string Hex;
        public SColor(Color color)
        {
            Rgb = color;
            Hex = ColorToHex(Rgb);
        }

        private string ColorToHex(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        } 
    }
}
