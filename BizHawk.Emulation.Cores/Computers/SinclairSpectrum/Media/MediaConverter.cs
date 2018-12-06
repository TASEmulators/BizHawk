using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Abtract class that represents all Media Converters
    /// </summary>
    public abstract class MediaConverter
    {
        /// <summary>
        /// The type of serializer
        /// </summary>
        public abstract MediaConverterType FormatType { get; }

        /// <summary>
        /// Signs whether this class can be used to read the data format
        /// </summary>
        public virtual bool IsReader
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Signs whether this class can be used to write the data format
        /// </summary>
        public virtual bool IsWriter
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
        public virtual void Read(byte[] data)
        {
            throw new NotImplementedException(this.GetType().ToString() + 
                "Read operation is not implemented for this converter");
        }

        /// <summary>
        /// DeSerialization method
        /// </summary>
        /// <param name="data"></param>
        public virtual void Write(byte[] data)
        {
            throw new NotImplementedException(this.GetType().ToString() + 
                "Write operation is not implemented for this converter");
        }

        /// <summary>
        /// Serializer does a quick check, returns TRUE if file is detected as this type
        /// </summary>
        /// <param name="data"></param>
        public virtual bool CheckType(byte[] data)
        {
            throw new NotImplementedException(this.GetType().ToString() +
                "Check type operation is not implemented for this converter");
        }

        #region Static Tools

        /// <summary>
        /// Converts an int32 value into a byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] GetBytes(int value)
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
        public static int GetInt32(byte[] buf, int offsetIndex)
        {
            return buf[offsetIndex] | buf[offsetIndex + 1] << 8 | buf[offsetIndex + 2] << 16 | buf[offsetIndex + 3] << 24;
        }

        /// <summary>
        /// Returns an int32 from a byte array based on offset (in BIG ENDIAN format)
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <returns></returns>
        public static int GetBEInt32(byte[] buf, int offsetIndex)
        {
            byte[] b = new byte[4];
            Array.Copy(buf, offsetIndex, b, 0, 4);
            byte[] buffer = b.Reverse().ToArray();
            int pos = 0;
            return buffer[pos++] | buffer[pos++] << 8 | buffer[pos++] << 16 | buffer[pos++] << 24;
        }

        /// <summary>
        /// Returns an int32 from a byte array based on the length of the byte array (in BIG ENDIAN format)
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static int GetBEInt32FromByteArray(byte[] buf)
        {
            byte[] b = buf.Reverse().ToArray();
            if (b.Length == 0)
                return 0;
            int res = b[0];
            int pos = 1;
            switch (b.Length)
            {
                case 1:
                default:
                    return res;
                case 2:
                    return res | b[pos] << (8 * pos++);
                case 3:
                    return res | b[pos] << (8 * pos++) | b[pos] << (8 * pos++);
                case 4:
                    return res | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++);
                case 5:
                    return res | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++);
                case 6:
                    return res | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++);
                case 7:
                    return res | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++) | b[pos] << (8 * pos++);
            }
        }

        /// <summary>
        /// Returns an int32 from a byte array based on offset
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <returns></returns>
        public static uint GetUInt32(byte[] buf, int offsetIndex)
        {
            return (uint)(buf[offsetIndex] | buf[offsetIndex + 1] << 8 | buf[offsetIndex + 2] << 16 | buf[offsetIndex + 3] << 24);
        }

        /// <summary>
        /// Returns an uint16 from a byte array based on offset
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <returns></returns>
        public static ushort GetWordValue(byte[] buf, int offsetIndex)
        {
            return (ushort)(buf[offsetIndex] | buf[offsetIndex + 1] << 8);
        }

        /// <summary>
        /// Updates a byte array with a uint16 value based on offset
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offsetIndex"></param>
        /// <param name="value"></param>
        public static void SetWordValue(byte[] buf, int offsetIndex, ushort value)
        {
            buf[offsetIndex] = (byte)value;
            buf[offsetIndex + 1] = (byte)(value >> 8);
        }

        /// <summary>
        /// Takes a PauseInMilliseconds value and returns the value in T-States
        /// </summary>
        /// <param name="pauseInMS"></param>
        /// <returns></returns>
        public static int TranslatePause(int pauseInMS)
        {
            // t-states per millisecond
            var tspms = (69888 * 50) / 1000;
            // get value
            int res = pauseInMS * tspms;

            return res;
        }

        /// <summary>
        /// Decompresses a byte array that is Z-RLE compressed
        /// </summary>
        /// <param name="sourceBuffer"></param>
        /// <param name="destBuffer"></param>
        public static void DecompressZRLE(byte[] sourceBuffer, ref byte[] destBuffer)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(sourceBuffer, 0, sourceBuffer.Length);
            stream.Position = 0;
            stream.ReadByte();
            stream.ReadByte();
            DeflateStream ds = new DeflateStream(stream, CompressionMode.Decompress, false);
            ds.Read(destBuffer, 0, destBuffer.Length);
        }


        public static byte[] SerializeRaw(object obj)
        {
            int rSize = Marshal.SizeOf(obj);
            IntPtr buff = Marshal.AllocHGlobal(rSize);
            Marshal.StructureToPtr(obj, buff, false);
            byte[] rData = new byte[rSize];
            Marshal.Copy(buff, rData, 0, rSize);
            return rData;
        }

        public static T DeserializeRaw<T>(byte[] rData, int pos)
        {
            int rSize = Marshal.SizeOf(typeof(T));
            if (rSize > rData.Length - pos)
                throw new Exception();
            IntPtr buff = Marshal.AllocHGlobal(rSize);
            Marshal.Copy(rData, pos, buff, rSize);
            T rObj = (T)Marshal.PtrToStructure(buff, typeof(T));
            Marshal.FreeHGlobal(buff);
            return rObj;
        }

        #endregion
    }
}
