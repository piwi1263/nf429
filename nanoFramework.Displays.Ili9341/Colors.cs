using System;

namespace nanoFramework.Displays.Ili9341
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
	public enum Color888
	{
        Black = 0x000000,
        Blue  = 0x0000FF,
        NanoBlue = 0x00AEEF,
        LightBlue = 0xA5A5FF,
        DarkBlue = 0x4342E6,
        Green = 0x00FF00,
        Red   = 0xFF0000,
        White = 0xFFFFFF
	}

    public static class ColorConversion
    {
        public static Color565 ToRgb565(this Color888 color)
        {
            UInt16 rgb = (UInt16)color;

            int bits = (((rgb >> 19) & 0x1f) << 11) | (((rgb >> 10) & 0x3f) << 6) | (((rgb >> 3) & 0x1f));

            return (Color565)bits;
        }

    }

    [Flags]
    public enum Color565 : UInt16
    {
        Black = 0x0000,
        Blue  = 0x001F,
        LightBlue = 0xA53F,
        DarkBlue = 0x421C,
        Green = 0x07E0,
        Red   = 0xF800,
        White = 0xFFFF
    }
}
