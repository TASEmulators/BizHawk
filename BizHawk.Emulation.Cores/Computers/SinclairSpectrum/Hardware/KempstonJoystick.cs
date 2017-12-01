using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class KempstonJoystick
    {
        private int _joyLine;
        private SpectrumBase _machine;

        public readonly string[] _bitPos = new string[]
        {
            "P1 Right",
            "P1 Left",
            "P1 Down",
            "P1 Up",
            "P1 Button"
        };

        /*
                Active bits high
                0 0 0 F U D L R
        */
        public int JoyLine
        {
            get { return _joyLine; }
            set { _joyLine = value; }
        }

        public KempstonJoystick(SpectrumBase machine)
        {
            _machine = machine;
            _joyLine = 0;
        }

        /// <summary>
        /// Sets the joystick line based on key pressed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isPressed"></param>
        public void SetJoyInput(string key, bool isPressed)
        {
            var pos = GetBitPos(key);
            if (isPressed)
                _joyLine |= (1 << pos);
            else
                _joyLine &= ~(1 << pos);
        }

        /// <summary>
        /// Gets the state of a particular joystick binding
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool GetJoyInput(string key)
        {
            var pos = GetBitPos(key);
            return (_joyLine & (1 << pos)) != 0;
        }

        /// <summary>
        /// Gets the bit position of a particular joystick binding from the matrix
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetBitPos(string key)
        {
            int index = Array.IndexOf(_bitPos, key);
            return index;
        }
    }
}
