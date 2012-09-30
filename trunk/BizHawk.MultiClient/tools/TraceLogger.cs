using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class TraceLogger : Form
	{
		List<string> Instructions = new List<string>();

		public TraceLogger()
		{
			InitializeComponent();
			
			TraceView.QueryItemText += new QueryItemTextHandler(TraceView_QueryItemText);
			TraceView.QueryItemBkColor += new QueryItemBkColorHandler(TraceView_QueryItemBkColor);
			TraceView.VirtualMode = true;

			Closing += (o, e) => SaveConfigSettings();
		}

		public void SaveConfigSettings()
		{
			Global.CoreInputComm.Tracer.Enabled = false;
		}

		private void TraceView_QueryItemBkColor(int index, int column, ref Color color)
		{
			//TODO
		}

		private void TraceView_QueryItemText(int index, int column, out string text)
		{
			text = Instructions[index];
		}

		private void TraceLogger_Load(object sender, EventArgs e)
		{
			ClearList();
			LoggingEnabled.Checked = true;
			Global.CoreInputComm.Tracer.Enabled = true;
		}

		public void UpdateValues()
		{
			DoInstructions();
		}

		public void Restart()
		{
			if (!this.IsHandleCreated || this.IsDisposed)
			{
				return;
			}
			else
			{
				ClearList();
			}
		}

		private void ClearList()
		{
			Instructions.Clear();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LoggingEnabled_CheckedChanged(object sender, EventArgs e)
		{
			Global.CoreInputComm.Tracer.Enabled = LoggingEnabled.Checked;
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearList();
		}

		private void DoInstructions()
		{
			string[] instructions = Global.CoreInputComm.Tracer.TakeContents().Split('\n');
			foreach (string s in instructions)
			{
				Instructions.Add(s);
			}
			TraceView.ItemCount = Instructions.Count;
		}

		private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TraceLoggerAutoLoad ^= true;
		}

		private void optionsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadToolStripMenuItem.Checked = Global.Config.TraceLoggerAutoLoad;
		}
	}
}
