using System.Collections.Generic;
using BizHawk.Emulation.Cores.Components.Z80AOpt;
using BizHawk.Emulation.Cores.Sound;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// 128K Constructor
	/// </summary>
	public partial class Pentagon128 : SpectrumBase
	{
		/// <summary>
		/// Main constructor
		/// </summary>
		public Pentagon128(ZXSpectrum spectrum, Z80AOpt<ZXSpectrum.CpuLink> cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks)
		{
			Spectrum = spectrum;
			CPU = cpu;

			CPUMon = new CPUMonitor(this) { machineType = MachineType.Pentagon128 };

			ROMPaged = 0;
			SHADOWPaged = false;
			RAMPaged = 0;
			PagingDisabled = false;

			ULADevice = new ScreenPentagon128(this);

			BuzzerDevice = new OneBitBeeper(44100, ULADevice.FrameLength, 50, "SystemBuzzer");

			TapeBuzzer = new OneBitBeeper(44100, ULADevice.FrameLength, 50, "TapeBuzzer");

			AYDevice = new AY38912(this);
			AYDevice.Init(44100, ULADevice.FrameLength);

			KeyboardDevice = new StandardKeyboard(this);

			InitJoysticks(joysticks);

			TapeDevice = new DatacorderDevice(spectrum.SyncSettings.AutoLoadTape);
			TapeDevice.Init(this);

			// Beta 128 disk interface (WD1793 + TR-DOS). Set up before media load so a .TRD can be inserted.
			var beta = new BetaDiskController
			{
				DriveType = spectrum.SyncSettings.PentagonDriveType == ZXSpectrum.PentagonDiskDriveType.Drive525Inch
					? BetaDiskController.DriveKind.FiveQuarterInch
					: BetaDiskController.DriveKind.ThreeHalfInch,
			};
			UPDDiskDevice = beta;
			UPDDiskDevice.Init(this);

			InitializeMedia(files);
		}
	}
}
