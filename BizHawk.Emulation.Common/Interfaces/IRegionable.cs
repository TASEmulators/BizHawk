namespace BizHawk.Emulation.Common
{
	public interface IRegionable : IEmulatorService
	{
		DisplayType Region { get; }
	}
}
