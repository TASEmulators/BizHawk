using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public unsafe class SharedMemoryBlock : IDisposable
	{
		public string Name;
		public string BlockName;
		public int Size;
		public byte* Ptr;
		byte[] bytes;
		GCHandle handle;

		public void Allocate()
		{
			bytes = new byte[Size];
			handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			Ptr = (byte*)handle.AddrOfPinnedObject();
		}

		public void Dispose()
		{
			handle.Free();
			bytes = null;
		}
	}
}