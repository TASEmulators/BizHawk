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
		// TODO: move _multiplayerMode to config, and support on load, also support being able to go back to single player mode.  Also, more nuanced options are preferrable
		private List<IVirtualPad> Pads
		{
			get
			{
				return ControllerBox.Controls
					.OfType<IVirtualPad>()
					.ToList();
			}
		}

		private List<string> CurrentFrameInputs
		{
			get
			{
				return Global.MovieSession.Movie
					.GetInput(Global.Emulator.Frame)
					.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
					.ToList();
			}
		}

		private readonly List<string> SupportedMultiplayerPlatforms = new List<string>
		{
			"A26",
			"A78",
			"NES",
			"N64",
			"SMS",
			"SG",
			"GG",
			"PCE",
			"PCECD",
			"SGX",
			"SNES",
			"GEN",
			"Coleco",
			"C64",
			"SAT"
		};

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
			LoadStartingPads();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (Global.MovieSession.Movie.IsPlaying && !Global.MovieSession.Movie.IsFinished)
			{
				var inputs = CurrentFrameInputs; // THis is necessary because of the weird situation on the frame after the end of the movie, it goes into finished mode AFTER the input is read so this will be empty on that frame
				if (inputs.Any())
				{
					for (int i = 0; i < Pads.Count; i++)
					{
						Pads[i].SetButtons(inputs[i]);
					}
				}
			}
			else if (!Global.Config.VirtualPadSticky)
			{
				Pads.ForEach(pad => pad.Clear());
			}

			if (Global.MovieSession.Movie.IsActive && !Global.MovieSession.Movie.IsFinished)
			{
				Pads
					.Where(x => x is VirtualPadN64)
					.Cast<VirtualPadN64>()
					.ToList()
					.ForEach(x =>
					{
						x.RefreshAnalog();
					});
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
			LoadStartingPads();
			if (Global.Config.VirtualPadMultiplayerMode)
			{
				SwitchToMultiplayer();
			}
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

		private void LoadStartingPads()
		{
			// Order matters! Add them in the of order of the mnemonic system
			switch (Global.Emulator.SystemId)
			{
				case "A26":
					ControllerBox.Controls.Add(new VirtualPadA26Control { Location = new Point(8, 109) });
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "A78":
					ControllerBox.Controls.Add(new VirtualPadA78Control { Location = new Point(8, 125) });
					ControllerBox.Controls.Add(new VirtualPadA78 { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "NES":
					ControllerBox.Controls.Add(new VirtualPadNESControl { Location = new Point(8, 109) });
					ControllerBox.Controls.Add(new VirtualPadNES { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "N64":
					ControllerBox.Controls.Add(new VirtualPadN64Control { Location = new Point(8, 350) });
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "SMS":
				case "SG":
				case "GG":
					ControllerBox.Controls.Add(new VirtualPadSMSControl { Location = new Point(8, 109) });	
					ControllerBox.Controls.Add(new VirtualPadSMS { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "SNES":
					ControllerBox.Controls.Add(new VirtualPadSNESControl { Location = new Point(8, 170) });
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "GB":
				case "GBC":
					ControllerBox.Controls.Add(new VirtualPadGBControl { Location = new Point(8, 109) });	
					ControllerBox.Controls.Add(new VirtualPadGB { Location = new Point(8, 19), Controller = string.Empty });
					break;
				case "DGB":
					ControllerBox.Controls.Add(new VirtualPadGBControl { Location = new Point(8, 109), Controller = string.Empty });
					ControllerBox.Controls.Add(new VirtualPadGB { Location = new Point(8, 19), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadGB { Location = new Point(188, 19), Controller = "P2" });
					break;
				case "GBA":
					ControllerBox.Controls.Add(new VirtualPadGBA { Location = new Point(8, 19), Controller = string.Empty });
					break;
				case "GEN":
					ControllerBox.Controls.Add(new VirtualPadNESControl { Location = new Point(8, 105) });
					ControllerBox.Controls.Add(new VirtualPadGen6Button { Location = new Point(8, 19), Controller = "P1" });
					break;
				case "Coleco":
					var coleco1 = new VirtualPadColeco { Location = new Point(8, 19), Controller = "P1" };
					ControllerBox.Controls.Add(coleco1);
					break;
				case "C64":
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(8, 159), Controller = "P1" });
					ControllerBox.Controls.Add(new VirtualPadC64Keyboard { Location = new Point(8, 19) });
					break;
				case "SAT":
					ControllerBox.Controls.Add(new VirtualPadSaturnControl { Location = new Point(8, 125) });
					ControllerBox.Controls.Add(new VirtualPadSaturn { Location = new Point(8, 19), Controller = "P1" });
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

		private void SwitchToMultiplayer()
		{
			switch (Global.Emulator.SystemId)
			{
				case "A26":
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(188, 19), Controller = "P2" });
					break;
				case "A78":
					ControllerBox.Controls.Add(new VirtualPadA78 { Location = new Point(150, 19), Controller = "P2" });
					break;
				case "NES":
					ControllerBox.Controls.Add(new VirtualPadNES { Location = new Point(188, 19), Controller = "P2" });
					break;
				case "N64":
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(208, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(408, 19), Controller = "P3" });
					ControllerBox.Controls.Add(new VirtualPadN64 { Location = new Point(608, 19), Controller = "P4" });
					break;
				case "SMS":
				case "SG":
				case "GG":
					ControllerBox.Controls.Add(new VirtualPadSMS { Location = new Point(188, 19), Controller = "P2" });
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(8, 109), Controller = "P3" });
					ControllerBox.Controls.Add(new VirtualPadPCE { Location = new Point(188, 109), Controller = "P4" });
					break;
				case "SNES":
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(188, 19), Controller = "P2" });
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(8, 95), Controller = "P3" });
					ControllerBox.Controls.Add(new VirtualPadSNES { Location = new Point(188, 95), Controller = "P4" });
					break;
				case "GEN":
					ControllerBox.Controls.Add(new VirtualPadGen6Button { Location = new Point(195, 19), Controller = "P2" });
					break;
				case "Coleco":
					ControllerBox.Controls.Add(new VirtualPadColeco { Location = new Point(130, 19), Controller = "P2" });
					break;
				case "C64":
					ControllerBox.Controls.Add(new VirtualPadA26 { Location = new Point(218, 159), Controller = "P2" });
					break;
				case "SAT":
					ControllerBox.Controls.Add(new VirtualPadSaturn { Location = new Point(213, 19), Controller = "P2" });
					break;
			}

			Global.Config.VirtualPadMultiplayerMode = true;
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
			Global.Config.VirtualPadMultiplayerMode = false;
			SwitchToSinglePlayer();
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

		private void SwitchToSinglePlayer()
		{
			var toRemove = Pads
					.Where(pad => !string.IsNullOrEmpty(pad.Controller) && pad.Controller != "P1")
					.ToList();

			foreach (var pad in toRemove)
			{
				ControllerBox.Controls.Remove(pad as Control);
			}

			Global.Config.VirtualPadMultiplayerMode = false;
		}

		#endregion

		#endregion

		private void MultiplayerModeMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Config.VirtualPadMultiplayerMode)
			{
				SwitchToSinglePlayer();
			}
			else
			{
				SwitchToMultiplayer();
			}
		}

		private void PadsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MultiplayerModeMenuItem.Enabled = SupportedMultiplayerPlatforms.Contains(Global.Emulator.SystemId);
			MultiplayerModeMenuItem.Checked = Global.Config.VirtualPadMultiplayerMode;
		}
	}
}
