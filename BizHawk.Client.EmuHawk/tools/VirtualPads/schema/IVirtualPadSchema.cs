using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPadSchema
	{
		IEnumerable<PadSchema> GetPadSchemas(IEmulator core);
	}
}
