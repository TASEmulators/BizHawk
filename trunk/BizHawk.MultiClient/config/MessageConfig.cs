using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class MessageConfig : Form
	{
		//TODO: 
		//Deal with typing into Numerics properly

		int DispFPSx = Global.Config.DispFPSx;
		int DispFPSy = Global.Config.DispFPSy;
		int DispFrameCx = Global.Config.DispFrameCx;
		int DispFrameCy = Global.Config.DispFrameCy;
		int DispLagx = Global.Config.DispLagx;
		int DispLagy = Global.Config.DispLagy;
		int DispInpx = Global.Config.DispInpx;
		int DispInpy = Global.Config.DispInpy;
		int DispRerecx = Global.Config.DispRecx;
		int DispRerecy = Global.Config.DispRecy;
		int LastInputColor = Global.Config.LastInputColor;
		int DispRecx = Global.Config.DispRecx;
		int DispRecy = Global.Config.DispRecy;
		int DispMultix = Global.Config.DispMultix;
		int DispMultiy = Global.Config.DispMultiy;
		int DispMessagex = Global.Config.DispMessagex;
		int DispMessagey = Global.Config.DispMessagey;
		int DispAutoholdx = Global.Config.DispAutoholdx;
		int DispAutoholdy = Global.Config.DispAutoholdy;

		int MessageColor = Global.Config.MessagesColor;
		int AlertColor = Global.Config.AlertMessageColor;
		int MovieInput = Global.Config.MovieInput;
		
		int DispFPSanchor = Global.Config.DispFPSanchor;
		int DispFrameanchor = Global.Config.DispFrameanchor;
		int DispLaganchor = Global.Config.DispLaganchor;
		int DispInputanchor = Global.Config.DispInpanchor;
		int DispRecanchor = Global.Config.DispRecanchor;
		int DispMultiAnchor = Global.Config.DispMultianchor;
		int DispMessageAnchor = Global.Config.DispMessageanchor;
		int DispAutoholdAnchor = Global.Config.DispAutoholdanchor;

		public Brush brush = Brushes.Black;
		int px = 0;
		int py = 0;
		bool mousedown = false;

		public MessageConfig()
		{
			InitializeComponent();
		}

		private void MessageConfig_Load(object sender, EventArgs e)
		{
			SetMaxXY();
			MessageColorDialog.Color = Color.FromArgb(MessageColor);
			AlertColorDialog.Color = Color.FromArgb(AlertColor);
			LInputColorDialog.Color = Color.FromArgb(LastInputColor);
			MovieInputColorDialog.Color = Color.FromArgb(MovieInput);
			SetColorBox();
			SetPositionInfo();
			StackMessagesCheckbox.Checked = Global.Config.StackOSDMessages;
		}

		private void SetMaxXY()
		{
			XNumeric.Maximum = Global.Emulator.VideoProvider.BufferWidth - 8;
			YNumeric.Maximum = Global.Emulator.VideoProvider.BufferHeight - 8;
			PositionPanel.Size = new Size(Global.Emulator.VideoProvider.BufferWidth, Global.Emulator.VideoProvider.BufferHeight);

			int width;
			if (Global.Emulator.VideoProvider.BufferWidth > 128)
				width = Global.Emulator.VideoProvider.BufferWidth + 44;
			else
				width = 128 + 44;

			PositionGroupBox.Size = new Size(width, Global.Emulator.VideoProvider.BufferHeight + 52);
		}

		private void SetColorBox()
		{
			MessageColor = MessageColorDialog.Color.ToArgb();
			ColorPanel.BackColor = MessageColorDialog.Color;
			ColorText.Text = String.Format("{0:X8}", MessageColor);

			AlertColor = AlertColorDialog.Color.ToArgb();
			AlertColorPanel.BackColor = AlertColorDialog.Color;
			AlertColorText.Text = String.Format("{0:X8}", AlertColor);

			LastInputColor = LInputColorDialog.Color.ToArgb();
			LInputColorPanel.BackColor = LInputColorDialog.Color;
			LInputText.Text = String.Format("{0:X8}", LastInputColor);

			MovieInput = MovieInputColorDialog.Color.ToArgb();
			MovieInputColor.BackColor = MovieInputColorDialog.Color;
			MovieInputText.Text = String.Format("{0:X8}", MovieInput);
		}

		private void SetAnchorRadio(int anchor)
		{
			switch (anchor)
			{
				default:
				case 0:
					TL.Checked = true; break;
				case 1:
					TR.Checked = true; break;
				case 2:
					BL.Checked = true; break;
				case 3:
					BR.Checked = true; break;
			}
		}

		private void SetPositionInfo()
		{
			if (FPSRadio.Checked)
			{
				XNumeric.Value = DispFPSx;
				YNumeric.Value = DispFPSy;
				px = DispFPSx;
				py = DispFPSy;
				SetAnchorRadio(DispFPSanchor);
			}
			else if (FrameCounterRadio.Checked)
			{
				XNumeric.Value = DispFrameCx;
				YNumeric.Value = DispFrameCy;
				px = DispFrameCx;
				py = DispFrameCy;
				SetAnchorRadio(DispFrameanchor);
			}
			else if (LagCounterRadio.Checked)
			{
				XNumeric.Value = DispLagx;
				YNumeric.Value = DispLagy;
				px = DispLagx;
				py = DispLagy;
				SetAnchorRadio(DispLaganchor);
			}
			else if (InputDisplayRadio.Checked)
			{
				XNumeric.Value = DispInpx;
				XNumeric.Value = DispInpy;
				px = DispInpx;
				py = DispInpy;
				SetAnchorRadio(DispInputanchor);
			}
			else if (MessagesRadio.Checked)
			{
				XNumeric.Value = DispMessagex;
				YNumeric.Value = DispMessagey;
				px = DispMessagex;
				py = DispMessagey;
				SetAnchorRadio(DispMessageAnchor);
			}
			else if (RerecordsRadio.Checked)
			{
				XNumeric.Value = DispRecx;
				YNumeric.Value = DispRecy;
				px = DispRecx;
				py = DispRecy;
				SetAnchorRadio(DispRecanchor);
			}
			else if (MultitrackRadio.Checked)
			{
				XNumeric.Value = DispMultix;
				YNumeric.Value = DispMultiy;
				px = DispMultix;
				py = DispMultiy;
				SetAnchorRadio(DispMultiAnchor);
			}
			else if (AutoholdRadio.Checked)
			{
				XNumeric.Value = DispAutoholdx;
				YNumeric.Value = DispAutoholdy;
				px = DispAutoholdx;
				py = DispAutoholdy;
				SetAnchorRadio(DispAutoholdAnchor);
			}

			PositionPanel.Refresh();
			XNumeric.Refresh();
			YNumeric.Refresh();
			SetPositionLabels();
		}

		private void SaveSettings()
		{
			Global.Config.DispFPSx = DispFPSx;
			Global.Config.DispFPSy = DispFPSy;
			Global.Config.DispFrameCx = DispFrameCx;
			Global.Config.DispFrameCy = DispFrameCy;
			Global.Config.DispLagx = DispLagx;
			Global.Config.DispLagy = DispLagy;
			Global.Config.DispInpx = DispInpx;
			Global.Config.DispInpy = DispInpy;
			Global.Config.DispRecx = DispRecx;
			Global.Config.DispRecy = DispRecy;
			Global.Config.DispMultix = DispMultix;
			Global.Config.DispMultiy = DispMultiy;
			Global.Config.DispMessagex = DispMessagex;
			Global.Config.DispMessagey = DispMessagey;
			Global.Config.DispAutoholdx = DispAutoholdx;
			Global.Config.DispAutoholdy = DispAutoholdy;

			Global.Config.MessagesColor = MessageColor;
			Global.Config.AlertMessageColor = AlertColor;
			Global.Config.LastInputColor = LastInputColor;
			Global.Config.MovieInput = MovieInput;
			Global.Config.DispFPSanchor = DispFPSanchor;
			Global.Config.DispFrameanchor = DispFrameanchor;
			Global.Config.DispLaganchor = DispLaganchor;
			Global.Config.DispInpanchor = DispInputanchor;
			Global.Config.DispRecanchor = DispRecanchor;
			Global.Config.DispMultianchor = DispMultiAnchor;
			Global.Config.DispMessageanchor = DispMessageAnchor;
			Global.Config.DispAutoholdanchor = DispAutoholdAnchor;

			Global.Config.StackOSDMessages = StackMessagesCheckbox.Checked;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			SaveSettings();
			Global.OSD.AddMessage("Message settings saved");
			this.Close();
		}

		private void FPSRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void FrameCounterRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void LagCounterRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void InputDisplayRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void MessagesRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void RerecordsRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void MultitrackRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void AutoholdRadio_CheckedChanged(object sender, EventArgs e)
		{
			SetPositionInfo();
		}

		private void XNumericChange()
		{
			px = (int)XNumeric.Value;
			SetPositionLabels();
			PositionPanel.Refresh();
		}

		private void YNumericChange()
		{
			py = (int)YNumeric.Value;
			SetPositionLabels();
			PositionPanel.Refresh();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Message config aborted");
			this.Close();
		}

		private void PositionPanel_MouseEnter(object sender, EventArgs e)
		{
			this.Cursor = Cursors.Hand;
		}

		private void PositionPanel_MouseLeave(object sender, EventArgs e)
		{
			this.Cursor = Cursors.Default;
		}

		private void PositionPanel_Paint(object sender, PaintEventArgs e)
		{
			int x = 0;
			int y = 0;

			if (TL.Checked)
			{
				x = px;
				y = py;
			}
			else if (TR.Checked)
			{
				x = (int)XNumeric.Maximum - px;
				y = py;
			}
			else if (BL.Checked)
			{
				x = px;
				y = (int)YNumeric.Maximum - py;
			}
			else if (BR.Checked)
			{
				x = (int)XNumeric.Maximum - px;
				y = (int)YNumeric.Maximum - py;
			}

			Pen p = new Pen(brush);
			e.Graphics.DrawLine(p, new Point(x - 4, y - 4), new Point(x + 4, y + 4));
			e.Graphics.DrawLine(p, new Point(x + 4, y - 4), new Point(x - 4, y + 4));
			Rectangle rect = new Rectangle(x - 4, y - 4, 8, 8);
			e.Graphics.DrawRectangle(p, rect);
		}

		private void PositionPanel_MouseDown(object sender, MouseEventArgs e)
		{
			this.Cursor = Cursors.Arrow;
			mousedown = true;
			SetNewPosition(e.X, e.Y);
		}

		private void PositionPanel_MouseUp(object sender, MouseEventArgs e)
		{
			this.Cursor = Cursors.Hand;
			mousedown = false;
		}

		private void SetNewPosition(int mx, int my)
		{
			if (mx < 0) mx = 0;
			if (my < 0) my = 0;
			if (mx > XNumeric.Maximum) mx = (int)XNumeric.Maximum;
			if (my > YNumeric.Maximum) my = (int)YNumeric.Maximum;


			if (TL.Checked)
			{
				//Do nothing
			}
			else if (TR.Checked)
			{
				mx = (int)XNumeric.Maximum - mx;
			}
			else if (BL.Checked)
			{
				my = (int)YNumeric.Maximum - my;
			}
			else if (BR.Checked)
			{
				mx = (int)XNumeric.Maximum - mx;
				my = (int)YNumeric.Maximum - my;
			}

			XNumeric.Value = mx;
			YNumeric.Value = my;
			px = mx;
			py = my;
			
			
			
			PositionPanel.Refresh();
			SetPositionLabels();
		}

		private void PositionPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (mousedown)
			{
				SetNewPosition(e.X, e.Y);
			}
		}

		private void SetPositionLabels()
		{
			if (FPSRadio.Checked)
			{
				DispFPSx = px;
				DispFPSy = py;
			}
			else if (FrameCounterRadio.Checked)
			{
				DispFrameCx = px;
				DispFrameCy = py;
			}
			else if (LagCounterRadio.Checked)
			{
				DispLagx = px;
				DispLagy = py;
			}
			else if (InputDisplayRadio.Checked)
			{
				DispInpx = px;
				DispInpy = py;
			}
			else if (RerecordsRadio.Checked)
			{
				DispRecx = px;
				DispRecy = py;
			}
			else if (MultitrackRadio.Checked)
			{
				DispMultix = px;
				DispMultiy = py;
			}
			else if (MessagesRadio.Checked)
			{
				DispMessagex = px;
				DispMessagey = py;
			}
			else if (AutoholdRadio.Checked)
			{
				DispAutoholdx = px;
				DispAutoholdy = py;
			}

			FpsPosLabel.Text = DispFPSx.ToString() + ", " + DispFPSy.ToString();
			FCLabel.Text = DispFrameCx.ToString() + ", " + DispFrameCy.ToString();
			LagLabel.Text = DispLagx.ToString() + ", " + DispLagy.ToString();
			InpLabel.Text = DispInpx.ToString() + ", " + DispInpy.ToString();
			RerecLabel.Text = DispRecx.ToString() + ", " + DispRecy.ToString();
			MultitrackLabel.Text = DispMultix.ToString() + ", " + DispMultiy.ToString();
			MessLabel.Text = DispMessagex.ToString() + ", " + DispMessagey.ToString();
			AutoholdLabel.Text = DispAutoholdx.ToString() + ", " + DispAutoholdy.ToString();
		}

		private void ResetDefaultsButton_Click(object sender, EventArgs e)
		{
			Global.Config.DispFPSx = 0;
			Global.Config.DispFPSy = 0;
			Global.Config.DispFrameCx = 0;
			Global.Config.DispFrameCy = 12;
			Global.Config.DispLagx = 0;
			Global.Config.DispLagy = 36;
			Global.Config.DispInpx = 0;
			Global.Config.DispInpy = 24;
			Global.Config.DispRecx = 0;
			Global.Config.DispRecy = 48;
			Global.Config.DispMultix = 36;
			Global.Config.DispMultiy = 0;
			Global.Config.DispMessagex = 3;
			Global.Config.DispMessagey = 0;
			Global.Config.DispAutoholdx = 0;
			Global.Config.DispAutoholdy = 0;

			Global.Config.MessagesColor = -1;
			Global.Config.AlertMessageColor = -65536;
			Global.Config.LastInputColor = -23296;
			Global.Config.MovieInput = -8355712;

			MessageColor = Global.Config.MessagesColor;
			AlertColor = Global.Config.AlertMessageColor;
			LastInputColor = Global.Config.LastInputColor;
			MovieInput = Global.Config.MovieInput;

			MessageColorDialog.Color = Color.FromArgb(MessageColor);
			AlertColorDialog.Color = Color.FromArgb(AlertColor);
			LInputColorDialog.Color = Color.FromArgb(LastInputColor);
			MovieInputColorDialog.Color = Color.FromArgb(MovieInput);

			Global.Config.DispFPSanchor = 0;
			Global.Config.DispFrameanchor = 0;
			Global.Config.DispLaganchor = 0;
			Global.Config.DispInpanchor = 0;
			Global.Config.DispRecanchor = 0;
			Global.Config.DispMultianchor = 0;
			Global.Config.DispMessageanchor = 2;
			Global.Config.DispAutoholdanchor = 1;

			DispFPSx = Global.Config.DispFPSx;
			DispFPSy = Global.Config.DispFPSy;
			DispFrameCx = Global.Config.DispFrameCx;
			DispFrameCy = Global.Config.DispFrameCy;
			DispLagx = Global.Config.DispLagx;
			DispLagy = Global.Config.DispLagy;
			DispInpx = Global.Config.DispInpx;
			DispInpy = Global.Config.DispInpy;
			DispRecx = Global.Config.DispRecx;
			DispRecy = Global.Config.DispRecy;
			DispMultix = Global.Config.DispMultix;
			DispMultiy = Global.Config.DispMultiy;
			DispMessagex = Global.Config.DispMessagex;
			DispMessagey = Global.Config.DispMessagey;
			DispAutoholdx = Global.Config.DispAutoholdx;
			DispAutoholdy = Global.Config.DispAutoholdy;

			DispFPSanchor = Global.Config.DispFPSanchor;
			DispFrameanchor = Global.Config.DispFrameanchor;
			DispLaganchor = Global.Config.DispLaganchor;
			DispInputanchor = Global.Config.DispInpanchor;
			DispRecanchor = Global.Config.DispRecanchor;
			DispMultiAnchor = Global.Config.DispMultianchor;
			DispMessageAnchor = Global.Config.DispMessageanchor;
			DispAutoholdAnchor = Global.Config.DispAutoholdanchor;

			SetMaxXY();
			SetColorBox();
			SetPositionInfo();

			StackMessagesCheckbox.Checked = Global.Config.StackOSDMessages = true;
		}

		private void SetAnchorValue(int value)
		{
			if (FPSRadio.Checked)
			{
				DispFPSanchor = value;
			}
			else if (FrameCounterRadio.Checked)
			{
				DispFrameanchor = value;
			}
			else if (LagCounterRadio.Checked)
			{
				DispLaganchor = value;
			}
			else if (InputDisplayRadio.Checked)
			{
				DispInputanchor = value;
			}
			else if (MessagesRadio.Checked)
			{
				DispMessageAnchor = value;
			}
			else if (RerecordsRadio.Checked)
			{
				DispRecanchor = value;
			}
			else if (MultitrackRadio.Checked)
			{
				DispMultiAnchor = value;
			}
			else if (AutoholdRadio.Checked)
			{
				DispAutoholdAnchor = value;
			}
		}

		private void TL_CheckedChanged(object sender, EventArgs e)
		{
			if (TL.Checked)
			{
				SetAnchorValue(0);
			}
			PositionPanel.Refresh();
		}

		private void TR_CheckedChanged(object sender, EventArgs e)
		{
			if (TR.Checked)
			{
				SetAnchorValue(1);
			}
			PositionPanel.Refresh();
		}

		private void BL_CheckedChanged(object sender, EventArgs e)
		{
			if (BL.Checked)
			{
				SetAnchorValue(2);
			}
			PositionPanel.Refresh();
		}

		private void BR_CheckedChanged(object sender, EventArgs e)
		{
			if (BR.Checked)
			{
				SetAnchorValue(3);
			}
			PositionPanel.Refresh();
		}

		private void XNumeric_Click(object sender, EventArgs e)
		{
			XNumericChange();
		}

		private void YNumeric_Click(object sender, EventArgs e)
		{
			YNumericChange();
		}

		private void ColorPanel_Click(object sender, EventArgs e)
		{
			if (MessageColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void AlertColorPanel_Click(object sender, EventArgs e)
		{
			if (AlertColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void LInputColorPanel_Click(object sender, EventArgs e)
		{
			if (LInputColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void MovieInputColor_Click(object sender, EventArgs e)
		{
			if (MovieInputColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}
	}
}
