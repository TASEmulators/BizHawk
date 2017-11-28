
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZXSpectrum
    {      

        /// <summary>
        /// The standard 48K Spectrum keyboard
        /// https://upload.wikimedia.org/wikipedia/commons/thumb/3/33/ZXSpectrum48k.jpg/1200px-ZXSpectrum48k.jpg
        /// </summary>
        private static readonly ControllerDefinition ZXSpectrumControllerDefinition48 = new ControllerDefinition
        {
            Name = "ZXSpectrum Controller 48K",
            BoolButtons =
            {
                // Joystick interface (not yet emulated) - Could be Kempston/Cursor/Protek
                "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
                // Keyboard - row 1    
                "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0",
                // Keyboard - row 2
                "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P",
                // Keyboard - row 3
                "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Return",
                // Keyboard - row 4
                "Key Caps Shift", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Sym Shift", "Key Space",
                // Tape functions
                "Play Tape", "Stop Tape", "RTZ Tape", "Record Tape"
            }
        };

        /// <summary>
        /// The newer spectrum keyboard (models 48k+, 128k)
        /// https://upload.wikimedia.org/wikipedia/commons/c/ca/ZX_Spectrum%2B.jpg
        /// </summary>
        private static readonly ControllerDefinition ZXSpectrumControllerDefinition128 = new ControllerDefinition
        {
            Name = "ZXSpectrum Controller 48KPlus",
            BoolButtons =
            {
                // Joystick interface (not yet emulated) - Could be Kempston/Cursor/Protek
                "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
                // Keyboard - row 1    
                "Key True Video", "Key Inv Video", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Break",
                // Keyboard - row 2
                "Key Delete", "Key Graph", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P",
                // Keyboard - row 3
                "Key Extend Mode", "Key Edit", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Return",
                // Keyboard - row 4
                "Key Caps Shift", "Key Caps Lock", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Period",
                // Keyboard - row 5
                "Key Symbol Shift", "Key Semi-Colon", "Key Inverted-Comma", "Key Left Cursor", "Key Right Cursor", "Key Space", "Key Up Cursor", "Key Down Cursor", "Key Comma", "Key Symbol Shift",
                // Tape functions
                "Play Tape", "Stop Tape", "RTZ Tape", "Record Tape"
            }
        };

        /// <summary>
        /// The amstrad models - same as the previous keyboard layout but with built in ZX Interface 2 joystick ports
        /// https://upload.wikimedia.org/wikipedia/commons/c/ca/ZX_Spectrum%2B.jpg
        /// </summary>
        private static readonly ControllerDefinition ZXSpectrumControllerDefinitionPlus = new ControllerDefinition
        {
            Name = "ZXSpectrum Controller 48KPlus",
            BoolButtons =
            {
                // Joystick interface (not yet emulated)
                "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
                // Keyboard - row 1    
                "Key True Video", "Key Inv Video", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Break",
                // Keyboard - row 2
                "Key Delete", "Key Graph", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P",
                // Keyboard - row 3
                "Key Extend Mode", "Key Edit", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Return",
                // Keyboard - row 4
                "Key Caps Shift", "Key Caps Lock", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Period",
                // Keyboard - row 5
                "Key Symbol Shift", "Key Semi-Colon", "Key Inverted-Comma", "Key Left Cursor", "Key Right Cursor", "Key Space", "Key Up Cursor", "Key Down Cursor", "Key Comma", "Key Symbol Shift",
                // Tape functions
                "Play Tape", "Stop Tape", "RTZ Tape", "Record Tape"
            }
        };
    }
}
