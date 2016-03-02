namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the means for a core to specify region information to the client, such NTSC vs PAL
	/// If provided the client will use this to asses FPS and also use it to calculate movie times
	/// </summary>
	public interface IRegionable : IEmulatorService
	{
		DisplayType Region { get; }
	}
}
