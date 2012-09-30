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
			//TODO
		}

		private void TraceView_QueryItemBkColor(int index, int column, ref Color color)
		{
			//TODO
		}

		void TraceView_QueryItemText(int index, int column, out string text)
		{
			text = Instructions[index];
		}

		private void TraceLogger_Load(object sender, EventArgs e)
		{
			ClearList();
			LoggingEnabled.Checked = true;
			//Global.CoreInputComm.CpuTraceEnable = true;
		}

		public void UpdateValues()
		{
			DoInstructions();
			TraceView.Refresh();
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
			TraceView.Clear();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void LoggingEnabled_CheckedChanged(object sender, EventArgs e)
		{
			Global.CoreInputComm.CpuTraceEnable = LoggingEnabled.Checked;
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			ClearList();
		}

		private void DoInstructions()
		{
			//using (Global.CoreInputComm.CpuTraceStream)
			//{
				Instructions.Add("FART 0x15");
				TraceView.ItemCount = Instructions.Count;
			//}
		}
	}
}
