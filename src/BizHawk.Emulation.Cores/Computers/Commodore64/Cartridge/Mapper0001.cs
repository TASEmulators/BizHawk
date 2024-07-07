using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	internal sealed class Mapper0001 : CartridgeDevice
	{
		private int[] _ram = new int[0x2000];
		private bool _ramEnabled;

		private readonly int[] _rom = new int[0x8000];

		private int _romOffset;
		private bool _cartEnabled;

		public Mapper0001(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			pinExRom = false;
			pinGame = false;
			for (var i = 0; i < newData.Count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					Array.Copy(newData[i], 0, _rom, 0x2000 * newBanks[i], 0x2000);
				}
			}

			_romOffset = 0;
			_cartEnabled = true;
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("RAM", ref _ram, useNull: false);
			ser.Sync("RAMEnabled", ref _ramEnabled);
			ser.Sync("ROMOffset", ref _romOffset);
			ser.Sync("CartEnabled", ref _cartEnabled);
		}

		public override void HardReset()
		{
			base.HardReset();
			pinExRom = false;
			pinGame = false;
			for (var i = 0; i < 0x2000; i++)
			{
				_ram[i] = 0x00;
			}

			_romOffset = 0;
			_cartEnabled = true;
		}

		public override int Peek8000(int addr)
		{
			return GetLoRom(addr);
		}

		public override int PeekA000(int addr)
		{
			return Peek8000(addr);
		}

		public override int PeekDF00(int addr)
		{
			return GetIo2(addr);
		}

		public override void Poke8000(int addr, int val)
		{
			SetLoRom(addr, val);
		}

		public override void PokeA000(int addr, int val)
		{
			Poke8000(addr, val);
		}

		public override void PokeDE00(int addr, int val)
		{
			SetState(val);
		}

		public override void PokeDF00(int addr, int val)
		{
			SetIo2(addr, val);
		}

		public override int Read8000(int addr)
		{
			return GetLoRom(addr);
		}

		public override int ReadA000(int addr)
		{
			return GetHiRom(addr);
		}

		public override int ReadDF00(int addr)
		{
			return GetIo2(addr);
		}

		public override void Write8000(int addr, int val)
		{
			SetLoRom(addr, val);
		}

		public override void WriteA000(int addr, int val)
		{
			SetLoRom(addr, val);
		}

		public override void WriteDE00(int addr, int val)
		{
			SetState(val);
		}

		public override void WriteDF00(int addr, int val)
		{
			SetIo2(addr, val);
		}

		private void SetState(int val)
		{
			pinGame = (val & 0x01) == 0;
			pinExRom = (val & 0x02) != 0;
			_cartEnabled = (val & 0x04) == 0;
			_romOffset = (val & 0x18) << 10;
			_ramEnabled = (val & 0x20) == 0;
		}

		private int GetLoRom(int addr)
		{
			return _ramEnabled
				? _ram[addr & 0x1FFF]
				: _rom[(addr & 0x1FFF) | _romOffset];
		}

		private int GetHiRom(int addr)
		{
			return _rom[(addr & 0x1FFF) | _romOffset];
		}

		private void SetLoRom(int addr, int val)
		{
			_ram[addr & 0x1FFF] = val;
		}

		private int GetIo2(int addr)
		{
			if (!_cartEnabled)
			{
				return ReadOpenBus();
			}

			return _ramEnabled
				? _ram[(addr & 0xFF) | 0x1F00]
				: _rom[(addr & 0xFF) | _romOffset | 0x1F00];
		}

		private void SetIo2(int addr, int val)
		{
			_ram[addr & 0x1FFF] = val & 0xFF;
		}
	}
}
