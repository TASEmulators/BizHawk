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

		public byte[] CloneSaveRam(bool clearDirty)
		{
			if (_saveramSize == 0)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			else
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
		}

		public void StoreSaveRam(byte[] data)
		{
			if (_saveramSize == 0)
				throw new InvalidOperationException("Core currently has no SRAM and should not be providing ISaveRam service.");
			else
			{
				int index = 0;
				foreach (var m in _saveramAreas)
				{
					Marshal.Copy(data, index, m.Data, (int)m.Size);
					index += (int)m.Size;
				}

				if (data.Length != index) throw new InvalidOperationException("Incorrect sram size.");
			}
		}
	}
}
