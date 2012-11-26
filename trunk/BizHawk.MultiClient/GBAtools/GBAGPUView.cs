using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.GBAtools
{
	public partial class GBAGPUView : Form
	{
		public GBAGPUView()
		{
			InitializeComponent();
		}

		public void Restart()
		{
			if (Global.Emulator is Emulation.Consoles.Nintendo.GBA.GBA)
			{
			}
			else
			{
				if (Visible)
					Close();
			}
		}

		/// <summary>belongs in ToolsBefore</summary>
		public void UpdateValues()
		{
		}
	}
}
