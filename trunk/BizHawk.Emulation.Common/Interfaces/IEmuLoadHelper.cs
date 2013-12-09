using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// some methods that an emulation core might want to have access to during its load sequence
	/// </summary>
	public interface IEmuLoadHelper
	{
		void ShowMessage(string msg);

		byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null);
	}
}
