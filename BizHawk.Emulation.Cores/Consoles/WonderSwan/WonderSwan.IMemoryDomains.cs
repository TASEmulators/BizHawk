using System;
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
				IntPtr name;
				int size;
				IntPtr data;
				if (!BizSwan.bizswan_getmemoryarea(Core, i, out name, out size, out data))
				{
					break;
				}

				if (size == 0)
				{
					continue;
				}

				string sname = Marshal.PtrToStringAnsi(name);
				mmd.Add(new MemoryDomainIntPtr(sname, MemoryDomain.Endian.Little, data, size, true, 1));
			}

			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(new MemoryDomainList(mmd));
		}
	}
}
