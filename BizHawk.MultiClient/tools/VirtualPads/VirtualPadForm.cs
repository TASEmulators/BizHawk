using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo.N64;

namespace BizHawk.MultiClient
{
	public partial class VirtualPadForm : Form
	{
		private int defaultWidth;     //For saving the default size of the dialog, so the user can restore if desired
		private int defaultHeight;
		private readonly List<IVirtualPad> Pads = new List<IVirtualPad>();

		public VirtualPadForm()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
		}

		private void VirtualPadForm_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			LoadPads();
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			StickyBox.Checked = Global.Config.VirtualPadSticky;

			if (Global.Config.VirtualPadSaveWindowPosition && Global.Config.VPadWndx >= 0 && Global.Config.VPadWndy >= 0)
			{
				Location = new Point(Global.Config.VPadWndx, Global.Config.VPadWndy);
			}

			if (Global.Config.VirtualPadSaveWindowPosition &&  Global.Config.VPadWidth >= 0 && Global.Config.VPadHeight >= 0)
			{
				Size = new Size(Global.Config.VPadWidth, Global.Config.VPadHeight);
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.VPadWndx = Location.X;
			Global.Config.VPadWndy = Location.Y;

			Global.Config.VPadWidth = Right - Left;
			Global.Config.VPadHeight = Bottom - Top;

			Pads.Clear();
		}

