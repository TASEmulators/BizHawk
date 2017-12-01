using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The 48k keyboard device
    /// </summary>
    public class Keyboard48 : IKeyboard
    {
        public SpectrumBase _machine { get; set; }
        private byte[] LineStatus;
        private string[] _keyboardMatrix;
        private int[] _keyLine;
        private bool _isIssue2Keyboard;
        private string[] _nonMatrixKeys;

        public bool IsIssue2Keyboard
        {
            get { return _isIssue2Keyboard; }
            set { _isIssue2Keyboard = value; }
        }

        public int[] KeyLine
        {
            get { return _keyLine; }
            set { _keyLine = value; }
        }

        public string[] KeyboardMatrix
        {
            get { return _keyboardMatrix; }
            set { _keyboardMatrix = value; }
        }

        public string[] NonMatrixKeys
        {
            get { return _nonMatrixKeys; }
            set { _nonMatrixKeys = value; }
        }

        public Keyboard48(SpectrumBase machine)
        {
            _machine = machine;

            KeyboardMatrix = new string[]
            {
				// 0xfefe	-	0 - 4
				"Key Caps Shift", "Key Z", "Key X", "Key C", "Key V",
				// 0xfdfe	-	5 - 9
				"Key A", "Key S", "Key D", "Key F", "Key G",
				// 0xfbfe	-	10 - 14
				"Key Q", "Key W", "Key E", "Key R", "Key T",
				// 0xf7fe	-	15 - 19
				"Key 1", "Key 2", "Key 3", "Key 4", "Key 5",
				// 0xeffe	-	20 - 24
				"Key 0", "Key 9", "Key 8", "Key 7", "Key 6",
				// 0xdffe	-	25 - 29
				"Key P", "Key O", "Key I", "Key U", "Key Y",
				// 0xbffe	-	30 - 34
				"Key Return", "Key L", "Key K", "Key J", "Key H",
				// 0x7ffe	-	35 - 39
				"Key Space", "Key Symbol Shift", "Key M", "Key N", "Key B"
            };

            var nonMatrix = new List<string>();
            foreach (var key in ZXSpectrum.ZXSpectrumControllerDefinition.BoolButtons)
            {
                if (!KeyboardMatrix.Any(s => s == key))
                    nonMatrix.Add(key);
            }
            NonMatrixKeys = nonMatrix.ToArray();

            LineStatus = new byte[8];
            _keyLine = new int[] { 255, 255, 255, 255, 255, 255, 255, 255 };
            IsIssue2Keyboard = true;
        }

        public void SetKeyStatus(string key, bool isPressed)
        {
            int k = GetByteFromKeyMatrix(key);
            
            if (k != 255)
            {
                var lineIndex = k / 5;
                var lineMask = 1 << k % 5;

                _keyLine[lineIndex] = isPressed
                    ? (byte)(_keyLine[lineIndex] & ~lineMask)
                    : (byte)(_keyLine[lineIndex] | lineMask);
            }
            
            /*
            if (isPressed)
            {
                switch (k)
                {
                    // 0xfefe	-	0 - 4
                    case 0: _keyLine[0] = (_keyLine[0] & ~(0x1)); break;
                    case 1: _keyLine[0] = (_keyLine[0] & ~(0x02)); break;
                    case 2: _keyLine[0] = (_keyLine[0] & ~(0x04)); break;
                    case 3: _keyLine[0] = (_keyLine[0] & ~(0x08)); break;
                    case 4: _keyLine[0] = (_keyLine[0] & ~(0x10)); break;
                    // 0xfdfe	-	5 - 9
                    case 5: _keyLine[1] = (_keyLine[1] & ~(0x1)); break;
                    case 6: _keyLine[1] = (_keyLine[1] & ~(0x02)); break;
                    case 7: _keyLine[1] = (_keyLine[1] & ~(0x04)); break;
                    case 8: _keyLine[1] = (_keyLine[1] & ~(0x08)); break;
                    case 9: _keyLine[1] = (_keyLine[1] & ~(0x10)); break;
                    // 0xfbfe	-	10 - 14
                    case 10: _keyLine[2] = (_keyLine[2] & ~(0x1)); break;
                    case 11: _keyLine[2] = (_keyLine[2] & ~(0x02)); break;
                    case 12: _keyLine[2] = (_keyLine[2] & ~(0x04)); break;
                    case 13: _keyLine[2] = (_keyLine[2] & ~(0x08)); break;
                    case 14: _keyLine[2] = (_keyLine[2] & ~(0x10)); break;
                    // 0xf7fe	-	15 - 19
                    case 15: _keyLine[3] = (_keyLine[3] & ~(0x1)); break;
                    case 16: _keyLine[3] = (_keyLine[3] & ~(0x02)); break;
                    case 17: _keyLine[3] = (_keyLine[3] & ~(0x04)); break;
                    case 18: _keyLine[3] = (_keyLine[3] & ~(0x08)); break;
                    case 19: _keyLine[3] = (_keyLine[3] & ~(0x10)); break;
                    // 0xeffe	-	20 - 24
                    case 20: _keyLine[4] = (_keyLine[4] & ~(0x1)); break;
                    case 21: _keyLine[4] = (_keyLine[4] & ~(0x02)); break;
                    case 22: _keyLine[4] = (_keyLine[4] & ~(0x04)); break;
                    case 23: _keyLine[4] = (_keyLine[4] & ~(0x08)); break;
                    case 24: _keyLine[4] = (_keyLine[4] & ~(0x10)); break;
                    // 0xdffe	-	25 - 29
                    case 25: _keyLine[5] = (_keyLine[5] & ~(0x1)); break;
                    case 26: _keyLine[5] = (_keyLine[5] & ~(0x02)); break;
                    case 27: _keyLine[5] = (_keyLine[5] & ~(0x04)); break;
                    case 28: _keyLine[5] = (_keyLine[5] & ~(0x08)); break;
                    case 29: _keyLine[5] = (_keyLine[5] & ~(0x10)); break;
                    // 0xbffe	-	30 - 34
                    case 30: _keyLine[6] = (_keyLine[6] & ~(0x1)); break;
                    case 31: _keyLine[6] = (_keyLine[6] & ~(0x02)); break;
                    case 32: _keyLine[6] = (_keyLine[6] & ~(0x04)); break;
                    case 33: _keyLine[6] = (_keyLine[6] & ~(0x08)); break;
                    case 34: _keyLine[6] = (_keyLine[6] & ~(0x10)); break;
                    // 0x7ffe	-	35 - 39
                    case 35: _keyLine[7] = (_keyLine[7] & ~(0x1)); break;
                    case 36: _keyLine[7] = (_keyLine[7] & ~(0x02)); break;
                    case 37: _keyLine[7] = (_keyLine[7] & ~(0x04)); break;
                    case 38: _keyLine[7] = (_keyLine[7] & ~(0x08)); break;
                    case 39: _keyLine[7] = (_keyLine[7] & ~(0x10)); break;
                }
            }
            else
            {
                switch (k)
                {
                    // 0xfefe	-	0 - 4
                    case 0: _keyLine[0] = (_keyLine[0] | (0x1)); break;
                    case 1: _keyLine[0] = (_keyLine[0] | (0x02)); break;
                    case 2: _keyLine[0] = (_keyLine[0] | (0x04)); break;
                    case 3: _keyLine[0] = (_keyLine[0] | (0x08)); break;
                    case 4: _keyLine[0] = (_keyLine[0] | (0x10)); break;
                    // 0xfdfe	-	5 - 9
                    case 5: _keyLine[1] = (_keyLine[1] | (0x1)); break;
                    case 6: _keyLine[1] = (_keyLine[1] | (0x02)); break;
                    case 7: _keyLine[1] = (_keyLine[1] | (0x04)); break;
                    case 8: _keyLine[1] = (_keyLine[1] | (0x08)); break;
                    case 9: _keyLine[1] = (_keyLine[1] | (0x10)); break;
                    // 0xfbfe	-	10 - 14
                    case 10: _keyLine[2] = (_keyLine[2] | (0x1)); break;
                    case 11: _keyLine[2] = (_keyLine[2] | (0x02)); break;
                    case 12: _keyLine[2] = (_keyLine[2] | (0x04)); break;
                    case 13: _keyLine[2] = (_keyLine[2] | (0x08)); break;
                    case 14: _keyLine[2] = (_keyLine[2] | (0x10)); break;
                    // 0xf7fe	-	15 - 19
                    case 15: _keyLine[3] = (_keyLine[3] | (0x1)); break;
                    case 16: _keyLine[3] = (_keyLine[3] | (0x02)); break;
                    case 17: _keyLine[3] = (_keyLine[3] | (0x04)); break;
                    case 18: _keyLine[3] = (_keyLine[3] | (0x08)); break;
                    case 19: _keyLine[3] = (_keyLine[3] | (0x10)); break;
                    // 0xeffe	-	20 - 24
                    case 20: _keyLine[4] = (_keyLine[4] | (0x1)); break;
                    case 21: _keyLine[4] = (_keyLine[4] | (0x02)); break;
                    case 22: _keyLine[4] = (_keyLine[4] | (0x04)); break;
                    case 23: _keyLine[4] = (_keyLine[4] | (0x08)); break;
                    case 24: _keyLine[4] = (_keyLine[4] | (0x10)); break;
                    // 0xdffe	-	25 - 29
                    case 25: _keyLine[5] = (_keyLine[5] | (0x1)); break;
                    case 26: _keyLine[5] = (_keyLine[5] | (0x02)); break;
                    case 27: _keyLine[5] = (_keyLine[5] | (0x04)); break;
                    case 28: _keyLine[5] = (_keyLine[5] | (0x08)); break;
                    case 29: _keyLine[5] = (_keyLine[5] | (0x10)); break;
                    // 0xbffe	-	30 - 34
                    case 30: _keyLine[6] = (_keyLine[6] | (0x1)); break;
                    case 31: _keyLine[6] = (_keyLine[6] | (0x02)); break;
                    case 32: _keyLine[6] = (_keyLine[6] | (0x04)); break;
                    case 33: _keyLine[6] = (_keyLine[6] | (0x08)); break;
                    case 34: _keyLine[6] = (_keyLine[6] | (0x10)); break;
                    // 0x7ffe	-	35 - 39
                    case 35: _keyLine[7] = (_keyLine[7] | (0x1)); break;
                    case 36: _keyLine[7] = (_keyLine[7] | (0x02)); break;
                    case 37: _keyLine[7] = (_keyLine[7] | (0x04)); break;
                    case 38: _keyLine[7] = (_keyLine[7] | (0x08)); break;
                    case 39: _keyLine[7] = (_keyLine[7] | (0x10)); break;                    
                }
            }

            */

            // Combination keys that are not in the keyboard matrix
            // but are available on the Spectrum+, 128k +2 & +3
            // (GetByteFromKeyMatrix() should return 255)
            // Processed after the matrix keys - only presses handled (unpressed get done above)
            if (k == 255)
            {
                if (isPressed)
                {
                    switch (key)
                    {
                        // Delete key (simulates Caps Shift + 0)
                        case "Key Delete":
                            _keyLine[0] = _keyLine[0] & ~(0x1);
                            _keyLine[4] = _keyLine[4] & ~(0x1);
                            break;
                        // Cursor left (simulates Caps Shift + 5)
                        case "Key Left Cursor":
                            _keyLine[0] = _keyLine[0] & ~(0x1);
                            _keyLine[3] = _keyLine[3] & ~(0x10);
                            break;
                        // Cursor right (simulates Caps Shift + 8)
                        case "Key Right Cursor":
                            _keyLine[0] = _keyLine[0] & ~(0x1);
                            _keyLine[4] = _keyLine[4] & ~(0x04);
                            break;
                        // Cursor up (simulates Caps Shift + 7)
                        case "Key Up Cursor":
                            _keyLine[0] = _keyLine[0] & ~(0x1);
                            _keyLine[4] = _keyLine[4] & ~(0x08);
                            break;
                        // Cursor down (simulates Caps Shift + 6)
                        case "Key Down Cursor":
                            _keyLine[0] = _keyLine[0] & ~(0x1);
                            _keyLine[4] = _keyLine[4] & ~(0x10);
                            break;
                    }
                }
            }
        }

        public bool GetKeyStatus(string key)
        {
            byte keyByte = GetByteFromKeyMatrix(key);
            var lineIndex = keyByte / 5;
            var lineMask = 1 << keyByte % 5;

            return (_keyLine[lineIndex] & lineMask) == 0;
        }

        public void ResetLineStatus()
        {
            lock (this)
            {
                for (int i = 0; i < KeyLine.Length; i++)
                    KeyLine[i] = 255;
            }
        }

        public byte GetLineStatus(byte lines)
        {            
            lock(this)
            {
                byte status = 0;
                lines = (byte)~lines;
                var lineIndex = 0;
                while (lines > 0)
                {
                    if ((lines & 0x01) != 0)
                        status |= (byte)_keyLine[lineIndex];
                    lineIndex++;
                    lines >>= 1;
                }
                var result = (byte)status;

                return result;
            }
            
            /*
                    switch (lines)
            {
                case 0xfe: return (byte)KeyLine[0];
                case 0xfd: return (byte)KeyLine[1];
                case 0xfb: return (byte)KeyLine[2];
                case 0xf7: return (byte)KeyLine[3];
                case 0xef: return (byte)KeyLine[4];
                case 0xdf: return (byte)KeyLine[5];
                case 0xbf: return (byte)KeyLine[6];
                case 0x7f: return (byte)KeyLine[7];
                default: return 0;
            }
            */
        }

        public byte ReadKeyboardByte(ushort addr)
        {
            return GetLineStatus((byte)(addr >> 8));
        }

        public byte GetByteFromKeyMatrix(string key)
        {
            int index = Array.IndexOf(KeyboardMatrix, key);
            return (byte)index;
        }

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("Keyboard");
            ser.Sync("LineStatus", ref LineStatus, false);
            ser.Sync("_keyLine", ref _keyLine, false);
            ser.EndSection();
        }
    }
}
