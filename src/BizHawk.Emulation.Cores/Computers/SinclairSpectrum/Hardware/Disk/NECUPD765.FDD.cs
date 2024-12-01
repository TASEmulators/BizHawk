using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// Floppy drive related stuff
	/// </summary>
	/*
        Implementation based on the information contained here:
        http://www.cpcwiki.eu/index.php/765_FDC
        and here:
        http://www.cpcwiki.eu/imgs/f/f3/UPD765_Datasheet_OCRed.pdf
    */
	public partial class NECUPD765 : IFDDHost
	{
		/// <summary>
		/// FDD Flag - motor on/off
		/// </summary>
		public bool FDD_FLAG_MOTOR;

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
		private DriveState ActiveDrive;

		/// <summary>
		/// Array that holds state information for each possible drive
		/// </summary>
		private readonly DriveState[] DriveStates = new DriveState[4];

		/// <summary>
		/// Initialization / reset of the floppy drive subsystem
		/// </summary>
		private void FDD_Init()
		{
			for (int i = 0; i < 4; i++)
			{
				DriveState ds = new DriveState(i, this);
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
				var next = trk.Sectors[index];
				// does the requested sector match the current sector
				if (next.SectorIDInfo.C == ActiveCommandParams.Cylinder
					&& next.SectorIDInfo.H == ActiveCommandParams.Head
					&& next.SectorIDInfo.R == ActiveCommandParams.Sector
					&& next.SectorIDInfo.N == ActiveCommandParams.SectorSize)
				{
					// sector has been found
					sector = next;

					UnSetBit(SR2_BC, ref Status2);
					UnSetBit(SR2_WC, ref Status2);
					break;
				}

				// check for bad cylinder
				if (next.SectorIDInfo.C is 255)
				{
					SetBit(SR2_BC, ref Status2);
				}
				// check for no cylinder
				else if (next.SectorIDInfo.C != ActiveCommandParams.Cylinder)
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

		/// <summary>
		/// Holds specfic state information about a drive
		/// </summary>
		private sealed class DriveState : IFDDHost
		{
			/// <summary>
			/// The drive ID from an FDC perspective
			/// </summary>
			public int ID;

			/// <summary>
			/// Signs whether this drive ready
			/// TRUE if both drive exists and has a disk inserted
			/// </summary>
			public bool FLAG_READY
			{
				get
				{
					if (!FDD_IsDiskLoaded || Disk.GetTrackCount() == 0 || !FDC.FDD_FLAG_MOTOR)
						return false;
					else
						return true;
				}
			}

			/// <summary>
			/// Disk is write protected (TRUE BY DEFAULT)
			/// </summary>
			public bool FLAG_WRITEPROTECT = false;

			/// <summary>
			/// Storage for seek steps
			/// One step for each indexpulse (track index) until seeked track
			/// </summary>
			public int SeekCounter;

			/// <summary>
			/// Seek status
			/// </summary>
			public int SeekStatus;

			/// <summary>
			/// Age counter
			/// </summary>
			public int SeekAge;

			/// <summary>
			/// The current side
			/// </summary>
			public byte CurrentSide;

			/// <summary>
			/// The current track index in the DiskTracks array
			/// </summary>
			public byte TrackIndex;

			/// <summary>
			/// The track ID of the current cylinder
			/// </summary>
			public byte CurrentTrackID
			{
				get
				{
					// default invalid track
					int id = 0xff;

					if (Disk == null)
						return (byte)id;

					if (Disk.DiskTracks.Length == 0)
						return (byte)id;

					if (TrackIndex >= Disk.GetTrackCount())
						TrackIndex = 0;
					else if (TrackIndex < 0)
						TrackIndex = 0;

					var track = Disk.DiskTracks[TrackIndex];

					id = track.TrackNumber;

					return (byte)id;
				}
				set
				{
					for (int i = 0; i < Disk.GetTrackCount(); i++)
					{
						if (Disk.DiskTracks[i].TrackNumber == value)
						{
							TrackIndex = (byte)i;
							break;
						}
					}
				}
			}


			/// <summary>
			/// The new track that the drive is seeking to
			/// (used in seek operations)
			/// </summary>
			public int SeekingTrack;

			/// <summary>
			/// The current sector index in the Sectors array
			/// </summary>
			public int SectorIndex;

			/// <summary>
			/// The currently loaded floppy disk
			/// </summary>
			public FloppyDisk Disk { get; set; }

			/// <summary>
			/// The parent controller
			/// </summary>
			private readonly NECUPD765 FDC;

			/// <summary>
			/// TRUE if we are on track 0
			/// </summary>
			public bool FLAG_TRACK0 => TrackIndex == 0;

			/*
            /// <summary>
            /// Moves the head across the disk cylinders
            /// </summary>
            public void MoveHead(SkipDirection direction, int cylinderCount)
            {
                // get total tracks
                int trackCount = Disk.DiskTracks.Length;

                int trk = 0;

                switch (direction)
                {
                    case SkipDirection.Increment:
                        trk = (int)CurrentTrack + cylinderCount;
                        if (trk >= trackCount)
                        {
                            // past the last track
                            trk = trackCount - 1;
                        }
                        else if (trk < 0)
                            trk = 0;
                        break;
                    case SkipDirection.Decrement:
                        trk = (int)CurrentTrack - cylinderCount;
                        if (trk < 0)
                        {
                            // before the first track
                            trk = 0;
                        }
                        else if (trk >= trackCount)
                            trk = trackCount - 1;
                        break;
                }

                // move the head
                CurrentTrack = (byte)trk;
            }
            */

			/*

            /// <summary>
            /// Finds a supplied sector
            /// </summary>
            public FloppyDisk.Sector FindSector(ref byte[] resBuffer, CommandParameters prms)
            {
                int index =CurrentSector;
                int lc = 0;
                FloppyDisk.Sector sector = null;

                bool found = false;

                do
                {
                    sector = Disk.DiskTracks[CurrentTrack].Sectors[index];
                    if (sector != null && sector.SectorID == prms.Sector)
                    {
                        // sector found
                        // check for data errors
                        if ((sector.Status1 & 0x20) != 0 || (sector.Status2 & 0x20) != 0)
                        {
                            // data errors found
                        }
                        found = true;
                        break;
                    }

                    // sector doesnt match
                    var c = Disk.DiskTracks[CurrentTrack].Sectors[index].TrackNumber;
                    if (c == 255)
                    {
                        // bad cylinder
                        resBuffer[RS_ST2] |= 0x02;
                    }
                    else if (prms.Cylinder != c)
                    {
                        // cylinder mismatch
                        resBuffer[RS_ST2] |= 0x10;
                    }

                    // increment index
                    index++;

                    if (index >= Disk.DiskTracks[CurrentTrack].NumberOfSectors)
                    {
                        // out of bounds
                        index = 0;
                        lc++;
                    }

                } while (lc < 2);

                if ((resBuffer[RS_ST2] & 0x02) != 0)
                {
                    // bad cylinder set - remove no cylinder
                    UnSetBit(SR2_WC, ref resBuffer[RS_ST2]);
                }

                // update current sector
                CurrentSector = index;

                if (found)
                    return sector;
                else
                    return null;
            }
      

            /// <summary>
            /// Populates a result buffer
            /// </summary>
            public void FillResult(ref byte[] resBuffer, CHRN chrn)
            {
                // clear results
                resBuffer[RS_ST0] = 0;
                resBuffer[RS_ST1] = 0;
                resBuffer[RS_ST2] = 0;
                resBuffer[RS_C] = 0;
                resBuffer[RS_H] = 0;
                resBuffer[RS_R] = 0;
                resBuffer[RS_N] = 0;

                if (chrn == null)
                {
                    // no chrn supplied
                    resBuffer[RS_ST0] = ST0;
                    resBuffer[RS_ST1] = 0;
                    resBuffer[RS_ST2] = 0;
                    resBuffer[RS_C] = 0;
                    resBuffer[RS_H] = 0;
                    resBuffer[RS_R] = 0;
                    resBuffer[RS_N] = 0;
                }
            }



            /// <summary>
            /// Populates the result buffer with ReadID data
            /// </summary>
            public void ReadID(ref byte[] resBuffer)
            {
                if (CheckDriveStatus() == false)
                {
                    // drive not ready
                    resBuffer[RS_ST0] = ST0;
                    return;
                }

                var track = Disk.DiskTracks.Where(a => a.TrackNumber == CurrentTrack).FirstOrDefault();

                if (track != null && track.NumberOfSectors > 0)
                {
                    // formatted track

                    // get the current sector
                    int index = CurrentSector;

                    // is the index out of bounds?
                    if (index >= track.NumberOfSectors)
                    {
                        // reset the index
                        index = 0;
                    }

                    // read the sector data
                    var data = track.Sectors[index];
                    resBuffer[RS_C] = data.TrackNumber;
                    resBuffer[RS_H] = data.SideNumber;
                    resBuffer[RS_R] = data.SectorID;
                    resBuffer[RS_N] = data.SectorSize;

                    resBuffer[RS_ST0] = ST0;

                    // increment the current sector
                    CurrentSector = index + 1;
                    return;
                }
                else
                {
                    // unformatted track?
                    resBuffer[RS_C] = FDC.CommBuffer[CM_C];
                    resBuffer[RS_H] = FDC.CommBuffer[CM_H];
                    resBuffer[RS_R] = FDC.CommBuffer[CM_R];
                    resBuffer[RS_N] = FDC.CommBuffer[CM_N];

                    SetBit(SR0_IC0, ref ST0);
                    resBuffer[RS_ST0] = ST0;
                    resBuffer[RS_ST1] = 0x01;
                    return;
                }
            }
            */

			/*

            /// <summary>
            /// The drive performs a seek operation if necessary
            /// Return value TRUE indicates seek complete
            /// </summary>
            public void DoSeek()
            {
                if (CurrentState != DriveMainState.Recalibrate &&
                    CurrentState != DriveMainState.Seek)
                {
                    // no seek/recalibrate has been asked for
                    return;
                }

                if (GetBit(ID, FDC.StatusMain))
                {
                    // drive is already seeking
                    return;
                }

                RunSeekCycle();
            }

            /// <summary>
            /// Runs a seek cycle
            /// </summary>
            public void RunSeekCycle()
            {
                for (;;)
                {
                    switch (SeekState)
                    {
                        // seek or recalibrate has been requested
                        case SeekSubState.Idle:

                            if (CurrentState == DriveMainState.Recalibrate)
                            {
                                // recalibrate always seeks to track 0
                                SeekingTrack = 0;
                            }
                            SeekState = SeekSubState.MoveInit;

                            // mark drive as busy
                            // this should be cleared by SIS command
                            SetBit(ID, ref FDC.StatusMain);

                            break;

                        // setup for the head move
                        case SeekSubState.MoveInit:

                            if (CurrentTrack == SeekingTrack)
                            {
                                // we are already at the required track
                                if (CurrentState == DriveMainState.Recalibrate &&
                                    !FLAG_TRACK0)
                                {
                                    // recalibration fail
                                    SeekIntState = SeekIntStatus.Abnormal;

                                    // raise seek interrupt
                                    FDC.ActiveInterrupt = InterruptState.Seek;

                                    // unset DB bit
                                    UnSetBit(ID, ref FDC.StatusMain);

                                    // equipment check
                                    SetBit(SR0_EC, ref FDC.Status0);

                                    SeekState = SeekSubState.PerformCompletion;
                                    break;
                                }

                                if (CurrentState == DriveMainState.Recalibrate &&
                                    FLAG_TRACK0)
                                {
                                    // recalibration success
                                    SeekIntState = SeekIntStatus.Normal;

                                    // raise seek interrupt
                                    FDC.ActiveInterrupt = InterruptState.Seek;

                                    // unset DB bit
                                    UnSetBit(ID, ref FDC.StatusMain);

                                    SeekState = SeekSubState.PerformCompletion;
                                    break;
                                }
                            }

                            // check for error
                            if (IntStatus >= IC_ABORTED_DISCREMOVED || Disk == null)
                            {
                                // drive not ready
                                FLAG_READY = false;

                                // drive not ready
                                SeekIntState = SeekIntStatus.DriveNotReady;

                                // cancel any interrupt
                                FDC.ActiveInterrupt = InterruptState.None;

                                // unset DB bit
                                UnSetBit(ID, ref FDC.StatusMain);

                                SeekState = SeekSubState.PerformCompletion;
                                break;
                            }

                            if (SeekCounter > 1)
                            {
                                // not ready to seek yet
                                SeekCounter--;
                                return;
                            }

                            if (FDC.SRT < 1 && CurrentTrack != SeekingTrack)
                            {
                                SeekState = SeekSubState.MoveImmediate;
                                break;
                            }

                            // head move
                            SeekState = SeekSubState.HeadMove;

                            break;

                        case SeekSubState.HeadMove:

                            // do the seek
                            SeekCounter = FDC.SRT;

                            if (CurrentTrack < SeekingTrack)
                            {
                                // we are seeking forward
                                var delta = SeekingTrack - CurrentTrack;
                                MoveHead(SkipDirection.Increment, 1);
                            }
                            else if (CurrentTrack > SeekingTrack)
                            {
                                // we are seeking backward
                                var delta = CurrentTrack - SeekingTrack;
                                MoveHead(SkipDirection.Decrement, 1);
                            }

                            // should the seek be completed now?
                            if (CurrentTrack == SeekingTrack)
                            {
                                SeekState = SeekSubState.PerformCompletion;
                                break;
                            }

                            // seek not finished yet
                            return;

                        // seek emulation processed immediately
                        case SeekSubState.MoveImmediate:
                            
                            if (CurrentTrack < SeekingTrack)
                            {
                                // we are seeking forward
                                var delta = SeekingTrack - CurrentTrack;
                                MoveHead(SkipDirection.Increment, delta);

                            }
                            else if (CurrentTrack > SeekingTrack)
                            {
                                // we are seeking backward
                                var delta = CurrentTrack - SeekingTrack;
                                MoveHead(SkipDirection.Decrement, delta);
                            }

                            SeekState = SeekSubState.PerformCompletion;
                            break;

                        case SeekSubState.PerformCompletion:
                            SeekDone();
                            SeekState = SeekSubState.SeekCompleted;
                            break;

                        case SeekSubState.SeekCompleted:
                            // seek has already completed
                            return;
                    }
                }
            }

            /// <summary>
            /// Called when a seek operation has completed
            /// </summary>
            public void SeekDone()
            {
                SeekCounter = 0;
                SeekingTrack = CurrentTrack;

                // generate ST0 register data

                // get only the IC bits
                IntStatus &= IC_ABORTED_DISCREMOVED;

                // drive ready?
                if (!FLAG_READY)
                {
                    SetBit(SR0_NR, ref IntStatus);
                    SetBit(SR0_EC, ref IntStatus);

                    // are we recalibrating?
                    if (CurrentState == DriveMainState.Recalibrate)
                    {
                        SetBit(SR0_EC, ref IntStatus);
                    }
                }                    

                // set seek end
                SetBit(SR0_SE, ref IntStatus);                
                /*
                // head address
                if (CurrentSide > 0)
                {
                    SetBit(SR0_HD, ref IntStatus);

                    // drive only supports 1 head
                    // set the EC bit
                    SetBit(SR0_EC, ref IntStatus);
                }
                */
			/*
			// UnitSelect
			SetUnitSelect(ID, ref IntStatus);

			// move to none state
			//CurrentState = DriveMainState.None;

			//SeekState = SeekSubState.SeekCompleted;

			// set the seek interrupt flag for this drive
			// this will be cleared at the next successful senseint
			FLAG_SEEK_INTERRUPT = true;

			//CurrentState = DriveMainState.None;

		}
	*/

			public DriveState(int driveID, NECUPD765 fdc)
			{
				ID = driveID;
				FDC = fdc;
			}

			/// <summary>
			/// Parses a new disk image and loads it into this floppy drive
			/// </summary>
			public void FDD_LoadDisk(byte[] diskData)
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
					throw new Exception($"{nameof(DriveState)}\n\nDisk image file could not be parsed. Potentially an unknown format.");
				}
			}

			/// <summary>
			/// Ejects the current disk
			/// </summary>
			public void FDD_EjectDisk()
			{
				Disk = null;
				//FLAG_READY = false;
			}

			/// <summary>
			/// Signs whether the current active drive has a disk inserted
			/// </summary>        
			public bool FDD_IsDiskLoaded
			{
				get
				{
					if (Disk != null)
						return true;
					else
						return false;
				}
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(ID), ref ID);
				ser.Sync(nameof(FLAG_WRITEPROTECT), ref FLAG_WRITEPROTECT);
				//ser.Sync(nameof(FLAG_DISKCHANGED), ref FLAG_DISKCHANGED);
				//ser.Sync(nameof(FLAG_RECALIBRATING), ref FLAG_RECALIBRATING);
				//ser.Sync(nameof(FLAG_SEEK_INTERRUPT), ref FLAG_SEEK_INTERRUPT);
				//ser.Sync(nameof(IntStatus), ref IntStatus);
				//ser.Sync(nameof(ST0), ref ST0);
				//ser.Sync(nameof(RecalibrationCounter), ref RecalibrationCounter);
				ser.Sync(nameof(SeekCounter), ref SeekCounter);
				ser.Sync(nameof(SeekStatus), ref SeekStatus);
				ser.Sync(nameof(SeekAge), ref SeekAge);
				ser.Sync(nameof(CurrentSide), ref CurrentSide);
				//ser.Sync(nameof(CurrentTrack), ref CurrentTrack);
				ser.Sync(nameof(TrackIndex), ref TrackIndex);
				ser.Sync(nameof(SeekingTrack), ref SeekingTrack);
				//ser.Sync(nameof(CurrentSector), ref CurrentSector);
				ser.Sync(nameof(SectorIndex), ref SectorIndex);
				//ser.Sync(nameof(RAngles), ref RAngles);
				//ser.Sync(nameof(DataPointer), ref DataPointer);
				//ser.SyncEnum(nameof(CurrentState), ref CurrentState);
				//ser.SyncEnum(nameof(SeekState), ref SeekState);
				//ser.SyncEnum(nameof(SeekIntState), ref SeekIntState);
			}
		}
	}
}
