#nullable enable

using System;

using BizHawk.API.Base;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk.APIImpl
{
	public class GlobalsAccessAPIEnvironment : CommonServicesAPIEnvironment
	{
		public Config GlobalConfig => Global.Config;

		public DisplayManager GlobalDisplayManager => GlobalWin.DisplayManager;

		public GameInfo GlobalGame => Global.Game;

		public InputManager GlobalInputManager => Global.InputManager;

		public MainForm GlobalMainForm => GlobalWin.MainForm;

		public IMovieSession GlobalMovieSession => Global.MovieSession;

		public ToolManager GlobalToolManager => GlobalWin.Tools;

		public GlobalsAccessAPIEnvironment(
			Action<string> logCallback,
			HistoricAPIEnvironment last,
			out HistoricAPIEnvironment keep
		) : base(
			logCallback,
			last,
			out keep
		) {}
	}
}
