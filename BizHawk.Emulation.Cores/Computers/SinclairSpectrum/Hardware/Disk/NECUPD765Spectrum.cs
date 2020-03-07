using System;

using BizHawk.Emulation.Cores.Computers.CPCSpectrumBase;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	public class NECUPD765Spectrum : NECUPD765<SpectrumBase, NECUPD765Spectrum.ZXDriveState>
	{
		protected override ZXDriveState ConstructDriveState(int driveID, NECUPD765<SpectrumBase, ZXDriveState> fdc) => new ZXDriveState(driveID, fdc);

		protected override void TimingInit()
		{
			// z80 timing
			double frameSize = _machine.ULADevice.FrameLength;
			double rRate = _machine.ULADevice.ClockSpeed / frameSize;
			long tPerSecond = (long)(frameSize * rRate);
			CPUCyclesPerMs = tPerSecond / 1000;

			// drive timing
			double dRate = DriveClock / frameSize;
			long dPerSecond = (long)(frameSize * dRate);
			DriveCyclesPerMs = dPerSecond / 1000;

			long TStatesPerDriveCycle = (long)((double)_machine.ULADevice.ClockSpeed / DriveClock);
			StatesPerDriveTick = TStatesPerDriveCycle;
		}

		public class ZXDriveState : NECUPD765DriveState
		{
			public ZXDriveState(int driveID, NECUPD765<SpectrumBase, ZXDriveState> fdc) : base(driveID, fdc) {}

			public override void FDD_LoadDisk(byte[] diskData)
			{
				// try dsk first
				FloppyDisk fdd = null;
				bool found = false;

				foreach (DiskType type in Enum.GetValues(typeof(DiskType)))
				{
					switch (type)
					{
						case DiskType.CPCExtended:
							fdd = new CPCExtendedFloppyDisk();
							found = fdd.ParseDisk(diskData);
							break;
						case DiskType.CPC:
							fdd = new CPCFloppyDisk();
							found = fdd.ParseDisk(diskData);
							break;
						case DiskType.IPF:
							fdd = new IPFFloppyDisk();
							found = fdd.ParseDisk(diskData);
							break;
						case DiskType.UDI:
							fdd = new UDI1_0FloppyDisk();
							found = fdd.ParseDisk(diskData);
							break;
					}

					if (found)
					{
						Disk = fdd;
						break;
					}
				}

				if (!found)
				{
					throw new Exception(this.GetType().ToString() +
						"\n\nDisk image file could not be parsed. Potentially an unknown format.");
				}
			}
		}
	}
}
