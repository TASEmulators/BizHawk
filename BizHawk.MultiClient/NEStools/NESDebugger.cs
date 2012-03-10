using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESDebugger : Form
	{
		const int ADDR_MAX = 0xFFFF;
		const int DISASM_LINE_COUNT = 100;

		int defaultWidth;       //For saving the default size of the dialog, so the user can restore if desired
		int defaultHeight;
		NES Nes;

		int pc;
		int addr;
		List<string> lines = new List<string>();

		public NESDebugger()
		{
			InitializeComponent();
			DebugView.QueryItemText += new QueryItemTextHandler(DebugView_QueryItemText);
			DebugView.QueryItemBkColor += new QueryItemBkColorHandler(DebugView_QueryItemBkColor);
			DebugView.VirtualMode = true;
			DebugView.ItemCount = ADDR_MAX + 1;
			Activated += (o, e) => UpdateValues();
			Closing += (o, e) => SaveConfigSettings();
		}

		public void Restart()
		{
			if (!(Global.Emulator is NES)) this.Close();
			if (!this.IsHandleCreated || this.IsDisposed) return;
			Nes = Global.Emulator as NES;
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			addr = pc = Nes.cpu.PC;
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
				string line = Nes.cpu.Disassemble((ushort)a, out advance);
				lines.Add(line);
				a += advance;
				if (a > ADDR_MAX) break;
			}
		}

		private void NESDebugger_Load(object sender, EventArgs e)
		{
			LoadConfigSettings();
			Nes = Global.Emulator as NES;
		}

		private void LoadConfigSettings()
		{
			defaultWidth = this.Size.Width;     //Save these first so that the user can restore to its original size
			defaultHeight = this.Size.Height;

			if (Global.Config.NESDebuggerSaveWindowPosition && Global.Config.NESDebuggerWndx >= 0 && Global.Config.NESDebuggerWndy >= 0)
				this.Location = new Point(Global.Config.NESDebuggerWndx, Global.Config.NESDebuggerWndy);

			if (Global.Config.NESDebuggerWidth >= 0 && Global.Config.NESDebuggerHeight >= 0)
			{
				this.Size = new System.Drawing.Size(Global.Config.NESDebuggerWidth, Global.Config.NESDebuggerHeight);
			}
		}

		public void SaveConfigSettings()
		{
			Global.Config.NESDebuggerWndx = this.Location.X;
			Global.Config.NESDebuggerWndy = this.Location.Y;
			Global.Config.NESDebuggerWidth = this.Right - this.Left;
			Global.Config.NESDebuggerHeight = this.Bottom - this.Top;
		}

		private void DebugView_QueryItemBkColor(int index, int column, ref Color color)
		{

		}

		void DebugView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0)
			{
				text = String.Format("{0:X4}", index);
			}
			else if (column == 1)
			{
				if (addr <= index && index < addr+lines.Count)
					text = lines[index-addr];
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
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
			this.Size = new System.Drawing.Size(defaultWidth, defaultHeight);
		}
	}
}
