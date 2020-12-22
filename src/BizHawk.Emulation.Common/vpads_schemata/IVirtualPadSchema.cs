using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Common
{
	public interface IVirtualPadSchema
	{
		IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox);
	}
}
