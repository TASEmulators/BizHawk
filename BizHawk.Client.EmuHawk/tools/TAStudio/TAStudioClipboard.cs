using System;
using System.Windows.Forms;

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

		public int Frame { get; }
		public IController ControllerState { get; }

		public override string ToString()
		{
			var lg = Global.MovieSession.Movie.LogGeneratorInstance();
			return lg.GenerateLogEntry();
		}

		public static IMovieController SetFromMnemonicStr(string inputLogEntry)
		{
			try
			{
				var lg = Global.MovieSession.MovieControllerInstance();
				lg.SetControllersAsMnemonic(inputLogEntry);

				foreach (var button in lg.Definition.BoolButtons)
				{
					Global.InputManager.ButtonOverrideAdapter.SetButton(button, lg.IsPressed(button));
				}

				foreach (var floatButton in lg.Definition.FloatControls)
				{
					Global.InputManager.ButtonOverrideAdapter.SetFloat(floatButton, lg.GetFloat(floatButton));
				}

				return lg;
			}
			catch (Exception)
			{
				MessageBox.Show($"Invalid mnemonic string: {inputLogEntry}", "Paste Input failed!");
				return null;
			}
		}
	}
}
