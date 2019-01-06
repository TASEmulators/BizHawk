using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// 48K construction
	/// </summary>
	public partial class ZX48 : SpectrumBase
	{
		#region Construction

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="spectrum"></param>
		/// <param name="cpu"></param>
		public ZX48(ZXSpectrum spectrum, Z80A cpu, ZXSpectrum.BorderType borderType, List<byte[]> files, List<JoystickType> joysticks)
		{
			Spectrum = spectrum;
			CPU = cpu;

			CPUMon = new CPUMonitor(this);
			ULADevice = new Screen48(this);

			BuzzerDevice = new Beeper(this);
			BuzzerDevice.Init(44100, ULADevice.FrameLength);

			TapeBuzzer = new Beeper(this);
			TapeBuzzer.Init(44100, ULADevice.FrameLength);

			KeyboardDevice = new StandardKeyboard(this);

			InitJoysticks(joysticks);

			TapeDevice = new DatacorderDevice(spectrum.SyncSettings.AutoLoadTape);
			TapeDevice.Init(this);

			InitializeMedia(files);
		}

		#endregion

		#region Reset

		public override void HardReset()
		{
			base.HardReset();

			Random rn = new Random();
			for (int d = 0; d < 6912; d++)
			{
				RAM0[d] = (byte)rn.Next(255);
			}
		}

		#endregion
	}
}
