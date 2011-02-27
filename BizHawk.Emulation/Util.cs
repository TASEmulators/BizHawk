using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BizHawk
{
    public struct Tuple<T1,T2> : IEquatable<Tuple<T1, T2>>
    {
        private readonly T1 first;
        private readonly T2 second;
        public T1 First { get { return first; } }
        public T2 Second { get { return second; } }

        public Tuple(T1 o1, T2 o2)
        {
            first = o1;
            second = o2;
        }

        public bool Equals(Tuple<T1, T2> other)
        {
            return first.Equals(other.first) &&
            second.Equals(other.second);
        }

        public override bool Equals(object obj)
        {
            if (obj is Tuple<T1, T2>)
                    return this.Equals((Tuple<T1, T2>)obj);
            else
                    return false;
        }

        public override int GetHashCode()
        {
            return first.GetHashCode() ^ second.GetHashCode();
        }
    }

    public static class Extensions
    {
        public static bool IsBinary(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '0' || c == '1')
                    continue;
                return false;
            }
            return true;
        }

        public static bool In(this string str, params string[] options)
        {
            foreach (string opt in options)
            {
                if (opt.Equals(str, StringComparison.CurrentCultureIgnoreCase)) return true;
            }
            return false;
        }

        public static bool In(this string str, IEnumerable<string> options)
        {
            foreach (string opt in options)
            {
                if (opt.Equals(str,StringComparison.CurrentCultureIgnoreCase)) return true;
            }
            return false;
        }

        public static bool In<T>(this string str, IEnumerable<T> options, Func<T, string, bool> eval)
        {
            foreach (T opt in options)
            {
                if (eval(opt, str) == true)
                    return true;
            }
            return false;
        }

        public static bool NotIn(this string str, params string[] options)
        {
            foreach (string opt in options)
            {
                if (opt.ToLower() == str.ToLower()) return false;
            }
            return true;
        }

        public static bool NotIn(this string str, IEnumerable<string> options)
        {
            foreach (string opt in options)
            {
                if (opt.ToLower() == str.ToLower()) return false;
            }
            return true;
        }
        
        public static bool In(this int i, params int[] options)
        {
            foreach (int j in options)
            {
                if (i == j) return true;
            }
            return false;
        }

        public static bool In(this int i, IEnumerable<int> options)
        {
            foreach (int j in options)
            {
                if (i == j) return true;
            }
            return false;
        }

        public static bool ContainsStartsWith(this IEnumerable<string> options, string str)
        {
            foreach (string opt in options)
            {
                if (opt.StartsWith(str)) return true;
            }
            return false;
        }

        public static string GetOptionValue(this IEnumerable<string> options, string str)
        {
            try
            {
                foreach (string opt in options)
                {
                    if (opt.StartsWith(str))
                    {
                        return opt.Split('=')[1];
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        public static bool IsValidRomExtentsion(this string str, params string[] romExtensions)
        {
            string strUpper = str.ToUpper();
            foreach (string ext in romExtensions)
            {
                if (strUpper.EndsWith(ext.ToUpper())) return true;
            }
            return false;
        }

        public static string ToCommaSeparated(this List<string> list)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(list[i]);
            }
            return sb.ToString();
        }

        public static void SaveAsHex(this byte[] buffer, TextWriter writer)
        {
            for (int i=0; i<buffer.Length; i++)
            {
                writer.Write("{0:X2}", buffer[i]);
            }
            writer.WriteLine();
        }

        public static void SaveAsHex(this byte[] buffer, TextWriter writer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                writer.Write("{0:X2}", buffer[i]);
            }
            writer.WriteLine();
        }

        public static void SaveAsHex(this short[] buffer, TextWriter writer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                writer.Write("{0:X4}", buffer[i]);
            }
            writer.WriteLine();
        }

        public static void SaveAsHex(this ushort[] buffer, TextWriter writer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                writer.Write("{0:X4}", buffer[i]);
            }
            writer.WriteLine();
        }

        public static void ReadFromHex(this byte[] buffer, string hex)
        {
            if (hex.Length % 2 != 0)
                throw new Exception("Hex value string does not appear to be properly formatted.");
            for (int i=0; i<buffer.Length && i*2<hex.Length; i++)
            {
                string bytehex = "" + hex[i*2] + hex[i*2 + 1];
                buffer[i] = byte.Parse(bytehex, NumberStyles.HexNumber);
            }
        }

        public static void ReadFromHex(this short[] buffer, string hex)
        {
            if (hex.Length % 4 != 0)
                throw new Exception("Hex value string does not appear to be properly formatted.");
            for (int i = 0; i < buffer.Length && i * 4 < hex.Length; i++)
            {
                string shorthex = "" + hex[i * 4] + hex[(i * 4) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3];
                buffer[i] = short.Parse(shorthex, NumberStyles.HexNumber);
            }
        }

        public static void ReadFromHex(this ushort[] buffer, string hex)
        {
            if (hex.Length % 4 != 0)
                throw new Exception("Hex value string does not appear to be properly formatted.");
            for (int i = 0; i < buffer.Length && i * 4 < hex.Length; i++)
            {
                string ushorthex = "" + hex[i*4] + hex[(i*4)+1] + hex[(i*4)+2] + hex[(i*4)+3];
                buffer[i] = ushort.Parse(ushorthex, NumberStyles.HexNumber);
            }
        }
    }

    public static class Colors
    {
        public static int ARGB(byte red, byte green, byte blue)
        {
            return (int) ((uint)((red << 0x10) | (green << 8) | blue | (0xFF << 0x18)));
        }

        public static int ARGB(byte red, byte green, byte blue, byte alpha)
        {
            return (int) ((uint)((red << 0x10) | (green << 8) | blue | (alpha << 0x18)));
        }

        public static int Luminosity(byte lum)
        {
            return (int)((uint)((lum << 0x10) | (lum << 8) | lum | (0xFF << 0x18)));
        }
    }



	//I think this is a little faster with uint than with byte
	struct Bit
	{
		Bit(uint val) { this.val = val; }
		uint val;
		public static implicit operator Bit(int rhs) { Debug.Assert((rhs & ~1) == 0); return new Bit((uint)(rhs)); }
		public static implicit operator Bit(uint rhs) { Debug.Assert((rhs & ~1) == 0); return new Bit((uint)(rhs)); }
		public static implicit operator Bit(byte rhs) { Debug.Assert((rhs & ~1) == 0); return new Bit((uint)(rhs)); }
		public static implicit operator Bit(bool rhs) { return new Bit(rhs ? (byte)1 : (byte)0); }
		public static implicit operator long(Bit rhs) { return (long)rhs.val; }
		public static implicit operator int(Bit rhs) { return (int)rhs.val; }
		public static implicit operator uint(Bit rhs) { return (uint)rhs.val; }
		public static implicit operator byte(Bit rhs) { return (byte)rhs.val; }
		public static implicit operator bool(Bit rhs) { return rhs.val != 0; }
		public override string ToString()
		{
			return val.ToString();
		}
		public static bool operator ==(Bit lhs, Bit rhs) { return lhs.val == rhs.val; }
		public static bool operator !=(Bit lhs, Bit rhs) { return lhs.val != rhs.val; }
		public override int GetHashCode() { return val.GetHashCode(); }
		public override bool Equals(object obj) { return this == (Bit)obj; } //this is probably wrong
	}



    public static class Util
    {
        public static int SaveRamBytesUsed(byte[] SaveRAM)
        {
            for (int j = SaveRAM.Length - 1; j >= 0; j--)
                if (SaveRAM[j] != 0)
                    return j + 1;
            return 0;
        }

		/// <summary>
		/// conerts bytes to an uppercase string of hex numbers in upper case without any spacing or anything
		/// </summary>
		public static string BytesToHexString(byte[] bytes)
		{
			StringBuilder sb = new StringBuilder();
			foreach (byte b in bytes)
				sb.AppendFormat("{0:X2}", b);
			return sb.ToString();
		}

		public static unsafe int memcmp(void* a, string b, int len)
		{
			fixed (byte* bp = System.Text.Encoding.ASCII.GetBytes(b))
				return memcmp(a, bp, len);
		}

		public static unsafe int memcmp(void* a, void* b, int len)
		{
			byte* ba = (byte*)a;
			byte* bb = (byte*)b;
			for (int i = 0; i < len; i++)
			{
				byte _a = ba[i];
				byte _b = bb[i];
				int c = _a - _b;
				if (c != 0) return c;
			}
			return 0;
		}

		public static unsafe void memset(void* ptr, int val, int len)
		{
			byte* bptr = (byte*)ptr;
			for (int i = 0; i < len; i++)
				bptr[i] = (byte)val;
		}

		public static byte[] ReadAllBytes(Stream stream)
		{
			const int BUFF_SIZE = 4096;
			byte[] buffer = new byte[BUFF_SIZE];

			int bytesRead = 0;
			var inStream = new BufferedStream(stream);
			var outStream = new MemoryStream();

			while ((bytesRead = inStream.Read(buffer, 0, BUFF_SIZE)) > 0)
			{
				outStream.Write(buffer, 0, bytesRead);
			}

			return outStream.ToArray();
		}
	}


	public static class BITREV
	{
		public static byte[] byte_8;
		static BITREV()
		{
			make_byte_8();
		}
		static void make_byte_8()
		{
			int bits = 8;
			int n = 1 << 8;
			byte_8 = new byte[n];

			int m = 1;
			int a = n >> 1;
			int j = 2;

			byte_8[0] = 0;
			byte_8[1] = (byte)a;

			while ((--bits) != 0)
			{
				m <<= 1;
				a >>= 1;
				for (int i = 0; i < m; i++)
					byte_8[j++] = (byte)(byte_8[i] + a);
			}
		}
	}


}
