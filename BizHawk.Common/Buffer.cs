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
}