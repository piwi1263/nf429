using System;
using System.Runtime.Versioning;
using System.Threading;

using nanoFramework.Displays.Ili9341;
using nanoFramework.Displays.Ili9341.HelperFonts;
using nanoFramework.Runtime.Native;

namespace nf429i
{
    public class Program
    {
        #region The Locals

        // For memory statistics
        private static uint _avail, _used; //, _current;

        // TFT declares
        private static LCD tft;
        private static HelpersFont font;

        // Help text
        private static string all, who, what, with, ver;

        // Indeces
        private static int idx1, idx2, idx3;

        // Fixed color, block buffer for on screen display
        private static ushort[] nano;
        private static ushort[] black;
        private static ushort nanoBlue;
        #endregion

        public static void Main()
        {
            try
            {
                // Block GC Messages
                Debug.EnableGCMessages(false);
                _avail = Debug.GC(false);

                // What version and name
                string a = SystemInfo.Version.ToString();
                
                // Setup the driver, init the text
                InitDisplay();

                // Show some board info
                DisplayBoardInfo();

                // Little block of 20 * 20 flip from left to right
                DisplayFlipBox();

                // Fill the screen with mosaic blocks
                DisplayMosaic();

                // Do incremental display
                DisplaySurprise();

                // Blinky the TFT way
                for (; ; )
                {
                    
                    Thread.Sleep(500);
                    tft.DrawRect(20, 28, 100, 112, Color565.LightBlue);
                    Thread.Sleep(500);
                    tft.DrawRect(20, 28, 100, 112, Color565.DarkBlue);
                }


                // Restore original screen 
                //DisplayBoardInfo();

                // Infinite loop to prevent from staling
                //for (; ; )
                //{
                //    Thread.Sleep(1000);
                //    _current = Debug.GC(false);
                //    Thread.Sleep(2000);
                //    tft.DrawString(121, 155, "                     ", Color565.Black, font);
                //    tft.DrawString(25, 155, "Available . " + _current.ToString(), Color565.White, font);
                //}
            }
            catch (Exception ex)
            {
                // Do whatever pleases you with the exception caught
                Console.WriteLine(ex.ToString());
                for (; ; )
                {
                    Thread.Sleep(200);
                }
            }
        }

        #region Helper Routines

        /// <summary>
        /// Fills the screen with square boxes for a number of times
        /// </summary>
        private static void DisplayMosaic()
        {
            tft.Mosaic(20, 1000);

            // Wait a bit
            //Thread.Sleep(3000);
        }

        /// <summary>
        /// Displays a flipping box with increasing speed
        /// </summary>
        private static void DisplayFlipBox()
        {
            // Create the auto bitmaps
            nano = new ushort[400];
            black = new ushort[400];
            nanoBlue = (ushort)ColorConversion.ToRgb565(Color888.NanoBlue);

            // Fill it with nanoFramework blue
            for (int i = 0; i < nano.Length; i++)
            {
                nano[i] = nanoBlue;
                black[i] = 0;
            }

            // Do the flip
            for (int varDelay = 250; varDelay > 30; varDelay -= 20)
            {
                for (int i = 0; i < 2; i++)
                {
                    tft.Flush(120, 180, 20, 20, nano);
                    tft.Flush(140, 180, 20, 20, black);
                    tft.Flush(120, 200, 20, 20, black);
                    tft.Flush(140, 200, 20, 20, nano);
                    Thread.Sleep(varDelay);
                    tft.Flush(120, 180, 20, 20, black);
                    tft.Flush(140, 180, 20, 20, nano);
                    tft.Flush(120, 200, 20, 20, nano);
                    tft.Flush(140, 200, 20, 20, black);
                    Thread.Sleep(varDelay);
                }
            }
        }

        /// <summary>
        /// Display some standard board info
        /// </summary>
        private static void DisplayBoardInfo()
        {
            // Well, clear the screen
            tft.ClearScreen();

            // And let us know who we are
            tft.DrawString(10, 10, "Hello World of nanoFramework", Color565.White, Color565.Black, font);
            tft.DrawString(10, 40, "System  . " + who, Color565.White, Color565.Black, font);
            tft.DrawString(10, 55, "Board . . " + what, Color565.White, Color565.Black, font);
            tft.DrawString(10, 70, "RTOS  . . " + with, Color565.White, Color565.Black, font);
            tft.DrawString(10, 85, "HAL . . . " + ver, Color565.White, Color565.Black, font);

            // Get the used memory 
            _used = _avail - Debug.GC(false);

            // Put it on screen
            tft.DrawString(10, 110, "Memory:", Color565.White, Color565.Black, font);
            tft.DrawString(25, 125, "Maximum . . " + _avail.ToString(), Color565.White, Color565.Black, font);
            tft.DrawString(25, 140, "Used  . . . " + _used.ToString(), Color565.White, Color565.Black, font);
        }

        /// <summary>
        /// Display standard startup screen
        /// </summary>
        private static void DisplaySurprise()
        {
            // 1. clear screen
            // 2. set screen color to LightBlue
            // 3. Fill a 80% or more box centered on the screen
            // 4. Draw the startup text to the inner block 
            // 5. Assume 40 chars and 15 lines
            // 
            //          1         2         3         4
            // 1234567890123456789012345678901234567890
            // 
            //    *** NANOFRAMEWORK PREVIEW 772 ***
            //
            //    8M RAM SYSTEM  8362776 BYTES FREE
            //
            // READY
            // [BLINK BLOCK]

            // Clear the screen
            tft.ClearScreen();

            // Fill the screen with light blue
            tft.FillScreen(Color565.LightBlue);

            // Draw a rectangular with a 20 pixel surrounding
            tft.DrawRect(20, 300, 20, 220, Color565.DarkBlue);

            // Now draw the text                      1.0.4.488
            tft.DrawString(25, 25, " *** NANOFRAMEWORK 1.0.4.488 *** ", Color565.LightBlue, Color565.DarkBlue, font);
            tft.DrawString(25, 55, "8M RAM SYSTEM  8362776 BYTES FREE", Color565.LightBlue, Color565.DarkBlue, font);
            tft.DrawString(20, 85, "READY.", Color565.LightBlue, Color565.DarkBlue, font);
        }

        /// <summary>
        /// Initiate the TFT:
        /// <list type="bullet">
        /// <item><description>Instantiate driver for a ILI9341</description></item>
        /// <item><description>Clear the screen</description></item>
        /// <item><description>Seton the backlight</description></item>
        /// <item><description>Fill the font structure</description></item>
        /// <item><description>Abstract board info in stand alone text</description></item>
        /// </list>
        /// </summary>
        private static void InitDisplay()
        {
            // Initiate our little driver
            tft = new LCD(Orientation.Landscape);

            // Well, clear the screen
            tft.ClearScreen();

            // Backlight is always on, on STM32F429
            tft.BackLight = true;

            // Get a helper font
            font = new HelpersFont(DejaVuMono8.Bitmaps, DejaVuMono8.Descriptors, DejaVuMono8.Height, DejaVuMono8.FontSpace);

            // Break apart the system build string
            all = SystemInfo.OEMString;
            idx1 = all.IndexOf('@');
            idx2 = all.IndexOf("built");
            idx3 = all.IndexOf("ChibiOS");

            who = all.Substring(0, idx1 - 1).Trim();
            what = all.Substring(idx1 + 1, idx2 - (idx1 + 1)).Trim();
            with = all.Substring(idx3).Trim();
            ver = SystemInfo.Version.ToString();
        }

        #endregion
    }
}
