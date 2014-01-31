using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualPadForm : Form, IToolForm
	{
		// TODO: UpdateValues doesn't support all cores, and is probably wrong for gens, also done in an unsustainable way
		private readonly List<IVirtualPad> _pads = new List<IVirtualPad>();

		private int _defaultWidth;
		private int _defaultHeight;

		#region Public API

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }

		public void ClearVirtualPadHolds()
		{
			ControllerBox.Controls
				.OfType<IVirtualPad>()
				.ToList()
				.ForEach(pad => pad.Clear());
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			ControllerBox.Controls.Clear();
			_pads.Clear();
			LoadPads();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				var str = Global.MovieSession.Movie.GetInput(Global.Emulator.Frame);
				if (Global.Config.VirtualPadsUpdatePads && str != string.Empty)
				{
					switch (Global.Emulator.SystemId)
					{
						case "NES":
							_pads[0].SetButtons(str.Substring(3, 8));
							_pads[1].SetButtons(str.Substring(12, 8));
							_pads[2].SetButtons(str[1].ToString());
							break;
						case "A26":
							_pads[0].SetButtons(str.Substring(4, 5));
							_pads[1].SetButtons(str.Substring(10, 5));
							_pads[2].SetButtons(str.Substring(1, 2));
							break;
						case "SMS":
						case "GG":
						case "SG":
							_pads[0].SetButtons(str.Substring(1, 6));
							_pads[1].SetButtons(str.Substring(8, 6));
							_pads[2].SetButtons(str.Substring(15, 2));
							break;
						case "PCE":
						case "SGX":
							_pads[0].SetButtons(str.Substring(3, 8));
							_pads[1].SetButtons(str.Substring(12, 8));
							_pads[2].SetButtons(str.Substring(21, 8));
							_pads[3].SetButtons(str.Substring(30, 8));
							break;
						case "TI83":
							_pads[0].SetButtons(str.Substring(2, 50));
							break;
						case "SNES":
							_pads[0].SetButtons(str.Substring(3, 12));
							_pads[1].SetButtons(str.Substring(16, 12));
							_pads[2].SetButtons(str.Substring(29, 12));
							_pads[3].SetButtons(str.Substring(42, 12));
							break;
						case "GEN":
							_pads[0].SetButtons(str.Substring(3, 8));
							_pads[1].SetButtons(str.Substring(12, 8));
							break;
						case "GB":
							_pads[0].SetButtons(str.Substring(3, 8));
							break;
						case "Coleco":
							_pads[0].SetButtons(str.Substring(1, 18));
							_pads[1].SetButtons(str.Substring(20, 18));
							break;
						case "C64":
							break;
						case "N64":
							_pads[0].SetButtons(str.Substring(3, 23));
							_pads[1].SetButtons(str.Substring(27, 23));
							_pads[2].SetButtons(str.Substring(51, 23));
							_pads[3].SetButtons(str.Substring(75, 23));
							break;
					}
				}
			}
			else if (!Global.Config.VirtualPadSticky)
			{
				_pads.ForEach(pad => pad.Clear());
			}
		}

		public VirtualPadForm()
		{
			InitializeComponent();
			Closing += (o, e) => SaveConfigSettings();
			TopMost = Global.Config.VirtualPadSettings.TopMost;
		}

		#endregion

		private void VirtualPadForm_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			LoadPads();
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			StickyBox.Checked = Global.Config.VirtualPadSticky;

			if (Global.Config.VirtualPadSettings.UseWindowPosition)
			{
				Location = Global.Config.VirtualPadSettings.WindowPosition;
			}

			if (Global.Config.VirtualPadSettings.UseWindowPosition)
			{
				Size = Global.Config.VirtualPadSettings.WindowSize;
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.VirtualPadSettings.Wndx = Location.X;
			Global.Config.VirtualPadSettings.Wndy = Location.Y;
			Global.Config.VirtualPadSettings.Width = Right - Left;
			Global.Config.VirtualPadSettings.Height = Bottom - Top;

			_pads.Clear();
		}

		private void LoadPads()
		{
			switch (Global.Emulator.SystemId)
			{
				case "A26":
					var ataripad1 = new VirtualPadA26 { Location = new Point(8, 19), Controller = "P1" };
					var ataripad2 = new VirtualPadA26 { Location = new Point(188, 19), Controller = "P2" };
					_pads.Add(ataripad1);
					_pads.Add(ataripad2);
					ControllerBox.Controls.Add(ataripad1);
					ControllerBox.Controls.Add(ataripad2);
					var ataricontrols = new VirtualPadA26Control { Location = new Point(8, 109) };
					_pads.Add(ataricontrols);
					ControllerBox.Controls.Add(ataricontrols);
					break;
				case "A78":
					var atari78pad1 = new VirtualPadA78 { Location = new Point(8, 19), Controller = "P1" };
					var atari78pad2 = new VirtualPadA78 { Location = new Point(150, 19), Controller = "P2" };
					_pads.Add(atari78pad1);
					_pads.Add(atari78pad2);
					ControllerBox.Controls.Add(atari78pad1);
					ControllerBox.Controls.Add(atari78pad2);
					var atari78controls = new VirtualPadA78Control { Location = new Point(8, 125) };
					_pads.Add(atari78controls);
					ControllerBox.Controls.Add(atari78controls);
					break;
				case "NES":
					var nespad1 = new VirtualPadNES { Location = new Point(8, 19), Controller = "P1" };
					var nespad2 = new VirtualPadNES { Location = new Point(188, 19), Controller = "P2" };
					_pads.Add(nespad1);
					_pads.Add(nespad2);
					ControllerBox.Controls.Add(nespad1);
					ControllerBox.Controls.Add(nespad2);
					var controlpad1 = new VirtualPadNESControl { Location = new Point(8, 109) };
					_pads.Add(controlpad1);
					ControllerBox.Controls.Add(controlpad1);
					break;
				case "N64":
					var n64pad1 = new VirtualPadN64 { Location = new Point(8, 19), Controller = "P1" };
					var n64pad2 = new VirtualPadN64 { Location = new Point(208, 19), Controller = "P2" };
					var n64pad3 = new VirtualPadN64 { Location = new Point(408, 19), Controller = "P3" };
					var n64pad4 = new VirtualPadN64 { Location = new Point(608, 19), Controller = "P4" };
					_pads.Add(n64pad1);
					_pads.Add(n64pad2);
					_pads.Add(n64pad3);
					_pads.Add(n64pad4);
					ControllerBox.Controls.Add(n64pad1);
					ControllerBox.Controls.Add(n64pad2);
					ControllerBox.Controls.Add(n64pad3);
					ControllerBox.Controls.Add(n64pad4);
					var n64controlpad1 = new VirtualPadN64Control { Location = new Point(8, 350) };
					_pads.Add(n64controlpad1);
					ControllerBox.Controls.Add(n64controlpad1);
					break;
				case "SMS":
				case "SG":
				case "GG":
					var smspad1 = new VirtualPadSMS { Location = new Point(8, 19), Controller = "P1" };
					var smspad2 = new VirtualPadSMS { Location = new Point(188, 19), Controller = "P2" };
					_pads.Add(smspad1);
					_pads.Add(smspad2);
					ControllerBox.Controls.Add(smspad1);
					ControllerBox.Controls.Add(smspad2);
					var controlpad2 = new VirtualPadSMSControl { Location = new Point(8, 109) };
					_pads.Add(controlpad2);
					ControllerBox.Controls.Add(controlpad2);
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					var pcepad1 = new VirtualPadPCE { Location = new Point(8, 19), Controller = "P1" };
					var pcepad2 = new VirtualPadPCE { Location = new Point(188, 19), Controller = "P2" };
					var pcepad3 = new VirtualPadPCE { Location = new Point(8, 109), Controller = "P3" };
					var pcepad4 = new VirtualPadPCE { Location = new Point(188, 109), Controller = "P4" };
					_pads.Add(pcepad1);
					_pads.Add(pcepad2);
					_pads.Add(pcepad3);
					_pads.Add(pcepad4);
					ControllerBox.Controls.Add(pcepad1);
					ControllerBox.Controls.Add(pcepad2);
					ControllerBox.Controls.Add(pcepad3);
					ControllerBox.Controls.Add(pcepad4);
					break;
				case "SNES":
					var snespad1 = new VirtualPadSNES { Location = new Point(8, 19), Controller = "P1" };
					var snespad2 = new VirtualPadSNES { Location = new Point(188, 19), Controller = "P2" };
					var snespad3 = new VirtualPadSNES { Location = new Point(8, 95), Controller = "P3" };
					var snespad4 = new VirtualPadSNES { Location = new Point(188, 95), Controller = "P4" };
					var snescontrolpad = new VirtualPadSNESControl { Location = new Point(8, 170) };
					_pads.Add(snespad1);
					_pads.Add(snespad2);
					_pads.Add(snespad3);
					_pads.Add(snespad4);
					_pads.Add(snescontrolpad);
					ControllerBox.Controls.Add(snespad1);
					ControllerBox.Controls.Add(snespad2);
					ControllerBox.Controls.Add(snespad3);
					ControllerBox.Controls.Add(snespad4);
					ControllerBox.Controls.Add(snescontrolpad);
					break;
				case "GB":
				case "GBC":
					var gbpad1 = new VirtualPadGB { Location = new Point(8, 19), Controller = string.Empty };
					_pads.Add(gbpad1);
					ControllerBox.Controls.Add(gbpad1);
					var gbcontrolpad = new VirtualPadGBControl { Location = new Point(8, 109) };
					_pads.Add(gbcontrolpad);
					ControllerBox.Controls.Add(gbcontrolpad);
					break;
				case "GBA":
					var gbapad1 = new VirtualPadGBA { Location = new Point(8, 19), Controller = string.Empty };
					_pads.Add(gbapad1);
					ControllerBox.Controls.Add(gbapad1);
					break;
				case "GEN":
					var genpad1 = new VirtualPadGen6Button { Location = new Point(8, 19), Controller = "P1" };
					var genpad2 = new VirtualPadGen6Button { Location = new Point(195, 19), Controller = "P2" };
					_pads.Add(genpad1);
					_pads.Add(genpad2);
					ControllerBox.Controls.Add(genpad1);
					ControllerBox.Controls.Add(genpad2);

					var gencontrol = new VirtualPadNESControl { Location = new Point(8, 105) };
					_pads.Add(gencontrol);
					ControllerBox.Controls.Add(gencontrol);

					break;
				case "Coleco":
					var coleco1 = new VirtualPadColeco { Location = new Point(8, 19), Controller = "P1" };
					var coleco2 = new VirtualPadColeco { Location = new Point(130, 19), Controller = "P2" };
					_pads.Add(coleco1);
					_pads.Add(coleco2);
					ControllerBox.Controls.Add(coleco1);
					ControllerBox.Controls.Add(coleco2);
					break;
				case "C64":
					var c64k = new VirtualPadC64Keyboard { Location = new Point(8, 19) };
					_pads.Add(c64k);
					ControllerBox.Controls.Add(c64k);
					var _ataripad1 = new VirtualPadA26 { Location = new Point(8, 159), Controller = "P1" };
					var _ataripad2 = new VirtualPadA26 { Location = new Point(218, 159), Controller = "P2" };
					_pads.Add(_ataripad1);
					_pads.Add(_ataripad2);
					ControllerBox.Controls.Add(_ataripad1);
					ControllerBox.Controls.Add(_ataripad2);
					break;
				case "SAT":
					var saturnpad1 = new VirtualPadSaturn { Location = new Point(8, 19), Controller = "P1" };
					var saturnpad2 = new VirtualPadSaturn { Location = new Point(213, 19), Controller = "P2" };
					_pads.Add(saturnpad1);
					_pads.Add(saturnpad2);
					ControllerBox.Controls.Add(saturnpad1);
					ControllerBox.Controls.Add(saturnpad2);
					var saturncontrols = new VirtualPadSaturnControl { Location = new Point(8, 125) };
					_pads.Add(saturncontrols);
					ControllerBox.Controls.Add(saturncontrols);
					break;
			}

			// Hack for now
			if (Global.Emulator.SystemId == "C64")
			{
				if (Width < 505)
				{
					Width = 505;
					ControllerBox.Width = Width - 37;
				}
			}
		}

		// TODO: multi-player
		public void BumpAnalogValue(int? dx, int? dy)
		{
			// TODO: make an analog flag in virtualpads that have it, and check the virtualpads loaded, instead of doing this hardcoded
			if (_pads[0] is VirtualPadN64)
			{
				(_pads[0] as VirtualPadN64).FudgeAnalog(dx, dy);

				UpdateValues();
			}
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.VirtualPadSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region Events

		#region Menu

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.AutoloadVirtualPad;
			SaveWindowPositionMenuItem.Checked = Global.Config.VirtualPadSettings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.VirtualPadSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.VirtualPadSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadVirtualPad ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSettings.TopMost ^= true;
			TopMost = Global.Config.VirtualPadSettings.TopMost;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);

			Global.Config.VirtualPadSettings.SaveWindowPosition = true;
			Global.Config.VirtualPadSettings.TopMost = TopMost = false;
			Global.Config.VirtualPadSettings.FloatingWindow = false;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Dialog, Controls, Context Menu

		protected override void OnClosed(EventArgs e)
		{
			Global.StickyXORAdapter.ClearStickies();
			Global.StickyXORAdapter.ClearStickyFloats();
		}

		private void ClearMenuItem_Click(object sender, EventArgs e)
		{
			ClearVirtualPadHolds();
		}

		private void StickyBox_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.VirtualPadSticky = StickyBox.Checked;
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
}
