using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroHost : ISaveRam
	{
		private readonly List<MemoryDomainIntPtr> _saveramAreas = new();
		private long _saveramSize = 0;

		public bool SaveRamModified => _saveramSize > 0;

		public byte[] CloneSaveRam()
		{
			if (_saveramSize > 0)
			{
				var buf = new byte[_saveramSize];
				int index = 0;
				foreach (var m in _saveramAreas)
				{
					Marshal.Copy(m.Data, buf, index, (int)m.Size);
					index += (int)m.Size;
				}
				return buf;
			}

			return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_saveramSize > 0)
			{
				int index = 0;
				foreach (var m in _saveramAreas)
				{
					Marshal.Copy(data, index, m.Data, (int)m.Size);
					index += (int)m.Size;
				}
			}
		}
	}
}
