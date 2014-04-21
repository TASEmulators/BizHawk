using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Atari.Atari2600;

namespace BizHawk.Client.EmuHawk
{
	public partial class Atari2600Debugger : Form, IToolForm
	{
		// TODO:
		// Take control of mainform
		// Consider how to handle trace logger (the two will compete with each other with the TakeContents() method)
		// Step Into
		// Step Out
		// Step
		// Advance 1 scanline?
		// Settable registers, also implement in lua
		// Breakpoints
		private Atari2600 _core = Global.Emulator as Atari2600;
		private readonly List<string> _instructions = new List<string>();

		private int _defaultWidth;
		private int _defaultHeight;

		//the opsize table is used to quickly grab the instruction sizes (in bytes)
		private readonly byte[] opsize = new byte[]
		{
		/*0x00*/	1,2,0,0,0,2,2,0,1,2,1,0,0,3,3,0,
		/*0x10*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x20*/	3,2,0,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0x30*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x40*/	1,2,0,0,0,2,2,0,1,2,1,0,3,3,3,0,
		/*0x50*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x60*/	1,2,0,0,0,2,2,0,1,2,1,0,3,3,3,0,
		/*0x70*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0x80*/	0,2,0,0,2,2,2,0,1,0,1,0,3,3,3,0,
		/*0x90*/	2,2,0,0,2,2,2,0,1,3,1,0,0,3,0,0,
		/*0xA0*/	2,2,2,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0xB0*/	2,2,0,0,2,2,2,0,1,3,1,0,3,3,3,0,
		/*0xC0*/	2,2,0,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0xD0*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0,
		/*0xE0*/	2,2,0,0,2,2,2,0,1,2,1,0,3,3,3,0,
		/*0xF0*/	2,2,0,0,0,2,2,0,1,3,0,0,0,3,3,0
		};


		/*the optype table is a quick way to grab the addressing mode for any 6502 opcode
		//
		//  0 = Implied\Accumulator\Immediate\Branch\NULL
		//  1 = (Indirect,X)
		//  2 = Zero Page
		//  3 = Absolute
		//  4 = (Indirect),Y
		//  5 = Zero Page,X
		//  6 = Absolute,Y
		//  7 = Absolute,X
		//  8 = Zero Page,Y
		*/
		private readonly byte[] optype = new byte[]
		{
		/*0x00*/	0,1,0,0,0,2,2,0,0,0,0,0,0,3,3,0,
		/*0x10*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x20*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0x30*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x40*/	0,1,0,0,0,2,2,0,0,0,0,0,0,3,3,0,
		/*0x50*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x60*/	0,1,0,0,0,2,2,0,0,0,0,0,3,3,3,0,
		/*0x70*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0x80*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0x90*/	0,4,0,0,5,5,8,0,0,6,0,0,0,7,0,0,
		/*0xA0*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0xB0*/	0,4,0,0,5,5,8,0,0,6,0,0,7,7,6,0,
		/*0xC0*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0xD0*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0,
		/*0xE0*/	0,1,0,0,2,2,2,0,0,0,0,0,3,3,3,0,
		/*0xF0*/	0,4,0,0,0,5,5,0,0,6,0,0,0,7,7,0
		};

		public Atari2600Debugger()
		{
			InitializeComponent();

			TraceView.QueryItemText += TraceView_QueryItemText;
			TraceView.VirtualMode = true;
			TopMost = Global.Config.Atari2600DebuggerSettings.TopMost;

			Closing += (o, e) => Shutdown();
		}

		private void Atari2600Debugger_Load(object sender, EventArgs e)
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			// TODO: some kind of method like PauseAndRelinquishControl() which will set a flag preventing unpausing by the user, and then a ResumeControl() method that is done on close
			//GlobalWin.MainForm.PauseEmulator();
			Global.CoreComm.Tracer.Enabled = true;

			if (Global.Config.Atari2600DebuggerSettings.UseWindowPosition)
			{
				Location = Global.Config.Atari2600DebuggerSettings.WindowPosition;
			}

