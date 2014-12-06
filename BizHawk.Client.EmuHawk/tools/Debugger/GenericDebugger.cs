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
		private readonly List<string> _instructions = new List<string>();

		public GenericDebugger()
		{
			InitializeComponent();
			TopMost = Global.Config.GenericDebuggerSettings.TopMost;
			Closing += (o, e) => DisengageDebugger();

			TraceView.QueryItemText += TraceView_QueryItemText;
			TraceView.VirtualMode = true;
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

		private void EngageDebugger()
		{
			try
			{
				Core.Tracer.Enabled = true;
				TraceView.Columns[0].Text = Core.Tracer.Header;
			}
			catch (NotImplementedException)
			{
				TracerBox.Enabled = false;
			}
		}

		private void DisengageDebugger()
		{
			SaveConfigSettings();
		}

		private void UpdateTraceLog()
		{
			if (TracerBox.Enabled)
			{
				var instructions = Core.Tracer.TakeContents().Split('\n');
				if (!string.IsNullOrWhiteSpace(instructions[0]))
				{
					_instructions.AddRange(instructions.Where(str => !string.IsNullOrEmpty(str)));
				}

				if (_instructions.Count >= Global.Config.TraceLoggerMaxLines)
				{
					_instructions.RemoveRange(0, _instructions.Count - Global.Config.TraceLoggerMaxLines);
				}

				TraceView.ItemCount = _instructions.Count;
			}
		}

		private void TraceView_QueryItemText(int index, int column, out string text)
		{
			text = index < _instructions.Count ? _instructions[index] : string.Empty;
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
