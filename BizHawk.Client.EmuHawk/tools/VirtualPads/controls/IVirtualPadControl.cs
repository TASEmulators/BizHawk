using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPadControl
	{
		void Clear();
		void Set(IController controller);
	}
}
