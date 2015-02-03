using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public static class PJMImport
	{
		public static Bk2Movie Import(string path, out string errorMsg, out string warningMsg)
		{
			errorMsg = string.Empty;
			warningMsg = string.Empty;
			return new Bk2Movie();
		}
	}
}
