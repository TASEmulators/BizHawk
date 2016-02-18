using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common.BizInvoke
{
	public interface IImportResolver
	{
		IntPtr Resolve(string entryPoint);
	}
}
