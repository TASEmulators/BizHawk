using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents a disk sector
    /// </summary>
    public class Sector
    {
        /// <summary>
        /// Used when the FDD returns the sector
        /// False means the sector does not exist
        /// </summary>
        public bool IsValid = true;

        /// <summary>
        /// track (equivalent to C parameter in NEC765 commands)
        /// </summary>
        private byte _track;
        public byte Track
        {
            get { return _track; }
            set { _track = value; }
        }

        /// <summary>
        /// side (equivalent to H parameter in NEC765 commands)
        /// </summary>
        private byte _side;
        public byte Side
        {
            get { return _side; }
            set { _side = value; }
        }

        /// <summary>
        /// sector ID (equivalent to R parameter in NEC765 commands)
        /// </summary>
        private byte _sectorID;
        public byte SectorID
        {
            get { return _sectorID; }
            set { _sectorID = value; }
        }

        /// <summary>
        /// sector size (equivalent to N parameter in NEC765 commands)
        /// </summary>
        private byte _sectorSize;
        public byte SectorSize
        {
            get { return _sectorSize; }
            set { _sectorSize = value; }
        }

        /// <summary>
        /// FDC status register 1 (equivalent to NEC765 ST1 status register)
        /// </summary>
        private byte _sT1;
        public byte ST1
        {
            get { return _sT1; }
            set { _sT1 = value; }
        }

        /// <summary>
        /// FDC status register 2 (equivalent to NEC765 ST2 status register)
        /// </summary>
        private byte _sT2;
        public byte ST2
        {
            get { return _sT2; }
            set { _sT2 = value; }
        }

        /// <summary>
        /// Actual total data length in bytes
        /// If this is greater than the sector size specified by N, then multiple copies
        /// of the sector data are present (representing weak/random copy protection data)
        /// </summary>
        private int _totalDataLength;
        public int TotalDataLength
        {
            get { return _totalDataLength; }
            set { _totalDataLength = value; }
        }

        /// <summary>
        /// The sector data entry point
        /// If multiple sector data is found this should return a randomly chosen data for this sector
        /// </summary>
        public byte[] SectorData
        {
            get
            {
                // number of sector data copies found
                int count = _sectorDatas.Count();

                if (count <= 0)
                {
                    // no sectors present
                    return new byte[0];
                }

                if (count == 1)
                {
                    // only one copy of data found - return this
                    return _sectorDatas.First();
                }

                if (count > 1)
                {
                    // there is more than one copy of sector data stored
                    // return at random to simulate weak/random copy protection sectors
                    Random rnd = new Random();
                    int pnt = rnd.Next(0, count - 1);
                    return _sectorDatas[pnt];
                }

                // probably shouldnt ever get this far
                return null;
            }
        }

        /// <summary>
        /// Adds sector data based on the copy index
        /// </summary>
        /// <param name="position"></param>
        public void AddSectorData(int position, byte[] data)
        {
            if (_sectorDatas.ElementAtOrDefault(position) == null)
            {
                // create byte arrays for uninstantiated indexes
                for (int i = 0; i < position + 1; i++)
                {
                    if (_sectorDatas.ElementAtOrDefault(i) == null)
                    {
                        _sectorDatas.Add(new byte[0]);
                    }
                }
            }

            _sectorDatas[position] = data;
        }

        /// <summary>
        /// Internal storage for sector data
        /// </summary>
        private List<byte[]> _sectorDatas = new List<byte[]>();


        /// <summary>
        /// Returns a CHRN object for this sector
        /// </summary>
        /// <returns></returns>
        public CHRN GetCHRN()
        {
            return new CHRN
            {
                C = Track,
                H = Side,
                R = SectorID,
                N = SectorSize,
                Flag1 = ST1,
                Flag2 = ST2,
                DataBytes = SectorData                
            };
        }

        

        /// <summary>
        /// State serialization
        /// Should only be called for the ActiveSector object in the floppy drive
        /// </summary>
        /// <param name="ser"></param>
        public void SyncState(Serializer ser)
        {
            ser.BeginSection("ActiveSector");

            ser.Sync("IsValid", ref IsValid);
            ser.Sync("_track", ref _track);
            ser.Sync("_side", ref _side);
            ser.Sync("_sectorID", ref _sectorID);
            ser.Sync("_sectorSize", ref _sectorSize);
            ser.Sync("_sT1", ref _sT1);
            ser.Sync("_sT2", ref _sT2);
            ser.Sync("_totalDataLength", ref _totalDataLength);

            if (ser.IsReader)
            {
                ser.Sync("SecCopySize", ref SecCopySize);

                byte[][] sds = new byte[SecCopySize][];

                for (int i = 0; i < SecCopySize; i++)
                {
                    ser.Sync("sec" + i, ref sds[i], false);
                }

                _sectorDatas = sds.ToList();                
            }

            if (ser.IsWriter)
            {
                SecCopySize = _sectorDatas.Count();
                ser.Sync("SecCopySize", ref SecCopySize);

                byte[][] sds = _sectorDatas.ToArray();

                for (int i = 0; i < SecCopySize; i++)
                {
                    ser.Sync("sec" + i, ref sds[i], false);
                }
            }

            ser.EndSection();
        }

        // Sector array size (for state serialization)
        private int SecCopySize;
    }
}
