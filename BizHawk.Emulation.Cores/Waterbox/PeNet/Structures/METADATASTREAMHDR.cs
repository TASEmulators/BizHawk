using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    /// The Meta Data Stream Header contains information about data streams (sections)
    /// in a .Net assembly.
    /// </summary>
    public class METADATASTREAMHDR : AbstractStructure
    {
        internal uint HeaderLength => GetHeaderLength();

        /// <summary>
        /// Create a new Meta Data Stream Header instance from a byte array.
        /// </summary>
        /// <param name="buff">Buffer which contains a Meta Data Stream Header.</param>
        /// <param name="offset">Offset in the buffer, where the header starts.</param>
        public METADATASTREAMHDR(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        /// <summary>
        /// Relative offset (from Meta Data Header) to 
        /// the stream.
        /// </summary>
        public uint offset
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        /// Size of the stream content.
        /// </summary>
        public uint size
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        /// Name of the stream.
        /// </summary>
        public string streamName => ParseStreamName(Offset + 0x8);

        private uint GetHeaderLength()
        {
            var maxHeaderLength = 100;
            var headerLength = 0;
            for (var inHdrOffset = 8; inHdrOffset < maxHeaderLength; inHdrOffset++)
            {
                if (Buff[Offset + inHdrOffset] == 0x00)
                {
                    headerLength = inHdrOffset;
                    break;
                }
                    
            }

            return (uint) AddHeaderPaddingLength(headerLength);
        }

        private int AddHeaderPaddingLength(int headerLength)
        {
            if (headerLength%4 == 0)
                return headerLength + 4;
            else
            {
                return headerLength + (4-(headerLength%4));
            }
        }

        private string ParseStreamName(uint nameOffset)
        {
            return Buff.GetCString(nameOffset);
        }

        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("METADATASTREAMHDR\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}