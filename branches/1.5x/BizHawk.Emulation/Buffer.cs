using System;
using System.Runtime.InteropServices;

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
		public int itemsize;

		public static CBuffer<T> malloc(int amt, int itemsize)
		{
			return new CBuffer<T>(amt, itemsize);
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

		public CBuffer(T[] arr, int itemsize)
		{
			this.itemsize = itemsize;
			len = arr.Length;
			this.arr = arr;
			hnd = GCHandle.Alloc(arr, GCHandleType.Pinned);
			ptr = hnd.AddrOfPinnedObject().ToPointer();
			byteptr = (byte*)ptr;
		}
		public CBuffer(int amt, int itemsize)
		{
			this.itemsize = itemsize;
			len = amt;
			arr = new T[amt];
			hnd = GCHandle.Alloc(arr, GCHandleType.Pinned);
			ptr = hnd.AddrOfPinnedObject().ToPointer();
			byteptr = (byte*)ptr;
			Util.memset(byteptr, 0, len * itemsize);
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
		public ByteBuffer(int amt) : base(amt,1) { }
		public ByteBuffer(byte[] arr) : base(arr,1) { }
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

	public class IntBuffer : CBuffer<int>
	{
		public IntBuffer(int amt) : base(amt, 4) { }
		public IntBuffer(int[] arr) : base(arr,4) { }
		public int this[int index]
		{
			#if DEBUG
				get { return arr[index]; }
				set { arr[index] = value; }
			#else
				set { Write32(index<<2, (uint) value); }
				get { return (int)Read32(index<<2);}
			#endif
		}
	}

	public class ShortBuffer : CBuffer<short>
	{
		public ShortBuffer(int amt) : base(amt, 2) { }
		public ShortBuffer(short[] arr) : base(arr, 2) { }
		public short this[int index]
		{
#if DEBUG
				get { return arr[index]; }
				set { arr[index] = value; }
#else
			set { Write32(index << 1, (uint)value); }
			get { return (short)Read16(index << 1); }
#endif
		}
	}
}