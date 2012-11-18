using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public interface IVirtualPad
	{
		string GetMnemonic();
		void Clear();
		void SetButtons(string buttons);
	}
}
