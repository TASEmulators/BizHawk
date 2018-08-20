using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Definitions
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
        #region Enums

        /// <summary>
        /// Defines the current phase of the controller
        /// </summary>
        private enum Phase
        {
            /// <summary>
            /// FDC is in an idle state, awaiting the next initial command byte
            /// </summary>
            Idle,

            /// <summary>
            /// FDC is in a state waiting for the next command instruction
            /// A command consists of a command byte (eventually including the MF, MK, SK bits), and up to eight parameter bytes
            /// </summary>
            Command,

            /// <summary>
            /// During this phase, the actual data is transferred (if any). Usually that are the data bytes for the read/written sector(s), except for the Format Track Command, 
            /// in that case four bytes for each sector are transferred
            /// </summary>
            Execution,

            /// <summary>
            /// Returns up to seven result bytes (depending on the command) that are containing status information. The Recalibrate and Seek Track commands do not return result bytes directly, 
            /// instead the program must wait until the Main Status Register signalizes that the command has been completed, and then it must (!) send a 
            /// Sense Interrupt State command to 'terminate' the Seek/Recalibrate command.
            /// </summary>
            Result
        }

        /// <summary>
        /// The lifecycle of an instruction
        /// Similar to phase, this describes the current 'sub-phase' we are in when dealing with an instruction
        /// </summary>
        private enum InstructionState
        {
            /// <summary>
            /// FDC has received a command byte and is currently reading parameter bytes from the data bus
            /// </summary>
            ReceivingParameters,

            /// <summary>
            /// All parameter bytes have been received. This phase allows any neccessary setup before instruction execution starts
            /// </summary>
            PreExecution,

            /// <summary>
            /// The start of instruction execution. This may end up with the FDC moving into result phase, 
            /// but also may also prepare the way for further processing to occur later in execution phase
            /// </summary>
            StartExecute,

            /// <summary>
            /// Data is read or written in execution phase
            /// </summary>
            ExecutionReadWrite,

            /// <summary>
            /// Execution phase is well under way. This state primarily deals with data transfer between CPU and FDC
            /// </summary>
            ExecutionWrite,

            /// <summary>
            /// Execution phase is well under way. This state primarily deals with data transfer between FDC and CPU
            /// </summary>
            ExecutionRead,

            /// <summary>
            /// Execution has finished and results bytes are ready to be read by the CPU
            /// Initial result setup
            /// </summary>
            StartResult,

            /// <summary>
            /// Result processing
            /// </summary>
            ProcessResult,

            /// <summary>
            /// Results are being sent
            /// </summary>
            SendingResults,

            /// <summary>
            /// Final cleanup tasks when the instruction has fully completed
            /// </summary>
            Completed
            
        }

        /// <summary>
        /// Represents internal interrupt state of the FDC
        /// </summary>
        public enum InterruptState
        {
            /// <summary>
            /// There is no interrupt
            /// </summary>
            None,
            /// <summary>
            /// Execution interrupt
            /// </summary>
            Execution,
            /// <summary>
            /// Result interrupt
            /// </summary>
            Result,
            /// <summary>
            /// Ready interrupt
            /// </summary>
            Ready,
            /// <summary>
            /// Seek interrupt
            /// </summary>
            Seek
        }

        /// <summary>
        /// Possible main states that each drive can be in
        /// </summary>
        public enum DriveMainState
        {
            /// <summary>
            /// Drive is not doing anything
            /// </summary>
            None,
            /// <summary>
            /// Seek operation is in progress
            /// </summary>
            Seek,
            /// <summary>
            /// Recalibrate operation is in progress
            /// </summary>
            Recalibrate,
            /// <summary>
            /// A scan data operation is in progress
            /// </summary>
            Scan,
            /// <summary>
            /// A read ID operation is in progress
            /// </summary>
            ReadID,
            /// <summary>
            /// A read data operation is in progress
            /// </summary>
            ReadData,
            /// <summary>
            /// A read diagnostic (read track) operation is in progress
            /// </summary>
            ReadDiagnostic,
            /// <summary>
            /// A write id (format track) operation is in progress
            /// </summary>
            WriteID,
            /// <summary>
            /// A write data operation is in progress
            /// </summary>
            WriteData,
        }

        /// <summary>
        /// State information during a seek/recalibration operation
        /// </summary>
        public enum SeekSubState
        {
            /// <summary>
            /// Seek hasnt started yet
            /// </summary>
            Idle,
            /// <summary>
            /// Delayed
            /// </summary>
            Wait,
            /// <summary>
            /// Setup for head move
            /// </summary>
            MoveInit,
            /// <summary>
            /// Seek is currently happening
            /// </summary>
            HeadMove,
            /// <summary>
            /// Head move with no delay
            /// </summary>
            MoveImmediate,
            /// <summary>
            /// Ready to complete
            /// </summary>
            PerformCompletion,
            /// <summary>
            /// Seek operation has completed
            /// </summary>
            SeekCompleted
        }

        /// <summary>
        /// Seek int code
        /// </summary>
        public enum SeekIntStatus
        {
            Normal,
            Abnormal,
            DriveNotReady,
        }

        /// <summary>
        /// The direction of a specific command
        /// </summary>
        private enum CommandDirection
        {
            /// <summary>
            /// Data flows from UPD765A to Z80
            /// </summary>
            OUT,
            /// <summary>
            /// Data flows from Z80 to UPD765A
            /// </summary>
            IN
        }

        /// <summary>
        /// Enum defining the different types of result that can be returned
        /// </summary>
        private enum ResultType
        {
            /// <summary>
            /// Standard 7 result bytes are returned
            /// </summary>
            Standard,
            /// <summary>
            /// 1 byte returned - ST3
            /// (used for SenseDriveStatus)
            /// </summary>
            ST3,
            /// <summary>
            /// 1 byte returned - ST0
            /// (used for version & invalid)
            /// </summary>
            ST0,
            /// <summary>
            /// 2 bytes returned for sense interrupt status command
            /// ST0
            /// CurrentCylinder
            /// </summary>
            Interrupt
        }

        /// <summary>
        /// Possible list of encountered drive status errors
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// No error detected
            /// </summary>
            None,
            /// <summary>
            /// An undefined error has been detected
            /// </summary>
            Undefined,
            /// <summary>
            /// Drive is not ready
            /// </summary>
            DriveNotReady,
            /// <summary>
            /// Invalid command received
            /// </summary>
            Invalid,
            /// <summary>
            /// The disk has its write protection tab enabled
            /// </summary>
            WriteProtected,
            /// <summary>
            /// The requested sector has not been found
            /// </summary>
            SectorNotFound
        }

        /// <summary>
        /// Represents the direction that the head is moving over the cylinders
        /// Increment:  Track number increasing (head moving from outside of disk inwards)
        /// Decrement:  Track number decreasing (head moving from inside of disk outwards)
        /// </summary>
        public enum SkipDirection
        {
            Increment,
            Decrement
        }
        
        #endregion

        #region Constants

        // Command Instruction Constants
        // Designates the default postitions within the cmdbuffer array

        public const int CM_HEAD = 0;
        /// <summary>
        /// C - Track
        /// </summary>
        public const int CM_C = 1;
        /// <summary>
        /// H - Side
        /// </summary>
        public const int CM_H = 2;
        /// <summary>
        /// R - Sector ID
        /// </summary>
        public const int CM_R = 3;
        /// <summary>
        /// N - Sector size
        /// </summary>
        public const int CM_N = 4;
        /// <summary>
        /// EOT - End of track
        /// </summary>
        public const int CM_EOT = 5;
        /// <summary>
        /// GPL - Gap length
        /// </summary>
        public const int CM_GPL = 6;
        /// <summary>
        /// DTL - Data length
        /// </summary>
        public const int CM_DTL = 7;
        /// <summary>
        /// STP - Step
        /// </summary>
        public const int CM_STP = 7;

        // Result Instruction Constants
        // Designates the default postitions within the cmdbuffer array

        /// <summary>
        /// Status register 0
        /// </summary>
        public const int RS_ST0 = 0;
        /// <summary>
        /// Status register 1
        /// </summary>
        public const int RS_ST1 = 1;
        /// <summary>
        /// Status register 2
        /// </summary>
        public const int RS_ST2 = 2;
        /// <summary>
        /// C - Track
        /// </summary>
        public const int RS_C = 3;
        /// <summary>
        /// H - Side
        /// </summary>
        public const int RS_H = 4;
        /// <summary>
        /// R - Sector ID
        /// </summary>
        public const int RS_R = 5;
        /// <summary>
        /// N - Sector size
        /// </summary>
        public const int RS_N = 6;

        // Main Status Register Constants
        // Designates the bit positions within the Main status register

        /// <summary>
        /// FDD0 Busy (seek/recalib active, until succesful sense intstat)
        /// FDD number 0 is in the seek mode. If any of the DnB bits IS set FDC will not accept read or write command.
        /// </summary>
        public const int MSR_D0B = 0;
        /// <summary>
        /// FDD1 Busy (seek/recalib active, until succesful sense intstat)
        /// FDD number 1 is in the seek mode. If any of the DnB bits IS set FDC will not accept read or write command.
        /// </summary>
        public const int MSR_D1B = 1;
        /// <summary>
        /// FDD2 Busy (seek/recalib active, until succesful sense intstat)
        /// FDD number 2 is in the seek mode. If any of the DnB bits IS set FDC will not accept read or write command.
        /// </summary>
        public const int MSR_D2B = 2;
        /// <summary>
        /// FDD3 Busy (seek/recalib active, until succesful sense intstat)
        /// FDD number 3 is in the seek mode. If any of the DnB bits IS set FDC will not accept read or write command.
        /// </summary>
        public const int MSR_D3B = 3;
        /// <summary>
        /// FDC Busy (still in command-, execution- or result-phase)
        /// A Read or Write command is in orocess. (FDC Busy) FDC will not accept any other command
        /// </summary>
        public const int MSR_CB = 4;
        /// <summary>
        /// Execution Mode (still in execution-phase, non_DMA_only)
        /// This bit is set only during execution ohase (Execution Mode) in non-DMA mode When DB5 goes low, execution phase has ended and result phase has started.It operates only during
        ///  non-DMA mode of operation
        /// </summary>
        public const int MSR_EXM = 5;
        /// <summary>
        /// Data Input/Output (0=CPU->FDC, 1=FDC->CPU) (see b7)
        /// Indicates direction of data transfer between FDC and data regrster If DIO = 1, then transfer is from data register to the
        /// processor.If DIO = 0, then transfer is from the processor to data register
        /// </summary>
        public const int MSR_DIO = 6;
        /// <summary>
        /// Request For Master (1=ready for next byte) (see b6 for direction)
        /// ndicates data register IS ready to send or receive data to or from the processor Both bits DIO and RQM should be 
        /// used to perform the hand-shaking functions of “ready” and “directron” to the processor
        /// </summary>
        public const int MSR_RQM = 7;

        // Status Register 0 Constants
        // Designates the bit positions within the status register 0

        /// <summary>
        /// Unit Select (driveno during interrupt)
        /// This flag IS used to indicate a drive unit number at interrupt
        /// </summary>
        public const int SR0_US0 = 0;

        /// <summary>
        /// Unit Select (driveno during interrupt)
        /// This flag IS used to indicate a drive unit number at interrupt
        /// </summary>
        public const int SR0_US1 = 1;

        /// <summary>
        /// Head Address (head during interrupt)
        /// State of the head at interrupt
        /// </summary>
        public const int SR0_HD = 2;

        /// <summary>
        /// Not Ready (drive not ready or non-existing 2nd head selected)
        /// Not Ready - When the FDD IS in the not-ready state and a Read or Write command IS Issued, this
        ///  flag IS set If a Read or Write command is issued to side 1 of a single-sided drive, then this flag IS set
        /// </summary>
        public const int SR0_NR = 3;

        /// <summary>
        /// Equipment Check (drive failure or recalibrate failed (retry))
        /// Equipment check - If a fault srgnal IS received from the FDD, or if the track 0 srgnal fails to occur after 77
        ///  step pulses(Recalibrate Command) then this flag is set
        /// </summary>
        public const int SR0_EC = 4;

        /// <summary>
        /// Seek End (Set if seek-command completed)
        /// Seek end - When the FDC completes the Seek command, this flag IS set lo 1 (high)
        /// </summary>
        public const int SR0_SE = 5;

        /// <summary>
        /// Interrupt Code (low byte)
        /// Interrupt Code (0=OK, 1=aborted:readfail/OK if EN, 2=unknown cmd
        /// or senseint with no int occured, 3=aborted:disc removed etc.)
        /// </summary>
        public const int SR0_IC0 = 6;

        /// <summary>
        /// Interrupt Code (high byte)
        /// Interrupt Code (0=OK, 1=aborted:readfail/OK if EN, 2=unknown cmd
        /// or senseint with no int occured, 3=aborted:disc removed etc.)
        /// </summary>
        public const int SR0_IC1 = 7;

        // Status Register 1 Constants
        // Designates the bit positions within the status register 1

        /// <summary>
        /// Missing Address Mark (Sector_ID or DAM not found)
        /// Missing address mark - This bit is set i f the FDC does not detect the IDAM before 2 index pulses It is also set if
        ///  the FDC cannot find the DAM or DDAM after the IDAM i s found.MD bit of ST2 is also set at this time
        /// </summary>
        public const int SR1_MA = 0;

        /// <summary>
        /// Not Writeable (tried to write/format disc with wprot_tab=on)
        /// Not writable (write protect) - During execution of Write Data, Write Deleted Data or Write ID command. if the FDC
        ///  detect: a write protect srgnal from the FDD.then this flag is Set
        /// </summary>
        public const int SR1_NW = 1;

        /// <summary>
        /// No Data
        /// No Data (Sector_ID not found, CRC fail in ID_field)
        /// 
        /// During execution of Read Data. Read Deleted Data Write Data.Write Deleted Data or Scan command, if the FDC cannot find
        /// the sector specified in the IDR(2)Register, this flag i s set.
        /// 
        /// During execution of the Read ID command. if the FDC cannot read the ID field without an error, then this flag IS set
        /// 
        /// During execution of the Read Diagnostic command. if the starting sector cannot be found, then this flag is set
        /// </summary>
        public const int SR1_ND = 2;

        /// <summary>
        /// Over Run (CPU too slow in execution-phase (ca. 26us/Byte))
        /// Overrun - If the FDC i s not serviced by the host system during data transfers within a certain time interval.this flaa i s set
        /// </summary>
        public const int SR1_OR = 4;

        /// <summary>
        /// Data Error (CRC-fail in ID- or Data-Field)
        /// Data error - When the FDC detects a CRC(1) error in either the ID field or the data field, this flag is set
        /// </summary>
        public const int SR1_DE = 5;

        /// <summary>
        /// End of Track (set past most read/write commands) (see IC)
        /// End of cylinder - When the FDC tries to access a sector beyond the final sector of a cylinder, this flag I S set
        /// </summary>
        public const int SR1_EN = 7;

        // Status Register 2 Constants
        // Designates the bit positions within the status register 2

        /// <summary>
        /// Missing Address Mark in Data Field (DAM not found)
        /// Missing address mark - When data IS read from the medium, i f the FDC cannot find a data address mark or deleted 
        /// data address mark, then this flag is set
        /// </summary>
        public const int SR2_MD = 0;

        /// <summary>
        /// Bad Cylinder (read/programmed track-ID different and read-ID = FF)
        /// Bad cylinder - This bit i s related to the ND bit. and when the contents of C on the medium is different
        /// from that stored i n the IDR and the contents  of C IS FFH.then this flag IS set
        /// </summary>
        public const int SR2_BC = 1;

        /// <summary>
        /// Scan Not Satisfied (no fitting sector found)
        /// Scan not satisfied - During execution of the Scan command, i f the F D cannot find a sector on the cylinder 
        /// which meets the condition.then this flag i s set
        /// </summary>
        public const int SR2_SN = 2;

        /// <summary>
        /// Scan Equal Hit (equal)
        /// Scan equal hit - During execution of the Scan command. i f the condition of “equal” is satisfied, this flag i s set
        /// </summary>
        public const int SR2_SH = 3;

        /// <summary>
        /// Wrong Cylinder (read/programmed track-ID different) (see b1)
        /// Wrong cylinder - This bit IS related to the ND bit, and when  the contents of C(3) on the medium is different 
        /// from that stored i n the IDR.this flag is set
        /// </summary>
        public const int SR2_WC = 4;

        /// <summary>
        /// Data Error in Data Field (CRC-fail in data-field)
        /// Data error in data field - If the FDC detects a CRC error i n the data field then this flag is set
        /// </summary>
        public const int SR2_DD = 5;

        /// <summary>
        /// Control Mark (read/scan command found sector with deleted DAM)
        /// Control mark - During execution of the Read Data or Scan command, if the FDC encounters a sector
        ///  which contains a deleted data address mark, this flag is set Also set if DAM is
        ///   found during Read Deleted Data
        /// </summary>
        public const int SR2_CM = 6;

        // Status Register 3 Constants
        // Designates the bit positions within the status register 3

        /// <summary>
        /// Unit select 0
        /// Unit Select (pin 28,29 of FDC)
        /// </summary>
        public const int SR3_US0 = 0;

        /// <summary>
        /// Unit select 1
        /// Unit Select (pin 28,29 of FDC)
        /// </summary>
        public const int SR3_US1 = 1;

        /// <summary>
        /// Head address (side select)
        /// Head Address (pin 27 of FDC)
        /// </summary>
        public const int SR3_HD = 2;

        /// <summary>
        /// Two Side (0=yes, 1=no (!))
        /// Two-side - This bit IS used to indicate the status of the two-side signal from the FDD
        /// </summary>
        public const int SR3_TS = 3;

        /// <summary>
        /// Track 0 (on track 0 we are)
        /// Track 0 - This bit IS used to indicate the status of the track 0 signal from the FDD
        /// </summary>
        public const int SR3_T0 = 4;

        /// <summary>
        /// Ready - status of the ready signal from the fdd
        /// Ready (drive ready signal)
        /// </summary>
        public const int SR3_RY = 5;

        /// <summary>
        /// Write Protected (write protected)
        /// Write protect - status of the wp signal from the fdd
        /// </summary>
        public const int SR3_WP = 6;

        /// <summary>
        /// Fault - This bit is used to indicate the status of the fault signal from the FDD
        /// Fault (if supported: 1=Drive failure)
        /// </summary>
        public const int SR3_FT = 7;

        // Interrupt Code Masks

        /// <summary>
        /// 1 = aborted:readfail / OK if EN (end of track)
        /// </summary>
        public const byte IC_OK = 0x00;

        /// <summary>
        /// 1 = aborted:readfail / OK if EN (end of track)
        /// </summary>
        public const byte IC_ABORTED_RF_OKEN = 0x40;

        /// <summary>
        /// 2 = unknown cmd or senseint with no int occured
        /// </summary>
        public const byte IC_NO_INT_OCCURED = 0x80;

        /// <summary>
        /// 3 = aborted:disc removed etc
        /// </summary>
        public const byte IC_ABORTED_DISCREMOVED = 0xC0;

        // command code constants
        public const int CC_READ_DATA = 0x06;
        public const int CC_READ_ID = 0x0a;
        public const int CC_SPECIFY = 0x03;
        public const int CC_READ_DIAGNOSTIC = 0x02;
        public const int CC_SCAN_EQUAL = 0x11;
        public const int CC_SCAN_HIGHOREQUAL = 0x1d;
        public const int CC_SCAN_LOWOREQUAL = 0x19;
        public const int CC_READ_DELETEDDATA = 0x0c;
        public const int CC_WRITE_DATA = 0x05;
        public const int CC_WRITE_ID = 0x0d;
        public const int CC_WRITE_DELETEDDATA = 0x09;
        public const int CC_SEEK = 0x0f;
        public const int CC_RECALIBRATE = 0x07;
        public const int CC_SENSE_INTSTATUS = 0x08;
        public const int CC_SENSE_DRIVESTATUS = 0x04;
        public const int CC_VERSION = 0x10;
        public const int CC_INVALID = 0x00;

        // drive seek state constants
        public const int SEEK_IDLE = 0;
        public const int SEEK_SEEK = 1;
        public const int SEEK_RECALIBRATE = 2;
        // seek interrupt
        public const int SEEK_INTACKNOWLEDGED = 3;
        public const int SEEK_NORMALTERM = 4;
        public const int SEEK_ABNORMALTERM = 5;
        public const int SEEK_DRIVENOTREADY = 6;

        #endregion

        #region Classes & Structs

        /// <summary>
        /// Class that holds information about a specific command
        /// </summary>
        private class Command
        {
            /// <summary>
            /// Mask to remove potential parameter bits (5,6, and or 7) in order to identify the command
            /// </summary>
            //public int BitMask { get; set; }
            /// <summary>
            /// The command code after bitmask has been applied
            /// </summary>
            public int CommandCode { get; set; }
            /// <summary>
            /// The number of bytes that make up the full command
            /// </summary>
            public int ParameterByteCount { get; set; }
            /// <summary>
            /// The number of result bytes that will be generated from the command
            /// </summary>
            public int ResultByteCount { get; set; }
            /// <summary>
            /// The command direction
            /// IN - Z80 to UPD765A
            /// OUT - UPD765A to Z80
            /// </summary>
            public CommandDirection Direction { get; set; }
            /// <summary>
            /// Command makes use of the MT bit
            /// </summary>
            public bool MT;
            /// <summary>
            /// Command makes use of the MF bit
            /// </summary>
            public bool MF;
            /// <summary>
            /// Command makes use of the SK bit
            /// </summary>
            public bool SK;
            /// <summary>
            /// Read/Write command that is READ
            /// </summary>
            public bool IsRead;
            /// <summary>
            /// Read/Write command that is WRITE
            /// </summary>
            public bool IsWrite;

            /// <summary>
            /// Delegate function that is called by this command
            /// bool 1: EXECUTE - if TRUE the command will be executed. if FALSE the method will instead parse commmand parameter bytes
            /// bool 2: RESULT - if TRUE
            /// </summary>
            public Action CommandDelegate { get; set; }
        }
       
        /// <summary>
        /// Storage for command parameters
        /// </summary>
        public class CommandParameters
        {
            /// <summary>
            /// The requested drive
            /// </summary>
            public byte UnitSelect;
            /// <summary>
            /// The requested physical side
            /// </summary>
            public byte Side;
            /// <summary>
            /// The requested track (C)
            /// </summary>
            public byte Cylinder;
            /// <summary>
            /// The requested head (H)
            /// </summary>
            public byte Head;
            /// <summary>
            /// The requested sector (R)
            /// </summary>
            public byte Sector;
            /// <summary>
            /// The specified sector size (N)
            /// </summary>
            public byte SectorSize;
            /// <summary>
            /// The end of track or last sector value (EOT)
            /// </summary>
            public byte EOT;
            /// <summary>
            /// Gap3 length (GPL)
            /// </summary>
            public byte Gap3Length;
            /// <summary>
            /// Data length (DTL) - When N is defined as 00, DTL stands for the data length 
            /// which users are going to read out or write into the sector
            /// </summary>
            public byte DTL;

            /// <summary>
            /// Clear down
            /// </summary>
            public void Reset()
            {
                UnitSelect = 0;
                Side = 0;
                Cylinder = 0;
                Head = 0;
                Sector = 0;
                SectorSize = 0;
                EOT = 0;
                Gap3Length = 0;
                DTL = 0;
            }

            public void SyncState(Serializer ser)
            {
                ser.BeginSection("ActiveCmdParams");

                ser.Sync("UnitSelect", ref UnitSelect);
                ser.Sync("Side", ref Side);
                ser.Sync("Cylinder", ref Cylinder);
                ser.Sync("Head", ref Head);
                ser.Sync("Sector", ref Sector);
                ser.Sync("SectorSize", ref SectorSize);
                ser.Sync("EOT", ref EOT);
                ser.Sync("Gap3Length", ref Gap3Length);
                ser.Sync("DTL", ref DTL);

                ser.EndSection();
            }
        }
       

        #endregion
    }
}
