using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk
{
	public interface ICart
	{
		int Parse(byte[] Rom);
		ushort? Read(ushort addr);
		bool Write(ushort addr, ushort value);
	}
}
