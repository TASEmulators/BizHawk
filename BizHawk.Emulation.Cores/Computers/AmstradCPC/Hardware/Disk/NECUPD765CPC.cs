using System;

using BizHawk.Emulation.Cores.Computers.CPCSpectrumBase;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	public class NECUPD765CPC : NECUPD765<CPCBase, NECUPD765CPC.CPCDriveState>
	{
		protected override CPCDriveState ConstructDriveState(int driveID, NECUPD765<CPCBase, CPCDriveState> fdc) => new CPCDriveState(driveID, fdc);

		protected override void TimingInit()
		{
			// z80 timing
			double frameSize = _machine.GateArray.FrameLength;
			double rRate = _machine.GateArray.Z80ClockSpeed / frameSize;
			long tPerSecond = (long)(frameSize * rRate);
			CPUCyclesPerMs = tPerSecond / 1000;

			// drive timing
			double dRate = DriveClock / frameSize;
			long dPerSecond = (long)(frameSize * dRate);
			DriveCyclesPerMs = dPerSecond / 1000;

			long TStatesPerDriveCycle = (long)((double)_machine.GateArray.Z80ClockSpeed / DriveClock);
			StatesPerDriveTick = TStatesPerDriveCycle;

		}

		public class CPCDriveState : NECUPD765DriveState
		{
			public CPCDriveState(int driveID, NECUPD765<CPCBase, CPCDriveState> fdc) : base(driveID, fdc) {}

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
