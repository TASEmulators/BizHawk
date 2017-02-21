using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieController: IController
	{
		new ControllerDefinition Definition { get; set; }

		void LatchPlayerFromSource(IController playerSource, int playerNum);

		void LatchFromSource(IController source);

		void SetControllersAsMnemonic(string mnemonic);
	}
}
