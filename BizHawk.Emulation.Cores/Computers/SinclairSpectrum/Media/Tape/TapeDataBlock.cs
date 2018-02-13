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
        public int BlockID = -1;

        /// <summary>
        /// Description of the block
        /// </summary>
        public string BlockDescription { get; set; }

        /// <summary>
        /// Byte array containing the raw block data
        /// </summary>
        public byte[] BlockData = null;

        /// <summary>
        /// List containing the pulse timing values
        /// </summary>
        public List<int> DataPeriods = new List<int>();

        /// <summary>
        /// Command that is raised by this data block
        /// (that may or may not need to be acted on)
        /// </summary>
        public TapeCommand Command = TapeCommand.NONE;

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
    }
}
