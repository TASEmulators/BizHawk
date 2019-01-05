using BizHawk.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// The 48k keyboard device
    /// </summary>
    public class StandardKeyboard : IKeyboard
    {
        public CPCBase _machine { get; set; }

        private int _currentLine;
        public int CurrentLine
        {
            get { return _currentLine; }
            set
            {
                // bits 0-3 contain the line
                var line = value & 0x0f;

                if (line > 0)
                {

                }

                _currentLine = line;
            }
        }

        private bool[] _keyStatus;
        public bool[] KeyStatus
        {
            get { return _keyStatus; }
            set { _keyStatus = value; }
        }

        private string[] _keyboardMatrix;
        public string[] KeyboardMatrix
        {
            get { return _keyboardMatrix; }
            set { _keyboardMatrix = value; }
        }

        private string[] _nonMatrixKeys;
        public string[] NonMatrixKeys
        {
            get { return _nonMatrixKeys; }
            set { _nonMatrixKeys = value; }
        }

        public StandardKeyboard(CPCBase machine)
        {
            _machine = machine;
            //_machine.AYDevice.PortA_IN_CallBack = INCallback;
            //_machine.AYDevice.PortA_OUT_CallBack = OUTCallback;

            // scancode rows, ascending (Bit0 - Bit7)
            KeyboardMatrix = new string[]
            {
                // 0x40
                "Key CURUP", "Key CURRIGHT", "Key CURDOWN", "Key NUM9", "Key NUM6", "Key NUM3", "Key ENTER", "Key NUMPERIOD",
                // 0x41
                "Key CURLEFT", "Key COPY", "Key NUM7", "Key NUM8", "Key NUM5", "Key NUM1", "Key NUM2", "Key NUM0",
                // 0x42
                "Key CLR", "Key LeftBracket", "Key RETURN", "Key RightBracket", "Key NUM4", "Key SHIFT", "Key BackSlash", "Key CONTROL",
                // 0x43
                "Key Hat", "Key Dash", "Key @", "Key P", "Key SemiColon", "Key Colon", "Key ForwardSlash", "Key Period",
                // 0x44
                "Key 0", "Key 9", "Key O", "Key I", "Key L", "Key K", "Key M", "Key Comma",
                // 0x45
                "Key 8", "Key 7", "Key U", "Key Y", "Key H", "Key J", "Key N", "Key SPACE",
                // 0x46
                "Key 6", "Key 5", "Key R", "Key T", "Key G", "Key F", "Key B", "Key V",
                // 0x47
                "Key 4", "Key 3", "Key E", "Key W", "Key S", "Key D", "Key C", "Key X",
                // 0x48
                "Key 1", "Key 2", "Key ESC", "Key Q", "Key TAB", "Key A", "Key CAPSLOCK", "Key Z",
                // 0x49
                "P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Fire1", "P1 Fire2", "P1 Fire3", "Key DEL",

            };

            // keystatus array to match the matrix
            KeyStatus = new bool[8 * 10];
            
            // nonmatrix keys (anything that hasnt already been taken)
            var nonMatrix = new List<string>();
            
            foreach (var key in _machine.CPC.AmstradCPCControllerDefinition.BoolButtons)
            {
                if (!KeyboardMatrix.Any(s => s == key))
                    nonMatrix.Add(key);
            }
           
            NonMatrixKeys = nonMatrix.ToArray();            
        }

        /// <summary>
        /// Reads the currently selected line
        /// </summary>
        /// <returns></returns>
        public byte ReadCurrentLine()
        {
            var lin = _currentLine; // - 0x40;
            var pos = lin * 8;
            var l = KeyStatus.Skip(pos).Take(8).ToArray();
            BitArray bi = new BitArray(l);
            byte[] bytes = new byte[1];
            bi.CopyTo(bytes, 0);
            byte inv = (byte)(~bytes[0]);
            return inv;
        }

        /// <summary>
        /// Returns the index of the key within the matrix
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetKeyIndexFromMatrix(string key)
        {
            int index = Array.IndexOf(KeyboardMatrix, key);
            return index;
        }

        /// <summary>
        /// Sets key status
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isPressed"></param>
        public void SetKeyStatus(string key, bool isPressed)
        {
            int index = GetKeyIndexFromMatrix(key);
            KeyStatus[index] = isPressed;
        }

        /// <summary>
        /// Gets a key's status
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool GetKeyStatus(string key)
        {
            int index = GetKeyIndexFromMatrix(key);
            return KeyStatus[index];
        }


        public void SyncState(Serializer ser)
        {
            ser.BeginSection("Keyboard");
            ser.Sync("currentLine", ref _currentLine);
            ser.Sync("keyStatus", ref _keyStatus, false);
            ser.EndSection();
        }
    }
}
