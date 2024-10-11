//using BizHawk.Emulation.Cores.Components.Z80A;
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
		public CPC6128(AmstradCPC cpc, LibFz80Wrapper cpu, List<byte[]> files, bool autoTape, AmstradCPC.BorderType borderType)
		{
			CPC = cpc;
			CPU = cpu;

			CRTC = CRTC.Create(1);
			GateArray = new GateArray(this, GateArrayType.Amstrad40010);
			CRTScreen = new CRTScreen(ScreenType.CTM064x, borderType);

			FrameLength = GateArray.FrameLength / 4;

			PPI = new PPI_8255(this);
			PAL = new PAL16L8(this);

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
