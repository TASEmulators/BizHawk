using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Via
	{
		private interface IPort
		{
			int ReadPra(int pra, int ddra);
			int ReadPrb(int prb, int ddrb);
			int ReadExternalPra();
			int ReadExternalPrb();

			void SyncState(Serializer ser);
		}

		private sealed class DisconnectedPort : IPort
		{
			public int ReadPra(int pra, int ddra)
			{
				return (pra | ~ddra) & 0xFF;
			}

			public int ReadPrb(int prb, int ddrb)
			{
				return (prb | ~ddrb) & 0xFF;
			}

			public int ReadExternalPra()
			{
				return 0xFF;
			}

			public int ReadExternalPrb()
			{
				return 0xFF;
			}

			public void SyncState(Serializer ser)
			{
				// Do nothing
			}
		}

		private sealed class DriverPort : IPort
		{
			private readonly Func<int> _readPrA;
			private readonly Func<int> _readPrB;

			public DriverPort(Func<int> readPrA, Func<int> readPrB)
			{
				_readPrA = readPrA;
				_readPrB = readPrB;
			}

			public int ReadPra(int pra, int ddra)
			{
				return (pra | ~ddra) & ReadExternalPra();
			}

			public int ReadPrb(int prb, int ddrb)
			{
				return (prb & ddrb) | (_readPrB() & ~ddrb);
			}

			public int ReadExternalPra()
			{
				return _readPrA();
			}

			public int ReadExternalPrb()
			{
				return _readPrB();
			}

			public void SyncState(Serializer ser)
			{
				// Do nothing
			}
		}

		private sealed class IecPort : IPort
		{
			private readonly Func<bool> _readClock;
			private readonly Func<bool> _readData;
			private readonly Func<bool> _readAtn;

			private int _driveNumber;

			public IecPort(Func<bool> readClock, Func<bool> readData, Func<bool> readAtn, int driveNumber)
			{
				_readClock = readClock;
				_readData = readData;
				_readAtn = readAtn;
				_driveNumber = (driveNumber & 0x3) << 5;
			}

			public int ReadPra(int pra, int ddra)
			{
				return (pra | ~ddra) & ReadExternalPra();
			}

			public int ReadPrb(int prb, int ddrb)
			{
				return (prb & ddrb) |
					   (~ddrb & 0xE5 & (
					   (_readClock() ? 0x04 : 0x00) |
					   (_readData() ? 0x01 : 0x00) |
					   (_readAtn() ? 0x80 : 0x00) |
					   _driveNumber));
			}

			public int ReadExternalPra()
			{
				return 0xFF;
			}

			public int ReadExternalPrb()
			{
				return
					(_readClock() ? 0x04 : 0x00) |
					(_readData() ? 0x01 : 0x00) |
					(_readAtn() ? 0x80 : 0x00) |
					_driveNumber;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(_driveNumber), ref _driveNumber);
			}
		}
	}
}
