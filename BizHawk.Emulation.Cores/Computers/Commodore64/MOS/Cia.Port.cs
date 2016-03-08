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

        private sealed class DisconnectedPort : Port
        {
            public override int ReadPra(int pra, int ddra, int prb, int ddrb)
            {
                return (pra | ~ddra) & 0xFF;
            }

            public override int ReadPrb(int pra, int ddra, int prb, int ddrb)
            {
                return (prb | ~ddrb) & 0xFF;
            }
        }

        private sealed class JoystickKeyboardPort : Port
        {
            [SaveState.DoNotSave] private int _ret;
            [SaveState.DoNotSave] private int _tst;
            [SaveState.DoNotSave] private readonly Func<bool[]> _readJoyData;
            [SaveState.DoNotSave] private readonly Func<bool[]> _readKeyData;

            public JoystickKeyboardPort(Func<bool[]> readJoyData, Func<bool[]> readKeyData)
            {
                _readJoyData = readJoyData;
                _readKeyData = readKeyData;
            }

            private int GetJoystick1()
            {
                var joyData = _readJoyData();
                return 0xE0 |
                       (joyData[0] ? 0x00 : 0x01) |
                       (joyData[1] ? 0x00 : 0x02) |
                       (joyData[2] ? 0x00 : 0x04) |
                       (joyData[3] ? 0x00 : 0x08) |
                       (joyData[4] ? 0x00 : 0x10);
            }

            private int GetJoystick2()
            {
                var joyData = _readJoyData();
                return 0xE0 |
                       (joyData[5] ? 0x00 : 0x01) |
                       (joyData[6] ? 0x00 : 0x02) |
                       (joyData[7] ? 0x00 : 0x04) |
                       (joyData[8] ? 0x00 : 0x08) |
                       (joyData[9] ? 0x00 : 0x10);
            }

            private int GetKeyboardRows(int activeColumns)
            {
                var keyData = _readKeyData();
                var result = 0xFF;
                for (var r = 0; r < 8; r++)
                {
                    if ((activeColumns & 0x1) == 0)
                    {
                        var i = r << 3;
                        for (var c = 0; c < 8; c++)
                        {
                            if (keyData[i++])
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
                var keyData = _readKeyData();
                var result = 0xFF;
                for (var c = 0; c < 8; c++)
                {
                    if ((activeRows & 0x1) == 0)
                    {
                        var i = c;
                        for (var r = 0; r < 8; r++)
                        {
                            if (keyData[i])
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
                return (pra & ddra) | (~ddra & _readIec());
            }

            public override int ReadPrb(int pra, int ddra, int prb, int ddrb)
            {
                return (prb | ~ddrb) & 0xFF;
            }
        }
    }
}
