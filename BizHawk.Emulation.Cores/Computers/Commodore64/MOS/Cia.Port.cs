using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Cia
    {
        private abstract class Port
        {
            public abstract int ReadPra(int pra, int ddra, int prb, int ddrb);
            public abstract int ReadPrb(int pra, int ddra, int prb, int ddrb);

            public void SyncState(Serializer ser)
            {
                SaveState.SyncObject(ser, this);
            }
        }

        private sealed class JoystickKeyboardPort : Port
        {
            [SaveState.DoNotSave] private int _ret;
            [SaveState.DoNotSave] private int _tst;
            [SaveState.DoNotSave] private readonly int[] _joyData;
            [SaveState.DoNotSave] private readonly int[] _keyData;

            public JoystickKeyboardPort(int[] joyData, int[] keyData)
            {
                _joyData = joyData;
                _keyData = keyData;
            }

            private int GetJoystick1()
            {
                return 0xE0 |
                       (_joyData[0] == 0 ? 0x01 : 0x00) |
                       (_joyData[1] == 0 ? 0x02 : 0x00) |
                       (_joyData[2] == 0 ? 0x04 : 0x00) |
                       (_joyData[3] == 0 ? 0x08 : 0x00) |
                       (_joyData[4] == 0 ? 0x10 : 0x00);
            }

            private int GetJoystick2()
            {
                return 0xE0 |
                       (_joyData[5] == 0 ? 0x01 : 0x00) |
                       (_joyData[6] == 0 ? 0x02 : 0x00) |
                       (_joyData[7] == 0 ? 0x04 : 0x00) |
                       (_joyData[8] == 0 ? 0x08 : 0x00) |
                       (_joyData[9] == 0 ? 0x10 : 0x00);
            }

            private int GetKeyboardRows(int activeColumns)
            {
                var result = 0xFF;
                for (var r = 0; r < 8; r++)
                {
                    if ((activeColumns & 0x1) == 0)
                    {
                        var i = r << 3;
                        for (var c = 0; c < 8; c++)
                        {
                            if (_keyData[i++] != 0)
                            {
                                result &= ~(1 << c);
                            }
                        }
                    }
                    activeColumns >>= 1;
                }
                return result;
            }

            private int GetKeyboardColumns(int activeRows)
            {
                var result = 0xFF;
                for (var c = 0; c < 8; c++)
                {
                    if ((activeRows & 0x1) == 0)
                    {
                        var i = c;
                        for (var r = 0; r < 8; r++)
                        {
                            if (_keyData[i] != 0)
                            {
                                result &= ~(1 << r);
                            }
                            i += 0x8;
                        }
                    }
                    activeRows >>= 1;
                }
                return result;
            }

            public override int ReadPra(int pra, int ddra, int prb, int ddrb)
            {
                _ret = (pra | ~ddra) & 0xFF;
                _tst = (prb | ~ddrb) & GetJoystick1();
                _ret &= GetKeyboardColumns(_tst);
                return _ret & GetJoystick2();
            }

            public override int ReadPrb(int pra, int ddra, int prb, int ddrb)
            {
                _ret = ~ddrb & 0xFF;
                _tst = (pra | ~ddra) & GetJoystick2();
                _ret &= GetKeyboardRows(_tst);
                return (_ret | (prb & ddrb)) & GetJoystick1();
            }
        }

        private sealed class IecPort : Port
        {
            private readonly Func<int> _readIec;

            public IecPort(Func<int> readIec)
            {
                _readIec = readIec;
            }

            public override int ReadPra(int pra, int ddra, int prb, int ddrb)
            {
                return ((pra | ~ddra) & 0x3F) | (0xF0 & _readIec());
            }

            public override int ReadPrb(int pra, int ddra, int prb, int ddrb)
            {
                return (prb | ~ddrb) & 0xFF;
            }
        }
    }
}
