using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Abtract class that represents all Media Serializers
    /// </summary>
    public abstract class MediaSerializer
    {
        /// <summary>
        /// The type of serializer
        /// </summary>
        public abstract MediaSerializationType FormatType { get; }

        /// <summary>
        /// Signs whether this class can be used to serialize
        /// </summary>
        public virtual bool IsSerializer
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Signs whether this class can be used to de-serialize
        /// </summary>
        public virtual bool IsDeSerializer
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Serialization method
        /// </summary>
        /// <param name="data"></param>
        public virtual void Serialize(byte[] data)
        {
            throw new NotImplementedException(this.GetType().ToString() + 
                "Serialize operation is not implemented for this serializer");
        }

        /// <summary>
        /// DeSerialization method
        /// </summary>
        /// <param name="data"></param>
        public virtual void DeSerialize(byte[] data)
        {
            throw new NotImplementedException(this.GetType().ToString() + 
                "DeSerialize operation is not implemented for this serializer");
        }

        #region Static Tools

        /// <summary>
        /// Converts an int32 value into a byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static byte[] GetBytes(int value)
        {
            byte[] buf = new byte[4];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            buf[2] = (byte)(value >> 16);
            buf[3] = (byte)(value >> 24);
            return buf;
        }

        /// <summary>
        /// Returns an int32 from a byte array based on offset
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <returns></returns>
        protected static int GetInt32(byte[] buf, int offsetIndex)
        {
            return buf[offsetIndex] | buf[offsetIndex + 1] << 8 | buf[offsetIndex + 2] << 16 | buf[offsetIndex + 3] << 24;
        }

        /// <summary>
        /// Returns an uint16 from a byte array based on offset
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <returns></returns>
        protected static ushort GetWordValue(byte[] buf, int offsetIndex)
        {
            return (ushort)(buf[offsetIndex] | buf[offsetIndex + 1] << 8);
        }

        /// <summary>
        /// Updates a byte array with a uint16 value based on offset
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <param name="value"></param>
        protected static void SetWordValue(byte[] buf, int offsetIndex, ushort value)
        {
            buf[offsetIndex] = (byte)value;
            buf[offsetIndex + 1] = (byte)(value >> 8);
        }

        #endregion
    }
}
