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

		public static IMovieController SetFromMnemonicStr(string inputLogEntry)
		{
			try
			{
				var controller = GlobalWin.MovieSession.GenerateMovieController();
				controller.SetFromMnemonic(inputLogEntry);

				foreach (var button in controller.Definition.BoolButtons)
				{
					GlobalWin.InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
				}

				foreach (var axisName in controller.Definition.Axes.Keys)
				{
					GlobalWin.InputManager.ButtonOverrideAdapter.SetAxis(axisName, controller.AxisValue(axisName));
				}

				return controller;
			}
			catch (Exception)
			{
				MessageBox.Show($"Invalid mnemonic string: {inputLogEntry}", "Paste Input failed!");
				return null;
			}
		}
	}
}
