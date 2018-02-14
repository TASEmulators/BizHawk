using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents a tape block
    /// </summary>
    public class TapeDataBlock
    {
        /// <summary>
        /// Either the TZX block ID, or -1 in the case of non-tzx blocks
        /// </summary>
        private int _blockID = -1;
        public int BlockID
        {
            get { return _blockID; }
            set { _blockID = value; }
        }

        /// <summary>
        /// Description of the block
        /// </summary>
        private string _blockDescription;
        public string BlockDescription
        {
            get { return _blockDescription; }
            set { _blockDescription = value; }
        }

        /// <summary>
        /// Byte array containing the raw block data
        /// </summary>
        private byte[] _blockData;
        public byte[] BlockData
        {
            get { return _blockData; }
            set { _blockData = value; }
        }

        /// <summary>
        /// List containing the pulse timing values
        /// </summary>
        public List<int> DataPeriods = new List<int>();

        /// <summary>
        /// Command that is raised by this data block
        /// (that may or may not need to be acted on)
        /// </summary>
        private TapeCommand _command = TapeCommand.NONE;
        public TapeCommand Command
        {
            get { return _command; }
            set { _command = value; }
        }

        /// <summary>
        /// Returns the data periods as an array
        /// (primarily to aid in bizhawk state serialization)
        /// </summary>
        /// <returns></returns>
        public int[] GetDataPeriodsArray()
        {
            return DataPeriods.ToArray();
        }

        /// <summary>
        /// Accepts an array of data periods and updates the DataPeriods list accordingly
        /// (primarily to aid in bizhawk state serialization)
        /// </summary>
        /// <returns></returns>
        public void SetDataPeriodsArray(int[] periodArray)
        {
            DataPeriods = new List<int>();

            if (periodArray == null)
                return;

            DataPeriods = periodArray.ToList();
        }

        /// <summary>
        /// Bizhawk state serialization
        /// </summary>
        /// <param name="ser"></param>
        public void SyncState(Serializer ser, int blockPosition)
        {
            ser.BeginSection("DataBlock" + blockPosition);

            ser.Sync("_blockID", ref _blockID);
            ser.SyncFixedString("_blockDescription", ref _blockDescription, 50);
            ser.Sync("_blockData", ref _blockData, true);
            ser.SyncEnum("_command", ref _command);

            int[] tempArray = null;

            if (ser.IsWriter)
            {
                tempArray = GetDataPeriodsArray();
                ser.Sync("_periods", ref tempArray, true);
            }
            else
            {
                ser.Sync("_periods", ref tempArray, true);
                SetDataPeriodsArray(tempArray);
            }

            ser.EndSection();
        }
    }
}
