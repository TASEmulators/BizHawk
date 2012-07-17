using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk
{
	public interface ICart
	{
		ushort? ReadCart(ushort addr);
		bool WriteCart(ushort addr, ushort value);
	}
}
