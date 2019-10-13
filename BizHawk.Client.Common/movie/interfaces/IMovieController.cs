using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieController : IController
	{
		new ControllerDefinition Definition { get; set; }

		void LatchPlayerFromSource(IController playerSource, int playerNum);

		void LatchFromSource(IController source);

		/// <summary>
		/// Used by tastudio when it appends new frames in HandleMovieAfterFrameLoop() and ExtendMovieForEdit().
		/// For buttons it latches autohold state, for analogs it latches mid value.
		/// All real user input latched by LatchFromPhysical() is ignored.
		/// </summary>
		void LatchSticky();

		void SetControllersAsMnemonic(string mnemonic);
	}
}
