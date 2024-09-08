using BizHawk.Emulation.Cores.Components.Z80A;

using System.Collections.Generic;
using BizHawk.Emulation.Cores.Sound;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// 48K construction
	/// </summary>
	public partial class ZX48 : SpectrumBase
	{
		/// <summary>
		/// Main constructor
		/// </summary>
		public ZX48(ZXSpectrum spectrum, Z80A<ZXSpectrum.CpuLink> cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks)
		{
			Spectrum = spectrum;
			CPU = cpu;

			CPUMon = new CPUMonitor(this);
			ULADevice = new Screen48(this);

			BuzzerDevice = new OneBitBeeper(44100, ULADevice.FrameLength, 50, "SystemBuzzer");

			TapeBuzzer = new OneBitBeeper(44100, ULADevice.FrameLength, 50, "TapeBuzzer");

			KeyboardDevice = new StandardKeyboard(this);

			InitJoysticks(joysticks);

			TapeDevice = new DatacorderDevice(spectrum.SyncSettings.AutoLoadTape);
			TapeDevice.Init(this);

			InitializeMedia(files);
		}

		public override void HardReset()
		{
			base.HardReset();

			Random rn = new Random();
			for (int d = 0; d < 6912; d++)
			{
				RAM0[d] = (byte)rn.Next(255);
			}
		}
	}
}
