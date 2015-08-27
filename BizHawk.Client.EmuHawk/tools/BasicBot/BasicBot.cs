using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BasicBot : Form , IToolFormAutoConfig
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IStatable StatableCore { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[ConfigPersist]
		public BasicBotSettings Settings { get; set; }

		public class BasicBotSettings
		{

		}

		public BasicBot()
		{
			InitializeComponent();
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{

		}

		#region IToolForm Implementation

		public bool UpdateBefore { get { return true; } }

		public void UpdateValues()
		{

		}

		public void FastUpdate()
		{

		}

		public void Restart()
		{

		}

		public bool AskSaveChanges()
		{
			return true; // TODO
		}

		#endregion

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
