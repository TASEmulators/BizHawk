using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESDebugger : Form
	{
		private const int ADDR_MAX = 0xFFFF;
		private const int DISASM_LINE_COUNT = 100;
		private int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
		private int defaultHeight;
		private NES _nes;
		private int pc;
		private int addr;
		private readonly List<DisasmOp> lines = new List<DisasmOp>();

		private struct DisasmOp
		{
			public readonly int size;
			public readonly string mnemonic;
			public DisasmOp(int s, string m) { size = s; mnemonic = m; }
		}
		

		public NESDebugger()
		{
			InitializeComponent();
			DebugView.QueryItemText += DebugView_QueryItemText;
			DebugView.QueryItemBkColor += DebugView_QueryItemBkColor;
			DebugView.VirtualMode = true;
			DebugView.ItemCount = ADDR_MAX + 1;
			Activated += (o, e) => UpdateValues();
			Closing += (o, e) => SaveConfigSettings();
		}

		public void Restart()
		{
			if (!(Global.Emulator is NES)) Close();
			if (!IsHandleCreated || IsDisposed) return;
			_nes = Global.Emulator as NES;
		}

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed) return;

			addr = pc = _nes.cpu.PC;
			UpdateDebugView();
		}

		private void UpdateDebugView()
		{
			DebugView.BlazingFast = true;
			Disasm(DISASM_LINE_COUNT);
			DebugView.ensureVisible(0xFFFF);
			DebugView.ensureVisible(pc);
			DebugView.Refresh();
			DebugView.BlazingFast = false;
		}

		private void Disasm(int line_count)
		{
			lines.Clear();
			int a = addr;
			for (int i = 0; i < line_count; ++i)
			{
				int advance;
				string line = _nes.cpu.Disassemble((ushort)a, out advance);
				lines.Add(new DisasmOp(advance, line));
				a += advance;
				if (a > ADDR_MAX) break;
			}
		}

		private void NESDebugger_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			_nes = Global.Emulator as NES;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = Size.Height;

			if (Global.Config.NESDebuggerSaveWindowPosition && Global.Config.NESDebuggerWndx >= 0 && Global.Config.NESDebuggerWndy >= 0)
				Location = new Point(Global.Config.NESDebuggerWndx, Global.Config.NESDebuggerWndy);

			if (Global.Config.NESDebuggerWidth >= 0 && Global.Config.NESDebuggerHeight >= 0)
			{
				Size = new Size(Global.Config.NESDebuggerWidth, Global.Config.NESDebuggerHeight);
			}
		}

		public void SaveConfigSettings()
		{
			Global.Config.NESDebuggerWndx = Location.X;
			Global.Config.NESDebuggerWndy = Location.Y;
			Global.Config.NESDebuggerWidth = Right - Left;
			Global.Config.NESDebuggerHeight = Bottom - Top;
		}

		private void DebugView_QueryItemBkColor(int index, int column, ref Color color)
		{

		}

		void DebugView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0)
			{
				if (addr <= index && index < addr+lines.Count)
				{
					int a = addr;
					for (int i = 0; i < index-addr; ++i)
						a += lines[i].size;
					text = String.Format("{0:X4}", a);
				}
			}
			else if (column == 1)
			{
				if (addr <= index && index < addr+lines.Count)
					text = lines[index-addr].mnemonic;
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadNESDebugger ^= true;
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NESDebuggerSaveWindowPosition ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.AutoLoadNESDebugger;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.NESDebuggerSaveWindowPosition;
		}

		private void restoreOriginalSizeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(defaultWidth, defaultHeight);
		}
	}
}
