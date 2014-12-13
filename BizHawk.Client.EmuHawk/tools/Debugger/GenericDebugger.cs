using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : Form, IToolForm, IControlMainform
	{
		private int _defaultWidth;
		private int _defaultHeight;

		private IDebuggable Core;

		public GenericDebugger()
		{
			InitializeComponent();
			TopMost = Global.Config.GenericDebuggerSettings.TopMost;
			Closing += (o, e) => DisengageDebugger();
		}

		private void GenericDebugger_Load(object sender, EventArgs e)
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.GenericDebuggerSettings.UseWindowPosition)
			{
				Location = Global.Config.GenericDebuggerSettings.WindowPosition;
			}

			if (Global.Config.GenericDebuggerSettings.UseWindowSize)
			{
				Size = Global.Config.GenericDebuggerSettings.WindowSize;
			}

			if (Global.Emulator.CanDebug())
			{
				Core = Global.Emulator.AsDebuggable();
			}
			else
			{
				Close();
			}

			EngageDebugger();
		}

		public void DisableRegisterBox()
		{
			RegistersGroupBox.Enabled = false;
		}

		private void EngageDebugger()
		{
			RegisterPanel.Core = Core;
			RegisterPanel.ParentDebugger = this;
			RegisterPanel.GenerateUI();

			// TODO: handle if unavailable
			BreakPointControl1.Core = Core;
			BreakPointControl1.ParentDebugger = this;
			BreakPointControl1.GenerateUI();
		}

		private void DisengageDebugger()
		{
			SaveConfigSettings();

			if (Core.CpuTraceAvailable())
			{
				Core.Tracer.Enabled = false;
			}

			BreakPointControl1.Shutdown();
		}

		private void SaveConfigSettings()
		{
			if (Global.Config.GenericDebuggerSettings.SaveWindowPosition)
			{
				Global.Config.GenericDebuggerSettings.Wndx = Location.X;
				Global.Config.GenericDebuggerSettings.Wndy = Location.Y;
				Global.Config.GenericDebuggerSettings.Width = Right - Left;
				Global.Config.GenericDebuggerSettings.Height = Bottom - Top;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.RamSearchSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region Menu Items

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.GenericDebuggerAutoload;
			SaveWindowPositionMenuItem.Checked = Global.Config.GenericDebuggerSettings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.GenericDebuggerSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.GenericDebuggerSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GenericDebuggerAutoload ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GenericDebuggerSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			TopMost = Global.Config.GenericDebuggerSettings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GenericDebuggerSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			Global.Config.GenericDebuggerSettings = new ToolDialogSettings();
			TopMost = Global.Config.GenericDebuggerSettings.TopMost;
			RefreshFloatingWindowControl();
		}

		#endregion
	}
}
