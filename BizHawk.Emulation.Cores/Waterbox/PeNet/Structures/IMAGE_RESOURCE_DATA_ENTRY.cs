using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The IMAGE_RESOURCE_DATA_ENTRY points to the data of
    ///     the resources in the PE file like version info, strings etc.
    /// </summary>
    public class IMAGE_RESOURCE_DATA_ENTRY : AbstractStructure
    {
        /// <summary>
        ///     Construct a IMAGE_RESOURCE_DATA_ENTRY at a given offset.
        /// </summary>
        /// <param name="buff">PE file as a byte array.</param>
        /// <param name="offset">Offset to the structure in the file.</param>
        public IMAGE_RESOURCE_DATA_ENTRY(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Offset to the data of the resource.
        /// </summary>
        public uint OffsetToData
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        ///     Size of the resource data.
        /// </summary>
        public uint Size1
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Code Page
        /// </summary>
        public uint CodePage
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        ///     Reserved
        /// </summary>
        public uint Reserved
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_RESOURCE_DATA_ENTRY\n");
            sb.Append(this.PropertiesToString("{0,-20}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}