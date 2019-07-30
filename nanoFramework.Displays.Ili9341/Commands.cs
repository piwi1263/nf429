namespace nanoFramework.Displays.Ili9341
{
	public enum Commands : byte
	{
        /// <summary>
        /// 
        /// </summary>
        NoOp = 0x00,

        /// <summary>
        /// 
        /// </summary>
        SoftwareReset = 0x01,

        EnterSleepMode = 0x10,
        SleepOut = 0x11,
        PartialModeOn = 0x12,
        NormalDisplayModeOn = 0x13,
        DisplayInversionOff = 0x20,
        DisplayInversionOn = 0x21,
        GammaSet = 0x26,

        /// <summary>
        /// 
        /// </summary>
        DisplayOff = 0x28,

        /// <summary>
        /// 
        /// </summary>
        DisplayOn = 0x29,

        ColumnAddressSet = 0x2A,
        PageAddressSet = 0x2B,
        MemoryWrite = 0x2C,
        ColorSet = 0x2D,
        MemoryRead = 0x2E,
        ParialArea = 0x30,
        VerticalScrollingDefinition = 0x33,
        TearingEffectLineOff = 0x34,
        TearingEffectLineOn = 0x35,
        MemoryAccessControl = 0x36,
        VerticalScrollingStartAddress = 0x37,
        IdleModeOff = 0x38,
        IdleModeOn = 0x39,
        PixelFormatSet = 0x3A,
        WriteMemoryContinue = 0x3C,
        ReadMemoryContinue = 0x3E,
        SetTearScanLine = 0x44,
        GetScanLine = 0x45,
        WriteDisplayBrightness = 0x51,
        ReadDisplayBrightness = 0x52,
        WriteCtrlDisplay = 0x53,
        ReadCtrlDisplay = 0x54,
        WriteContentAdaptiveBrightnessControl = 0x55,
        ReadContentAdaptiveBrightnessControl = 0x56,
        WriteCabcMinimumBrightness = 0x5E,
        ReadCabcMinimumBrightness = 0x5F,

        /// <summary>
        /// Manufacturer only
        /// </summary>
        RgbInterfaceSignalControl = 0xB0,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        FrameControlNormal = 0xB1,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        FrameControlIdle = 0xB2,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        FrameControlPartial = 0xB3,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        DisplayInversionControl = 0xB4,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BlankingPorchControl = 0xB5,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        DisplayFunctionControl = 0xB6,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        EntryModeSet = 0xB7,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl1 = 0xB8,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl2 = 0xB9,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl3 = 0xBA,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl4 = 0xBB,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl5 = 0xBC,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl6 = 0xBD,// BacklightControl6 did not exist in the Ilitek documentation, BD is assumed
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl7 = 0xBE,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        BacklightControl8 = 0xBF,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        PowerControl1 = 0xC0,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        PowerControl2 = 0xC1,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        VCOMControl1 = 0xC5,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        VCOMControl2 = 0xC7,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        NVMemoryWrite = 0xD0,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        NVMemoryProtectionKey = 0xD1,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        NVMemoryStatusRead = 0xD2,
        /// <summary>
        /// Manufacturer only
        /// </summary>
        ReadId4 = 0xD3,
        ReadId1 = 0xDA,
        ReadId2 = 0xDB,
        ReadId3 = 0xDC,
        PositiveGammaCorrection = 0xE0,
        NegativeGammaCorrection = 0xE1,
        DigitalGammaControl1 = 0xE2,
        DigitalGammaControl2 = 0xE3,
        InterfaceControl = 0xF6
    }

    public enum Orientation : byte
    {
        Portrait = 0x48,
        Landscape = 0xE8,
        Portrait180 = 0x88,
        Landscape180 = 0x28
    }
}
