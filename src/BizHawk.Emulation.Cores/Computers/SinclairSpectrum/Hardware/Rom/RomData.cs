
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Information about spectrum ROM
	/// </summary>
	public class RomData
	{
		/// <summary>
		/// ROM Contents
		/// </summary>
		public byte[] RomBytes { get; set; }

		/// <summary>
		/// Useful ROM addresses that are needed during tape operations
		/// </summary>
		public ushort SaveBytesRoutineAddress { get; set; }
		public ushort LoadBytesRoutineAddress { get; set; }
		public ushort SaveBytesResumeAddress { get; set; }
		public ushort LoadBytesResumeAddress { get; set; }
		public ushort LoadBytesInvalidHeaderAddress
		{
			get => _loadBytesInvalidHeaderAddress;
			set => _loadBytesInvalidHeaderAddress = value;
		}

		private ushort _loadBytesInvalidHeaderAddress;

		public static RomData InitROM(MachineType machineType, byte[] rom)
		{
			RomData RD = new() { RomBytes = new byte[rom.Length] };
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
