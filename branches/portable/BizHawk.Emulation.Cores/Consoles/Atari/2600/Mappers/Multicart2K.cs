using System.Linq;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
		Mapper used for multi-cart 2K games
	*/
	internal class Multicart2K : MapperBase
	{
		private int _gameTotal;
		private int _currentGame;

		public Multicart2K(int gametotal)
		{
			_gameTotal = gametotal;
			_currentGame = 0;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("gameTotal", ref _gameTotal);
			ser.Sync("currentGame", ref _currentGame);
			base.SyncState(ser);
		}

		public override void HardReset()
		{
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

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return this.Core.Rom[(addr & 0x7FF) + (_currentGame * 2048)];
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}
	}
}
