using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MacroInputTool : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		public MacroInputTool()
		{
			InitializeComponent();
		}


		private void MacroInputTool_Load(object sender, EventArgs e)
		{

		}

		public void Restart()
		{

		}

		public void UpdateValues()
		{
			
		}

		public void FastUpdate()
		{

		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return true; } // TODO: think about this
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
