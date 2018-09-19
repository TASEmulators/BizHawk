using BizHawk.Common.NumberExtensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// FDC State and Methods
    /// </summary>
    #region Attribution
    /*
        Implementation based on the information contained here:
        http://www.cpcwiki.eu/index.php/765_FDC
        and here:
        http://www.cpcwiki.eu/imgs/f/f3/UPD765_Datasheet_OCRed.pdf
    */
    #endregion
    public partial class NECUPD765
    {
        #region Controller State

        /// <summary>
        /// Signs whether the drive is active
        /// </summary>
        public bool DriveLight;

        /// <summary>
        /// Collection of possible commands
        /// </summary>
        private List<Command> CommandList;

        /// <summary>
        /// State parameters relating to the Active command
        /// </summary>
        public CommandParameters ActiveCommandParams = new CommandParameters();

        /// <summary>
        /// The current active phase of the controller
        /// </summary>
        private Phase ActivePhase = Phase.Command;

        /// <summary>
        /// The currently active interrupt
        /// </summary>
        private InterruptState ActiveInterrupt = InterruptState.None;
        /// <summary>
        /// Command buffer
        /// This does not contain the initial command byte (only parameter bytes)
        /// </summary>
        private byte[] CommBuffer = new byte[9];

        /// <summary>
        /// Current index within the command buffer
        /// </summary>
        private int CommCounter = 0;

        /// <summary>
        /// Initial command byte flag
        /// Bit7  Multi Track (continue multi-sector-function on other head)
        /// </summary>
        private bool CMD_FLAG_MT;

        /// <summary>
        /// Initial command byte flag
        /// Bit6  MFM-Mode-Bit (Default 1=Double Density)
        /// </summary>
        private bool CMD_FLAG_MF;

        /// <summary>
        /// Initial command byte flag
        /// Bit5  Skip-Bit (set if secs with deleted DAM shall be skipped)
        /// </summary>
        private bool CMD_FLAG_SK;

        /// <summary>
        /// Step Rate Time (supplied via the specify command)
        /// SRT stands for the steooino rate for the FDD ( 1 to 16 ms in 1 ms increments). 
        /// Stepping rate applies to all drives(FH= 1ms, EH= 2ms, etc.).
        /// </summary>
        private int SRT;

        /// <summary>
        /// Keeps track of the current SRT state
        /// </summary>
        private int SRT_Counter;

        /// <summary>
        /// Head Unload Time (supplied via the specify command)
        /// HUT stands for the head unload time after a Read or Write operation has occurred 
        /// (16 to 240 ms in 16 ms Increments)
        /// </summary>
        private int HUT;

        /// <summary>
        /// Keeps track of the current HUT state
        /// </summary>
        private int HUT_Counter;

        /// <summary>
        /// Head load Time (supplied via the specify command)
        /// HLT stands for the head load time in the FDD (2 to 254 ms in 2 ms Increments)
        /// </summary>
        private int HLT;

        /// <summary>
        /// Keeps track of the current HLT state
        /// </summary>
        private int HLT_Counter;

        /// <summary>
        /// Non-DMA Mode (supplied via the specify command)
        /// ND stands for operation in the non-DMA mode
        /// </summary>
        private bool ND;

        /// <summary>
        /// In lieu of actual timing, this will count status reads in execution phase
        /// where the CPU hasnt actually read any bytes
        /// </summary>
        private int OverrunCounter;

        /// <summary>
        /// Contains result bytes in result phase
        /// </summary>
        private byte[] ResBuffer = new byte[7];

        /// <summary>
        /// Contains sector data to be written/read in execution phase
        /// </summary>
        private byte[] ExecBuffer = new byte[0x8000];

        /// <summary>
        /// Interrupt result buffer
        /// Persists (and returns when needed) the last result data when a sense interrupt status command happens
        /// </summary>
        private byte[] InterruptResultBuffer = new byte[2];

        /// <summary>
        /// Current index within the result buffer
        /// </summary>
        private int ResCounter = 0;

        /// <summary>
        /// The byte length of the currently active command
        /// This may or may not be the same as the actual command resultbytes value
        /// </summary>
        private int ResLength = 0;

        /// <summary>
        /// Index for sector data within the result buffer
        /// </summary>
        private int ExecCounter = 0;

        /// <summary>
        /// The length of the current exec command
        /// </summary>
        private int ExecLength = 0;

        /// <summary>
        /// The last write byte that was received during execution phase
        /// </summary>
        private byte LastSectorDataWriteByte = 0;

        /// <summary>
        /// The last read byte to be sent during execution phase
        /// </summary>
        private byte LastSectorDataReadByte = 0;

        /// <summary>
        /// The last parameter byte that was written to the FDC
        /// </summary>
        private byte LastByteReceived = 0;

        /// <summary>
        /// Delay for reading sector
        /// </summary>
        private int SectorDelayCounter = 0;

        /// <summary>
        /// The phyical sector ID
        /// </summary>
        private int SectorID = 0;

        /// <summary>
        /// Counter for index pulses
        /// </summary>
        private int IndexPulseCounter;

        /// <summary>
        /// Specifies the index of the currently selected command (in the CommandList)
        /// </summary>
        public int CMDIndex
        {
            get { return _cmdIndex; }
            set
            {
                _cmdIndex = value;
                ActiveCommand = CommandList[_cmdIndex];
            }
        }
        private int _cmdIndex;

        /// <summary>
        /// The currently active command
        /// </summary>
        private Command ActiveCommand;

        /// <summary>
        /// Main status register (accessed via reads to port 0x2ffd)
        /// </summary>
        /*
             b0..3  DB  FDD0..3 Busy (seek/recalib active, until succesful sense intstat)
             b4     CB  FDC Busy (still in command-, execution- or result-phase)
             b5     EXM Execution Mode (still in execution-phase, non_DMA_only)
             b6     DIO Data Input/Output (0=CPU->FDC, 1=FDC->CPU) (see b7)
             b7     RQM Request For Master (1=ready for next byte) (see b6 for direction)
        */
        private byte StatusMain;

        /// <summary>
        /// Status Register 0
        /// </summary>
        /*
            b0,1   US  Unit Select (driveno during interrupt)
            b2     HD  Head Address (head during interrupt)
            b3     NR  Not Ready (drive not ready or non-existing 2nd head selected)
            b4     EC  Equipment Check (drive failure or recalibrate failed (retry))
            b5     SE  Seek End (Set if seek-command completed)
            b6,7   IC  Interrupt Code (0=OK, 1=aborted:readfail/OK if EN, 2=unknown cmd
                    or senseint with no int occured, 3=aborted:disc removed etc.)
        */
        private byte Status0;

        /// <summary>
        /// Status Register 1
        /// </summary>
        /*
            b0     MA  Missing Address Mark (Sector_ID or DAM not found)
            b1     NW  Not Writeable (tried to write/format disc with wprot_tab=on)
            b2     ND  No Data (Sector_ID not found, CRC fail in ID_field)
            b3,6   0   Not used
            b4     OR  Over Run (CPU too slow in execution-phase (ca. 26us/Byte))
            b5     DE  Data Error (CRC-fail in ID- or Data-Field)
            b7     EN  End of Track (set past most read/write commands) (see IC)
        */
        private byte Status1;

        /// <summary>
        /// Status Register 2
        /// </summary>
        /*
            b0     MD  Missing Address Mark in Data Field (DAM not found)
            b1     BC  Bad Cylinder (read/programmed track-ID different and read-ID = FF)
            b2     SN  Scan Not Satisfied (no fitting sector found)
            b3     SH  Scan Equal Hit (equal)
            b4     WC  Wrong Cylinder (read/programmed track-ID different) (see b1)
            b5     DD  Data Error in Data Field (CRC-fail in data-field)
            b6     CM  Control Mark (read/scan command found sector with deleted DAM)
            b7     0   Not Used
        */
        private byte Status2;

        /// <summary>
        /// Status Register 3
        /// </summary>
        /*
            b0,1   US  Unit Select (pin 28,29 of FDC)
            b2     HD  Head Address (pin 27 of FDC)
            b3     TS  Two Side (0=yes, 1=no (!))
            b4     T0  Track 0 (on track 0 we are)
            b5     RY  Ready (drive ready signal)
            b6     WP  Write Protected (write protected)
            b7     FT  Fault (if supported: 1=Drive failure)
        */
        private byte Status3;

        #endregion

        #region UPD Internal Functions

        #region READ Commands

        /// <summary>
        /// Read Data
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data transfer between FDD and FDC
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ReadData()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received - setup for execution phase

                        // clear exec buffer and status registers
                        ClearExecBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // temp sector index
                        byte secIdx = ActiveCommandParams.Sector;

                        // hack for when another drive (non-existent) is being called
                        if (ActiveDrive.ID != 0)
                            DiskDriveIndex = 0; 

                        // do we have a valid disk inserted?
                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        int buffPos = 0;
                        int sectorSize = 0;
                        int maxTransferCap = 0;

                        // calculate requested size of data required
                        if (ActiveCommandParams.SectorSize == 0)
                        {
                            // When N=0, then DTL defines the data length which the FDC must treat as a sector. If DTL is smaller than the actual 
                            // data length in a sector, the data beyond DTL in the sector is not sent to the Data Bus. The FDC reads (internally) 
                            // the complete sector performing the CRC check and, depending upon the manner of command termination, may perform 
                            // a Multi-Sector Read Operation.
                            sectorSize = ActiveCommandParams.DTL;

                            // calculate maximum transfer capacity
                            if (!CMD_FLAG_MF)
                                maxTransferCap = 3328;
                        }
                        else
                        {
                            // When N is non - zero, then DTL has no meaning and should be set to ffh
                            ActiveCommandParams.DTL = 0xFF;

                            // calculate maximum transfer capacity
                            switch (ActiveCommandParams.SectorSize)
                            {
                                case 1:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 6656;
                                    else
                                        maxTransferCap = 3840;
                                    break;
                                case 2:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 7680;
                                    else
                                        maxTransferCap = 4096;
                                    break;
                                case 3:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 8192;
                                    else
                                        maxTransferCap = 4096;
                                    break;
                            }

                            sectorSize = 0x80 << ActiveCommandParams.SectorSize;
                        }

                        // get the current track
                        var track = ActiveDrive.Disk.DiskTracks.Where(a => a.TrackNumber == ActiveDrive.CurrentTrackID).FirstOrDefault();

                        if (track == null || track.NumberOfSectors <= 0)
                        {
                            // track could not be found
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();

                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        FloppyDisk.Sector sector = null;

                        // sector read loop
                        for (;;)
                        {
                            bool terminate = false;

                            // lookup the sector
                            sector = GetSector();  

                            if (sector == null)
                            {
                                // sector was not found after two passes of the disk index hole
                                SetBit(SR1_ND, ref Status1);
                                SetBit(SR0_IC0, ref Status0);
                                UnSetBit(SR0_IC1, ref Status0);

                                // result requires the actual track id, rather than the sector track id
                                ActiveCommandParams.Cylinder = track.TrackNumber;

                                CommitResultCHRN();
                                CommitResultStatus();
                                ActivePhase = Phase.Result;
                                break;
                            }

                            // sector ID was found on this track

                            // get status regs from sector
                            Status1 = sector.Status1;
                            Status2 = sector.Status2;

                            // we dont need EN
                            UnSetBit(SR1_EN, ref Status1);

                            // If SK=1, the FDC skips the sector with the Deleted Data Address Mark and reads the next sector. 
                            // The CRC bits in the deleted data field are not checked when SK=1
                            if (CMD_FLAG_SK && Status2.Bit(SR2_CM))
                            {
                                if (ActiveCommandParams.Sector != ActiveCommandParams.EOT)
                                {
                                    // increment the sector ID and search again
                                    ActiveCommandParams.Sector++;
                                    continue;
                                }
                                else
                                {
                                    // no execution phase
                                    SetBit(SR0_IC0, ref Status0);
                                    UnSetBit(SR0_IC1, ref Status0);

                                    // result requires the actual track id, rather than the sector track id
                                    ActiveCommandParams.Cylinder = track.TrackNumber;

                                    CommitResultCHRN();
                                    CommitResultStatus();
                                    ActivePhase = Phase.Result;
                                    break;
                                }
                            }

                            // read the sector
                            for (int i = 0; i < sector.DataLen; i++)
                            {
                                ExecBuffer[buffPos++] = sector.ActualData[i];
                            }

                            // mark the sector read
                            sector.SectorReadCompleted();

                            // any CRC errors?
                            if (Status1.Bit(SR1_DE) || Status2.Bit(SR2_DD))
                            {
                                SetBit(SR0_IC0, ref Status0);
                                UnSetBit(SR0_IC1, ref Status0);
                                terminate = true;
                            }

                            if (!CMD_FLAG_SK && Status2.Bit(SR2_CM))
                            {
                                // deleted address mark was detected with NO skip flag set
                                ActiveCommandParams.EOT = ActiveCommandParams.Sector;
                                SetBit(SR2_CM, ref Status2);
                                SetBit(SR0_IC0, ref Status0);
                                UnSetBit(SR0_IC1, ref Status0);
                                terminate = true;
                            }

                            if (sector.SectorID == ActiveCommandParams.EOT || terminate)
                            {
                                // this was the last sector to read
                                // or termination requested

                                SetBit(SR1_EN, ref Status1);

                                int keyIndex = 0;
                                for (int i = 0; i < track.Sectors.Length; i++)
                                {
                                    if (track.Sectors[i].SectorID == sector.SectorID)
                                    {
                                        keyIndex = i;
                                        break;
                                    }
                                }

                                if (keyIndex == track.Sectors.Length - 1)
                                {
                                    // last sector on the cylinder, set EN
                                    SetBit(SR1_EN, ref Status1);

                                    // increment cylinder
                                    ActiveCommandParams.Cylinder++;

                                    // reset sector
                                    ActiveCommandParams.Sector = sector.SectorID; // 1;
                                    ActiveDrive.SectorIndex = 0;
                                }
                                else
                                {
                                    ActiveDrive.SectorIndex++;
                                }

                                UnSetBit(SR0_IC1, ref Status0);
                                if (terminate)
                                    SetBit(SR0_IC0, ref Status0);
                                else
                                    UnSetBit(SR0_IC0, ref Status0);

                                SetBit(SR0_IC0, ref Status0);

                                // result requires the actual track id, rather than the sector track id
                                ActiveCommandParams.Cylinder = track.TrackNumber;

                                CommitResultCHRN();
                                CommitResultStatus();
                                ActivePhase = Phase.Execution;
                                break;
                            }
                            else
                            {
                                // continue with multi-sector read operation
                                ActiveCommandParams.Sector++;
                                //ActiveDrive.SectorIndex++;
                            }
                        }

                        if (ActivePhase == Phase.Execution)
                        {
                            ExecLength = buffPos;
                            ExecCounter = buffPos;

                            DriveLight = true;
                        }
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:

                    var index = ExecLength - ExecCounter;

                    LastSectorDataReadByte = ExecBuffer[index];

                    OverrunCounter--;
                    ExecCounter--;                    

                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Read Deleted Data
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data transfer between the FDD and FDC
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ReadDeletedData()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received - setup for execution phase

                        // clear exec buffer and status registers
                        ClearExecBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // temp sector index
                        byte secIdx = ActiveCommandParams.Sector;

                        // do we have a valid disk inserted?
                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        int buffPos = 0;
                        int sectorSize = 0;
                        int maxTransferCap = 0;

                        // calculate requested size of data required
                        if (ActiveCommandParams.SectorSize == 0)
                        {
                            // When N=0, then DTL defines the data length which the FDC must treat as a sector. If DTL is smaller than the actual 
                            // data length in a sector, the data beyond DTL in the sector is not sent to the Data Bus. The FDC reads (internally) 
                            // the complete sector performing the CRC check and, depending upon the manner of command termination, may perform 
                            // a Multi-Sector Read Operation.
                            sectorSize = ActiveCommandParams.DTL;

                            // calculate maximum transfer capacity
                            if (!CMD_FLAG_MF)
                                maxTransferCap = 3328;
                        }
                        else
                        {
                            // When N is non - zero, then DTL has no meaning and should be set to ffh
                            ActiveCommandParams.DTL = 0xFF;

                            // calculate maximum transfer capacity
                            switch (ActiveCommandParams.SectorSize)
                            {
                                case 1:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 6656;
                                    else
                                        maxTransferCap = 3840;
                                    break;
                                case 2:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 7680;
                                    else
                                        maxTransferCap = 4096;
                                    break;
                                case 3:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 8192;
                                    else
                                        maxTransferCap = 4096;
                                    break;
                            }

                            sectorSize = 0x80 << ActiveCommandParams.SectorSize;
                        }

                        // get the current track
                        var track = ActiveDrive.Disk.DiskTracks.Where(a => a.TrackNumber == ActiveDrive.CurrentTrackID).FirstOrDefault();

                        if (track == null || track.NumberOfSectors <= 0)
                        {
                            // track could not be found
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();

                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        FloppyDisk.Sector sector = null;

                        // sector read loop
                        for (;;)
                        {
                            bool terminate = false;

                            // lookup the sector
                            sector = GetSector();

                            if (sector == null)
                            {
                                // sector was not found after two passes of the disk index hole
                                SetBit(SR1_ND, ref Status1);
                                SetBit(SR0_IC0, ref Status0);
                                UnSetBit(SR0_IC1, ref Status0);

                                // result requires the actual track id, rather than the sector track id
                                ActiveCommandParams.Cylinder = track.TrackNumber;

                                CommitResultCHRN();
                                CommitResultStatus();
                                ActivePhase = Phase.Result;
                                break;
                            }

                            // sector ID was found on this track

                            // get status regs from sector
                            Status1 = sector.Status1;
                            Status2 = sector.Status2;

                            // we dont need EN
                            UnSetBit(SR1_EN, ref Status1);

                            // invert CM for read deleted data command
                            if (Status2.Bit(SR2_CM))
                                UnSetBit(SR2_CM, ref Status2);
                            else
                                SetBit(SR2_CM, ref Status2);

                            // skip flag is set and no DAM found
                            if (CMD_FLAG_SK && Status2.Bit(SR2_CM))
                            {
                                if (ActiveCommandParams.Sector != ActiveCommandParams.EOT)
                                {
                                    // increment the sector ID and search again
                                    ActiveCommandParams.Sector++;
                                    continue;
                                }
                                else
                                {
                                    // no execution phase
                                    SetBit(SR0_IC0, ref Status0);
                                    UnSetBit(SR0_IC1, ref Status0);

                                    // result requires the actual track id, rather than the sector track id
                                    ActiveCommandParams.Cylinder = track.TrackNumber;

                                    CommitResultCHRN();
                                    CommitResultStatus();
                                    ActivePhase = Phase.Result;
                                    break;
                                }
                            }
                            // we can read this sector
                            else
                            {
                                // if DAM is not set this will be the last sector to read
                                if (Status2.Bit(SR2_CM))
                                {
                                    ActiveCommandParams.EOT = ActiveCommandParams.Sector;
                                }

                                if (!CMD_FLAG_SK && !Status2.Bit(SR2_CM) && 
                                    ActiveDrive.Disk.Protection == ProtectionType.PaulOwens)
                                {
                                    ActiveCommandParams.EOT = ActiveCommandParams.Sector;
                                    SetBit(SR2_CM, ref Status2);
                                    SetBit(SR0_IC0, ref Status0);
                                    UnSetBit(SR0_IC1, ref Status0);
                                    terminate = true;
                                }

                                // read the sector
                                for (int i = 0; i < sectorSize; i++)
                                {
                                    ExecBuffer[buffPos++] = sector.ActualData[i];
                                }

                                // mark the sector read
                                sector.SectorReadCompleted();

                                if (sector.SectorID == ActiveCommandParams.EOT)
                                {
                                    // this was the last sector to read

                                    SetBit(SR1_EN, ref Status1);

                                    int keyIndex = 0;
                                    for (int i = 0; i < track.Sectors.Length; i++)
                                    {
                                        if (track.Sectors[i].SectorID == sector.SectorID)
                                        {
                                            keyIndex = i;
                                            break;
                                        }
                                    }

                                    if (keyIndex == track.Sectors.Length - 1)
                                    {
                                        // last sector on the cylinder, set EN
                                        SetBit(SR1_EN, ref Status1);

                                        // increment cylinder
                                        ActiveCommandParams.Cylinder++;

                                        // reset sector
                                        ActiveCommandParams.Sector = 1;
                                        ActiveDrive.SectorIndex = 0;
                                    }
                                    else
                                    {
                                        ActiveDrive.SectorIndex++;
                                    }

                                    UnSetBit(SR0_IC1, ref Status0);
                                    if (terminate)
                                        SetBit(SR0_IC0, ref Status0);
                                    else
                                        UnSetBit(SR0_IC0, ref Status0);

                                    SetBit(SR0_IC0, ref Status0);

                                    // result requires the actual track id, rather than the sector track id
                                    ActiveCommandParams.Cylinder = track.TrackNumber;

                                    // remove CM (appears to be required to defeat Alkatraz copy protection)
                                    UnSetBit(SR2_CM, ref Status2);

                                    CommitResultCHRN();
                                    CommitResultStatus();
                                    ActivePhase = Phase.Execution;
                                    break;
                                }
                                else
                                {
                                    // continue with multi-sector read operation
                                    ActiveCommandParams.Sector++;
                                    //ActiveDrive.SectorIndex++;
                                }
                            }
                        }

                        if (ActivePhase == Phase.Execution)
                        {
                            ExecLength = buffPos;
                            ExecCounter = buffPos;
                            DriveLight = true;
                        }
                    }
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    var index = ExecLength - ExecCounter;

                    LastSectorDataReadByte = ExecBuffer[index];

                    OverrunCounter--;
                    ExecCounter--;
                    
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Read Diagnostic (read track)
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data transfer between FDD and FDC. FDC reads all data fields from index hole to EDT
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ReadDiagnostic()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received - setup for execution phase

                        // clear exec buffer and status registers
                        ClearExecBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // temp sector index
                        byte secIdx = ActiveCommandParams.Sector;

                        // do we have a valid disk inserted?
                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        int buffPos = 0;
                        int sectorSize = 0;
                        int maxTransferCap = 0;

                        // calculate requested size of data required
                        if (ActiveCommandParams.SectorSize == 0)
                        {
                            // When N=0, then DTL defines the data length which the FDC must treat as a sector. If DTL is smaller than the actual 
                            // data length in a sector, the data beyond DTL in the sector is not sent to the Data Bus. The FDC reads (internally) 
                            // the complete sector performing the CRC check and, depending upon the manner of command termination, may perform 
                            // a Multi-Sector Read Operation.
                            sectorSize = ActiveCommandParams.DTL;

                            // calculate maximum transfer capacity
                            if (!CMD_FLAG_MF)
                                maxTransferCap = 3328;
                        }
                        else
                        {
                            // When N is non - zero, then DTL has no meaning and should be set to ffh
                            ActiveCommandParams.DTL = 0xFF;

                            // calculate maximum transfer capacity
                            switch (ActiveCommandParams.SectorSize)
                            {
                                case 1:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 6656;
                                    else
                                        maxTransferCap = 3840;
                                    break;
                                case 2:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 7680;
                                    else
                                        maxTransferCap = 4096;
                                    break;
                                case 3:
                                    if (CMD_FLAG_MF)
                                        maxTransferCap = 8192;
                                    else
                                        maxTransferCap = 4096;
                                    break;
                            }

                            sectorSize = 0x80 << ActiveCommandParams.SectorSize;
                        }

                        // get the current track
                        var track = ActiveDrive.Disk.DiskTracks.Where(a => a.TrackNumber == ActiveDrive.CurrentTrackID).FirstOrDefault();

                        if (track == null || track.NumberOfSectors <= 0)
                        {
                            // track could not be found
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();

                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        FloppyDisk.Sector sector = null;
                        ActiveDrive.SectorIndex = 0;

                        int secCount = 0;

                        // read the whole track
                        for (int i = 0; i < track.Sectors.Length; i++)
                        {
                            if (secCount >= ActiveCommandParams.EOT)
                            {
                                break;
                            }

                            var sec = track.Sectors[i];
                            for (int b = 0; b < sec.ActualData.Length; b++)
                            {
                                ExecBuffer[buffPos++] = sec.ActualData[b];
                            }

                            // mark the sector read
                            sec.SectorReadCompleted();

                            // end of sector - compare IDs
                            if (sec.TrackNumber != ActiveCommandParams.Cylinder ||
                                sec.SideNumber != ActiveCommandParams.Head ||
                                sec.SectorID != ActiveCommandParams.Sector ||
                                sec.SectorSize != ActiveCommandParams.SectorSize)
                            {
                                SetBit(SR1_ND, ref Status1);
                            }

                            secCount++;
                            ActiveDrive.SectorIndex = i;
                        }

                        if (secCount == ActiveCommandParams.EOT)
                        {
                            // this was the last sector to read
                            // or termination requested

                            int keyIndex = 0;
                            for (int i = 0; i < track.Sectors.Length; i++)
                            {
                                if (track.Sectors[i].SectorID == track.Sectors[ActiveDrive.SectorIndex].SectorID)
                                {
                                    keyIndex = i;
                                    break;
                                }
                            }

                            if (keyIndex == track.Sectors.Length - 1)
                            {
                                // last sector on the cylinder, set EN
                                SetBit(SR1_EN, ref Status1);

                                // increment cylinder
                                ActiveCommandParams.Cylinder++;

                                // reset sector
                                ActiveCommandParams.Sector = 1;
                                ActiveDrive.SectorIndex = 0;
                            }
                            else
                            {
                                ActiveDrive.SectorIndex++;
                            }

                            UnSetBit(SR0_IC1, ref Status0);
                            UnSetBit(SR0_IC0, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            ActivePhase = Phase.Execution;
                        }                            

                        if (ActivePhase == Phase.Execution)
                        {
                            ExecLength = buffPos;
                            ExecCounter = buffPos;

                            DriveLight = true;
                        }
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:

                    var index = ExecLength - ExecCounter;

                    LastSectorDataReadByte = ExecBuffer[index];

                    OverrunCounter--;
                    ExecCounter--;                    

                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Read ID
        /// COMMAND:    1 parameter byte
        /// EXECUTION:  The first correct ID information on the cylinder is stored in the data register
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ReadID()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        DriveLight = true;

                        // all parameter bytes received
                        ClearResultBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // set unit select
                        //SetUnitSelect(ActiveDrive.ID, ref Status0);

                        // HD should always be 0
                        UnSetBit(SR0_HD, ref Status0);

                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            // it is at this point the +3 detects whether a disk is present
                            // if not (and after another readid and SIS) it will eventually proceed to loading from tape
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            // setup the result buffer
                            ResBuffer[RS_ST0] = Status0;
                            for (int i = 1; i < 7; i++)
                                ResBuffer[i] = 0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        var track = ActiveDrive.Disk.DiskTracks.Where(a => a.TrackNumber == ActiveDrive.CurrentTrackID).FirstOrDefault();

                        if (track != null && track.NumberOfSectors > 0 && track.TrackNumber != 0xff)
                        {
                            // formatted track

                            // is the index out of bounds?
                            if (ActiveDrive.SectorIndex >= track.NumberOfSectors)
                            {
                                // reset the index
                                ActiveDrive.SectorIndex = 0;
                            }

                            if (ActiveDrive.SectorIndex == 0 && ActiveDrive.Disk.DiskTracks[ActiveDrive.CurrentTrackID].Sectors.Length > 1)
                            {
                                // looks like readid always skips the first sector on a track
                                ActiveDrive.SectorIndex++;
                            }

                            // read the sector data
                            var data = track.Sectors[ActiveDrive.SectorIndex]; //.GetCHRN();
                            ResBuffer[RS_C] = data.TrackNumber;
                            ResBuffer[RS_H] = data.SideNumber;
                            ResBuffer[RS_R] = data.SectorID;
                            ResBuffer[RS_N] = data.SectorSize;

                            ResBuffer[RS_ST0] = Status0;

                            // check for DAM & CRC
                            //if (data.Status2.Bit(SR2_CM))
                                //SetBit(SR2_CM, ref ResBuffer[RS_ST2]);


                            // increment the current sector
                            ActiveDrive.SectorIndex++;

                            // is the index out of bounds?
                            if (ActiveDrive.SectorIndex >= track.NumberOfSectors)
                            {
                                // reset the index
                                ActiveDrive.SectorIndex = 0;
                            }
                        }
                        else
                        {
                            // unformatted track?
                            CommitResultCHRN();

                            SetBit(SR0_IC0, ref Status0);
                            ResBuffer[RS_ST0] = Status0;
                            ResBuffer[RS_ST1] = 0x01;
                        }

                        ActivePhase = Phase.Result;
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        #endregion

        #region WRITE Commands

        /// <summary>
        /// Write Data
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data transfer between FDC and FDD
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_WriteData()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received - setup for execution phase

                        // clear exec buffer and status registers
                        ClearExecBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // temp sector index
                        byte secIdx = ActiveCommandParams.Sector;

                        // hack for when another drive (non-existent) is being called
                        if (ActiveDrive.ID != 0)
                            DiskDriveIndex = 0;

                        // do we have a valid disk inserted?
                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        // check write protect tab
                        if (ActiveDrive.FLAG_WRITEPROTECT)
                        {
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR1_NW, ref Status1);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }
                        else
                        {

                            // calculate the number of bytes to write
                            int byteCounter = 0;
                            byte startSecID = ActiveCommandParams.Sector;
                            byte endSecID = ActiveCommandParams.EOT;
                            bool lastSec = false;

                            // get the first sector
                            var track = ActiveDrive.Disk.DiskTracks[ActiveCommandParams.Cylinder];
                            int secIndex = 0;
                            for (int s = 0; s < track.Sectors.Length; s++)
                            {
                                if (track.Sectors[s].SectorID == endSecID)
                                    lastSec = true;

                                for (int i = 0; i < 0x80 << ActiveCommandParams.SectorSize; i++)
                                {
                                    byteCounter++;

                                    if (i == (0x80 << ActiveCommandParams.SectorSize) - 1 && lastSec)
                                    {
                                        break;
                                    }
                                }

                                if (lastSec)
                                    break;
                            }

                            ExecCounter = byteCounter;
                            ExecLength = byteCounter;
                            ActivePhase = Phase.Execution;
                            DriveLight = true;
                            break;
                        }
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:

                    var index = ExecLength - ExecCounter;

                    ExecBuffer[index] = LastSectorDataWriteByte;

                    OverrunCounter--;
                    ExecCounter--;

                    if (ExecCounter <= 0)
                    {
                        int cnt = 0;

                        // all data received
                        byte startSecID = ActiveCommandParams.Sector;
                        byte endSecID = ActiveCommandParams.EOT;
                        bool lastSec = false;
                        var track = ActiveDrive.Disk.DiskTracks[ActiveCommandParams.Cylinder];
                        int secIndex = 0;

                        for (int s = 0; s < track.Sectors.Length; s++)
                        {
                            if (cnt == ExecLength)
                                break;

                            ActiveCommandParams.Sector = track.Sectors[s].SectorID;

                            if (track.Sectors[s].SectorID == endSecID)
                                lastSec = true;

                            int size = 0x80 << track.Sectors[s].SectorSize;

                            for (int d = 0; d < size; d++)
                            {
                                track.Sectors[s].SectorData[d] = ExecBuffer[cnt++];
                            }

                            if (lastSec)
                                break;
                        }     

                        SetBit(SR0_IC0, ref Status0);
                        SetBit(SR1_EN, ref Status1);

                        CommitResultCHRN();
                        CommitResultStatus();
                    }

                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Write ID (format write)
        /// COMMAND:    5 parameter bytes
        /// EXECUTION:  Entire track is formatted
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_WriteID()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received - setup for execution phase
                        DriveLight = true;

                        // clear exec buffer and status registers
                        ClearExecBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // temp sector index
                        byte secIdx = ActiveCommandParams.Sector;

                        // hack for when another drive (non-existent) is being called
                        if (ActiveDrive.ID != 0)
                            DiskDriveIndex = 0;

                        // do we have a valid disk inserted?
                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        // check write protect tab
                        if (ActiveDrive.FLAG_WRITEPROTECT)
                        {
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR1_NW, ref Status1);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }
                        else
                        {
                            // not implemented yet
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR1_NW, ref Status1);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Write Deleted Data
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data transfer between FDC and FDD
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_WriteDeletedData()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received - setup for execution phase

                        // clear exec buffer and status registers
                        ClearExecBuffer();
                        Status0 = 0;
                        Status1 = 0;
                        Status2 = 0;
                        Status3 = 0;

                        // temp sector index
                        byte secIdx = ActiveCommandParams.Sector;

                        // hack for when another drive (non-existent) is being called
                        if (ActiveDrive.ID != 0)
                            DiskDriveIndex = 0;

                        // do we have a valid disk inserted?
                        if (!ActiveDrive.FLAG_READY)
                        {
                            // no disk, no tracks or motor is not on
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR0_NR, ref Status0);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }

                        // check write protect tab
                        if (ActiveDrive.FLAG_WRITEPROTECT)
                        {
                            SetBit(SR0_IC0, ref Status0);
                            SetBit(SR1_NW, ref Status1);

                            CommitResultCHRN();
                            CommitResultStatus();
                            //ResBuffer[RS_ST0] = Status0;

                            // move to result phase
                            ActivePhase = Phase.Result;
                            break;
                        }
                        else
                        {

                            // calculate the number of bytes to write
                            int byteCounter = 0;
                            byte startSecID = ActiveCommandParams.Sector;
                            byte endSecID = ActiveCommandParams.EOT;
                            bool lastSec = false;

                            // get the first sector
                            var track = ActiveDrive.Disk.DiskTracks[ActiveCommandParams.Cylinder];
                            int secIndex = 0;
                            for (int s = 0; s < track.Sectors.Length; s++)
                            {
                                if (track.Sectors[s].SectorID == endSecID)
                                    lastSec = true;

                                for (int i = 0; i < 0x80 << ActiveCommandParams.SectorSize; i++)
                                {
                                    byteCounter++;

                                    if (i == (0x80 << ActiveCommandParams.SectorSize) - 1 && lastSec)
                                    {
                                        break;
                                    }
                                }

                                if (lastSec)
                                    break;
                            }

                            ExecCounter = byteCounter;
                            ExecLength = byteCounter;
                            ActivePhase = Phase.Execution;
                            DriveLight = true;
                            break;
                        }
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:

                    var index = ExecLength - ExecCounter;

                    ExecBuffer[index] = LastSectorDataWriteByte;

                    OverrunCounter--;
                    ExecCounter--;

                    if (ExecCounter <= 0)
                    {
                        int cnt = 0;

                        // all data received
                        byte startSecID = ActiveCommandParams.Sector;
                        byte endSecID = ActiveCommandParams.EOT;
                        bool lastSec = false;
                        var track = ActiveDrive.Disk.DiskTracks[ActiveCommandParams.Cylinder];
                        int secIndex = 0;

                        for (int s = 0; s < track.Sectors.Length; s++)
                        {
                            if (cnt == ExecLength)
                                break;

                            ActiveCommandParams.Sector = track.Sectors[s].SectorID;

                            if (track.Sectors[s].SectorID == endSecID)
                                lastSec = true;

                            int size = 0x80 << track.Sectors[s].SectorSize;

                            for (int d = 0; d < size; d++)
                            {
                                track.Sectors[s].SectorData[d] = ExecBuffer[cnt++];
                            }

                            if (lastSec)
                                break;
                        }

                        SetBit(SR0_IC0, ref Status0);
                        SetBit(SR1_EN, ref Status1);

                        CommitResultCHRN();
                        CommitResultStatus();
                    }

                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        #endregion

        #region SCAN Commands

        /// <summary>
        /// Scan Equal
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data compared between the FDD and FDC
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ScanEqual()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Scan Low or Equal
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data compared between the FDD and FDC
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ScanLowOrEqual()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Scan High or Equal
        /// COMMAND:    8 parameter bytes
        /// EXECUTION:  Data compared between the FDD and FDC
        /// RESULT:     7 result bytes
        /// </summary>
        private void UPD_ScanHighOrEqual()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        #endregion

        #region OTHER Commands

        /// <summary>
        /// Specify
        /// COMMAND:    2 parameter bytes
        /// EXECUTION:  NO execution phase
        /// RESULT:     NO result phase
        /// 
        /// Looks like specify command returns status 0x80 throughout its lifecycle
        /// so CB is NOT set
        /// </summary>
        private void UPD_Specify()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:

                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    byte currByte = CommBuffer[CommCounter];
                    BitArray bi = new BitArray(new byte[] { currByte });

                    switch (CommCounter)
                    {
                        // SRT & HUT
                        case 0:
                            SRT = 16 - (currByte >> 4) & 0x0f;
                            HUT = (currByte & 0x0f) << 4;
                            if (HUT == 0)
                            {
                                HUT = 255;
                            }
                            break;
                        // HLT & ND
                        case 1:
                            if (bi[0])
                                ND = true;
                            else
                                ND = false;

                            HLT = currByte & 0xfe;
                            if (HLT == 0)
                            {
                                HLT = 255;
                            }
                            break;
                    }

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received
                        ActivePhase = Phase.Idle;
                    }

                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Seek
        /// COMMAND:    2 parameter bytes
        /// EXECUTION:  Head is positioned over proper cylinder on disk
        /// RESULT:     NO result phase
        /// </summary>
        private void UPD_Seek()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    byte currByte = CommBuffer[CommCounter];
                    switch (CommCounter)
                    {
                        case 0:
                            ParseParamByteStandard(CommCounter);
                            break;
                        case 1:
                            ActiveDrive.SeekingTrack = currByte;
                            break;
                    }

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received
                        DriveLight = true;
                        ActivePhase = Phase.Execution;
                        ActiveCommand.CommandDelegate();
                    }
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    // set seek flag
                    ActiveDrive.SeekStatus = SEEK_SEEK;

                    if (ActiveDrive.CurrentTrackID == CommBuffer[CM_C])
                    {
                        // we are already on the correct track
                        ActiveDrive.SectorIndex = 0;
                    }
                    else
                    {
                        // immediate seek
                        ActiveDrive.CurrentTrackID = CommBuffer[CM_C];

                        ActiveDrive.SectorIndex = 0;

                        if (ActiveDrive.Disk.DiskTracks[ActiveDrive.CurrentTrackID].Sectors.Length > 1)
                        {
                            // always read the first sector
                            //ActiveDrive.SectorIndex++;
                        }
                    }             

                    // skip execution mode and go directly to idle
                    // result is determined by SIS command
                    ActivePhase = Phase.Idle;
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Recalibrate (seek track 0)
        /// COMMAND:    1 parameter byte
        /// EXECUTION:  Head retracted to track 0
        /// RESULT:     NO result phase
        /// </summary>
        private void UPD_Recalibrate()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received
                        DriveLight = true;
                        ActivePhase = Phase.Execution;
                        ActiveCommand.CommandDelegate();
                    }
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    
                    // immediate recalibration
                    ActiveDrive.TrackIndex = 0;
                    ActiveDrive.SectorIndex = 0;

                    // recalibrate appears to always skip the first sector
                    //if (ActiveDrive.Disk.DiskTracks[ActiveDrive.TrackIndex].Sectors.Length > 1)
                        //ActiveDrive.SectorIndex++;

                    // set seek flag
                    ActiveDrive.SeekStatus = SEEK_RECALIBRATE;

                    // skip execution mode and go directly to idle
                    // result is determined by SIS command
                    ActivePhase = Phase.Idle;
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Sense Interrupt Status
        /// COMMAND:    NO parameter bytes
        /// EXECUTION:  NO execution phase
        /// RESULT:     2 result bytes
        /// </summary>
        private void UPD_SenseInterruptStatus()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    // SIS should return 2 bytes if sucessfully sensed an interrupt
                    // 1 byte otherwise

                    // it seems like the +3 ROM makes 3 SIS calls for each seek/recalibrate call for some reason
                    // possibly one for each drive???
                    // 1 - the interrupt is acknowleged with ST0 = 32 and track number
                    // 2 - second sis returns 1 ST0 byte with 192
                    // 3 - third SIS call returns standard 1 byte 0x80 (unknown cmd or SIS with no interrupt occured)
                    // for now I will assume that the first call is aimed at DriveA, the second at DriveB (which we are NOT implementing)

                    // check active drive first
                    if (ActiveDrive.SeekStatus == SEEK_RECALIBRATE ||
                        ActiveDrive.SeekStatus == SEEK_SEEK)
                    {
                        // interrupt has been raised for this drive
                        // acknowledge
                        ActiveDrive.SeekStatus = SEEK_IDLE;// SEEK_INTACKNOWLEDGED;

                        // result length 2
                        ResLength = 2;

                        // first byte ST0 0x20
                        Status0 = 0x20;
                        ResBuffer[0] = Status0;
                        // second byte is the current track id
                        ResBuffer[1] = ActiveDrive.CurrentTrackID;
                    }
                    /*
                    else if (ActiveDrive.SeekStatus == SEEK_INTACKNOWLEDGED)
                    {
                        // DriveA interrupt has already been acknowledged
                        ActiveDrive.SeekStatus = SEEK_IDLE;

                        ResLength = 1;
                        Status0 = 192;
                        ResBuffer[0] = Status0;
                    }
                    */
                    else if (ActiveDrive.SeekStatus == SEEK_IDLE)
                    {
                        // SIS with no interrupt
                        ResLength = 1;
                        Status0 = 0x80;
                        ResBuffer[0] = Status0;
                    }

                    ActivePhase = Phase.Result;

                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Sense Drive Status
        /// COMMAND:    1 parameter byte
        /// EXECUTION:  NO execution phase
        /// RESULT:     1 result byte
        /// 
        /// The ZX spectrum appears to only specify drive 1 as the parameter byte, NOT drive 0
        /// After the final param byte is received main status changes to 0xd0
        /// Data register (ST3) result is 0x51 if drive/disk not available
        /// 0x71 if disk is present in 2nd drive
        /// </summary>
        private void UPD_SenseDriveStatus()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    // store the parameter in the command buffer
                    CommBuffer[CommCounter] = LastByteReceived;

                    // process parameter byte
                    ParseParamByteStandard(CommCounter);

                    // increment command parameter counter
                    CommCounter++;

                    // was that the last parameter byte?
                    if (CommCounter == ActiveCommand.ParameterByteCount)
                    {
                        // all parameter bytes received
                        ActivePhase = Phase.Execution;
                        UPD_SenseDriveStatus();
                    }
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    // one ST3 byte required

                    // set US
                    Status3 = (byte)ActiveDrive.ID;

                    if (Status3 != 0)
                    {
                        // we only support 1 drive
                        SetBit(SR3_FT, ref Status3);
                    }
                    else
                    {
                        // HD - only one side
                        UnSetBit(SR3_HD, ref Status3);

                        // write protect
                        if (ActiveDrive.FLAG_WRITEPROTECT)
                            SetBit(SR3_WP, ref Status3);

                        // track 0
                        if (ActiveDrive.FLAG_TRACK0)
                            SetBit(SR3_T0, ref Status3);

                        // rdy
                        if (ActiveDrive.Disk != null)
                            SetBit(SR3_RY, ref Status3);
                    }

                    ResBuffer[0] = Status3;
                    ActivePhase = Phase.Result;                    
                    
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    break;
            }
        }

        /// <summary>
        /// Version
        /// COMMAND:    NO parameter bytes
        /// EXECUTION:  NO execution phase
        /// RESULT:     1 result byte
        /// </summary>
        private void UPD_Version()
        {
            switch (ActivePhase)
            {
                case Phase.Idle:
                case Phase.Command:
                case Phase.Execution:
                case Phase.Result:
                    UPD_Invalid();
                    break;
            }
        }

        /// <summary>
        /// Invalid
        /// COMMAND:    NO parameter bytes
        /// EXECUTION:  NO execution phase
        /// RESULT:     1 result byte
        /// </summary>
        private void UPD_Invalid()
        {
            switch (ActivePhase)
            {
                //----------------------------------------
                //  FDC is waiting for a command byte
                //----------------------------------------
                case Phase.Idle:
                    break;

                //----------------------------------------
                //  Receiving command parameter bytes
                //----------------------------------------
                case Phase.Command:
                    break;

                //----------------------------------------
                //  FDC in execution phase reading/writing bytes
                //----------------------------------------
                case Phase.Execution:
                    // no execution phase
                    ActivePhase = Phase.Result;
                    UPD_Invalid();
                    break;

                //----------------------------------------
                //  Result bytes being sent to CPU
                //----------------------------------------
                case Phase.Result:
                    ResBuffer[0] = 0x80;                    
                    break;
            }           
        }

        #endregion

        #endregion

        #region Controller Methods

        /// <summary>
        /// Called when a status register read is required
        /// This can be called at any time
        /// The main status register appears to be queried nearly all the time
        /// so needs to be kept updated. It keeps the CPU informed of the current state
        /// </summary>
        private byte ReadMainStatus()
        {
            SetBit(MSR_RQM, ref StatusMain);

            switch (ActivePhase)
            {
                case Phase.Idle:
                    UnSetBit(MSR_DIO, ref StatusMain);
                    UnSetBit(MSR_CB, ref StatusMain);
                    UnSetBit(MSR_EXM, ref StatusMain);
                    break;
                case Phase.Command:
                    UnSetBit(MSR_DIO, ref StatusMain);
                    SetBit(MSR_CB, ref StatusMain);
                    UnSetBit(MSR_EXM, ref StatusMain);
                    break;
                case Phase.Execution:
                    if (ActiveCommand.Direction == CommandDirection.OUT)
                        SetBit(MSR_DIO, ref StatusMain);
                    else
                        UnSetBit(MSR_DIO, ref StatusMain);

                    SetBit(MSR_EXM, ref StatusMain);
                    SetBit(MSR_CB, ref StatusMain);

                    // overrun detection                    
                    OverrunCounter++;
                    if (OverrunCounter >= 64)
                    {
                        // CPU has read the status register 64 times without reading the data register
                        // switch the current command into result phase
                        ActivePhase = Phase.Result;

                        // reset the overun counter
                        OverrunCounter = 0;
                    }

                    break;
                case Phase.Result:
                    SetBit(MSR_DIO, ref StatusMain);
                    SetBit(MSR_CB, ref StatusMain);
                    UnSetBit(MSR_EXM, ref StatusMain);
                    break;
            }

            //if (!CheckTiming())
            //{
            //    UnSetBit(MSR_EXM, ref StatusMain);
            //}

            return StatusMain;            
        }
        private int testCount = 0;
        /// <summary>
        /// Handles CPU reading from the data register
        /// </summary>
        /// <returns></returns>
        private byte ReadDataRegister()
        {
            // default return value
            byte res = 0xff;

            // check RQM flag status
            if (!GetBit(MSR_RQM, StatusMain))
            {
                // FDC is not ready to return data
                return res;
            }

            // check active direction
            if (!GetBit(MSR_DIO, StatusMain))
            {
                // FDC is expecting to receive, not send data
                return res;
            }

            switch (ActivePhase)
            {
                case Phase.Execution:
                    // reset overrun counter
                    OverrunCounter = 0;

                    // execute read
                    ActiveCommand.CommandDelegate();

                    res = LastSectorDataReadByte;

                    if (ExecCounter <= 0)
                    {
                        // end of execution phase
                        ActivePhase = Phase.Result;
                    }

                    return res;
                    
                case Phase.Result:

                    DriveLight = false;

                    ActiveCommand.CommandDelegate();

                    // result byte reading
                    res = ResBuffer[ResCounter];

                    // increment result counter
                    ResCounter++;

                    if (ResCounter >= ResLength)
                    {
                        ActivePhase = Phase.Idle;
                    }

                    break;
            }

            return res;
        }

        /// <summary>
        /// Handles CPU writing to the data register
        /// </summary>
        /// <param name="data"></param>
        private void WriteDataRegister(byte data)
        {
            if (!GetBit(MSR_RQM, StatusMain) || GetBit(MSR_DIO, StatusMain))
            {
                // FDC will not receive and process any bytes
                return;
            }

            // store the incoming byte
            LastByteReceived = data;

            // process incoming bytes
            switch (ActivePhase)
            {
                //// controller is idle awaiting the first command byte of a new instruction
                case Phase.Idle:                    
                    ParseCommandByte(data);
                    break;
                //// we are in command phase
                case Phase.Command:
                    // attempt to process this parameter byte
                    //ProcessCommand(data);      
                    ActiveCommand.CommandDelegate();           
                    break;
                //// we are in execution phase
                case Phase.Execution:
                    // CPU is going to be sending data bytes to the FDC to be written to disk
                    
                    // store the byte
                    LastSectorDataWriteByte = data;
                    ActiveCommand.CommandDelegate();

                    if (ExecCounter <= 0)
                    {
                        // end of execution phase
                        ActivePhase = Phase.Result;
                    }

                    break;
                //// result phase
                case Phase.Result:
                    // data register will not receive bytes during result phase
                    break;
            }
        }

        /// <summary>
        /// Processes the first command byte (within a command instruction)
        /// Returns TRUE if successful. FALSE if otherwise
        /// Called only in idle phase
        /// </summary>
        /// <param name="cmdByte"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private bool ParseCommandByte(byte cmdByte)
        {
            // clear counters
            CommCounter = 0;
            ResCounter = 0;

            // get the first 4 bytes
            byte cByte = (byte)(cmdByte & 0x0f);

            // get MT, MD and SK states
            CMD_FLAG_MT = cmdByte.Bit(7);
            CMD_FLAG_MF = cmdByte.Bit(6);
            CMD_FLAG_SK = cmdByte.Bit(5);

            cmdByte = cByte;

            // lookup the command
            var cmd = CommandList.Where(a => a.CommandCode == cmdByte).FirstOrDefault();

            if (cmd == null)
            {
                // no command found - use invalid
                CMDIndex = CommandList.Count() - 1;
            }
            else
            {
                // valid command found
                CMDIndex = CommandList.FindIndex(a => a.CommandCode == cmdByte);

                // check validity of command byte flags
                // if a flag is set but not valid for this command then it is invalid
                bool invalid = false;

                if (!ActiveCommand.MT)
                    if (CMD_FLAG_MT)
                        invalid = true;
                if (!ActiveCommand.MF)
                    if (CMD_FLAG_MF)
                        invalid = true;
                if (!ActiveCommand.SK)
                    if (CMD_FLAG_SK)
                        invalid = true;

                if (invalid)
                {
                    // command byte included spurious bit 5,6 or 7 flags
                    CMDIndex = CommandList.Count() - 1;
                }

                /*
                if ((CMD_FLAG_MF && !ActiveCommand.MF) ||
                    (CMD_FLAG_MT && !ActiveCommand.MT) ||
                    (CMD_FLAG_SK && !ActiveCommand.SK))
                {
                    // command byte included spurious bit 5,6 or 7 flags
                    CMDIndex = CommandList.Count() - 1;
                }
                */
            }

            CommCounter = 0;
            ResCounter = 0;

            // there will now be an active command set
            // move to command phase
            ActivePhase = Phase.Command;            

            /*
            // check for invalid SIS
            if (ActiveInterrupt == InterruptState.None && CMDIndex == CC_SENSE_INTSTATUS)
            {
                CMDIndex = CC_INVALID;
                //ActiveCommand.CommandDelegate(InstructionState.StartResult);
            }
            */

            // set reslength
            ResLength = ActiveCommand.ResultByteCount;
    
            // if there are no expected param bytes to receive - go ahead and run the command
            if (ActiveCommand.ParameterByteCount == 0)
            {
                ActivePhase = Phase.Execution;
                ActiveCommand.CommandDelegate();
            }

            return true;
        }

        /// <summary>
        /// Parses the first 5 command argument bytes that are of the standard format
        /// </summary>
        /// <param name="paramIndex"></param>
        private void ParseParamByteStandard(int index)
        {
            byte currByte = CommBuffer[index];
            BitArray bi = new BitArray(new byte[] { currByte });

            switch (index)
            {
                // HD & US
                case CM_HEAD:
                    if (bi[2])
                        ActiveCommandParams.Side = 1;
                    else
                        ActiveCommandParams.Side = 0;

                    ActiveCommandParams.UnitSelect = (byte)(GetUnitSelect(currByte));
                    DiskDriveIndex = ActiveCommandParams.UnitSelect;
                    break;
                
                // C
                case CM_C:
                    ActiveCommandParams.Cylinder = currByte;
                    break;

                // H
                case CM_H:
                    ActiveCommandParams.Head = currByte;
                    break;

                // R
                case CM_R:
                    ActiveCommandParams.Sector = currByte;
                    break;

                // N
                case CM_N:
                    ActiveCommandParams.SectorSize = currByte;
                    break;

                // EOT
                case CM_EOT:
                    ActiveCommandParams.EOT = currByte;
                    break;

                // GPL
                case CM_GPL:
                    ActiveCommandParams.Gap3Length = currByte;
                    break;

                // DTL
                case CM_DTL:
                    ActiveCommandParams.DTL = currByte;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Clears the result buffer
        /// </summary>
        public void ClearResultBuffer()
        {
            for (int i = 0; i < ResBuffer.Length; i++)
            {
                ResBuffer[i] = 0;
            }
        }

        /// <summary>
        /// Clears the result buffer
        /// </summary>
        public void ClearExecBuffer()
        {
            for (int i = 0; i < ExecBuffer.Length; i++)
            {
                ExecBuffer[i] = 0;
            }
        }

        /// <summary>
        /// Populates the result status registers
        /// </summary>
        private void CommitResultStatus()
        {
            // check for read diag
            if (ActiveCommand.CommandCode == 0x02)
            {
                // commit to result buffer
                ResBuffer[RS_ST0] = Status0;
                ResBuffer[RS_ST1] = Status1;
                return;
            }

            // check for error bits
            if (GetBit(SR1_DE, Status1) ||
                GetBit(SR1_MA, Status1) ||
                GetBit(SR1_ND, Status1) ||
                GetBit(SR1_NW, Status1) ||
                GetBit(SR1_OR, Status1) ||
                GetBit(SR2_BC, Status2) ||
                GetBit(SR2_CM, Status2) ||
                GetBit(SR2_DD, Status2) ||
                GetBit(SR2_MD, Status2) ||
                GetBit(SR2_SN, Status2) ||
                GetBit(SR2_WC, Status2))
            {
                // error bits set - unset end of track
                UnSetBit(SR1_EN, ref Status1);
            }

            // check for data errors
            if (GetBit(SR1_DE, Status1) ||
                GetBit(SR2_DD, Status2))
            {
                // unset control mark
                UnSetBit(SR2_CM, ref Status2);
            }
            else if (GetBit(SR2_CM, Status2))
            {
                // DAM found - unset IC and US0
                UnSetBit(SR0_IC0, ref Status0);
                UnSetBit(SR0_US0, ref Status0);
            }

            // commit to result buffer
            ResBuffer[RS_ST0] = Status0;
            ResBuffer[RS_ST1] = Status1;
            ResBuffer[RS_ST2] = Status2;
            
        }

        /// <summary>
        /// Populates the result CHRN values
        /// </summary>
        private void CommitResultCHRN()
        {
            ResBuffer[RS_C] = ActiveCommandParams.Cylinder;
            ResBuffer[RS_H] = ActiveCommandParams.Head;
            ResBuffer[RS_R] = ActiveCommandParams.Sector;
            ResBuffer[RS_N] = ActiveCommandParams.SectorSize;
        }

        /// <summary>
        /// Moves active phase into idle
        /// </summary>
        public void SetPhase_Idle()
        {
            ActivePhase = Phase.Idle;

            // active direction
            UnSetBit(MSR_DIO, ref StatusMain);
            // CB
            UnSetBit(MSR_CB, ref StatusMain);
            // RQM
            SetBit(MSR_RQM, ref StatusMain);

            CommCounter = 0;
            ResCounter = 0;
        }

        /// <summary>
        /// Moves to result phase
        /// </summary>
        public void SetPhase_Result()
        {
            ActivePhase = Phase.Result;

            // active direction
            SetBit(MSR_DIO, ref StatusMain);
            // CB
            SetBit(MSR_CB, ref StatusMain);
            // RQM
            SetBit(MSR_RQM, ref StatusMain);
            // EXM
            UnSetBit(MSR_EXM, ref StatusMain);

            CommCounter = 0;
            ResCounter = 0;
        }

        /// <summary>
        /// Moves to command phase
        /// </summary>
        public void SetPhase_Command()
        {
            ActivePhase = Phase.Command;

            // default 0x80 - just RQM
            SetBit(MSR_RQM, ref StatusMain);
            UnSetBit(MSR_DIO, ref StatusMain);
            UnSetBit(MSR_CB, ref StatusMain);
            UnSetBit(MSR_EXM, ref StatusMain);
            CommCounter = 0;
            ResCounter = 0;
        }

        /// <summary>
        /// Moves to execution phase
        /// </summary>
        public void SetPhase_Execution()
        {
            ActivePhase = Phase.Execution;

            // EXM
            SetBit(MSR_EXM, ref StatusMain);
            // CB
            SetBit(MSR_CB, ref StatusMain);
            // RQM
            UnSetBit(MSR_RQM, ref StatusMain);

            CommCounter = 0;
            ResCounter = 0;
        }

        #endregion
    }
}
