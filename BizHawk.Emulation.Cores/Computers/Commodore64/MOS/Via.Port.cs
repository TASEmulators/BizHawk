using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Via
    {
        private abstract class Port
        {
            public abstract int ReadPra(int pra, int ddra);
            public abstract int ReadPrb(int prb, int ddrb);
            public abstract int ReadExternalPra();
            public abstract int ReadExternalPrb();

            public void SyncState(Serializer ser)
            {
                SaveState.SyncObject(ser, this);
            }
        }

        private sealed class DisconnectedPort : Port
        {
            public override int ReadPra(int pra, int ddra)
            {
                return (pra | ~ddra) & 0xFF;
            }

            public override int ReadPrb(int prb, int ddrb)
            {
                return (prb | ~ddrb) & 0xFF;
            }

            public override int ReadExternalPra()
            {
                return 0xFF;
            }

            public override int ReadExternalPrb()
            {
                return 0xFF;
            }
        }

        private sealed class DriverPort : Port
        {
            private readonly Func<int> _readPrA; 
            private readonly Func<int> _readPrB;

            public DriverPort(Func<int> readPrA, Func<int> readPrB)
            {
                _readPrA = readPrA;
                _readPrB = readPrB;
            }

            public override int ReadPra(int pra, int ddra)
            {
                return _readPrA();
            }

            public override int ReadPrb(int prb, int ddrb)
            {
                return (prb & ddrb) | (_readPrB() & ~ddrb);
            }

            public override int ReadExternalPra()
            {
                return _readPrA();
            }

            public override int ReadExternalPrb()
            {
                return _readPrB();
            }
        }

        private sealed class IecPort : Port
        {
            private readonly Func<bool> _readClock;
            private readonly Func<bool> _readData;
            private readonly Func<bool> _readAtn;
            private readonly int _driveNumber;

            public IecPort(Func<bool> readClock, Func<bool> readData, Func<bool> readAtn, int driveNumber)
            {
                _readClock = readClock;
                _readData = readData;
                _readAtn = readAtn;
                _driveNumber = (driveNumber & 0x3) << 5;
            }

            public override int ReadPra(int pra, int ddra)
            {
                return (pra | ~ddra) & 0xFF;
            }

            public override int ReadPrb(int prb, int ddrb)
            {
                return (prb & ddrb) |
                       (~ddrb & 0xE5 & (
                       (_readClock() ? 0x04 : 0x00) |
                       (_readData() ? 0x01 : 0x00) |
                       (_readAtn() ? 0x80 : 0x00) |
                       _driveNumber)
                       );
            }

            public override int ReadExternalPra()
            {
                return 0xFF;
            }

            public override int ReadExternalPrb()
            {
                return 
                       (_readClock() ? 0x04 : 0x00) |
                       (_readData() ? 0x01 : 0x00) |
                       (_readAtn() ? 0x80 : 0x00) |
                       _driveNumber;
            }
        }
    }
}
