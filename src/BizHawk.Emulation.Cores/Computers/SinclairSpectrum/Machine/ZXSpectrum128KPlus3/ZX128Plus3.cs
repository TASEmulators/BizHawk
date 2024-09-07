using BizHawk.Emulation.Cores.Components.Z80A;
using System.Collections.Generic;
using BizHawk.Emulation.Cores.Sound;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// +3 Constructor
	/// </summary>
	public partial class ZX128Plus3 : SpectrumBase
	{
		/// <summary>
		/// Main constructor
		/// </summary>
		public ZX128Plus3(ZXSpectrum spectrum, Z80A<ZXSpectrum.CpuLink> cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks)
		{
			Spectrum = spectrum;
			CPU = cpu;

			CPUMon = new CPUMonitor(this) { machineType = MachineType.ZXSpectrum128Plus3 };

			ROMPaged = 0;
			SHADOWPaged = false;
			RAMPaged = 0;
			PagingDisabled = false;

			ULADevice = new Screen128Plus2a(this);

			BuzzerDevice = new OneBitBeeper(44100, ULADevice.FrameLength, 50, "SystemBuzzer");

			TapeBuzzer = new OneBitBeeper(44100, ULADevice.FrameLength, 50, "TapeBuzzer");

			AYDevice = new AY38912(this);
			AYDevice.Init(44100, ULADevice.FrameLength);

			KeyboardDevice = new StandardKeyboard(this);

			InitJoysticks(joysticks);

			TapeDevice = new DatacorderDevice(spectrum.SyncSettings.AutoLoadTape);
			TapeDevice.Init(this);

			UPDDiskDevice = new NECUPD765();
			UPDDiskDevice.Init(this);

			InitializeMedia(files);
		}
	}
}
