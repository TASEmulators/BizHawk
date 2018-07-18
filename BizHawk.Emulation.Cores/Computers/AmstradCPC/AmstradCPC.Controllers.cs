using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * Controllers *
    /// </summary>
    public partial class AmstradCPC
    {
        /// <summary>
        /// The one CPCHawk ControllerDefinition
        /// </summary>
        public ControllerDefinition AmstradCPCControllerDefinition
        {
            get
            {
                ControllerDefinition definition = new ControllerDefinition();
                definition.Name = "AmstradCPC Controller";

                // joysticks
                List<string> joys1 = new List<string>
                {
                    // P1 Joystick
                    "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Fire1", "P1 Fire2", "P1 Fire3"
                };

                foreach (var s in joys1)
                {
                    definition.BoolButtons.Add(s);
                    definition.CategoryLabels[s] = "J1";
                }

                List<string> joys2 = new List<string>
                {
                    // P2 Joystick
                    "P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Fire",
                };

                foreach (var s in joys2)
                {
                    definition.BoolButtons.Add(s);
                    definition.CategoryLabels[s] = "J2";
                }

                // keyboard
                List<string> keys = new List<string>
                {
                    /// http://www.cpcwiki.eu/index.php/Programming:Keyboard_scanning
                    /// http://www.cpcwiki.eu/index.php/File:Grimware_cpc464_version3_case_top.jpg
        
                    // Keyboard - row 1
                    "Key ESC", "Key 1", "Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Dash", "Key Hat", "Key CLR", "Key DEL",
                    // Keyboard - row 2
                    "Key TAB", "Key Q", "Key W", "Key E", "Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P", "Key @", "Key LeftBracket", "Key RETURN", 
                    // Keyboard - row 3
                    "Key CAPSLOCK", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J", "Key K", "Key L", "Key Colon", "Key SemiColon", "Key RightBracket", 
                    // Keyboard - row 4
                    "Key SHIFT", "Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Comma", "Key Period", "Key ForwardSlash", "Key BackSlash", 
                    // Keyboard - row 5
                    "Key SPACE", "Key CONTROL", 
                    // Keyboard - Cursor
                    "Key CURUP", "Key CURDOWN", "Key CURLEFT", "Key CURRIGHT", "Key COPY", 
                    // Keyboard - Numpad
                    "Key NUM0", "Key NUM1", "Key NUM2", "Key NUM3", "Key NUM4", "Key NUM5", "Key NUM6", "Key NUM7", "Key NUM8", "Key NUM9", "Key NUMPERIOD", "KEY ENTER"
                };

                foreach (var s in keys)
                {
                    definition.BoolButtons.Add(s);
                    definition.CategoryLabels[s] = "Keyboard";
                }

                // Power functions
                List<string> power = new List<string>
                {
                    // Power functions
                    "Reset", "Power"
                };

                foreach (var s in power)
                {
                    definition.BoolButtons.Add(s);
                    definition.CategoryLabels[s] = "Power";
                }

                // Datacorder (tape device)
                List<string> tape = new List<string>
                {
                    // Tape functions
                    "Play Tape", "Stop Tape", "RTZ Tape", "Record Tape", "Insert Next Tape",
                    "Insert Previous Tape", "Next Tape Block", "Prev Tape Block", "Get Tape Status"
                };

                foreach (var s in tape)
                {
                    definition.BoolButtons.Add(s);
                    definition.CategoryLabels[s] = "Datacorder";
                }

                // Datacorder (tape device)
                List<string> disk = new List<string>
                {
                    // Tape functions
                    "Insert Next Disk", "Insert Previous Disk", /*"Eject Current Disk",*/ "Get Disk Status"
                };

                foreach (var s in disk)
                {
                    definition.BoolButtons.Add(s);
                    definition.CategoryLabels[s] = "Amstrad Disk Drive";
                }

                return definition;
            }
        }
    }

    /// <summary>
    /// The possible joystick types
    /// </summary>
    public enum JoystickType
    {
        NULL,
        Joystick1,
        Joystick2
    }
}
