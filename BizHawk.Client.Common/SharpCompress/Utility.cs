using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
#if NETCORE
using SharpCompress.Buffers;
#endif
using SharpCompress.Readers;

namespace SharpCompress
{
    internal static class Utility
    {
        public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> items)
        {
            return new ReadOnlyCollection<T>(items.ToList());
        }

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        public static int URShift(int number, int bits)
        {
            if (number >= 0)
            {
                return number >> bits;
            }
            return (number >> bits) + (2 << ~bits);
        }

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        public static long URShift(long number, int bits)
        {
            if (number >= 0)
            {
                return number >> bits;
            }
            return (number >> bits) + (2L << ~bits);
        }

        /// <summary>
        /// Fills the array with an specific value from an specific index to an specific index.
        /// </summary>
        /// <param name="array">The array to be filled.</param>
        /// <param name="fromindex">The first index to be filled.</param>
        /// <param name="toindex">The last index to be filled.</param>
        /// <param name="val">The value to fill the array with.</param>
        public static void Fill<T>(T[] array, int fromindex, int toindex, T val) where T : struct
        {
            if (array.Length == 0)
            {
                throw new NullReferenceException();
            }
            if (fromindex > toindex)
            {
                throw new ArgumentException();
            }
            if ((fromindex < 0) || array.Length < toindex)
            {
                throw new IndexOutOfRangeException();
            }
            for (int index = (fromindex > 0) ? fromindex-- : fromindex; index < toindex; index++)
            {
                array[index] = val;
            }
        }

#if NET45
        // super fast memset, up to 40x faster than for loop on large arrays
        // see https://stackoverflow.com/questions/1897555/what-is-the-equivalent-of-memset-in-c
        private static readonly Action<IntPtr, byte, uint> MemsetDelegate = CreateMemsetDelegate();

        private static Action<IntPtr, byte, uint> CreateMemsetDelegate() {
            var dynamicMethod = new DynamicMethod(
                "Memset",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                null,
                new[] { typeof(IntPtr), typeof(byte), typeof(uint) },
                typeof(Utility),
                true);
            var generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Initblk);
            generator.Emit(OpCodes.Ret);
            return (Action<IntPtr, byte, uint>)dynamicMethod.CreateDelegate(typeof(Action<IntPtr, byte, uint>));
        }

        public static void Memset(byte[] array, byte what, int length)
        {
            var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            MemsetDelegate(gcHandle.AddrOfPinnedObject(), what, (uint)length);
            gcHandle.Free();
        }
#else
        public static void Memset(byte[] array, byte what, int length)
        {
            for(var i = 0; i < length; i++)
            {
                array[i] = what;
            }
        }
