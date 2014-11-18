using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Client.Common
{
	interface IZipWriter : IDisposable
	{
		void WriteItem(string name, Action<Stream> callback);
	}
}
