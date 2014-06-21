using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
		Mapper used for multi-cart 8K games
	*/
	internal class Multicart8K : MapperBase
	{
		private int _bank4K;
		
		private int _gameTotal;
		private int _currentGame;

		public Multicart8K(int gametotal)
		{
			_gameTotal = gametotal;
			_currentGame = 0;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("bank_4k", ref _bank4K);
			ser.Sync("gameTotal", ref _gameTotal);
			ser.Sync("currentGame", ref _currentGame);
			base.SyncState(ser);
		}

		public override void HardReset()
		{
			_bank4K = 0;
			IncrementGame();
		}

		private void IncrementGame()
		{
			_currentGame++;
			if (_currentGame >= _gameTotal)
			{
				_currentGame = 0;
			}
		}

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return Core.Rom[(_bank4K << 12) + (addr & 0xFFF) + (_currentGame * 8192)];
		}

		public override byte ReadMemory(ushort addr)
		{
			return ReadMem(addr, false);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMem(addr, true);
		}

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (!poke)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: false);
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: true);
		}

		private void Address(ushort addr)
		{
			if (addr == 0x1FF8)
			{
				_bank4K = 0;
			}
			else if (addr == 0x1FF9)
			{
				_bank4K = 1;
			}
		}
	}
}
