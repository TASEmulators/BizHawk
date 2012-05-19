using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using BizHawk.Core;
using BizHawk.MultiClient;

namespace BizHawk.Emulation.Consoles.Gameboy
{
	public partial class Debugger : Form, Gameboy.IDebuggerAPI
	{
		Gameboy gb;
		public Debugger()
		{
			InitializeComponent();
		}

		public void LoadCore(Gameboy gb)
		{
			this.gb = gb;
			gb.DebuggerAPI = this;
			Refresh();
		}

		void Gameboy.IDebuggerAPI.DoEvents()
		{
			System.Windows.Forms.Application.DoEvents();
		}

		private void viewDisassembly_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.Clear(SystemColors.Control);
			StringBuilder sb = new StringBuilder();
			ushort addr = (ushort)vScrollBar1.Value;
			for (int i = 0; i < 16; i++)
			{
				ushort size;
				string str = BizHawk.Emulation.CPUs.Z80GB.Disassembler.DAsm(addr, gb.Cpu.ReadMemory, out size);
				addr += size;
				sb.AppendLine(str);
			}
			using (Font font = new Font("Courier New", 8))
			{
				e.Graphics.DrawString(sb.ToString(), font, Brushes.Black, 0, 0);
			}
		}

		public override void Refresh()
		{
			txtRegAF.Text = string.Format("{0:X4}", gb.Cpu.RegisterAF);
			txtRegDE.Text = string.Format("{0:X4}", gb.Cpu.RegisterDE);
			txtRegPC.Text = string.Format("{0:X4}", gb.Cpu.RegisterPC);
			txtRegBC.Text = string.Format("{0:X4}", gb.Cpu.RegisterBC);
			txtRegHL.Text = string.Format("{0:X4}", gb.Cpu.RegisterHL);
			txtRegSP.Text = string.Format("{0:X4}", gb.Cpu.RegisterSP);
			checkFlag_Z.Checked = gb.Cpu.FlagZ;
			checkFlag_N.Checked = gb.Cpu.FlagN;
			checkFlag_H.Checked = gb.Cpu.FlagH;
			checkFlag_C.Checked = gb.Cpu.FlagC;
			txtFrame.Text = gb.Registers.Timing.frame.ToString();
			txtLine.Text = gb.Registers.Timing.line.ToString();
			txtDot.Text = gb.Registers.Timing.dot.ToString();
			base.Refresh();
		}

		void DoStepInto()
		{
			DoBreak();
			gb.SingleStepInto();
			vScrollBar1.Value = gb.Cpu.RegisterPC;
			Refresh();
		}

		bool Running = false;
		void DoRun()
		{
			Global.MainForm.UnpauseEmulator();
			//Running = true;
			//gb.RunForever();
			//Running = false;
		}

		void DoBreak()
		{
			Global.MainForm.PauseEmulator(); //adelikat: This is probably "rounding" the break to the nearest frame, but without it, break fails
			gb.DebugBreak = true;
		}

		private void btnStepInto_Click(object sender, EventArgs e)
		{
			DoStepInto();
		}

