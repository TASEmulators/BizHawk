using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LuaInterface;

namespace BizHawk.MultiClient.tools
{
	public partial class LuaWinform : Form
	{
		public List<LuaFunction> Events = new List<LuaFunction>();

		public LuaWinform()
		{
			InitializeComponent();
			Closing += (o, e) => CloseThis();
		}

		private void LuaWinform_Load(object sender, EventArgs e)
		{

		}

		public void CloseThis()
		{
			Global.MainForm.LuaConsole1.LuaImp.WindowClosed(Handle);
		}
	}
}
