#nullable disable

using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Implements a data simple data buffer with proper life cycle and no bounds checking
	/// </summary>
	public unsafe class CBuffer<T> : IDisposable
	{
		public GCHandle Hnd;
		public T[] Arr;
		public void* Ptr;
		public byte* Byteptr;
		public int Len;
		public int Itemsize;

		public void Write08(int addr, byte val) { Byteptr[addr] = val; }
		public void Write32(int addr, uint val) { *(uint*)(Byteptr + addr) = val; }
		public byte Read08(int addr) { return Byteptr[addr]; }
		public ushort Read16(int addr) { return *(ushort*)(Byteptr + addr); }
		public uint Read32(int addr) { return *(uint*)(Byteptr + addr); }

		public static CBuffer<T> malloc(int amt, int itemsize)
		{
			return new CBuffer<T>(amt, itemsize);
		}

		public CBuffer(T[] arr, int itemsize)
		{
			Itemsize = itemsize;
			Len = arr.Length;
			Arr = arr;
			Hnd = GCHandle.Alloc(arr, GCHandleType.Pinned);
			Ptr = Hnd.AddrOfPinnedObject().ToPointer();
			Byteptr = (byte*)Ptr;
		}
		public CBuffer(int amt, int itemsize)
		{
			Itemsize = itemsize;
			Len = amt;
			Arr = new T[amt];
			Hnd = GCHandle.Alloc(this.Arr, GCHandleType.Pinned);
			Ptr = Hnd.AddrOfPinnedObject().ToPointer();
			Byteptr = (byte*)Ptr;
			Util.Memset(Byteptr, 0, Len * itemsize);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Arr != null)
				{
					Hnd.Free();
				}
				Arr = null;
			}
		}

		~CBuffer() { Dispose(true); }
	}

	public sealed class ByteBuffer : CBuffer<byte>
	{
		public ByteBuffer(int amt) : base(amt,1) { }
		public ByteBuffer(byte[] arr) : base(arr,1) { }
		public byte this[int index]
		{
			#if DEBUG
				get { return this.Arr[index]; }
				set { this.Arr[index] = value; }
			#else
				set { Write08(index, value); } 
				get { return Read08(index);}
			#endif
		}
	}

	public sealed class IntBuffer : CBuffer<int>
	{
		public IntBuffer(int amt) : base(amt, 4) { }
		public IntBuffer(int[] arr) : base(arr,4) { }
		public int this[int index]
		{
			#if DEBUG
				get { return this.Arr[index]; }
				set { this.Arr[index] = value; }
			#else
				set { Write32(index<<2, (uint) value); }
				get { return (int)Read32(index<<2);}
			#endif
		}
	}

	public sealed class ShortBuffer : CBuffer<short>
	{
		public ShortBuffer(int amt) : base(amt, 2) { }
		public ShortBuffer(short[] arr) : base(arr, 2) { }
		public short this[int index]
		{
#if DEBUG
				get { return this.Arr[index]; }
				set { this.Arr[index] = value; }
#else
			set { Write32(index << 1, (uint)value); }
			get { return (short)Read16(index << 1); }
#endif
		}
	}
}