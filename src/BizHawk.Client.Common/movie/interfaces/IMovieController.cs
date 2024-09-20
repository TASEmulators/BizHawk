using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieController : IController
	{
		/// <summary>
		/// Latches to the given <see cref="IController" />
		/// </summary>
		void SetFrom(IController source);

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
		void SetAxis(string buttonName, int value);
	}
}
