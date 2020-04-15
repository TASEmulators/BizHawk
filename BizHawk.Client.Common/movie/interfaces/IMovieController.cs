using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieController : IController
	{
		/// <summary>
		/// Latches to the given <see cref="IController" />
		/// </summary>
		void LatchFrom(IController source);

		/// <summary>
		/// Latches to only the buttons in the given <see cref="IController" /> for the given controller
		/// </summary>
		void LatchPlayerFrom(IController playerSource, int controllerNum);

		/// <summary>
		/// Latches to the given <see cref="IStickyController" />
		/// For buttons it latches autohold state, for analogs it latches mid value.
		/// </summary>
		void LatchFromSticky(IStickyController controller);

		/// <summary>
		/// Sets the controller to the state represented by the given mnemonic string
		/// </summary>
		void SetFromMnemonic(string mnemonic);

		/// <summary>
		/// Sets the given boolean button to the given value
		/// </summary>
		void SetBool(string buttonName, bool value);

		/// <summary>
		/// Sets the given axis button to the given value
		/// </summary>
		void SetAxis(string buttonName, float value);
	}
}
