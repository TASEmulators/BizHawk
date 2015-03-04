using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class GBA : ISaveRam
	{
		public bool SaveRamModified
		{
			get
			{
				if (disposed)
					throw new ObjectDisposedException(this.GetType().ToString());
				return LibMeteor.libmeteor_hassaveram();
			}
		}

		public byte[] CloneSaveRam()
		{
			throw new Exception("This needs to be fixed to match the VBANext Core!");
#if false
			if (disposed)
				throw new ObjectDisposedException(this.GetType().ToString());
			if (!LibMeteor.libmeteor_hassaveram())
				return null;
			IntPtr data = IntPtr.Zero;
			uint size = 0;
			if (!LibMeteor.libmeteor_savesaveram(ref data, ref size))
				throw new Exception("libmeteor_savesaveram() returned false!");
			byte[] ret = new byte[size];
			Marshal.Copy(data, ret, 0, (int)size);
			LibMeteor.libmeteor_savesaveram_destroy(data);
			return ret;
#endif
		}

		public void StoreSaveRam(byte[] data)
		{
			throw new Exception("This needs to be fixed to match the VBANext Core!");
#if false
			if (disposed)
				throw new ObjectDisposedException(this.GetType().ToString());
			if (!LibMeteor.libmeteor_loadsaveram(data, (uint)data.Length))
				throw new Exception("libmeteor_loadsaveram() returned false!");
#endif
		}
	}
}
