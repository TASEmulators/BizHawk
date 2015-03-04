using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//using BizHawk.Client.Common;
//using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class HistoryBox : UserControl
	{
		public TAStudio Tastudio { get; set; }
		
		public HistoryBox()
		{
			InitializeComponent();
		}

		public void UpdateValues()
		{
			
		}
	}
}
