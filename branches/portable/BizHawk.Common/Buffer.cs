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

		public static CBuffer<T> malloc(int amt, int itemsize)
		{
			return new CBuffer<T>(amt, itemsize);
		}

		public void Write08(uint addr, byte val) { this.Byteptr[addr] = val; }
		public void Write16(uint addr, ushort val) { *(ushort*)(this.Byteptr + addr) = val; }
		public void Write32(uint addr, uint val) { *(uint*)(this.Byteptr + addr) = val; }
		public void Write64(uint addr, ulong val) { *(ulong*)(this.Byteptr + addr) = val; }
		public byte Read08(uint addr) { return this.Byteptr[addr]; }
		public ushort Read16(uint addr) { return *(ushort*)(this.Byteptr + addr); }
		public uint Read32(uint addr) { return *(uint*)(this.Byteptr + addr); }
		public ulong Read64(uint addr) { return *(ulong*)(this.Byteptr + addr); }
		public void Write08(int addr, byte val) { this.Byteptr[addr] = val; }
		public void Write16(int addr, ushort val) { *(ushort*)(this.Byteptr + addr) = val; }
		public void Write32(int addr, uint val) { *(uint*)(this.Byteptr + addr) = val; }
		public void Write64(int addr, ulong val) { *(ulong*)(this.Byteptr + addr) = val; }
		public byte Read08(int addr) { return this.Byteptr[addr]; }
		public ushort Read16(int addr) { return *(ushort*)(this.Byteptr + addr); }
		public uint Read32(int addr) { return *(uint*)(this.Byteptr + addr); }
		public ulong Read64(int addr) { return *(ulong*)(this.Byteptr + addr); }

		public CBuffer(T[] arr, int itemsize)
		{
			this.Itemsize = itemsize;
			this.Len = arr.Length;
			this.Arr = arr;
			this.Hnd = GCHandle.Alloc(arr, GCHandleType.Pinned);
			this.Ptr = this.Hnd.AddrOfPinnedObject().ToPointer();
			this.Byteptr = (byte*)this.Ptr;
		}
		public CBuffer(int amt, int itemsize)
		{
			this.Itemsize = itemsize;
			this.Len = amt;
			this.Arr = new T[amt];
			this.Hnd = GCHandle.Alloc(this.Arr, GCHandleType.Pinned);
			this.Ptr = this.Hnd.AddrOfPinnedObject().ToPointer();
			this.Byteptr = (byte*)this.Ptr;
			Util.Memset(this.Byteptr, 0, this.Len * itemsize);
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
				if (this.Arr != null)
				{
					this.Hnd.Free();
				}
				this.Arr = null;
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