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
	public partial class VirtualPadForm : Form
	{
		//TODO: clicky vs sticky
		//Remember window size
		//Restore defaults

		List<IVirtualPad> Pads = new List<IVirtualPad>();

		public VirtualPadForm()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
		}

		private void VirtualPadForm_Load(object sender, EventArgs e)
		{
			StickyBox.Checked = Global.Config.VirtualPadSticky;

			if (Global.Config.VirtualPadSaveWindowPosition && Global.Config.VPadWndx >= 0 && Global.Config.VPadWndy >= 0)
			{
				this.Location = new Point(Global.Config.VPadWndx, Global.Config.VPadWndy);
			}

			LoadPads();
		}

		private void SaveConfigSettings()
		{
			Global.Config.VPadWndx = this.Location.X;
			Global.Config.VPadWndy = this.Location.Y;
			Pads.Clear();
		}

		private void LoadPads()
		{
			switch (Global.Emulator.SystemId)
			{
				case "NULL":
				default:
					break;
				case "A26":
					VirtualPadA26 ataripad1 = new VirtualPadA26();
					ataripad1.Location = new Point(8, 19);
					ataripad1.Controller = "P1";
					VirtualPadA26 ataripad2 = new VirtualPadA26();
					ataripad2.Location = new Point(188, 19);
					ataripad2.Controller = "P2";
					Pads.Add(ataripad1);
					Pads.Add(ataripad2);
					ControllerBox.Controls.Add(ataripad1);
					ControllerBox.Controls.Add(ataripad2);
					VirtualPadA26Control ataricontrols = new VirtualPadA26Control();
					ataricontrols.Location = new Point(8, 109);
					Pads.Add(ataricontrols);
					ControllerBox.Controls.Add(Pads[2] as Control);
					break;
				case "NES":
					VirtualPadNES nespad1 = new VirtualPadNES();
					nespad1.Location = new Point(8, 19);
					nespad1.Controller = "P1";
					VirtualPadNES nespad2 = new VirtualPadNES();
					nespad2.Location = new Point(188, 19);
					nespad2.Controller = "P2";
					Pads.Add(nespad1);
					Pads.Add(nespad2);
					ControllerBox.Controls.Add(nespad1);
					ControllerBox.Controls.Add(nespad2);
					VirtualPadNESControl controlpad1 = new VirtualPadNESControl();
					controlpad1.Location = new Point(8, 109);
					Pads.Add(controlpad1);
					ControllerBox.Controls.Add(controlpad1);
					break;
				case "SMS":
				case "SG":
				case "GG":
					VirtualPadSMS smspad1 = new VirtualPadSMS();
					smspad1.Location = new Point(8, 19);
					smspad1.Controller = "P1";
					VirtualPadSMS smspad2 = new VirtualPadSMS();
					smspad2.Location = new Point(188, 19);
					smspad2.Controller = "P2";
					Pads.Add(smspad1);
					Pads.Add(smspad2);
					ControllerBox.Controls.Add(smspad1);
					ControllerBox.Controls.Add(smspad2);
					VirtualPadSMSControl controlpad2 = new VirtualPadSMSControl();
					controlpad2.Location = new Point(8, 109);
					Pads.Add(controlpad2);
					ControllerBox.Controls.Add(Pads[2] as Control);
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					VirtualPadPCE pcepad1 = new VirtualPadPCE();
					pcepad1.Location = new Point(8, 19);
					pcepad1.Controller = "P1";
					VirtualPadPCE pcepad2 = new VirtualPadPCE();
					pcepad2.Location = new Point(188, 19);
					pcepad2.Controller = "P2";
					VirtualPadPCE pcepad3 = new VirtualPadPCE();
					pcepad3.Location = new Point(8, 109);
					pcepad3.Controller = "P3";
					VirtualPadPCE pcepad4 = new VirtualPadPCE();
					pcepad4.Location = new Point(188, 109);
					pcepad4.Controller = "P4";
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
					VirtualPadSNES snespad1 = new VirtualPadSNES();
					snespad1.Location = new Point(8, 19);
					snespad1.Controller = "P1";
					VirtualPadSNES snespad2 = new VirtualPadSNES();
					snespad2.Location = new Point(188, 19);
					snespad2.Controller = "P2";
					VirtualPadSNES snespad3 = new VirtualPadSNES();
					snespad3.Location = new Point(8, 95);
					snespad3.Controller = "P3";
					VirtualPadSNES snespad4 = new VirtualPadSNES();
					snespad4.Location = new Point(188, 95);
					snespad4.Controller = "P4";
					VirtualPadSNESControl snescontrolpad = new VirtualPadSNESControl();
					snescontrolpad.Location = new Point(8, 170);
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
					VirtualPadGB gbpad1 = new VirtualPadGB();
					gbpad1.Location = new Point(8, 19);
					gbpad1.Controller = "";
					Pads.Add(gbpad1);
					ControllerBox.Controls.Add(gbpad1);
					VirtualPadGBControl gbcontrolpad = new VirtualPadGBControl();
					gbcontrolpad.Location = new Point(8, 109);
					Pads.Add(gbcontrolpad);
					ControllerBox.Controls.Add(gbcontrolpad);
					break;
				case "GBA":
					VirtualPadGBA gbapad1 = new VirtualPadGBA();
					gbapad1.Location = new Point(8, 19);
					gbapad1.Controller = "";
					Pads.Add(gbapad1);
					ControllerBox.Controls.Add(gbapad1);
					break;
				case "GEN":
					VirtualPadGen3Button genpad1 = new VirtualPadGen3Button();
					genpad1.Location = new Point(8, 19);
					genpad1.Controller = "P1";
					Pads.Add(genpad1);
					ControllerBox.Controls.Add(genpad1);
					break;
				case "Coleco":
					VirtualPadColeco coleco1 = new VirtualPadColeco();
					coleco1.Location = new Point(8, 19);
					coleco1.Controller = "P1";
					VirtualPadColeco coleco2 = new VirtualPadColeco();
					coleco2.Location = new Point(130, 19);
					coleco2.Controller = "P2";
					Pads.Add(coleco1);
					Pads.Add(coleco2);
					ControllerBox.Controls.Add(coleco1);
					ControllerBox.Controls.Add(coleco2);
					break;
				case "C64":
					VirtualPadC64Keyboard c64k = new VirtualPadC64Keyboard();
					c64k.Location = new Point(8, 19);
					Pads.Add(c64k);
					ControllerBox.Controls.Add(c64k);

					VirtualPadA26 _ataripad1 = new VirtualPadA26();
					_ataripad1.Location = new Point(8, 159);
					_ataripad1.Controller = "P1";
					VirtualPadA26 _ataripad2 = new VirtualPadA26();
					_ataripad2.Location = new Point(218, 159);
					_ataripad2.Controller = "P2";
					Pads.Add(_ataripad1);
					Pads.Add(_ataripad2);
					ControllerBox.Controls.Add(_ataripad1);
					ControllerBox.Controls.Add(_ataripad2);
					break;
			}

			//Hack for now
			if (Global.Emulator.SystemId == "C64")
			{
				if (this.Width < 505)
				{
					this.Width = 505;
					ControllerBox.Width = this.Width - 37;
				}
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		public void ClearVirtualPadHolds()
		{
			foreach (var controller in ControllerBox.Controls)
			{
				if (controller is IVirtualPad)
					((IVirtualPad)controller).Clear();
			}
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			ControllerBox.Controls.Clear();
			Pads.Clear();
			LoadPads();
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				string str = Global.MovieSession.Movie.GetInput(Global.Emulator.Frame);
				if (Global.Config.TASUpdatePads == true && str != "")
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
						default:
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
	}
}

