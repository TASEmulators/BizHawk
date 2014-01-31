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
		private List<IVirtualPad> Pads
		{
			get
			{
				return ControllerBox.Controls.OfType<IVirtualPad>().ToList();
			}
		}

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
			else if (!Global.Config.VirtualPadSticky)
			{
				Pads.ForEach(pad => pad.Clear());
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
		}

		private void LoadPads()
		{
			switch (Global.Emulator.SystemId)
			{
				case "A26":
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadA26Control { Location = new Point(8, 109) });
					break;
				case "A78":
					ControllerBox.Controls.Add(new VirtualPadA78 { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadA78 { Location = new Point(150, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadA78Control { Location = new Point(8, 125) });
					break;
				case "NES":
					ControllerBox.Controls.Add(new VirtualPadNES { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadNES { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadNESControl { Location = new Point(8, 109) });
					break;
				case "N64":
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(208, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(408, 19), Controller = "P3" });
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(608, 19), Controller = "P4" });
					ControllerBox.Controls.Add(new VirtualPadN64Control { Location = new Point(8, 350) });
					break;
				case "SMS":
				case "SG":
				case "GG":
					ControllerBox.Controls.Add(new VirtualPadSMS { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadSMS { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadSMSControl { Location = new Point(8, 109) });
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(8, 109), Controller = "P3" });
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(188, 109), Controller = "P4" });
					break;
				case "SNES":
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(8, 95), Controller = "P3" });
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(188, 95), Controller = "P4" });
					ControllerBox.Controls.Add(new VirtualPadSNESControl { Location = new Point(8, 170) });
					break;
				case "GB":
				case "GBC":
					ControllerBox.Controls.Add(new VirtualPadGB { Location = new Point(8, 19), Controller = string.Empty });
					ControllerBox.Controls.Add(new VirtualPadGBControl { Location = new Point(8, 109) });
					break;
				case "GBA":
					ControllerBox.Controls.Add(new VirtualPadGBA { Location = new Point(8, 19), Controller = string.Empty });
					break;
				case "GEN":
					ControllerBox.Controls.Add(new VirtualPadGen6Button { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadGen6Button { Location = new Point(195, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadNESControl { Location = new Point(8, 105) });
					break;
				case "Coleco":
					var coleco1 = new VirtualPadColeco { Location = new Point(8, 19), Controller = "P1" };
					var coleco2 = new VirtualPadColeco { Location = new Point(130, 19), Controller = "P2" };
					ControllerBox.Controls.Add(coleco1);
					ControllerBox.Controls.Add(coleco2);
					break;
				case "C64":
					ControllerBox.Controls.Add(new VirtualPadC64Keyboard { Location = new Point(8, 19) });
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(8, 159), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(218, 159), Controller = "P2" });
					break;
				case "SAT":
					ControllerBox.Controls.Add(new VirtualPadSaturn { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadSaturn { Location = new Point(213, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadSaturnControl { Location = new Point(8, 125) });
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

		public void BumpAnalogValue(int? dx, int? dy) // TODO: multi-player
		{
			// TODO: make an analog flag in virtualpads that have it, and check the virtualpads loaded, instead of doing this hardcoded
			if (Pads[0] is VirtualPadN64)
			{
				(Pads[0] as VirtualPadN64).FudgeAnalog(dx, dy);

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
