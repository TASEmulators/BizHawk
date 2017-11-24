
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Identified AD or DA converter types
    /// </summary>
    public enum TzxAdOrDaConverterType : byte
    {
        HarleySystemsAdc8P2 = 0x00,
        BlackboardElectronics = 0x01
    }

    /// <summary>
    /// Identified computer types
    /// </summary>
    public enum TzxComputerType : byte
    {
        ZxSpectrum16 = 0x00,
        ZxSpectrum48OrPlus = 0x01,
        ZxSpectrum48Issue1 = 0x02,
        ZxSpectrum128 = 0x03,
        ZxSpectrum128P2 = 0x04,
        ZxSpectrum128P2AOr3 = 0x05,
        Tc2048 = 0x06,
        Ts2068 = 0x07,
        Pentagon128 = 0x08,
        SamCoupe = 0x09,
        DidaktikM = 0x0A,
        DidaktikGama = 0x0B,
        Zx80 = 0x0C,
        Zx81 = 0x0D,
        ZxSpectrum128Spanish = 0x0E,
        ZxSpectrumArabic = 0x0F,
        Tk90X = 0x10,
        Tk95 = 0x11,
        Byte = 0x12,
        Elwro800D3 = 0x13,
        ZsScorpion256 = 0x14,
        AmstradCpc464 = 0x15,
        AmstradCpc664 = 0x16,
        AmstradCpc6128 = 0x17,
        AmstradCpc464P = 0x18,
        AmstradCpc6128P = 0x19,
        JupiterAce = 0x1A,
        Enterprise = 0x1B,
        Commodore64 = 0x1C,
        Commodore128 = 0x1D,
        InvesSpectrumP = 0x1E,
        Profi = 0x1F,
        GrandRomMax = 0x20,
        Kay1024 = 0x21,
        IceFelixHc91 = 0x22,
        IceFelixHc2000 = 0x23,
        AmaterskeRadioMistrum = 0x24,
        Quorum128 = 0x25,
        MicroArtAtm = 0x26,
        MicroArtAtmTurbo2 = 0x27,
        Chrome = 0x28,
        ZxBadaloc = 0x29,
        Ts1500 = 0x2A,
        Lambda = 0x2B,
        Tk65 = 0x2C,
        Zx97 = 0x2D
    }

    /// <summary>
    /// Identified digitizer types
    /// </summary>
    public enum TzxDigitizerType : byte
    {
        RdDigitalTracer = 0x00,
        DkTronicsLightPen = 0x01,
        MicrographPad = 0x02,
        RomnticRobotVideoface = 0x03
    }

    /// <summary>
    /// Identified EPROM programmer types
    /// </summary>
    public enum TzxEpromProgrammerType : byte
    {
        OrmeElectronics = 0x00
    }

    /// <summary>
    /// Identified external storage types
    /// </summary>
    public enum TzxExternalStorageType : byte
    {
        ZxMicroDrive = 0x00,
        OpusDiscovery = 0x01,
        MgtDisciple = 0x02,
        MgtPlusD = 0x03,
        RobotronicsWafaDrive = 0x04,
        TrDosBetaDisk = 0x05,
        ByteDrive = 0x06,
        Watsford = 0x07,
        Fiz = 0x08,
        Radofin = 0x09,
        DidaktikDiskDrive = 0x0A,
        BsDos = 0x0B,
        ZxSpectrumP3DiskDrive = 0x0C,
        JloDiskInterface = 0x0D,
        TimexFdd3000 = 0x0E,
        ZebraDiskDrive = 0x0F,
        RamexMillenia = 0x10,
        Larken = 0x11,
        KempstonDiskInterface = 0x12,
        Sandy = 0x13,
        ZxSpectrumP3EHardDisk = 0x14,
        ZxAtaSp = 0x15,
        DivIde = 0x16,
        ZxCf = 0x17
    }

    /// <summary>
    /// Identified graphics types
    /// </summary>
    public enum TzxGraphicsType : byte
    {
        WrxHiRes = 0x00,
        G007 = 0x01,
        Memotech = 0x02,
        LambdaColour = 0x03
    }

    /// <summary>
    /// Represents the hardware types that can be defined
    /// </summary>
    public enum TzxHwType : byte
    {
        Computer = 0x00,
        ExternalStorage = 0x01,
        RomOrRamTypeAddOn = 0x02,
        SoundDevice = 0x03,
        JoyStick = 0x04,
        Mouse = 0x05,
        OtherController = 0x06,
        SerialPort = 0x07,
        ParallelPort = 0x08,
        Printer = 0x09,
        Modem = 0x0A,
        Digitizer = 0x0B,
        NetworkAdapter = 0x0C,
        Keyboard = 0x0D,
        AdOrDaConverter = 0x0E,
        EpromProgrammer = 0x0F,
        Graphics = 0x10
    }

    /// <summary>
    /// Identified joystick types
    /// </summary>
    public enum TzxJoystickType
    {
        Kempston = 0x00,
        ProtekCursor = 0x01,
        Sinclair2Left = 0x02,
        Sinclair1Right = 0x03,
        Fuller = 0x04
    }

    /// <summary>
    /// Identified keyboard and keypad types
    /// </summary>
    public enum TzxKeyboardType : byte
    {
        KeypadForZxSpectrum128K = 0x00
    }

    /// <summary>
    /// Identified modem types
    /// </summary>
    public enum TzxModemTypes : byte
    {
        PrismVtx5000 = 0x00,
        Westridge2050 = 0x01
    }

    /// <summary>
    /// Identified mouse types
    /// </summary>
    public enum TzxMouseType : byte
    {
        AmxMouse = 0x00,
        KempstonMouse = 0x01
    }

    /// <summary>
    /// Identified network adapter types
    /// </summary>
    public enum TzxNetworkAdapterType : byte
    {
        ZxInterface1 = 0x00
    }

    /// <summary>
    /// Identified other controller types
    /// </summary>
    public enum TzxOtherControllerType : byte
    {
        Trisckstick = 0x00,
        ZxLightGun = 0x01,
        ZebraGraphicTablet = 0x02,
        DefnederLightGun = 0x03
    }

    /// <summary>
    /// Identified parallel port types
    /// </summary>
    public enum TzxParallelPortType : byte
    {
        KempstonS = 0x00,
        KempstonE = 0x01,
        ZxSpectrum3P = 0x02,
        Tasman = 0x03,
        DkTronics = 0x04,
        Hilderbay = 0x05,
        InesPrinterface = 0x06,
        ZxLprintInterface3 = 0x07,
        MultiPrint = 0x08,
        OpusDiscovery = 0x09,
        Standard8255 = 0x0A
    }

    /// <summary>
    /// Identified printer types
    /// </summary>
    public enum TzxPrinterType : byte
    {
        ZxPrinter = 0x00,
        GenericPrinter = 0x01,
        EpsonCompatible = 0x02
    }

    /// <summary>
    /// Identifier ROM or RAM add-on types
    /// </summary>
    public enum TzxRomRamAddOnType : byte
    {
        SamRam = 0x00,
        MultifaceOne = 0x01,
        Multiface128K = 0x02,
        MultifaceP3 = 0x03,
        MultiPrint = 0x04,
        Mb02 = 0x05,
        SoftRom = 0x06,
        Ram1K = 0x07,
        Ram16K = 0x08,
        Ram48K = 0x09,
        Mem8To16KUsed = 0x0A
    }

    /// <summary>
    /// Identified serial port types
    /// </summary>
    public enum TzxSerialPortType : byte
    {
        ZxInterface1 = 0x00,
        ZxSpectrum128 = 0x01
    }

    /// <summary>
    /// Identified sound device types
    /// </summary>
    public enum TzxSoundDeviceType : byte
    {
        ClassicAy = 0x00,
        FullerBox = 0x01,
        CurrahMicroSpeech = 0x02,
        SpectDrum = 0x03,
        MelodikAyAcbStereo = 0x04,
        AyAbcStereo = 0x05,
        RamMusinMachine = 0x06,
        Covox = 0x07,
        GeneralSound = 0x08,
        IntecEdiB8001 = 0x09,
        ZonXAy = 0x0A,
        QuickSilvaAy = 0x0B,
        JupiterAce = 0x0C
    }
}
