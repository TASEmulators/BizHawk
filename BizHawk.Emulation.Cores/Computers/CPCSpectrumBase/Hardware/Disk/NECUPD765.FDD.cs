using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.CPCSpectrumBase
{
	/// <summary>
	/// Floppy drive related stuff
	/// </summary>
	#region Attribution
	/*
		Implementation based on the information contained here:
		http://www.cpcwiki.eu/index.php/765_FDC
		and here:
		http://www.cpcwiki.eu/imgs/f/f3/UPD765_Datasheet_OCRed.pdf
	*/
	#endregion
	public abstract partial class NECUPD765<TMachine, TDriveState> : IFDDHost, INECUPD765
	{
		#region Drive State

		/// <summary>
		/// FDD Flag - motor on/off
		/// </summary>
		public bool FDD_FLAG_MOTOR;

		bool INECUPD765.FDD_FLAG_MOTOR => FDD_FLAG_MOTOR;

		/// <summary>
		/// The index of the currently active disk drive
		/// </summary>
		public int DiskDriveIndex
		{
			get => _diskDriveIndex;
			set
			{
				// when index is changed update the ActiveDrive
				_diskDriveIndex = value;
				ActiveDrive = DriveStates[_diskDriveIndex];
			}
		}
		private int _diskDriveIndex = 0;

		/// <summary>
		/// The currently active drive
		/// </summary>
		private NECUPD765DriveState ActiveDrive;

		/// <summary>
		/// Array that holds state information for each possible drive
		/// </summary>
		private NECUPD765DriveState[] DriveStates = new NECUPD765DriveState[4];

		#endregion

		#region FDD Methods

		protected abstract TDriveState ConstructDriveState(int driveID, NECUPD765<TMachine, TDriveState> fdc);

		/// <summary>
		/// Initialization / reset of the floppy drive subsystem
		/// </summary>
		private void FDD_Init()
		{
			for (int i = 0; i < 4; i++)
			{
				NECUPD765DriveState ds = ConstructDriveState(i, this);
				DriveStates[i] = ds;
			}
		}

		/// <summary>
		/// Searches for the requested sector
		/// </summary>
		private FloppyDisk.Sector GetSector()
		{
			FloppyDisk.Sector sector = null;

			// get the current track
			var trk = ActiveDrive.Disk.DiskTracks[ActiveDrive.TrackIndex];

			// get the current sector index
			int index = ActiveDrive.SectorIndex;

			// make sure this index exists
			if (index > trk.Sectors.Length)
			{
				index = 0;
			}

			// index hole count
			int iHole = 0;

			// loop through the sectors in a track
			// the loop ends with either the sector being found
			// or the index hole being passed twice
			while (iHole <= 2)
			{
				// does the requested sector match the current sector
				if (trk.Sectors[index].SectorIDInfo.C == ActiveCommandParams.Cylinder &&
					trk.Sectors[index].SectorIDInfo.H == ActiveCommandParams.Head &&
					trk.Sectors[index].SectorIDInfo.R == ActiveCommandParams.Sector &&
					trk.Sectors[index].SectorIDInfo.N == ActiveCommandParams.SectorSize)
				{
					// sector has been found
					sector = trk.Sectors[index];

					UnSetBit(SR2_BC, ref Status2);
					UnSetBit(SR2_WC, ref Status2);
					break;
				}

				// check for bad cylinder
				if (trk.Sectors[index].SectorIDInfo.C == 255)
				{
					SetBit(SR2_BC, ref Status2);
				}
				// check for no cylinder
				else if (trk.Sectors[index].SectorIDInfo.C != ActiveCommandParams.Cylinder)
				{
					SetBit(SR2_WC, ref Status2);
				}

				// incrememnt sector index
				index++;

				// have we reached the index hole?
				if (trk.Sectors.Length <= index)
				{
					// wrap around
					index = 0;
					iHole++;
				}
			}

			// search loop has completed and the sector may or may not have been found

			// bad cylinder detected?
			if (Status2.Bit(SR2_BC))
			{
				// remove WC
				UnSetBit(SR2_WC, ref Status2);
			}

			// update sectorindex on drive
			ActiveDrive.SectorIndex = index;

			return sector;
		}

		#endregion

		#region IFDDHost

		// IFDDHost methods that fall through to the currently active drive

		/// <summary>
		/// Parses a new disk image and loads it into this floppy drive
		/// </summary>
		public void FDD_LoadDisk(byte[] diskData)
		{
			// we are only going to load into the first drive
			DriveStates[0].FDD_LoadDisk(diskData);
		}

		/// <summary>
		/// Ejects the current disk
		/// </summary>
		public void FDD_EjectDisk()
		{
			DriveStates[0].FDD_EjectDisk();
		}

		/// <summary>
		/// Signs whether the current active drive has a disk inserted
		/// </summary>
		public bool FDD_IsDiskLoaded => DriveStates[DiskDriveIndex].FDD_IsDiskLoaded;

		/// <summary>
		/// Returns the disk object from drive 0
		/// </summary>
		public FloppyDisk DiskPointer => DriveStates[0].Disk;

		public FloppyDisk Disk { get; set; }

		#endregion
	}
}