		bool TryParse16(string str, out ushort val)
		{
			return ushort.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out val);
		}

		private void reg16_Validating(object sender, CancelEventArgs e)
		{
			var tb = (TextBox)sender;
			ushort val = 0;
			TryParse16(tb.Text, out val);
			if (sender == txtRegAF) gb.Cpu.RegisterAF = val;
			if (sender == txtRegDE) gb.Cpu.RegisterDE = val;
			if (sender == txtRegPC) gb.Cpu.RegisterPC = val;
			if (sender == txtRegBC) gb.Cpu.RegisterBC = val;
			if (sender == txtRegHL) gb.Cpu.RegisterHL = val;
			if (sender == txtRegSP) gb.Cpu.RegisterSP = val;
			Refresh();
		}

		private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
		{
			Refresh();
		}

		private void btnSeekPC_Click(object sender, EventArgs e)
		{
			vScrollBar1.Value = gb.Cpu.RegisterPC;
			Refresh();
		}

		private void btnSeekUser_Click(object sender, EventArgs e)
		{
			ushort val;
			if (TryParse16(txtSeekUser.Text, out val))
			{
				vScrollBar1.Value = val;
				Refresh();
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.F11:
					DoStepInto();
					break;
				case Keys.F5:
					DoRun();
					break;
				case Keys.Escape:
					DoBreak();
					break;
				default:
					return false;
			}
			return true;
		}

		private void menuContextBreakpoints_Opening(object sender, CancelEventArgs e)
		{
			miBreakpointDelete.Enabled = false;
		}

		private void viewTiles0x8000_Paint(object sender, PaintEventArgs e)
		{
			using (Bitmap bmp = new Bitmap(160, 144, e.Graphics))
			{
				var linebuf = new byte[128];
				for (int y = 0; y < 144; y++)
				{
					gb.RenderTileLine(y, linebuf, 0x8000);
					for (int x = 0; x < 128; x++)
					{
						int gray = linebuf[x] << 6;
						bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
					}
				}
				e.Graphics.DrawImageUnscaled(bmp, 0, 0);
			}
		}

		private void viewTiles0x9000_Paint(object sender, PaintEventArgs e)
		{
			using (Bitmap bmp = new Bitmap(160, 144, e.Graphics))
			{
				var linebuf = new byte[128];
				for (int y = 0; y < 144; y++)
				{
					gb.RenderTileLine(y, linebuf, 0x8800);
					for (int x = 0; x < 128; x++)
					{
						int gray = linebuf[x] << 6;
						bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
					}
				}
				e.Graphics.DrawImageUnscaled(bmp, 0, 0);
			}
		}


		private void viewBG_Paint(object sender, PaintEventArgs e)
		{
			using (Bitmap bmp = new Bitmap(160, 144, e.Graphics))
			{
				var linebuf = new byte[160];
				for (int y = 0; y < 144; y++)
				{
					if(checkViewBg.Checked) gb.RenderBGLine(y, linebuf, false);
					if (checkViewObj.Checked) gb.RenderOBJLine(y, linebuf, !checkViewObjNoLimit.Checked);
					for (int x = 0; x < 160; x++)
					{
						int gray = linebuf[x]<<6;
						bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
					}
				}
				e.Graphics.DrawImageUnscaled(bmp, 0, 0);
			}
		}

		private void timerRunUpdate_Tick(object sender, EventArgs e)
		{
			if(Running)
				Refresh();
		}

		private void btnRun_Click(object sender, EventArgs e)
		{
			DoRun();
		}

		private void btnBreak_Click(object sender, EventArgs e)
		{
			DoBreak();
		}

		private void checkFlag_Z_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void cpuflag_checkChanged(object sender, EventArgs e)
		{
			var cb = (CheckBox)sender;
			if (sender == checkFlag_Z) gb.Cpu.FlagZ = cb.Checked;
			if (sender == checkFlag_N) gb.Cpu.FlagN = cb.Checked;
			if (sender == checkFlag_H) gb.Cpu.FlagH = cb.Checked;
			if (sender == checkFlag_C) gb.Cpu.FlagC = cb.Checked;
			Refresh();
		}

		static char Remap(byte val)
		{
			if (val < ' ') return '.';
			else if (val >= 0x80) return '.';
			else return (char)val;
		}

		private void panelMemory_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.Clear(SystemColors.Control);
			StringBuilder sb = new StringBuilder();
			ushort addr = (ushort)(panelMemory.Scrollbar.Value * 16);
			for (int i = 0; i < 16; i++)
			{
				sb.AppendFormat("{0}:{1:X4}  ", gb.DescribeParagraph(addr),addr);
				for (int x = 0; x < 16; x++)
					sb.AppendFormat("{0:X2} ", gb.ReadMemory((ushort)(addr+x)));
				sb.AppendFormat("| ");
				for (int x = 0; x < 16; x++)
					sb.Append(Remap(gb.ReadMemory((ushort)(addr + x))));
				sb.AppendLine();
				addr += 16;
			}
			using (Font font = new Font("Courier New", 8))
			{
				e.Graphics.DrawString(sb.ToString(), font, Brushes.Black, 0, 0);
			}
		}

		private void panelMemory_Scroll(object sender, ScrollEventArgs e)
		{
			Refresh();
		}

		private void checkViewBg_CheckedChanged(object sender, EventArgs e)
		{
			Refresh();
		}

		private void checkViewObj_CheckedChanged(object sender, EventArgs e)
		{
			Refresh();
		}

		private void checkViewObjNoLimit_CheckedChanged(object sender, EventArgs e)
		{
			Refresh();
		}

		private void viewBG_KeyUp(object sender, KeyEventArgs e)
		{
			UpdateKeys();
		}

		private void viewBG_KeyPress(object sender, KeyPressEventArgs e)
		{
			UpdateKeys();
		}

		private void viewBG_KeyDown(object sender, KeyEventArgs e)
		{
			UpdateKeys();
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (viewBG.Focused) return true;
			else return base.ProcessDialogKey(keyData);
		}

		void UpdateKeys()
		{
			gb.Registers.Input.up = Keyboard.IsKeyDown(Keys.Up);
			gb.Registers.Input.down = Keyboard.IsKeyDown(Keys.Down);
			gb.Registers.Input.left = Keyboard.IsKeyDown(Keys.Left);
			gb.Registers.Input.right = Keyboard.IsKeyDown(Keys.Right);
			gb.Registers.Input.a = Keyboard.IsKeyDown(Keys.X);
			gb.Registers.Input.b = Keyboard.IsKeyDown(Keys.Z);
			gb.Registers.Input.select = Keyboard.IsKeyDown(Keys.E);
			gb.Registers.Input.right = Keyboard.IsKeyDown(Keys.R);
		}

		private void viewBG_Enter(object sender, EventArgs e)
		{
			lblInputActive.ForeColor = Color.Red;
			UpdateKeys();
		}

		private void viewBG_Leave(object sender, EventArgs e)
		{
			lblInputActive.ForeColor = SystemColors.ControlText;
			gb.Registers.Input.up = false;
			gb.Registers.Input.down = false;
			gb.Registers.Input.left = false;
			gb.Registers.Input.right = false;
			gb.Registers.Input.a = false;
			gb.Registers.Input.b = false;
			gb.Registers.Input.select = false;
			gb.Registers.Input.right = false;
		}

		static class Keyboard
		{
			[Flags]
			private enum KeyStates
			{
				None = 0,
				Down = 1,
				Toggled = 2
			}

			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			private static extern short GetKeyState(int keyCode);

			private static KeyStates GetKeyState(Keys key)
			{
				KeyStates state = KeyStates.None;

				short retVal = GetKeyState((int)key);

				//If the high-order bit is 1, the key is down
				//otherwise, it is up.
				if ((retVal & 0x8000) == 0x8000)
					state |= KeyStates.Down;

				//If the low-order bit is 1, the key is toggled.
				if ((retVal & 1) == 1)
					state |= KeyStates.Toggled;

				return state;
			}

			public static bool IsKeyDown(Keys key)
			{
				return KeyStates.Down == (GetKeyState(key) & KeyStates.Down);
			}

			public static bool IsKeyToggled(Keys key)
			{
				return KeyStates.Toggled == (GetKeyState(key) & KeyStates.Toggled);
			}
		}

		private void Debugger_Load(object sender, EventArgs e)
		{

		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadGBDebugger ^= true; 
		}

		private void settingsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.AutoloadGBDebugger;
		}

		public void UpdateValues()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;
			Refresh();
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed) return;

			if (Global.Emulator is Gameboy)
			{
				LoadCore(Global.Emulator as Gameboy);
			}
			else
			{
				this.Close();
			}
		}
	}
}
