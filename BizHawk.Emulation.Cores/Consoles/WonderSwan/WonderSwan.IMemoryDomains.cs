using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	public partial class WonderSwan
	{
		private void InitIMemoryDomains()
		{
			var mmd = new List<MemoryDomain>();
			for (int i = 0;; i++)
			{
				if (!BizSwan.bizswan_getmemoryarea(Core, i, out var name, out var size, out var data))
				{
					break;
				}

				if (size == 0)
				{
					continue;
				}

				string sName = Marshal.PtrToStringAnsi(name);
				mmd.Add(new MemoryDomainIntPtr(sName, MemoryDomain.Endian.Little, data, size, true, 1));
			}

			((BasicServiceProvider) ServiceProvider).Register<IMemoryDomains>(new MemoryDomainList(mmd));
		}
	}
}
