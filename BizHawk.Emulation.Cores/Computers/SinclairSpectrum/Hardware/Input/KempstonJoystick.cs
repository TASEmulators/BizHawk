using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public class KempstonJoystick : IJoystick
    {
        private int _joyLine;
        private SpectrumBase _machine;

        #region Construction

        public KempstonJoystick(SpectrumBase machine, int playerNumber)
        {
            _machine = machine;
            _joyLine = 0;
            _playerNumber = playerNumber;

            ButtonCollection = new List<string>
            {
                "P" + _playerNumber + " Right",
                "P" + _playerNumber + " Left",
                "P" + _playerNumber + " Down",
                "P" + _playerNumber + " Up",
                "P" + _playerNumber + " Button",
            }.ToArray();
        }

        #endregion

        #region IJoystick

        public JoystickType JoyType => JoystickType.Kempston;

        public string[] ButtonCollection { get; set; }

        private int _playerNumber;
        public int PlayerNumber
        {
            get { return _playerNumber; }
            set { _playerNumber = value; }
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
        
        #endregion

        /// <summary>
        /// Active bits high
        /// 0 0 0 F U D L R
        /// </summary>
        public int JoyLine
        {
            get { return _joyLine; }
            set { _joyLine = value; }
        }

        /// <summary>
        /// Gets the bit position of a particular joystick binding from the matrix
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetBitPos(string key)
        {
            int index = Array.IndexOf(ButtonCollection, key);
            return index;
        }


        /*
       public readonly string[] _bitPos = new string[]
       {
           "P1 Right",
           "P1 Left",
           "P1 Down",
           "P1 Up",
           "P1 Button"
       };
       */
    }
}
