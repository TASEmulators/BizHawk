using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace BizHawk
{
	/// <summary>
	/// Implements a data simple data buffer with proper life cycle and no bounds checking
	/// </summary>
	public unsafe class CBuffer<T> : IDisposable
	{
		public GCHandle hnd;
		public T[] arr;
		public void* ptr;
		public byte* byteptr;
		public int len;

		public static CBuffer<T> malloc(int amt)
		{
			return new CBuffer<T>(amt);
		}

		public void Write08(uint addr, byte val) { byteptr[addr] = val; }
		public void Write16(uint addr, ushort val) { *(ushort*)(byteptr + addr) = val; }
		public void Write32(uint addr, uint val) { *(uint*)(byteptr + addr) = val; }
		public void Write64(uint addr, ulong val) { *(ulong*)(byteptr + addr) = val; }
		public byte Read08(uint addr) { return byteptr[addr]; }
		public ushort Read16(uint addr) { return *(ushort*)(byteptr + addr); }
		public uint Read32(uint addr) { return *(uint*)(byteptr + addr); }
		public ulong Read64(uint addr) { return *(ulong*)(byteptr + addr); }
		public void Write08(int addr, byte val) { byteptr[addr] = val; }
		public void Write16(int addr, ushort val) { *(ushort*)(byteptr + addr) = val; }
		public void Write32(int addr, uint val) { *(uint*)(byteptr + addr) = val; }
		public void Write64(int addr, ulong val) { *(ulong*)(byteptr + addr) = val; }
		public byte Read08(int addr) { return byteptr[addr]; }
		public ushort Read16(int addr) { return *(ushort*)(byteptr + addr); }
		public uint Read32(int addr) { return *(uint*)(byteptr + addr); }
		public ulong Read64(int addr) { return *(ulong*)(byteptr + addr); }

		public CBuffer(T[] arr)
		{
			len = arr.Length;
			this.arr = arr;
			hnd = GCHandle.Alloc(arr, GCHandleType.Pinned);
			ptr = hnd.AddrOfPinnedObject().ToPointer();
			byteptr = (byte*)ptr;
		}
		public CBuffer(int amt)
		{
			len = amt;
			arr = new T[amt];
			hnd = GCHandle.Alloc(arr, GCHandleType.Pinned);
			ptr = hnd.AddrOfPinnedObject().ToPointer();
			byteptr = (byte*)ptr;
		}

		public void Dispose()
		{
			if (arr != null)
				hnd.Free();
			arr = null;
		}

		~CBuffer() { Dispose(); }
	}

	public class ByteBuffer : CBuffer<byte>
	{
		public ByteBuffer(int amt) : base(amt) { }
		public ByteBuffer(byte[] arr) : base(arr) { }
		public byte this[int index]
		{
			#if DEBUG
				get { return arr[index]; }
				set { arr[index] = value; }
			#else
				set { Write08(index, value); } 
				get { return Read08(index);}
			#endif
		}
	}
}