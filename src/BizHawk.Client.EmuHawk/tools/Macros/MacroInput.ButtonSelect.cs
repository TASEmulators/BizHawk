using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class MacroInputTool
	{
		private CheckBox[] _buttonBoxes;
		private void SetUpButtonBoxes()
		{
			var def = Emulator.ControllerDefinition;
			int count = def.BoolButtons.Count + def.Axes.Count;
			_buttonBoxes = new CheckBox[count];

			for (int i = 0; i < def.Axes.Count; i++)
			{
				var box = new CheckBox { Text = def.Axes[i] };
				_buttonBoxes[i] = box;
			}
			for (int i = 0; i < def.BoolButtons.Count; i++)
			{
				var box = new CheckBox { Text = def.BoolButtons[i] };
				_buttonBoxes[i + def.Axes.Count] = box;
			}

			foreach (var box in _buttonBoxes)
			{
				box.Parent = this;
				box.AutoSize = true;
				box.Checked = true;
				box.CheckedChanged += ButtonBox_CheckedChanged;
			}

			PositionBoxes();
		}

		private void ButtonBox_CheckedChanged(object sender, EventArgs e)
		{
			if (SelectedZone == null)
			{
				return;
			}

			CheckBox s = (CheckBox)sender;
			s.ForeColor = s.Checked ? SystemColors.ControlText : SystemColors.ButtonShadow;
			s.Refresh();

			// Update the selected zone's key
			string key = Bk2LogEntryGenerator.GenerateLogKey(MovieSession.MovieController.Definition);
			key = key.Replace("#", "");

			foreach (var box in _buttonBoxes)
			{
				if (!box.Checked)
				{
					key = key.Replace($"{box.Text}|", "");
				}
			}
			SelectedZone.InputKey = key.Substring(startIndex: 0, length: key.Length - 1); // drop final char
		}

		private void PositionBoxes()
		{
			int x = ClientSize.Width - 3;
			int y = ClientSize.Height - _buttonBoxes[0].Height - 3;

			for (int i = _buttonBoxes.Length - 1; i >= 0; i--)
			{
				x -= _buttonBoxes[i].Width;
				if (x <= 3)
				{
					x = ClientSize.Width - 3 - _buttonBoxes[i].Width;
					y -= (_buttonBoxes[0].Height + 6);
				}

				_buttonBoxes[i].Location = new Point(x, y);
			}
		}
		private void MacroInputTool_Resize(object sender, EventArgs e)
		{
			if (_initializing)
			{
				return;
			}

			PositionBoxes();
		}
	}
}
