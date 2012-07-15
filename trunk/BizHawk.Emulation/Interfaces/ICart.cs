using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk
{
	public interface ICart
	{
		ushort ReadMemory(ushort addr, out bool responded);
		void WriteMemory(ushort addr, ushort value, out bool responded);
	}
}