			if (Global.Config.Atari2600DebuggerSettings.UseWindowSize)
			{
				Size = Global.Config.Atari2600DebuggerSettings.WindowSize;
			}
		}

		private void Shutdown()
		{
			//TODO: add a Mainform.ResumeControl() call
			Global.CoreComm.Tracer.TakeContents();
			Global.CoreComm.Tracer.Enabled = false;
		}

		public void Restart()
		{
			// TODO
		}

		public bool AskSave()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; } // TODO: think about this
		}

		public void UpdateValues()
		{
			var flags = _core.GetCpuFlagsAndRegisters();
			PCRegisterBox.Text = flags["PC"].ToString();

			SPRegisterBox.Text = flags["S"].ToString();
			SPRegisterHexBox.Text = string.Format("{0:X2}", flags["S"]);
			SPRegisterBinaryBox.Text = ToBinStr(flags["S"]);

			ARegisterBox.Text = flags["A"].ToString();
			ARegisterHexBox.Text = string.Format("{0:X2}", flags["A"]);
			ARegisterBinaryBox.Text = ToBinStr(flags["A"]);

			XRegisterBox.Text = flags["X"].ToString();
			XRegisterHexBox.Text = string.Format("{0:X2}", flags["X"]);
			XRegisterBinaryBox.Text = ToBinStr(flags["X"]);

			YRegisterBox.Text = flags["Y"].ToString();
			YRegisterHexBox.Text = string.Format("{0:X2}", flags["Y"]);
			YRegisterBinaryBox.Text = ToBinStr(flags["Y"]);

			NFlagCheckbox.Checked = flags["Flag N"] == 1;
			VFlagCheckbox.Checked = flags["Flag V"] == 1;
			TFlagCheckbox.Checked = flags["Flag T"] == 1;
			BFlagCheckbox.Checked = flags["Flag B"] == 1;

			DFlagCheckbox.Checked = flags["Flag D"] == 1;
			IFlagCheckbox.Checked = flags["Flag I"] == 1;
			ZFlagCheckbox.Checked = flags["Flag Z"] == 1;
			CFlagCheckbox.Checked = flags["Flag C"] == 1;

			FrameCountBox.Text = _core.Frame.ToString();
			ScanlineBox.Text = _core.CurrentScanLine.ToString();
			VSyncChexkbox.Checked = _core.IsVsync;
			VBlankCheckbox.Checked = _core.IsVBlank;
			UpdateTraceLog();
		}

		private void UpdateTraceLog()
		{
			var instructions = Global.CoreComm.Tracer.TakeContents().Split('\n');
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

		private string ToBinStr(int val)
		{
			return Convert.ToString((uint)val, 2).PadLeft(8, '0');
		}

		private void TraceView_QueryItemText(int index, int column, out string text)
		{
			text = index < _instructions.Count ? _instructions[index] : string.Empty;
		}

		#region Events

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void StepBtn_Click(object sender, EventArgs e)
		{
			_core.CycleAdvance();
			UpdateValues();

		}

		private void ScanlineAdvanceBtn_Click(object sender, EventArgs e)
		{
			_core.ScanlineAdvance();
			UpdateValues();
		}

		private void FrameAdvButton_Click(object sender, EventArgs e)
		{
			_core.FrameAdvance(true, true);
			UpdateValues();
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.RamSearchSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#endregion

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.Atari2600DebuggerAutoload;
			SaveWindowPositionMenuItem.Checked = Global.Config.Atari2600DebuggerSettings.SaveWindowPosition;
			TopmostMenuItem.Checked = Global.Config.Atari2600DebuggerSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.Atari2600DebuggerSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600DebuggerAutoload ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600DebuggerSettings.SaveWindowPosition ^= true;
		}

		private void TopmostMenuItem_Click(object sender, EventArgs e)
		{
			TopMost = Global.Config.Atari2600DebuggerSettings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600DebuggerSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			Global.Config.Atari2600DebuggerSettings = new ToolDialogSettings();
			TopMost = Global.Config.Atari2600DebuggerSettings.TopMost;
			RefreshFloatingWindowControl();
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}
	}
}
