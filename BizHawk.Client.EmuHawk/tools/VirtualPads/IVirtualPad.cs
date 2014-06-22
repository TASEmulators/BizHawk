using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPad
	{
		IController Get();
		void Set(IController controller);
		void Clear();
	}
}
