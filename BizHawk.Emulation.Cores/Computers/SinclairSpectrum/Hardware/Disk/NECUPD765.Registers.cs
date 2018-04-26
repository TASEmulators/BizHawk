using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Registers
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
        /*

        #region Main Status Register

        /// <summary>
        /// Main status register (accessed via reads to port 0x2ffd)
        /// </summary>
        private byte _RegMain;

        /// <summary>
        /// FDD0 Busy (seek/recalib active, until succesful sense intstat)
        /// </summary>
        private bool MainDB0
        {
            get { return GetBit(0, _RegMain); }
            set
            {
                if (value) { SetBit(0, ref _RegMain); }
                else { UnSetBit(0, ref _RegMain); }
            }
        }

        /// <summary>
        /// FDD1 Busy (seek/recalib active, until succesful sense intstat)
        /// </summary>
        private bool MainDB1
        {
            get { return GetBit(1, _RegMain); }
            set
            {
                if (value) { SetBit(1, ref _RegMain); }
                else { UnSetBit(1, ref _RegMain); }
            }
        }

        /// <summary>
        /// FDD2 Busy (seek/recalib active, until succesful sense intstat)
        /// </summary>
        private bool MainDB2
        {
            get { return GetBit(2, _RegMain); }
            set
            {
                if (value) { SetBit(2, ref _RegMain); }
                else { UnSetBit(2, ref _RegMain); }
            }
        }

        /// <summary>
        /// FDD3 Busy (seek/recalib active, until succesful sense intstat)
        /// </summary>
        private bool MainDB3
        {
            get { return GetBit(3, _RegMain); }
            set
            {
                if (value) { SetBit(3, ref _RegMain); }
                else { UnSetBit(3, ref _RegMain); }
            }
        }

        /// <summary>
        /// FDC Busy (still in command-, execution- or result-phase)
        /// </summary>
        private bool MainCB
        {
            get { return GetBit(4, _RegMain); }
            set
            {
                if (value) { SetBit(4, ref _RegMain); }
                else { UnSetBit(4, ref _RegMain); }
            }
        }

        /// <summary>
        /// Execution Mode (still in execution-phase, non_DMA_only)
        /// </summary>
        private bool MainEXM
        {
            get { return GetBit(5, _RegMain); }
            set
            {
                if (value) { SetBit(5, ref _RegMain); }
                else { UnSetBit(5, ref _RegMain); }
            }
        }

        /// <summary>
        /// Data Input/Output (0=CPU->FDC, 1=FDC->CPU) (see b7)
        /// </summary>
        private bool MainDIO
        {
            get { return GetBit(6, _RegMain); }
            set
            {
                if (value) { SetBit(6, ref _RegMain); }
                else { UnSetBit(6, ref _RegMain); }
            }
        }

        /// <summary>
        /// Request For Master (1=ready for next byte) (see b6 for direction)
        /// </summary>
        private bool MainRQM
        {
            get { return GetBit(7, _RegMain); }
            set
            {
                if (value) { SetBit(7, ref _RegMain); }
                else { UnSetBit(7, ref _RegMain); }
            }
        }

        #endregion

        #region Status Register 0

        /// <summary>
        /// Status Register 0
        /// </summary>
        private byte _Reg0;

        /// <summary>
        /// Unit Select (driveno during interrupt)
        /// </summary>
        private bool ST0US0
        {
            get { return GetBit(0, _Reg0); }
            set
            {
                if (value) { SetBit(0, ref _Reg0); }
                else { UnSetBit(0, ref _Reg0); }
            }
        }

        /// <summary>
        /// Unit Select (driveno during interrupt)
        /// </summary>
        private bool ST0US1
        {
            get { return GetBit(1, _Reg0); }
            set
            {
                if (value) { SetBit(1, ref _Reg0); }
                else { UnSetBit(1, ref _Reg0); }
            }
        }

        /// <summary>
        /// Head Address (head during interrupt)
        /// </summary>
        private bool ST0HD
        {
            get { return GetBit(2, _Reg0); }
            set
            {
                if (value) { SetBit(2, ref _Reg0); }
                else { UnSetBit(2, ref _Reg0); }
            }
        }

        /// <summary>
        /// Not Ready (drive not ready or non-existing 2nd head selected)
        /// </summary>
        private bool ST0NR
        {
            get { return GetBit(3, _Reg0); }
            set
            {
                if (value) { SetBit(3, ref _Reg0); }
                else { UnSetBit(3, ref _Reg0); }
            }
        }

        /// <summary>
        /// Equipment Check (drive failure or recalibrate failed (retry))
        /// </summary>
        private bool ST0EC
        {
            get { return GetBit(4, _Reg0); }
            set
            {
                if (value) { SetBit(4, ref _Reg0); }
                else { UnSetBit(4, ref _Reg0); }
            }
        }

        /// <summary>
        /// Seek End (Set if seek-command completed)
        /// </summary>
        private bool ST0SE
        {
            get { return GetBit(5, _Reg0); }
            set
            {
                if (value) { SetBit(5, ref _Reg0); }
                else { UnSetBit(5, ref _Reg0); }
            }
        }

        /// <summary>
        /// Interrupt Code (0=OK, 1=aborted:readfail/OK if EN, 2=unknown cmd
        /// or senseint with no int occured, 3=aborted:disc removed etc.)
        /// </summary>
        private bool ST0IC0
        {
            get { return GetBit(6, _Reg0); }
            set
            {
                if (value) { SetBit(6, ref _Reg0); }
                else { UnSetBit(6, ref _Reg0); }
            }
        }

        /// <summary>
        /// Interrupt Code (0=OK, 1=aborted:readfail/OK if EN, 2=unknown cmd
        /// or senseint with no int occured, 3=aborted:disc removed etc.)
        /// </summary>
        private bool ST0IC1
        {
            get { return GetBit(7, _Reg0); }
            set
            {
                if (value) { SetBit(7, ref _Reg0); }
                else { UnSetBit(7, ref _Reg0); }
            }
        }

        #endregion

        #region Status Register 1

        /// <summary>
        /// Status Register 1
        /// </summary>
        private byte _Reg1;

        /// <summary>
        /// Missing Address Mark (Sector_ID or DAM not found)
        /// </summary>
        private bool ST1MA
        {
            get { return GetBit(0, _Reg1); }
            set
            {
                if (value) { SetBit(0, ref _Reg1); }
                else { UnSetBit(0, ref _Reg1); }
            }
        }

        /// <summary>
        /// Not Writeable (tried to write/format disc with wprot_tab=on)
        /// </summary>
        private bool ST1NW
        {
            get { return GetBit(1, _Reg1); }
            set
            {
                if (value) { SetBit(1, ref _Reg1); }
                else { UnSetBit(1, ref _Reg1); }
            }
        }

        /// <summary>
        /// No Data (Sector_ID not found, CRC fail in ID_field)
        /// </summary>
        private bool ST1ND
        {
            get { return GetBit(2, _Reg1); }
            set
            {
                if (value) { SetBit(2, ref _Reg1); }
                else { UnSetBit(2, ref _Reg1); }
            }
        }

        /// <summary>
        /// Over Run (CPU too slow in execution-phase (ca. 26us/Byte))
        /// </summary>
        private bool ST1OR
        {
            get { return GetBit(4, _Reg1); }
            set
            {
                if (value) { SetBit(4, ref _Reg1); }
                else { UnSetBit(4, ref _Reg1); }
            }
        }

        /// <summary>
        /// Data Error (CRC-fail in ID- or Data-Field)
        /// </summary>
        private bool ST1DE
        {
            get { return GetBit(5, _Reg1); }
            set
            {
                if (value) { SetBit(5, ref _Reg1); }
                else { UnSetBit(5, ref _Reg1); }
            }
        }

        /// <summary>
        /// End of Track (set past most read/write commands) (see IC)
        /// </summary>
        private bool ST1EN
        {
            get { return GetBit(7, _Reg1); }
            set
            {
                if (value) { SetBit(7, ref _Reg1); }
                else { UnSetBit(7, ref _Reg1); }
            }
        }

        #endregion

        #region Status Register 2

        /// <summary>
        /// Status Register 2
        /// </summary>
        private byte _Reg2;

        /// <summary>
        /// Missing Address Mark in Data Field (DAM not found)
        /// </summary>
        private bool ST2MD
        {
            get { return GetBit(0, _Reg2); }
            set
            {
                if (value) { SetBit(0, ref _Reg2); }
                else { UnSetBit(0, ref _Reg2); }
            }
        }

        /// <summary>
        /// Bad Cylinder (read/programmed track-ID different and read-ID = FF)
        /// </summary>
        private bool ST2BC
        {
            get { return GetBit(1, _Reg2); }
            set
            {
                if (value) { SetBit(1, ref _Reg2); }
                else { UnSetBit(1, ref _Reg2); }
            }
        }

        /// <summary>
        /// Scan Not Satisfied (no fitting sector found)
        /// </summary>
        private bool ST2SN
        {
            get { return GetBit(2, _Reg2); }
            set
            {
                if (value) { SetBit(2, ref _Reg2); }
                else { UnSetBit(2, ref _Reg2); }
            }
        }

        /// <summary>
        /// Scan Equal Hit (equal)
        /// </summary>
        private bool ST2SH
        {
            get { return GetBit(3, _Reg2); }
            set
            {
                if (value) { SetBit(3, ref _Reg2); }
                else { UnSetBit(3, ref _Reg2); }
            }
        }

        /// <summary>
        /// Wrong Cylinder (read/programmed track-ID different) (see b1)
        /// </summary>
        private bool ST2WC
        {
            get { return GetBit(4, _Reg2); }
            set
            {
                if (value) { SetBit(4, ref _Reg2); }
                else { UnSetBit(4, ref _Reg2); }
            }
        }

        /// <summary>
        /// Data Error in Data Field (CRC-fail in data-field)
        /// </summary>
        private bool ST2DD
        {
            get { return GetBit(5, _Reg2); }
            set
            {
                if (value) { SetBit(5, ref _Reg2); }
                else { UnSetBit(5, ref _Reg2); }
            }
        }

        /// <summary>
        /// Control Mark (read/scan command found sector with deleted DAM)
        /// </summary>
        private bool ST2CM
        {
            get { return GetBit(6, _Reg2); }
            set
            {
                if (value) { SetBit(6, ref _Reg2); }
                else { UnSetBit(6, ref _Reg2); }
            }
        }

        #endregion

        #region Status Register 3

        /// <summary>
        /// Status Register 3
        /// </summary>
        private byte _Reg3;

        /// <summary>
        /// Unit Select (pin 28,29 of FDC)
        /// </summary>
        private bool ST3US0
        {
            get { return GetBit(0, _Reg3); }
            set
            {
                if (value) { SetBit(0, ref _Reg3); }
                else { UnSetBit(0, ref _Reg3); }
            }
        }

        /// <summary>
        /// Unit Select (pin 28,29 of FDC)
        /// </summary>
        private bool ST3US1
        {
            get { return GetBit(1, _Reg3); }
            set
            {
                if (value) { SetBit(1, ref _Reg3); }
                else { UnSetBit(1, ref _Reg3); }
            }
        }

        /// <summary>
        /// Head Address (pin 27 of FDC)
        /// </summary>
        private bool ST3HD
        {
            get { return GetBit(2, _Reg3); }
            set
            {
                if (value) { SetBit(2, ref _Reg3); }
                else { UnSetBit(2, ref _Reg3); }
            }
        }

        /// <summary>
        /// Two Side (0=yes, 1=no (!))
        /// </summary>
        private bool ST3TS
        {
            get { return GetBit(3, _Reg3); }
            set
            {
                if (value) { SetBit(3, ref _Reg3); }
                else { UnSetBit(3, ref _Reg3); }
            }
        }

        /// <summary>
        /// Track 0 (on track 0 we are)
        /// </summary>
        private bool ST3T0
        {
            get { return GetBit(4, _Reg3); }
            set
            {
                if (value) { SetBit(4, ref _Reg3); }
                else { UnSetBit(4, ref _Reg3); }
            }
        }

        /// <summary>
        /// Ready (drive ready signal)
        /// </summary>
        private bool ST3RY
        {
            get { return GetBit(5, _Reg3); }
            set
            {
                if (value) { SetBit(5, ref _Reg3); }
                else { UnSetBit(5, ref _Reg3); }
            }
        }

        /// <summary>
        /// Write Protected (write protected)
        /// </summary>
        private bool ST3WP
        {
            get { return GetBit(6, _Reg3); }
            set
            {
                if (value) { SetBit(6, ref _Reg3); }
                else { UnSetBit(6, ref _Reg3); }
            }
        }

        /// <summary>
        /// Fault (if supported: 1=Drive failure)
        /// </summary>
        private bool ST3FT
        {
            get { return GetBit(7, _Reg3); }
            set
            {
                if (value) { SetBit(7, ref _Reg3); }
                else { UnSetBit(7, ref _Reg3); }
            }
        }

        #endregion

    */
    }
}
