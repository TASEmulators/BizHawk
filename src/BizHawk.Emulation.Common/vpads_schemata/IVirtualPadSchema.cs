using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IVirtualPadSchema
	{
		IEnumerable<PadSchema> GetPadSchemas(IEmulator core, Action<string> showMessageBox);
	}
}