		private void LoadPads()
		{
			switch (Global.Emulator.SystemId)
			{
				case "A26":
					VirtualPadA26 ataripad1 = new VirtualPadA26 {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadA26 ataripad2 = new VirtualPadA26 {Location = new Point(188, 19), Controller = "P2"};
					Pads.Add(ataripad1);
					Pads.Add(ataripad2);
					ControllerBox.Controls.Add(ataripad1);
					ControllerBox.Controls.Add(ataripad2);
					VirtualPadA26Control ataricontrols = new VirtualPadA26Control {Location = new Point(8, 109)};
					Pads.Add(ataricontrols);
					ControllerBox.Controls.Add(Pads[2] as Control);
					break;
				case "A78":
					VirtualPadA78 atari78pad1 = new VirtualPadA78 {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadA78 atari78pad2 = new VirtualPadA78 {Location = new Point(150, 19), Controller = "P2"};
					Pads.Add(atari78pad1);
					Pads.Add(atari78pad2);
					ControllerBox.Controls.Add(atari78pad1);
					ControllerBox.Controls.Add(atari78pad2);
					VirtualPadA78Control atari78controls = new VirtualPadA78Control {Location = new Point(8, 125)};
					Pads.Add(atari78controls);
					ControllerBox.Controls.Add(Pads[2] as Control);
					break;
				case "NES":
					VirtualPadNES nespad1 = new VirtualPadNES {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadNES nespad2 = new VirtualPadNES {Location = new Point(188, 19), Controller = "P2"};
					Pads.Add(nespad1);
					Pads.Add(nespad2);
					ControllerBox.Controls.Add(nespad1);
					ControllerBox.Controls.Add(nespad2);
					VirtualPadNESControl controlpad1 = new VirtualPadNESControl {Location = new Point(8, 109)};
					Pads.Add(controlpad1);
					ControllerBox.Controls.Add(controlpad1);
					break;
				case "SMS":
				case "SG":
				case "GG":
					VirtualPadSMS smspad1 = new VirtualPadSMS {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadSMS smspad2 = new VirtualPadSMS {Location = new Point(188, 19), Controller = "P2"};
					Pads.Add(smspad1);
					Pads.Add(smspad2);
					ControllerBox.Controls.Add(smspad1);
					ControllerBox.Controls.Add(smspad2);
					VirtualPadSMSControl controlpad2 = new VirtualPadSMSControl {Location = new Point(8, 109)};
					Pads.Add(controlpad2);
					ControllerBox.Controls.Add(Pads[2] as Control);
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					VirtualPadPCE pcepad1 = new VirtualPadPCE {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadPCE pcepad2 = new VirtualPadPCE {Location = new Point(188, 19), Controller = "P2"};
					VirtualPadPCE pcepad3 = new VirtualPadPCE {Location = new Point(8, 109), Controller = "P3"};
					VirtualPadPCE pcepad4 = new VirtualPadPCE {Location = new Point(188, 109), Controller = "P4"};
					Pads.Add(pcepad1);
					Pads.Add(pcepad2);
					Pads.Add(pcepad3);
					Pads.Add(pcepad4);
					ControllerBox.Controls.Add(pcepad1);
					ControllerBox.Controls.Add(pcepad2);
					ControllerBox.Controls.Add(pcepad3);
					ControllerBox.Controls.Add(pcepad4);
					break;
				case "SNES":
					VirtualPadSNES snespad1 = new VirtualPadSNES {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadSNES snespad2 = new VirtualPadSNES {Location = new Point(188, 19), Controller = "P2"};
					VirtualPadSNES snespad3 = new VirtualPadSNES {Location = new Point(8, 95), Controller = "P3"};
					VirtualPadSNES snespad4 = new VirtualPadSNES {Location = new Point(188, 95), Controller = "P4"};
					VirtualPadSNESControl snescontrolpad = new VirtualPadSNESControl {Location = new Point(8, 170)};
					Pads.Add(snespad1);
					Pads.Add(snespad2);
					Pads.Add(snespad3);
					Pads.Add(snespad4);
					Pads.Add(snescontrolpad);
					ControllerBox.Controls.Add(snespad1);
					ControllerBox.Controls.Add(snespad2);
					ControllerBox.Controls.Add(snespad3);
					ControllerBox.Controls.Add(snespad4);
					ControllerBox.Controls.Add(snescontrolpad);
					break;
				case "GB":
				case "GBC":
					VirtualPadGB gbpad1 = new VirtualPadGB {Location = new Point(8, 19), Controller = ""};
					Pads.Add(gbpad1);
					ControllerBox.Controls.Add(gbpad1);
					VirtualPadGBControl gbcontrolpad = new VirtualPadGBControl {Location = new Point(8, 109)};
					Pads.Add(gbcontrolpad);
					ControllerBox.Controls.Add(gbcontrolpad);
					break;
				case "GBA":
					VirtualPadGBA gbapad1 = new VirtualPadGBA {Location = new Point(8, 19), Controller = ""};
					Pads.Add(gbapad1);
					ControllerBox.Controls.Add(gbapad1);
					break;
				case "GEN":
					VirtualPadGen3Button genpad1 = new VirtualPadGen3Button {Location = new Point(8, 19), Controller = "P1"};
					Pads.Add(genpad1);
					ControllerBox.Controls.Add(genpad1);
					break;
				case "Coleco":
					VirtualPadColeco coleco1 = new VirtualPadColeco {Location = new Point(8, 19), Controller = "P1"};
					VirtualPadColeco coleco2 = new VirtualPadColeco {Location = new Point(130, 19), Controller = "P2"};
					Pads.Add(coleco1);
					Pads.Add(coleco2);
					ControllerBox.Controls.Add(coleco1);
					ControllerBox.Controls.Add(coleco2);
					break;
				case "C64":
					VirtualPadC64Keyboard c64k = new VirtualPadC64Keyboard {Location = new Point(8, 19)};
					Pads.Add(c64k);
					ControllerBox.Controls.Add(c64k);
					VirtualPadA26 _ataripad1 = new VirtualPadA26 {Location = new Point(8, 159), Controller = "P1"};
					VirtualPadA26 _ataripad2 = new VirtualPadA26 {Location = new Point(218, 159), Controller = "P2"};
					Pads.Add(_ataripad1);
					Pads.Add(_ataripad2);
					ControllerBox.Controls.Add(_ataripad1);
					ControllerBox.Controls.Add(_ataripad2);
					break;
				case "N64":
					VirtualPadN64 n64pad1 = new VirtualPadN64 { Location = new Point(8, 19), Controller = "P1" };
					VirtualPadN64 n64pad2 = new VirtualPadN64 { Location = new Point(208, 19), Controller = "P2" };
					VirtualPadN64 n64pad3 = new VirtualPadN64 { Location = new Point(408, 19), Controller = "P3" };
					VirtualPadN64 n64pad4 = new VirtualPadN64 { Location = new Point(608, 19), Controller = "P4" };
					Pads.Add(n64pad1);
					Pads.Add(n64pad2);
					Pads.Add(n64pad3);
					Pads.Add(n64pad4);
					ControllerBox.Controls.Add(n64pad1);
					ControllerBox.Controls.Add(n64pad2);
					ControllerBox.Controls.Add(n64pad3);
					ControllerBox.Controls.Add(n64pad4);
					break;
				case "SAT":
					VirtualPadSaturn saturnpad1 = new VirtualPadSaturn { Location = new Point(8, 19), Controller = "P1" };
					VirtualPadSaturn saturnpad2 = new VirtualPadSaturn { Location = new Point(213, 19), Controller = "P2" };
					Pads.Add(saturnpad1);
					Pads.Add(saturnpad2);
					ControllerBox.Controls.Add(saturnpad1);
					ControllerBox.Controls.Add(saturnpad2);
					VirtualPadSaturnControl saturncontrols = new VirtualPadSaturnControl { Location = new Point(8, 125) };
					Pads.Add(saturncontrols);
					ControllerBox.Controls.Add(saturncontrols);
					break;
			}

			//Hack for now
			if (Global.Emulator.SystemId == "C64")
			{
				if (Width < 505)
				{
					Width = 505;
					ControllerBox.Width = Width - 37;
				}
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		public void ClearVirtualPadHolds()
		{
			foreach (var controller in ControllerBox.Controls)
			{
				var pad = controller as IVirtualPad;
				if (pad != null)
				{
					pad.Clear();
				}
			}
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed) return;
			ControllerBox.Controls.Clear();
			Pads.Clear();
			LoadPads();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;

			if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				string str = Global.MovieSession.Movie.GetInput(Global.Emulator.Frame);
				if (Global.Config.TASUpdatePads && str != "")
				{
					switch (Global.Emulator.SystemId)
					{
						case "NES":
							Pads[0].SetButtons(str.Substring(3, 8));
							Pads[1].SetButtons(str.Substring(12, 8));
							Pads[2].SetButtons(str[1].ToString());
							break;
						case "A26":
							Pads[0].SetButtons(str.Substring(4, 5));
							Pads[1].SetButtons(str.Substring(10, 5));
							Pads[2].SetButtons(str.Substring(1, 2));
							break;
						case "SMS":
						case "GG":
						case "SG":
							Pads[0].SetButtons(str.Substring(1, 6));
							Pads[1].SetButtons(str.Substring(8, 6));
							Pads[2].SetButtons(str.Substring(15, 2));
							break;
						case "PCE":
						case "SGX":
							Pads[0].SetButtons(str.Substring(3, 8));
							Pads[1].SetButtons(str.Substring(12, 8));
							Pads[2].SetButtons(str.Substring(21, 8));
							Pads[3].SetButtons(str.Substring(30, 8));
							break;
						case "TI83":
							Pads[0].SetButtons(str.Substring(2, 50));
							break;
						case "SNES":
							Pads[0].SetButtons(str.Substring(3, 12));
							Pads[1].SetButtons(str.Substring(16, 12));
							Pads[2].SetButtons(str.Substring(29, 12));
							Pads[3].SetButtons(str.Substring(42, 12));
							break;
						case "GEN":
							Pads[0].SetButtons(str.Substring(3, 8));
							Pads[1].SetButtons(str.Substring(12, 8));
							break;
						case "GB":
							Pads[0].SetButtons(str.Substring(3, 8));
							break;
						case "Coleco":
							Pads[0].SetButtons(str.Substring(1, 18));
							Pads[1].SetButtons(str.Substring(20, 18));
							break;
						case "C64":
							break;
						case "N64":
							Pads[0].SetButtons(str.Substring(3, 23));
							Pads[1].SetButtons(str.Substring(27, 23));
							Pads[2].SetButtons(str.Substring(51, 23));
							Pads[3].SetButtons(str.Substring(75, 23));
							break;
					}
				}
			}
			else
			{
				if (!Global.Config.VirtualPadSticky)
				{
					foreach (IVirtualPad v in Pads)
					{
						v.Clear();
					}
				}
			}
		}

		private void StickyBox_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSticky = StickyBox.Checked;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.VirtualPadSaveWindowPosition;
			autolaodToolStripMenuItem.Checked = Global.Config.AutoloadVirtualPad;
		}

		private void autolaodToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadVirtualPad ^= true;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSaveWindowPosition ^= true;
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearVirtualPadHolds();
		}

		private void restoreDefaultSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RestoreDefaultSettings();
		}

		private void RestoreDefaultSettings()
		{
			Size = new Size(defaultWidth, defaultHeight);

			Global.Config.VirtualPadSaveWindowPosition = true;
			Global.Config.VPadHeight = -1;
			Global.Config.VPadWidth = -1;
		}

		//TODO: multi-player
		public void BumpAnalogValue(int? dx, int? dy)
		{
			//TODO: make an analog flag in virtualpads that have it, and check the virtualpads loaded, instead of doing this hardcoded
			if (Global.Emulator is N64)
			{
				(Pads[0] as VirtualPadN64).FudgeAnalog(dx, dy);

				UpdateValues();
			}
		}
	}
}

