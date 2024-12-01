namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// ROM chips
	public sealed class Chip23128
	{
		private readonly int[] _rom;

		public Chip23128()
		{
			_rom = new int[0x4000];
		}

		public Chip23128(byte[] data) : this()
		{
			Flash(data);
		}

		public void Flash(byte[] data)
		{
			// ensures ROM is mirrored
			for (var i = 0; i < _rom.Length; i += data.Length)
			{
				Array.Copy(data, 0, _rom, i, data.Length);
			}
		}

		public int Peek(int addr)
		{
			return _rom[addr & 0x3FFF];
		}

		public int Read(int addr)
		{
			return _rom[addr & 0x3FFF];
		}
	}
}
