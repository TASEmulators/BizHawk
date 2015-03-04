using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan
	{
		void InitIMemoryDomains()
		{
			var mmd = new List<MemoryDomain>();
			for (int i = 0; ; i++)
			{
				IntPtr name;
				int size;
				IntPtr data;
				if (!BizSwan.bizswan_getmemoryarea(Core, i, out name, out size, out data))
					break;
				if (size == 0)
					continue;
				string sname = Marshal.PtrToStringAnsi(name);
				mmd.Add(MemoryDomain.FromIntPtr(sname, size, MemoryDomain.Endian.Little, data));
			}
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(new MemoryDomainList(mmd));
		}
	}
}
