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
		private IDisassemblable Disassembler;

		public IDictionary<Type, object> EmulatorServices { private get; set; }

		public GenericDebugger()
		{
			InitializeComponent();
			TopMost = Global.Config.GenericDebuggerSettings.TopMost;
			Closing += (o, e) => DisengageDebugger();

			DisassemblerView.QueryItemText += DisassemblerView_QueryItemText;
			DisassemblerView.QueryItemBkColor += DisassemblerView_QueryItemBkColor;
			DisassemblerView.VirtualMode = true;
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

			Disassembler = Global.Emulator.AsDissassembler();

			EngageDebugger();
		}

		private void DisassemblerView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
		}

		private void DisassemblerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			
		}

		public void DisableRegisterBox()
		{
			RegistersGroupBox.Enabled = false;
		}

		private void OnCpuDropDownIndexChanged(object sender, EventArgs e)
		{
			Disassembler.Cpu = (sender as ComboBox).SelectedItem.ToString();
		}

		private void EngageDebugger()
		{
			if (Core.CanDisassemble())
			{
				try
				{
					// Quick way to check if setting is implemented
					Disassembler.Cpu = Disassembler.Cpu;

					if (Disassembler.AvailableCpus.Count() > 1)
					{
						var c = new ComboBox
						{
							Location = new Point(35, 17),
							DropDownStyle = ComboBoxStyle.DropDownList
						};

						c.Items.AddRange(Core.AsDissassembler().AvailableCpus.ToArray());
						c.SelectedItem = Core.AsDissassembler().Cpu;
						c.SelectedIndexChanged += OnCpuDropDownIndexChanged;

						DisassemblerBox.Controls.Add(c);
					}
					else
					{
						DisassemblerBox.Controls.Add(new Label
						{
							Location = new Point(30, 23),
							Text = Disassembler.Cpu
						});
					}
				}
				catch (NotImplementedException)
				{
					DisassemblerBox.Controls.Add(new Label
					{
						Location = new Point(30, 23),
						Text = Disassembler.Cpu
					});
				}
			}
			else
			{
				DisassemblerBox.Enabled = false;
				DisassemblerBox.Controls.Add(new Label
				{
					Location = new Point(35, 23),
					Text = "Unknown"
				});
			}

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