#endif

        public static void Memset<T>(T[] array, T what, int length)
        {
            for(var i = 0; i < length; i++)
            {
                array[i] = what;
            }
        }

        public static void FillFast<T>(T[] array, T val) where T : struct
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = val;
            }
        }

        public static void FillFast<T>(T[] array, int start, int length, T val) where T : struct
        {
            int toIndex = start + length;
            for (int i = start; i < toIndex; i++)
            {
                array[i] = val;
            }
        }


        /// <summary>
        /// Fills the array with an specific value.
        /// </summary>
        /// <param name="array">The array to be filled.</param>
        /// <param name="val">The value to fill the array with.</param>
        public static void Fill<T>(T[] array, T val) where T : struct
        {
            Fill(array, 0, array.Length, val);
        }

        public static void SetSize(this List<byte> list, int count)
        {
            if (count > list.Count)
            {
                for (int i = list.Count; i < count; i++)
                {
                    list.Add(0x0);
                }
            }
            else
            {
                byte[] temp = new byte[count];
                list.CopyTo(temp, 0);
                list.Clear();
                list.AddRange(temp);
            }
        }

        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            foreach (T item in source)
            {
                destination.Add(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (T item in items)
            {
                action(item);
            }
        }
        
        public static void Copy(Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length)
        {
            if (sourceIndex > Int32.MaxValue || sourceIndex < Int32.MinValue)
                throw new ArgumentOutOfRangeException();
            if (destinationIndex > Int32.MaxValue || destinationIndex < Int32.MinValue)
                throw new ArgumentOutOfRangeException();
            if (length > Int32.MaxValue || length < Int32.MinValue)
                throw new ArgumentOutOfRangeException();

            Array.Copy(sourceArray, (int)sourceIndex, destinationArray, (int)destinationIndex, (int)length);
        }

        public static IEnumerable<T> AsEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static void CheckNotNull(this object obj, string name)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static void CheckNotNullOrEmpty(this string obj, string name)
        {
            obj.CheckNotNull(name);
            if (obj.Length == 0)
            {
                throw new ArgumentException("String is empty.");
            }
        }

        public static void Skip(this Stream source, long advanceAmount)
        {
            if (source.CanSeek)
            {
                source.Position += advanceAmount;
                return;
            }

            byte[] buffer = GetTransferByteArray();
            try
            {
                int read = 0;
                int readCount = 0;
                do
                {
                    readCount = buffer.Length;
                    if (readCount > advanceAmount)
                    {
                        readCount = (int)advanceAmount;
                    }
                    read = source.Read(buffer, 0, readCount);
                    if (read <= 0)
                    {
                        break;
                    }
                    advanceAmount -= read;
                    if (advanceAmount == 0)
                    {
                        break;
                    }
                }
                while (true);
            }
            finally
            {
#if NETCORE
                ArrayPool<byte>.Shared.Return(buffer);
#endif
            }
        }

        public static void Skip(this Stream source)
        {
            byte[] buffer = GetTransferByteArray();
            try
            {
                do
                {
                }
                while (source.Read(buffer, 0, buffer.Length) == buffer.Length);
            }
            finally
            {
#if NETCORE
                ArrayPool<byte>.Shared.Return(buffer);
#endif
            }
        }

        public static DateTime DosDateToDateTime(UInt16 iDate, UInt16 iTime)
        {
            int year = iDate / 512 + 1980;
            int month = iDate % 512 / 32;
            int day = iDate % 512 % 32;
            int hour = iTime / 2048;
            int minute = iTime % 2048 / 32;
            int second = iTime % 2048 % 32 * 2;

            if (iDate == UInt16.MaxValue || month == 0 || day == 0)
            {
                year = 1980;
                month = 1;
                day = 1;
            }

            if (iTime == UInt16.MaxValue)
            {
                hour = minute = second = 0;
            }

            DateTime dt;
            try
            {
                dt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
            }
            catch
            {
                dt = new DateTime();
            }
            return dt;
        }

        public static uint DateTimeToDosTime(this DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return 0;
            }

            var localDateTime = dateTime.Value.ToLocalTime();

            return (uint)(
                             (localDateTime.Second / 2) | (localDateTime.Minute << 5) | (localDateTime.Hour << 11) |
                             (localDateTime.Day << 16) | (localDateTime.Month << 21) |
                             ((localDateTime.Year - 1980) << 25));
        }

        public static DateTime DosDateToDateTime(UInt32 iTime)
        {
            return DosDateToDateTime((UInt16)(iTime / 65536),
                                     (UInt16)(iTime % 65536));
        }

        /// <summary>
        /// Convert Unix time value to a DateTime object.
        /// </summary>
        /// <param name="unixtime">The Unix time stamp you want to convert to DateTime.</param>
        /// <returns>Returns a DateTime object that represents value of the Unix time.</returns>
        public static DateTime UnixTimeToDateTime(long unixtime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return sTime.AddSeconds(unixtime);
        }

        public static long TransferTo(this Stream source, Stream destination)
        {
            byte[] array = GetTransferByteArray();
            try
            {
                int count;
                long total = 0;
                while (ReadTransferBlock(source, array, out count))
                {
                    total += count;
                    destination.Write(array, 0, count);
                }
                return total;
            }
            finally
            {
#if NETCORE
                ArrayPool<byte>.Shared.Return(array);
#endif
            }
        }

        public static long TransferTo(this Stream source, Stream destination, Common.Entry entry, IReaderExtractionListener readerExtractionListener)
        {
            byte[] array = GetTransferByteArray();
            try
            {
                int count;
                var iterations = 0;
                long total = 0;
                while (ReadTransferBlock(source, array, out count))
                {
                    total += count;
                    destination.Write(array, 0, count);
                    iterations++;
                    readerExtractionListener.FireEntryExtractionProgress(entry, total, iterations);
                }
                return total;
            }
            finally
            {
#if NETCORE
                ArrayPool<byte>.Shared.Return(array);
#endif
            }
        }

        private static bool ReadTransferBlock(Stream source, byte[] array, out int count)
        {
            return (count = source.Read(array, 0, array.Length)) != 0;
        }

        private static byte[] GetTransferByteArray()
        {
#if NETCORE
            return ArrayPool<byte>.Shared.Rent(81920);
#else
            return new byte[81920];
#endif
        }

        public static bool ReadFully(this Stream stream, byte[] buffer)
        {
            int total = 0;
            int read;
            while ((read = stream.Read(buffer, total, buffer.Length - total)) > 0)
            {
                total += read;
                if (total >= buffer.Length)
                {
                    return true;
                }
            }
            return (total >= buffer.Length);
        }

        public static string TrimNulls(this string source)
        {
            return source.Replace('\0', ' ').Trim();
        }

        public static bool BinaryEquals(this byte[] source, byte[] target)
        {
            if (source.Length != target.Length)
            {
                return false;
            }
            for (int i = 0; i < source.Length; ++i)
            {
                if (source[i] != target[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}