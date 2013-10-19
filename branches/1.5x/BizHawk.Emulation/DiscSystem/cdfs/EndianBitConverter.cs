using System;
using System.Collections.Generic;
using System.Text;

namespace ISOParser {
    /// <summary>
    /// Helper class to convert big and little endian numbers from a byte
    /// array to a value.
    /// 
    /// This code was modified from the endian bit converter presented by
    /// Robert Unoki in his blog post:
    /// http://blogs.msdn.com/robunoki/archive/2006/04/05/568737.aspx
    /// 
    /// I have added support for more data types and the ability to
    /// specify an offset into the array to be converted where the value
    /// begins.
    /// </summary>
    public class EndianBitConverter {
        #region Static Constructors

        /// <summary>
        /// Build a converter from little endian to the system endian-ness.
        /// </summary>
        /// <returns>The converter</returns>
        public static EndianBitConverter CreateForLittleEndian() {
            return new EndianBitConverter(!BitConverter.IsLittleEndian);
        }

        /// <summary>
        /// Build a converter from big endian to the system endian-ness.
        /// </summary>
        /// <returns>The converter</returns>
        public static EndianBitConverter CreateForBigEndian() {
            return new EndianBitConverter(BitConverter.IsLittleEndian);
        }

        #endregion

        #region Private Properties

        /// <summary>
        /// Keep track of whether we need to swap the bytes or not
        /// </summary>
        private bool swap;

        #endregion

        #region Private Constructor

        /// <summary>
        /// Create the converter with the given endian-ness.
        /// </summary>
        /// <param name="swapBytes">Whether or not to swap bytes.</param>
        private EndianBitConverter(bool swapBytes) {
            swap = swapBytes;
        }

        #endregion

        #region 16-bit

        public Int16 ToInt16(byte[] data) {
            return ToInt16(data, 0);
        }
        public Int16 ToInt16(byte[] data, int offset) {
            byte[] corrected;
            if (swap) {
                corrected = (byte[])data.Clone();
                Array.Reverse(corrected, offset, 2);
            }
            else {
                corrected = data;
            }
            return BitConverter.ToInt16(corrected, offset);
        }

        #endregion

        #region 32-bit

        public Int32 ToInt32(byte[] data) {
            return ToInt32(data, 0);
        }
        public Int32 ToInt32(byte[] data, int offset) {
            byte[] corrected;
            if (swap) {
                corrected = (byte[])data.Clone();
                Array.Reverse(corrected, offset, 4);
            }
            else {
                corrected = data;
            }
            return BitConverter.ToInt32(corrected, offset);
        }

        #endregion

        #region 64-bit

        public Int64 ToInt64(byte[] data) {
            return ToInt64(data, 0);
        }
        public Int64 ToInt64(byte[] data, int offset) {
            byte[] corrected;
            if (swap) {
                corrected = (byte[])data.Clone();
                Array.Reverse(corrected, offset, 8);
            }
            else {
                corrected = data;
            }
            return BitConverter.ToInt64(corrected, offset);
        }

        #endregion
    }
}
