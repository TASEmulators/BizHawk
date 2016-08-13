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
	[ToolAttributes(released: false, supportedSystems: null)]
	public partial class AutoHawk : Form, IToolFormAutoConfig
	{
		public AutoHawk()
		{
			InitializeComponent();
		}

		private void AutoHawk_Load(object sender, EventArgs e)
		{

		}

		[RequiredService]
		public IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		public IStatable StatableCore { get; set; }

		[ConfigPersist]
		public ConfigVariables Config { get; set; }

		public class ConfigVariables
		{
			// anything that needs to be saved in config.ini should go here
		}

		#region IToolForm Implementation

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			// TODO: per frame stuff goes here
		}

		public void FastUpdate()
		{
			// TODO: when the user is turboing this will be called, slow things like updating graphics should be avoided, but critical operations must still be done here
		}

		public void Restart()
		{
			// When the user changes to a new ROM, closes a ROM, starts a movie, etc, this will be called
		}

		public bool UpdateBefore { get { return true; } }

		public bool AskSaveChanges()
		{
			return true;
		}

		#endregion

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
