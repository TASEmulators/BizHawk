using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MacroInputTool
	{
		private CheckBox[] _buttonBoxes;
		private void SetUpButtonBoxes()
		{
			ControllerDefinition def = Global.Emulator.ControllerDefinition;
			int count = def.BoolButtons.Count + def.FloatControls.Count;
			_buttonBoxes = new CheckBox[count];

			for (int i = 0; i < def.FloatControls.Count; i++)
			{
				CheckBox box = new CheckBox();
				box.Text = def.FloatControls[i];
				_buttonBoxes[i] = box;
			}
			for (int i = 0; i < def.BoolButtons.Count; i++)
			{
				CheckBox box = new CheckBox();
				box.Text = def.BoolButtons[i];
				_buttonBoxes[i + def.FloatControls.Count] = box;
			}

			for (int i = 0; i < _buttonBoxes.Length; i++)
			{
				_buttonBoxes[i].Parent = this;
				_buttonBoxes[i].AutoSize = true;
				_buttonBoxes[i].Checked = true;
				_buttonBoxes[i].CheckedChanged += ButtonBox_CheckedChanged;
			}

			PositionBoxes();
		}

		private bool _setting = false;
		private void ButtonBox_CheckedChanged(object sender, EventArgs e)
		{
			if (selectedZone == null || _setting)
				return;

			CheckBox s = sender as CheckBox;
			s.ForeColor = s.Checked ? SystemColors.ControlText : SystemColors.ButtonShadow;
			s.Refresh();

			// Update the selected zone's key
			var lg = Global.MovieSession.LogGeneratorInstance() as Bk2LogEntryGenerator;
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			string key = lg.GenerateLogKey();
			key = key.Replace("LogKey:", "").Replace("#", "");

			for (int i = 0; i < _buttonBoxes.Length; i++)
			{
				if (!_buttonBoxes[i].Checked)
					key = key.Replace(_buttonBoxes[i].Text + "|", "");
			}
			key = key.Substring(0, key.Length - 1);

			selectedZone.InputKey = key;
		}
		private void SetButtonBoxes()
		{
			if (selectedZone == null)
				return;

			_setting = true;
			for (int i = 0; i < _buttonBoxes.Length; i++)
				_buttonBoxes[i].Checked = selectedZone.InputKey.Contains(_buttonBoxes[i].Text);
			_setting = false;
		}

		private void PositionBoxes()
		{
			int X = this.ClientSize.Width - 3;
			int Y = this.ClientSize.Height - _buttonBoxes[0].Height - 3;

			for (int i = _buttonBoxes.Length - 1; i >= 0; i--)
			{
				X -= _buttonBoxes[i].Width;
				if (X <= 3)
				{
					X = this.ClientSize.Width - 3 - _buttonBoxes[i].Width;
					Y -= (_buttonBoxes[0].Height + 6);
				}

				_buttonBoxes[i].Location = new Point(X, Y);
			}
		}
		private void MacroInputTool_Resize(object sender, EventArgs e)
		{
			if (_initializing)
				return;

			PositionBoxes();
		}

	}
}