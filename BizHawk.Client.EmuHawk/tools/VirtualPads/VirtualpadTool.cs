using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class VirtualpadTool : Form, IToolForm
	{
		private int _defaultWidth;
		private int _defaultHeight;

		private List<IVirtualPad> Pads
		{
			get
			{
				return ControllerBox.Controls
					.OfType<IVirtualPad>()
					.ToList();
			}
		}

		public VirtualpadTool()
		{
			InitializeComponent();
		}

		private void VirtualpadTool_Load(object sender, EventArgs e)
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

			CreatePads();
		}

		public void ClearVirtualPadHolds()
		{
			ControllerBox.Controls
				.OfType<IVirtualPad>()
				.ToList()
				.ForEach(pad => pad.Clear());
		}

		public void BumpAnalogValue(int? dx, int? dy) // TODO: multi-player
		{

		}

		private void CreatePads()
		{
			ControllerBox.Controls.Clear();

			switch(Global.Emulator.SystemId)
			{
				case "NES":
					ControllerBox.Controls.Add(new VirtualPadControl(
						NesSchema.StandardController(1))
						{
							Location = new Point(15, 15)
						});
					ControllerBox.Controls.Add(new VirtualPadControl(
						NesSchema.StandardController(2))
						{
							Location = new Point(200, 15)
						});
					break;
				
			}
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.VirtualPadSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region IToolForm Implementation

		public bool AskSave() { return true; }
		public bool UpdateBefore { get { return false; } }

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			CreatePads();
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (!Global.Config.VirtualPadSticky)
			{
				Pads.ForEach(pad => pad.Clear());
			}
		}

		#endregion

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
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void ClearAllMenuItem_Click(object sender, EventArgs e)
		{
			ClearVirtualPadHolds();
		}

		#endregion

		#endregion
	}
}
