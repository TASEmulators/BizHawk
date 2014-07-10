using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class TasClipboardEntry
	{
		public TasClipboardEntry(int frame, IController controllerState)
		{
			Frame = frame;
			ControllerState = controllerState;
		}

		public int Frame { get; private set; }
		public IController ControllerState { get; private set; }

		public override string ToString()
		{
			var lg = Global.MovieSession.Movie.LogGeneratorInstance();
			return lg.GenerateLogEntry();
		}
	}
}
