using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause : ISaveRam
	{
		public byte[] CloneSaveRam()
		{
			if (Disposed)
			{
				if (DisposedSaveRam != null)
				{
					return (byte[])DisposedSaveRam.Clone();
				}
				else
				{
					return new byte[0];
				}
			}
			else
			{
				var ms = new MemoryStream();
				var fp = new FilePiping();
				fp.Get(ms);
				bool success = LibYabause.libyabause_savesaveram(fp.GetPipeNameNative());
				fp.Finish();
				if (!success)
					throw new Exception("libyabause_savesaveram() failed!");
				var ret = ms.ToArray();
				ms.Dispose();
				return ret;
			}

		}

		public void StoreSaveRam(byte[] data)
		{
			if (Disposed)
			{
				throw new Exception("It's a bit late for that");
			}
			else
			{
				var fp = new FilePiping();
				fp.Offer(data);
				bool success = LibYabause.libyabause_loadsaveram(fp.GetPipeNameNative());
				fp.Finish();
				if (!success)
				{
					throw new Exception("libyabause_loadsaveram() failed!");
				}
			}
		}

		public bool SaveRamModified
		{
			get
			{
				if (Disposed)
				{
					return DisposedSaveRam != null;
				}
				else
				{
					return LibYabause.libyabause_saveramodified();
				}
			}
		}
	}
}
