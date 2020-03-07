using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Computers.CPCSpectrumBase
{
	public interface CPCSpectrumBase
	{
		Z80A CPU { get; set; }
	}
}
