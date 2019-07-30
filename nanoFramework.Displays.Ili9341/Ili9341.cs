using System;
using System.Threading;

using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace nanoFramework.Displays.Ili9341
{
    public partial class LCD
    {
        private GpioController _gpio;
        private GpioPin _dataCommandPin;
        private GpioPin _resetPin;
        private GpioPin _backlightPin;
        private SpiDevice _display;
        private SpiConnectionSettings _settings;

        // Fixed for STM32F429I-DISCOVERY only
        private readonly int GPIO_PIN_PC2 = 2 * 16 + 2;
        private readonly int GPIO_PIN_PD12 = 3 * 16 + 12;
        private readonly int GPIO_PIN_PD13 = 3 * 16 + 13;
        private readonly int GPIO_PIN_PG13 = 6 * 16 + 13;

        private int Width = 240;
        private int Height = 320;

        #region Public Properties

        private bool _backlight;

        public bool BackLight
        {
            get { return _backlight; }
            set
            {
                if (_backlightPin != null)
                {
                    if (value == true)
                    {
                        _backlightPin.Write(GpioPinValue.High);
                        _backlight = true;
                    }
                    else
                    {
                        _backlightPin.Write(GpioPinValue.Low);
                        _backlight = false;
                    }
                }
            }
        }
        #endregion

        #region Constructors

        /// <summary>
        /// Initial constructor use for STM32F429I-DISCOVERY Only.
        /// <para>
        /// Since the discovery board is using fixed pins for reset, data command, select. 
        /// It can be set to their values and a short hand constructor can be used.
        /// </para>
        /// </summary>
        /// <param name="orientation">The orientation of the display comes in four flavours.</param>
        public LCD(Orientation orientation)
        {
            // Since this is for STM32F429I-DISCOVERY we can use the fixed pins.
            InitDriver("SPI5", GPIO_PIN_PC2, GPIO_PIN_PD13, GPIO_PIN_PD12, GPIO_PIN_PG13, orientation);
        }

        /// <summary>
        /// The constructor that uses all parameters to initiate the ILI9341 driver.
        /// </summary>
        /// <param name="spiDevice">The string representing the SPI Device name. Such as SPI5.</param>
        /// <param name="selectPin">The int for what pin the SPI Device will be selected on the SPI Bus.</param>
        /// <param name="dataCommandPin">The integer where what pin is flipping between sending a command or a data value to the ILI9341.</param>
        /// <param name="resetPin">The integer where a reset will be invoked.</param>
        /// <param name="backlightPin">The pin integer where the backlight will be lit.</param>
        /// <param name="orientation">The orientation of the display comes in four flavours.</param>
        public LCD(string spiDevice, int selectPin, int dataCommandPin, int resetPin, int backlightPin, Orientation orientation)
        {
            // Trap for invalid or not supported SPI devices.
            if (spiDevice == null) { throw new System.ArgumentNullException("spiDevice", "Parameter can not be null"); }
            if (spiDevice.Length != 4) { throw new System.ArgumentException("spiDevice", "Parameter can not be less than 4 position, like SPI5"); }
            if (SpiDevice.GetDeviceSelector(spiDevice).Length != 4) { throw new System.ArgumentException("spiDevice", "Parameter contains an SPI device that is not found."); }

            // Init the driver with all parms
            InitDriver(spiDevice, selectPin, dataCommandPin, resetPin, backlightPin, orientation);
        }

        protected virtual void InitDriver(string spiDevice, int selectPin, int dataCommandPin, int resetPin, int backlightPin, Orientation orientation)
        {
            // Init the SPI
            _settings = new SpiConnectionSettings(selectPin)
            {
                Mode = SpiMode.Mode0,
                ClockFrequency = 40 * 1000 * 1000,
                DataBitLength = 8
            };
            _display = SpiDevice.FromId(spiDevice, _settings);

            // Start using the Gpio
            _gpio = GpioController.GetDefault();

            // The data/command line
            _dataCommandPin = _gpio.OpenPin(dataCommandPin);
            _dataCommandPin.SetDriveMode(GpioPinDriveMode.Output);
            _dataCommandPin.Write(GpioPinValue.High);

            // The backlight line
            _backlightPin = _gpio.OpenPin(backlightPin);
            _backlightPin.SetDriveMode(GpioPinDriveMode.Output);
            _backlightPin.Write(GpioPinValue.Low);

            // The reset line
            _resetPin = _gpio.OpenPin(resetPin);
            _resetPin.SetDriveMode(GpioPinDriveMode.Output);
            _resetPin.Write(GpioPinValue.High);

            // Init and Set orientation
            InitDisplay(orientation);

        }


        #endregion

        #region Public Methods

        public void DrawString(int x, int y, string s, Color565 foreground, Color565 background, Font font)
        {
            var currentX = x;
            char[] chars = s.ToCharArray();

            foreach (char c in chars)
            {
                var character = font.GetFontData(c);

                if (c == '\n') //line feed
                {
                    y += character.Height;
                }
                else if (c == '\r') //carriage return
                {
                    currentX = x;
                }
                else
                {
                    if (currentX + character.Width > Width)
                    {
                        currentX = x; //start over at the left and go to a new line.
                        y += character.Height;
                    }

                    DrawChar(currentX, y, foreground, background, character);
                    currentX += character.Width + character.Space;
                }
            }
        }

        public void DrawChar(int x, int y, Color565 foreground, Color565 background, FontCharacter character)
        {
            lock (this)
            {
                SetWindow(x, x + character.Width - 1, y, y + character.Height - 1);

                var pixels = new ushort[character.Width * character.Height];
                var pixelPosition = 0;

                for (var segmentIndex = 0; segmentIndex < character.Data.Length; segmentIndex++)
                {
                    var segment = character.Data[segmentIndex];
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x80) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x40) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x20) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x10) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x8) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x4) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x2) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                    if (pixelPosition < pixels.Length) { pixels[pixelPosition] = (segment & 0x1) != 0 ? (ushort)foreground : (ushort)background; pixelPosition++; }
                }

                //uncomment this to see the characters in the debug window that would be displayed on the screen.
                //var currentBuffer = string.Empty;
                //for (var pixel = 0; pixel < pixels.Length; pixel++)
                //{
                //    if (pixels[pixel] > 0)
                //    {
                //        currentBuffer += "X";
                //    }
                //    else
                //    {
                //        currentBuffer += "-";
                //    }
                //    if (currentBuffer.Length >= character.Width)
                //    {
                //        Console.Write(currentBuffer);
                //        currentBuffer = string.Empty;
                //    }
                //}

                SendData(pixels);
            }
        }

        public void SetOrientation(Orientation orientation)
        {
            lock (this)
            {
                switch (orientation)
                {
                    case Orientation.Portrait:
                    case Orientation.Portrait180:
                        Width = 240;
                        Height = 320;
                        break;
                    case Orientation.Landscape:
                    case Orientation.Landscape180:
                        Width = 320;
                        Height = 240;
                        break;
                }

                SendCommand(Commands.MemoryAccessControl);
                SendData((byte)orientation);

                SetWindow(0, Width - 1, 0, Height - 1);
            }
        }

        public void ClearScreen()
        {
            lock (this)
            {
                FillScreen(Color565.Black);
            }
        }

        public void FillScreen(Color565 color)
        {
            lock (this)
            {
                DrawRect(0, Width - 1, 0, Height - 1, color);
            }
        }

        public void LoadBitmapAt(int x, int y, int width, int height)
        {
            // For the first proof of concept we have a static little bitmap
            lock (this)
            {
                // Why can't we set the window and send the complete bitmap buffer ???????
                // Rather as done now per line ?
                // Let us just try it 
                int len = bmp.Length;
                int offset = 0;
                int bmp_width = 20;
                int bmp_height = 20;

                // Call bottom, right
                int bottom = y + height;
                if (bottom > Height - 1) bottom = Height - 1;
                int right = x + bmp_width;
                if (right > Width - 1) right = Width - 1;

                // we ignore the setting coords, since we are fixed
                SetWindow(x, right, y, bottom);
                //SetWindow(101, 120, 101, 120);

                var buffer = new ushort[20];

                for (int j = 0; j < bmp_height; j++)
                {
                    // Fill buffer with line of pixel color bytes 
                    for (var i = 0; i < bmp_width; i++)
                    {
                        buffer[i] = (ushort)(bmp[offset] << 8 | bmp[offset + 1]);
                        offset += 2;
                    }
                    SendData(buffer);
                }
            }
        }

        public void DrawRect(int left, int right, int top, int bottom, Color565 color)
        {
            lock (this)
            {
                ushort _color = (ushort)color;

                SetWindow(left, right, top, bottom);

                SendCommand(Commands.MemoryWrite);
                _display.ConnectionSettings.DataBitLength = 16;
                int size = (right - left) * (bottom - top);
                if (size % 2 == 0)
                {
                    ushort[] block = new ushort[size];

                    // Fill the array with the color
                    for (int j = 0; j < block.Length; j++)
                    {
                        block[j] = _color;
                    }

                    SendData(block);
                }
                else
                {
                    // Byte mode we have to transfer per line
                    ushort[] line = new ushort[right - left];

                    // Fill line with random color
                    for (int c = 0; c < line.Length; c++)
                    {
                        line[c] = _color;
                    }

                    // Set data mode                
                    _dataCommandPin.Write(GpioPinValue.High);

                    for (int r = 0; r < size; r++)
                    {
                        Write(line);
                    }
                }
                _display.ConnectionSettings.DataBitLength = 8;
            }
        }

        public void Mosaic(int SquareSize, int Repeats)
        {
            lock (this)
            {
                // Needed vars
                ushort rndC;
                int rndX = 0;
                int rndY = 0;

                // Temp for the 20 * 20 rect
                ushort[] block = new ushort[SquareSize * SquareSize];
                ushort[] line = new ushort[SquareSize];

                // Get our random color
                Random random = new Random();

                // Do this 100 times for a start
                for (int i = 0; i < Repeats; i++)
                {
                    // Generate the random numbers for color, x and y position
                    rndC = (ushort)random.Next(UInt16.MaxValue);
                    rndX = random.Next(Width - SquareSize + 1);
                    rndY = random.Next(Height - SquareSize + 1);

                    // Fill the array with random color
                    for (int j = 0; j < block.Length; j++)
                    {
                        block[j] = rndC;
                    }

                    // We first have a fixed 20*20 block of one color to show
                    SetWindow(rndX, rndX + SquareSize - 1, rndY, rndY + SquareSize - 1);

                    SendCommand(Commands.MemoryWrite);
                    _display.ConnectionSettings.DataBitLength = 16;
                    if (SquareSize % 2 == 0)
                    {
                        SendData(block);
                    }
                    else
                    {
                        // Byte mode we have to transfer per line
                        // Fill line with random color
                        for (int c = 0; c < line.Length; c++)
                        {
                            line[c] = rndC;
                        }

                        // Set data mode                
                        _dataCommandPin.Write(GpioPinValue.High);

                        for (int r = 0; r < SquareSize; r++)
                        {
                            Write(line);
                        }
                    }
                }
                _display.ConnectionSettings.DataBitLength = 8;
            }
        }

        public void Flush(int x, int y, int width, int height, ushort[] bitmap)
        {
            if (x < 0 || y < 0 || width <= 0 || height <= 0) { return; }
            if ((x + width) > Width) width = Width;
            if ((y + height) > Height) height = Height;

            // We first have a fixed 20*20 list of ushorts called bmp2
            // So let us try that first;

            SetWindow(x, x + width - 1, y, y + height - 1);

            SendCommand(Commands.MemoryWrite);
            _display.ConnectionSettings.DataBitLength = 16;
            if (width % 2 == 0)
            {
                SendData(bitmap);
            }
            else
            {
                // Byte mode we have to transfer per line
                ushort[] line = new ushort[width];
                int offset = 0;

                // Set data mode                
                _dataCommandPin.Write(GpioPinValue.High);

                for (int i = 0; i < height; i++)
                {
                    // Fill buffer with line of pixel color bytes 
                    for (var j = 0; j < width; j++)
                    {
                        line[j] = bitmap[offset++];
                    }
                    Write(line);
                }
            }
            _display.ConnectionSettings.DataBitLength = 8;
        }

        public void LoadBitmap3(int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || width <= 0 || height <= 0) { return; }
            if ((x + width) > Width) width = Width;
            if ((y + height) > Height) height = Height;

            // We first have a fixed 20*20 list of ushorts called bmp2
            // So let us try that first;

            SetWindow(x, x + width - 1, y, y + height - 1);

            SendCommand(Commands.MemoryWrite);
            _display.ConnectionSettings.DataBitLength = 16;
            if (width % 2 == 0)
            {
                SendData(bmp3);
            }
            else
            {
                // Byte mode we have to transfer per line
                ushort[] line = new ushort[width];
                int offset = 0;

                // Set data mode                
                _dataCommandPin.Write(GpioPinValue.High);

                for (int i = 0; i < height; i++)
                {
                    // Fill buffer with line of pixel color bytes 
                    for (var j = 0; j < width; j++)
                    {
                        line[j] = bmp3[offset++];
                    }
                    Write(line);
                }
            }
            _display.ConnectionSettings.DataBitLength = 8;
        }

        public void LoadBitmap(int left, int right, int top, int bottom)
        {
            // For the first proof of concept we have a static little bitmap
            lock (this)
            {
                // Why can't we set the window and send the complete bitmap buffer ???????
                // Rather as done now per line ?

                int len = bmp.Length;
                int offset = 0;

                // we ignore the setting coords, since we are fixed
                SetWindow(left, right, top, bottom);
                //SetWindow(101, 120, 101, 120);

                var buffer = new ushort[20];

                for (int y = 0; y < 20; y++)
                {
                    // Fill buffer with line of pixel color bytes 
                    for (var i = 0; i < 20; i++)
                    {
                        buffer[i] = (ushort)(bmp[offset] << 8 | bmp[offset + 1]);
                        offset += 2;
                    }
                    SendData(buffer);
                }
            }
        }

        public void DrawCircle(UInt16 x0, UInt16 y0, UInt16 radius, Color565 color)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                        DrawPixel((UInt16)(x + x0), (UInt16)(y + y0), color);

        }

        public void DrawCircle(UInt16 x0, UInt16 y0, UInt16 radius, Color888 color)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                        DrawPixel((UInt16)(x + x0), (UInt16)(y + y0), color);

        }

        public void DrawPixel(UInt16 x, UInt16 y, Color565 color)
        {
            lock (this)
            {
                SetWindow(x, x, y, y);
                SendData((ushort)color);
            }
        }

        public void DrawPixel(UInt16 x, UInt16 y, Color888 color)
        {
            lock (this)
            {
                SetWindow(x, x, y, y);
                SendData((ushort)ColorConversion.ToRgb565(color));
            }
        }

        public void SetPixel(int x, int y, Color565 color)
        {
            lock (this)
            {
                SetWindow(x, x, y, y);
                SendData((ushort)color);
            }
        }

        #endregion

        #region None public methods

        protected virtual void InitDisplay(Orientation orientation)
        {
            lock (this)
            {
                // Reset first
                _resetPin.Write(GpioPinValue.Low);
                // Give the display some time
                // According to manuals > 10 ms
                Thread.Sleep(10);
                // Take out of hard reset
                _resetPin.Write(GpioPinValue.High);
                // Perform a software reset
                SendCommand(Commands.SoftwareReset);
                // Give display time 
                Thread.Sleep(10);
                // Set the display to off for the moment
                SendCommand(Commands.DisplayOff);

                // Set the orientation
                SetOrientation(orientation);
                //SendCommand(Commands.MemoryAccessControl);
                //SendData((byte)Orientation.Portrait);

                // Set to 16-bits pro pixels
                SendCommand(Commands.PixelFormatSet);
                SendData(0x55);//16-bits per pixel

                SendCommand(Commands.FrameControlNormal);
                SendData(0x00, 0x1B);

                SendCommand(Commands.GammaSet);
                SendData(0x01);

                // Width of the screen
                SendCommand(Commands.ColumnAddressSet);
                SendData(0x00, 0x00, 0x00, 0xEF);

                // Height of the screen
                SendCommand(Commands.PageAddressSet);
                SendData(0x00, 0x00, 0x01, 0x3F);

                SendCommand(Commands.EntryModeSet);
                SendData(0x07);

                SendCommand(Commands.DisplayFunctionControl);
                SendData(0x0A, 0x82, 0x27, 0x00);

                SendCommand(Commands.SleepOut);
                Thread.Sleep(120);

                SendCommand(Commands.DisplayOn);
                Thread.Sleep(100);

                SendCommand(Commands.MemoryWrite);
            }
        }

        protected virtual void SetWindow(int left, int right, int top, int bottom)
        {
            lock (this)
            {
                SendCommand(Commands.ColumnAddressSet);
                SendData((byte)((left >> 8) & 0xFF),
                         (byte)(left & 0xFF),
                         (byte)((right >> 8) & 0xFF),
                         (byte)(right & 0xFF));
                SendCommand(Commands.PageAddressSet);
                SendData((byte)((top >> 8) & 0xFF),
                         (byte)(top & 0xFF),
                         (byte)((bottom >> 8) & 0xFF),
                         (byte)(bottom & 0xFF));
                SendCommand(Commands.MemoryWrite);
            }
        }

        protected virtual void SendCommand(Commands command)
        {
            // Go into command mode
            _dataCommandPin.Write(GpioPinValue.Low);
            Write(new[] { (byte)command });
        }

        protected virtual void SendData(params byte[] data)
        {
            _dataCommandPin.Write(GpioPinValue.High);
            Write(data);
        }

        protected virtual void SendData(params ushort[] data)
        {
            _dataCommandPin.Write(GpioPinValue.High);
            Write(data);
        }

        protected virtual void Write(byte[] data)
        {
            _display.Write(data);
        }

        protected virtual void Write(ushort[] data)
        {
            _display.Write(data);
        }

        #endregion

        #region bitmaps

        private byte[] bmp = new byte[] {
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8,
                    0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8, 0x07, 0xF8
                };

        private ushort[] bmp2 = new ushort[] {
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8,
                    0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8, 0x07F8
                };

        private ushort[] bmp3 = new ushort[] {
            0x0000, 0x0000
        };

        #endregion

    }
}
