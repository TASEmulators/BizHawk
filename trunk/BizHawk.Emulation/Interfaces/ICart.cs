using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk
{
	public interface ICart
	{
		int Parse(byte[] Rom);
		ushort? ReadCart(ushort addr);
		bool WriteCart(ushort addr, ushort value);
	}
}
