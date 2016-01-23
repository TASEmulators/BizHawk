using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// ROM chips
	// 2332: 32 kbit (4kbyte)
	// 2364: 64 kbit (8kbyte)
	// 23128: 128 kbit (16kbyte)

	public enum Chip23XXmodel
	{
		Chip2332,
		Chip2364,
		Chip23128
	}

	public sealed class Chip23XX
	{
	    private readonly int _addrMask;
	    private readonly int[] _rom;

		public Chip23XX(Chip23XXmodel model, byte[] data)
		{
			switch (model)
			{
				case Chip23XXmodel.Chip2332:
					_rom = new int[0x1000];
					_addrMask = 0xFFF;
					break;
				case Chip23XXmodel.Chip2364:
					_rom = new int[0x2000];
					_addrMask = 0x1FFF;
					break;
				case Chip23XXmodel.Chip23128:
					_rom = new int[0x4000];
					_addrMask = 0x3FFF;
					break;
				default:
					throw new Exception("Invalid chip model.");
			}
			Array.Copy(data, _rom, _rom.Length);
		}

		public int Peek(int addr)
		{
			return _rom[addr & _addrMask];
		}

		public int Read(int addr)
		{
			return _rom[addr & _addrMask];
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
