using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class VirtualPadC64Keyboard : UserControl , IVirtualPad
	{
		public VirtualPadC64Keyboard()
		{
			InitializeComponent();
		}

		private void VirtualPadC64Keyboard_Load(object sender, EventArgs e)
		{
			
		}

		public void Clear()
		{
			//TODO
		}

		public string GetMnemonic()
		{
			return ""; //TODO
		}

		public void SetButtons(string buttons)
		{
			//TODO
		}
	}
}
