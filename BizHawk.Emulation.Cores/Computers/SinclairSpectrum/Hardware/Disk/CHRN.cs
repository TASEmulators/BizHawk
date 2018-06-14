
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Used for the sector CHRN structure
    /// </summary>
    public class CHRN
    {
        /// <summary>
        /// Track
        /// </summary>
        public byte C { get; set; }

        /// <summary>
        /// Side
        /// </summary>
        public byte H { get; set; }

        /// <summary>
        /// Sector ID
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// Sector Size
        /// </summary>
        public byte N { get; set; }

        /// <summary>
        /// Status register 1
        /// </summary>
        private byte _flag1;
        public byte Flag1
        {
            get { return _flag1; }
            set { _flag1 = value; }
        }

        /// <summary>
        /// Status register 2
        /// </summary>
        private byte _flag2;
        public byte Flag2
        {
            get { return _flag2; }
            set { _flag2 = value; }
        }

        /// <summary>
        /// Used to store the last transmitted/received data bytes
        /// </summary>
        public byte[] DataBytes { get; set; }

        /// <summary>
        /// ID for the read/write data command
        /// </summary>
        public int DataID { get; set; }

        #region Helper Methods

        /// <summary>
        /// Missing Address Mark (Sector_ID or DAM not found)
        /// </summary>
        public bool ST1MA
        {
            get { return NECUPD765.GetBit(0, _flag1); }
            set
            {
                if (value) { NECUPD765.SetBit(0, ref _flag1); }
                else { NECUPD765.UnSetBit(0, ref _flag1); }
            }
        }

        /// <summary>
        /// No Data (Sector_ID not found, CRC fail in ID_field)
        /// </summary>
        public bool ST1ND
        {
            get { return NECUPD765.GetBit(2, _flag1); }
            set
            {
                if (value) { NECUPD765.SetBit(2, ref _flag1); }
                else { NECUPD765.UnSetBit(2, ref _flag1); }
            }
        }

        /// <summary>
        /// Data Error (CRC-fail in ID- or Data-Field)
        /// </summary>
        public bool ST1DE
        {
            get { return NECUPD765.GetBit(5, _flag1); }
            set
            {
                if (value) { NECUPD765.SetBit(5, ref _flag1); }
                else { NECUPD765.UnSetBit(5, ref _flag1); }
            }
        }

        /// <summary>
        /// End of Track (set past most read/write commands) (see IC)
        /// </summary>
        public bool ST1EN
        {
            get { return NECUPD765.GetBit(7, _flag1); }
            set
            {
                if (value) { NECUPD765.SetBit(7, ref _flag1); }
                else { NECUPD765.UnSetBit(7, ref _flag1); }
            }
        }

        /// <summary>
        /// Missing Address Mark in Data Field (DAM not found)
        /// </summary>
        public bool ST2MD
        {
            get { return NECUPD765.GetBit(0, _flag2); }
            set
            {
                if (value) { NECUPD765.SetBit(0, ref _flag2); }
                else { NECUPD765.UnSetBit(0, ref _flag2); }
            }
        }

        /// <summary>
        /// Bad Cylinder (read/programmed track-ID different and read-ID = FF)
        /// </summary>
        public bool ST2BC
        {
            get { return NECUPD765.GetBit(1, _flag2); }
            set
            {
                if (value) { NECUPD765.SetBit(1, ref _flag2); }
                else { NECUPD765.UnSetBit(1, ref _flag2); }
            }
        }

        /// <summary>
        /// Wrong Cylinder (read/programmed track-ID different) (see b1)
        /// </summary>
        public bool ST2WC
        {
            get { return NECUPD765.GetBit(4, _flag2); }
            set
            {
                if (value) { NECUPD765.SetBit(4, ref _flag2); }
                else { NECUPD765.UnSetBit(4, ref _flag2); }
            }
        }

        /// <summary>
        /// Data Error in Data Field (CRC-fail in data-field)
        /// </summary>
        public bool ST2DD
        {
            get { return NECUPD765.GetBit(5, _flag2); }
            set
            {
                if (value) { NECUPD765.SetBit(5, ref _flag2); }
                else { NECUPD765.UnSetBit(5, ref _flag2); }
            }
        }

        /// <summary>
        /// Control Mark (read/scan command found sector with deleted DAM)
        /// </summary>
        public bool ST2CM
        {
            get { return NECUPD765.GetBit(6, _flag2); }
            set
            {
                if (value) { NECUPD765.SetBit(6, ref _flag2); }
                else { NECUPD765.UnSetBit(6, ref _flag2); }
            }
        }

        #endregion
    }
}
