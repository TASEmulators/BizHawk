using BizHawk.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            set {
                _blockID = value;

                if (MetaData == null)
                    MetaData = new Dictionary<BlockDescriptorTitle, string>();

                AddMetaData(BlockDescriptorTitle.Block_ID, value.ToString());
            }
        }

        /// <summary>
        /// The block type
        /// </summary>
        private BlockType _blockType;
        public BlockType BlockDescription
        {
            get { return _blockType; }
            set {
                _blockType = value;
                if (MetaData == null)
                    MetaData = new Dictionary<BlockDescriptorTitle, string>();
            }
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

		/*

        /// <summary>
        /// An array of bytearray encoded strings (stored in this format for easy Bizhawk serialization)
        /// Its basically tape information
        /// </summary>
        private byte[][] _tapeDescriptionData; 

        /// <summary>
        /// Returns the Tape Description Data in a human readable format
        /// </summary>
        public List<string> TapeDescriptionData
        {
            get
            {
                List<string> data = new List<string>();

                foreach (byte[] b in _tapeDescriptionData)
                {
                    data.Add(Encoding.ASCII.GetString(b));
                }

                return data;
            }
        }
		*/


        #region Block Meta Data

        /// <summary>
        /// Dictionary of block related data
        /// </summary>
        public Dictionary<BlockDescriptorTitle, string> MetaData { get; set; }

        /// <summary>
        /// Adds a single metadata item to the Dictionary
        /// </summary>
        public void AddMetaData(BlockDescriptorTitle descriptor, string data)
        {
            // check whether entry already exists
            bool check = MetaData.ContainsKey(descriptor);
            if (check)
            {
                // already exists - update
                MetaData[descriptor] = data;
            }
            else
            {
                // create new
                MetaData.Add(descriptor, data);
            }
        }

        #endregion



        /// <summary>
        /// List containing the pulse timing values
        /// </summary>
        public List<int> DataPeriods = new List<int>();

        public bool InitialPulseLevel;

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
        /// The defined post-block pause
        /// </summary>
        private int _pauseInMS;
        public int PauseInMS
        {
            get { return _pauseInMS; }
            set { _pauseInMS = value; }
        }


        /// <summary>
        /// Returns the data periods as an array
        /// (primarily to aid in bizhawk state serialization)
        /// </summary>
        public int[] GetDataPeriodsArray()
        {
            return DataPeriods.ToArray();
        }

        /// <summary>
        /// Accepts an array of data periods and updates the DataPeriods list accordingly
        /// (primarily to aid in bizhawk state serialization)
        /// </summary>
        public void SetDataPeriodsArray(int[] periodArray)
        {
            DataPeriods = periodArray?.ToList() ?? new List<int>();
        }

        /// <summary>
        /// Bizhawk state serialization
        /// </summary>
        public void SyncState(Serializer ser, int blockPosition)
        {
            ser.BeginSection("DataBlock" + blockPosition);

            ser.Sync(nameof(_blockID), ref _blockID);
            //ser.SyncFixedString(nameof(_blockDescription), ref _blockDescription, 200);
            ser.SyncEnum(nameof(_blockType), ref _blockType);
            ser.Sync(nameof(_blockData), ref _blockData, true);
            ser.SyncEnum(nameof(_command), ref _command);

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

    /// <summary>
    /// The types of TZX blocks
    /// </summary>
    public enum BlockType
    {
        Standard_Speed_Data_Block = 0x10,
        Turbo_Speed_Data_Block = 0x11,
        Pure_Tone = 0x12,
        Pulse_Sequence = 0x13,
        Pure_Data_Block = 0x14,
        Direct_Recording = 0x15,
        CSW_Recording = 0x18,
        Generalized_Data_Block = 0x19,
        Pause_or_Stop_the_Tape = 0x20,
        Group_Start = 0x21,
        Group_End = 0x22,
        Jump_to_Block = 0x23,
        Loop_Start = 0x24,
        Loop_End = 0x25,
        Call_Sequence = 0x26,
        Return_From_Sequence = 0x27,
        Select_Block = 0x28,
        Stop_the_Tape_48K = 0x2A,
        Set_Signal_Level = 0x2B,
        Text_Description = 0x30,
        Message_Block = 0x31,
        Archive_Info = 0x32,
        Hardware_Type = 0x33,
        Custom_Info_Block = 0x35,
        Glue_Block = 0x5A,

        // depreciated blocks
        C64_ROM_Type_Data_Block = 0x16,
        C64_Turbo_Tape_Data_Block = 0x17,
        Emulation_Info = 0x34,
        Snapshot_Block = 0x40,

        // unsupported / undetected
        Unsupported,

        // PZX blocks
        PZXT,
        PULS,
        DATA,
        BRWS,
        PAUS,

        // zxhawk proprietry
        PAUSE_BLOCK,

        WAV_Recording
    }
    

    /// <summary>
    /// Different title possibilities
    /// </summary>
    public enum BlockDescriptorTitle
    {
        Undefined,
        Block_ID,
        Program,
        Data_Bytes,
        Bytes,

        Pilot_Pulse_Length,
        Pilot_Pulse_Count,
        First_Sync_Length,
        Second_Sync_Length,
        Zero_Bit_Length,
        One_Bit_Length,
        Data_Length,
        Bits_In_Last_Byte,
        Pause_After_Data,

        Pulse_Length,
        Pulse_Count,

        Text_Description,
        Title,
        Publisher,
        Author,
        Year,
        Language,
        Type,
        Price,
        Protection,
        Origin,
        Comments,

        Needs_Parsing
    }
}
