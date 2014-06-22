using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPadSchema
	{
		IEnumerable<VirtualPad> GetPads();
	}
}
