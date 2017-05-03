namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service is use by link cable capable cores to manage the status of the link cable
	/// If available, the client will display the link cable status
	/// </summary>
	public interface ILinkable : ISpecializedEmulatorService
	{
		/// <summary>
		/// Gets a value indicating whether or not the link cable is currently connected
		/// </summary>
		bool LinkConnected { get; }
	}
}
