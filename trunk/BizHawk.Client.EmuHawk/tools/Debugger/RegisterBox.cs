using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// This control shows Cpu Flags and Registers from the currently running emulator core
	/// </summary>
	public partial class RegisterBox : UserControl
	{
		public RegisterBox()
		{
			InitializeComponent();
		}

		private void RegisterBox_Load(object sender, EventArgs e)
		{

		}
	}
}
