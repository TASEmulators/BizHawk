namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public sealed partial class Drive1541
	{
		private int _overflowFlagDelaySr;

		private byte CpuPeek(ushort addr) => unchecked((byte)Peek(addr));

#pragma warning disable IDE0051
		private byte CpuRead(ushort addr) => unchecked((byte)Read(addr));

		private void CpuWrite(ushort addr, byte val) => Write(addr, val);
#pragma warning restore IDE0051

		private bool ViaReadClock()
		{
			bool inputClock = ReadMasterClk();
			bool outputClock = ReadDeviceClk();
			return !(inputClock && outputClock);
		}

		private bool ViaReadData()
		{
			bool inputData = ReadMasterData();
			bool outputData = ReadDeviceData();
			return !(inputData && outputData);
		}

		private bool ViaReadAtn()
		{
			bool inputAtn = ReadMasterAtn();
			return !inputAtn;
		}

		private int ReadVia1PrA() => _bitHistory & 0xFF;

		private int ReadVia1PrB() => (_motorStep & 0x03) | (_motorEnabled ? 0x04 : 0x00) | (_sync ? 0x00 : 0x80) | (_diskWriteProtected ? 0x00 : 0x10);

		public int Peek(int addr)
		{
			switch (addr & 0xFC00)
			{
				case 0x1800:
					return Via0.Peek(addr);
				case 0x1C00:
					return Via1.Peek(addr);
			}

			if ((addr & 0x8000) != 0)
			{
				return DriveRom.Peek(addr & 0x3FFF);
			}

			if ((addr & 0x1F00) < 0x800)
			{
				return _ram[addr & 0x7FF];
			}

			return (addr >> 8) & 0xFF;
		}

		public int PeekVia0(int addr) => Via0.Peek(addr);

		public int PeekVia1(int addr) => Via1.Peek(addr);

		public void Poke(int addr, int val)
		{
			switch (addr & 0xFC00)
			{
				case 0x1800:
					Via0.Poke(addr, val);
					break;
				case 0x1C00:
					Via1.Poke(addr, val);
					break;
				default:
					if ((addr & 0x8000) == 0 && (addr & 0x1F00) < 0x800)
					{
						_ram[addr & 0x7FF] = val & 0xFF;
					}

					break;
			}
		}

		public void PokeVia0(int addr, int val) => Via0.Poke(addr, val);

		public void PokeVia1(int addr, int val) => Via1.Poke(addr, val);

		public int Read(int addr)
		{
			switch (addr & 0xFC00)
			{
				case 0x1800:
					return Via0.Read(addr);
				case 0x1C00:
					return Via1.Read(addr);
			}

			if ((addr & 0x8000) != 0)
			{
				return DriveRom.Read(addr & 0x3FFF);
			}

			if ((addr & 0x1F00) < 0x800)
			{
				return _ram[addr & 0x7FF];
			}

			return (addr >> 8) & 0xFF;
		}

		public void Write(int addr, int val)
		{
			switch (addr & 0xFC00)
			{
				case 0x1800:
					Via0.Write(addr, val);
					break;
				case 0x1C00:
					Via1.Write(addr, val);
					break;
				default:
					if ((addr & 0x8000) == 0 && (addr & 0x1F00) < 0x800)
					{
						_ram[addr & 0x7FF] = val & 0xFF;
					}

					break;
			}
		}

		public override bool ReadDeviceClk()
		{
			bool viaOutputClock = (Via0.DdrB & 0x08) != 0 && (Via0.PrB & 0x08) != 0;
			return !viaOutputClock;
		}

		public override bool ReadDeviceData()
		{
			// PB1 (input not pulled up)
			bool viaOutputData = (Via0.DdrB & 0x02) != 0 && (Via0.PrB & 0x02) != 0;
			// inverted from c64, input, not pulled up to PB7/CA1
			bool viaInputAtn = ViaReadAtn();
			// PB4 (input not pulled up)
			bool viaOutputAtna = (Via0.DdrB & 0x10) != 0 && (Via0.PrB & 0x10) != 0;

			return !(viaOutputAtna ^ viaInputAtn) && !viaOutputData;
		}

		public override bool ReadDeviceLight() => _driveLightOffTime > 0;
	}
}
