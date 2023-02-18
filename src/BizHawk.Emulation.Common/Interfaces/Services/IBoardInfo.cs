namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// An <see cref="IEmulatorService"/> that returns cart/mapper/board information about the Game hardware itself, if available
	/// Currently the board name is the only property but this could be expanded to support more detailed information
	/// </summary>
	/// <seealso cref="IEmulator"/>
	public interface IBoardInfo : ISpecializedEmulatorService
	{
		/// <summary>
		/// Gets the identifying information about a "mapper", cart, board or similar capability.
		/// </summary>
		string BoardName { get; }
	}
}
