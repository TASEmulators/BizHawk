namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// An <seealso cref="IEmulatorService"/> that returns cart/mapper/board information about the Game hardware itself, if available
	/// Currently the board name is the only property but this could be expanded to support more detailed information
	/// </summary>
	/// <seealso cref="IEmulator"/>
	/// <seealso cref="IEmulatorService"/>
	public interface IBoardInfo : IEmulatorService
	{
		/// <summary>
		/// Gets the identifying information about a "mapper", cart, board or similar capability.
		/// </summary>
		string BoardName { get; }
	}
}
