﻿
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Information about spectrum ROM
	/// </summary>
	[CLSCompliant(false)]
	public class RomData
	{
		/// <summary>
		/// ROM Contents
		/// </summary>
		public byte[] RomBytes
		{
			get => _romBytes;
			set => _romBytes = value;
		}

		/// <summary>
		/// Useful ROM addresses that are needed during tape operations
		/// </summary>
		public ushort SaveBytesRoutineAddress
		{
			get => _saveBytesRoutineAddress;
			set => _saveBytesRoutineAddress = value;
		}
		public ushort LoadBytesRoutineAddress
		{
			get => _loadBytesRoutineAddress;
			set => _loadBytesRoutineAddress = value;
		}
		public ushort SaveBytesResumeAddress
		{
			get => _saveBytesResumeAddress;
			set => _saveBytesResumeAddress = value;
		}
		public ushort LoadBytesResumeAddress
		{
			get => _loadBytesResumeAddress;
			set => _loadBytesResumeAddress = value;
		}
		public ushort LoadBytesInvalidHeaderAddress
		{
			get => _loadBytesInvalidHeaderAddress;
			set => _loadBytesInvalidHeaderAddress = value;
		}

		private byte[] _romBytes;
		private ushort _saveBytesRoutineAddress;
		private ushort _loadBytesRoutineAddress;
		private ushort _saveBytesResumeAddress;
		private ushort _loadBytesResumeAddress;
		private ushort _loadBytesInvalidHeaderAddress;

		public static RomData InitROM(MachineType machineType, byte[] rom)
		{
			RomData RD = new RomData { RomBytes = new byte[rom.Length] };
			RD.RomBytes = rom;

			switch (machineType)
			{
				case MachineType.ZXSpectrum48:
					RD.SaveBytesRoutineAddress = 0x04C2;
					RD.SaveBytesResumeAddress = 0x0000;
					RD.LoadBytesRoutineAddress = 0x0808; //0x0556; //0x056C;
					RD.LoadBytesResumeAddress = 0x05E2;
					RD.LoadBytesInvalidHeaderAddress = 0x05B6;
					break;

				case MachineType.ZXSpectrum128:
					RD.SaveBytesRoutineAddress = 0x04C2;
					RD.SaveBytesResumeAddress = 0x0000;
					RD.LoadBytesRoutineAddress = 0x0808; //0x0556; //0x056C;
					RD.LoadBytesResumeAddress = 0x05E2;
					RD.LoadBytesInvalidHeaderAddress = 0x05B6;
					break;
			}

			return RD;
		}
	}
}
