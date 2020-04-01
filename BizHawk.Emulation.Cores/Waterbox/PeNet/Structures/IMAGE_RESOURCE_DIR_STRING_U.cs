using System;
using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     Represents a Unicode string used for resource names
    ///     in the resource section.
    /// </summary>
    public class IMAGE_RESOURCE_DIR_STRING_U : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_RESOURCE_DIR_STRING_U Unicode string.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset of the string.</param>
        public IMAGE_RESOURCE_DIR_STRING_U(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Length of the string in Unicode characters, *not* in bytes.
        ///     1 Unicode char = 2 bytes.
        /// </summary>
        public ushort Length
        {
            get { return Buff.BytesToUInt16(Offset); }
            set { Buff.SetUInt16(Offset, value); }
        }

        /// <summary>
        ///     The Unicode string as a .Net string.
        /// </summary>
        public string NameString
        {
            get
            {
                var subarray = new byte[Length*2];
                Array.Copy(Buff, Offset + 2, subarray, 0, Length*2);

                return Encoding.Unicode.GetString(subarray);
            }
        }
    }
}