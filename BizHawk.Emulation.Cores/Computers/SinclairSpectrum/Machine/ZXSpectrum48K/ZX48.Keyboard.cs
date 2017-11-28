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
        private  byte[] LineStatus;
        public bool Issue2 { get; set; }
        private string[] _keyboardMatrix;

        public string[] KeyboardMatrix
        {
            get { return _keyboardMatrix; }
            set { _keyboardMatrix = value; }
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
				"Key Space", "Key Sym Shift", "Key M", "Key N", "Key B"
            };

            LineStatus = new byte[8];
        }

		public void SetKeyStatus(string key, bool isPressed)
        {
            byte keyByte = GetByteFromKeyMatrix(key);
            var lineIndex = keyByte / 5;
            var lineMask = 1 << keyByte % 5;

            LineStatus[lineIndex] = isPressed ? (byte)(LineStatus[lineIndex] | lineMask)
                : (byte)(LineStatus[lineIndex] & ~lineMask);
        }

		public bool GetKeyStatus(string key)
        {
            byte keyByte = GetByteFromKeyMatrix(key);
            var lineIndex = keyByte / 5;
            var lineMask = 1 << keyByte % 5;
            return (LineStatus[lineIndex] & lineMask) != 0;
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
                        status |= LineStatus[lineIndex];
                    lineIndex++;
                    lines >>= 1;
                }
                var result = (byte)~status;

                return result;
            }
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
            ser.EndSection();
        }
    }
}
