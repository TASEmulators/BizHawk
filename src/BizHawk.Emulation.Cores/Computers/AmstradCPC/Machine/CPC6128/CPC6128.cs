using BizHawk.Emulation.Cores.Components.Z80A;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CPC6128 construction
	/// </summary>
	public partial class CPC6128 : CPCBase
	{
		/// <summary>
		/// Main constructor
		/// </summary>
		public CPC6128(AmstradCPC cpc, Z80A<AmstradCPC.CpuLink> cpu, List<byte[]> files, bool autoTape, AmstradCPC.BorderType borderType)
		{
			CPC = cpc;
			CPU = cpu;

			CRCT = new CRTC(0);
			GateArray = new GateArray(this, GateArrayType.Amstrad40010);

			FrameLength = GateArray.FrameLength / 4;

			PPI = new PPI_8255(this);

			TapeBuzzer = new Beeper(this);
			TapeBuzzer.Init(44100, FrameLength);

			//AYDevice = new PSG(this, PSG.ay38910_type_t.AY38910_TYPE_8912, GateArray.PSGClockSpeed, 882 * 50);
			AYDevice = new AY38912(this);
			AYDevice.Init(44100, FrameLength);

			KeyboardDevice = new StandardKeyboard(this);

			TapeDevice = new DatacorderDevice(autoTape);
			TapeDevice.Init(this);

			UPDDiskDevice = new NECUPD765();
			UPDDiskDevice.Init(this);

			InitializeMedia(files);
		}
	}
}
