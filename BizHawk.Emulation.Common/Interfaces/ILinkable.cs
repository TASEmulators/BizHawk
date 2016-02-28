namespace BizHawk.Emulation.Common
{
	public interface ILinkable : ISpecializedEmulatorService
	{
		/// <summary>
		/// Whether or not the link cable is currently connected
		/// </summary>
		bool LinkConnected { get; }
	}
}
