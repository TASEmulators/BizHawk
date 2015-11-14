using System;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.Globalization;

namespace BizHawk.Client.EmuHawk
{
	public partial class GameShark : Form, IToolForm, IToolFormAutoConfig
	{
		[ToolAttributes(released: true, supportedSystems: new[] { "GB" })]
		//We are using Memory Domains, so we NEED this.
		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }
		public GameShark()
		{
			InitializeComponent();
		}

		public bool UpdateBefore
		{
			get
			{
				return true;
			}
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public void FastUpdate()
		{
			
		}

		public void Restart()
		{

		}

		public void UpdateValues()
		{
			
		}

		private void btnGo_Click(object sender, EventArgs e)
		{
			//This line ONLY applies to GB/GBC codes.
			if (txtCheat.Text.Length != 8)
			{
				MessageBox.Show("All GameShark and CodeBreaker cheats need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//Sample Input for GB/GBC:
			//010FF6C1
			//Becomes:
			//Address C1F6
			//Value 0F
			string parseString = null;
			string RAMAddress = null;
			string RAMValue = null;
			parseString = txtCheat.Text.Remove(0, 2);
			//Now we need to break it down a little more.
			RAMValue = parseString.Remove(2, 4);
			parseString = parseString.Remove(0, 2);
			//The issue is Endian...  Time to get ultra clever.  And Regret it.
			//First Half
			RAMAddress = parseString.Remove(0, 2);
			RAMAddress = RAMAddress + parseString.Remove(2, 2);
			//We now have our values.
			//This part, is annoying...
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, false);
				//Take Watch, Add our Value we want, and it should be active when addded?
                Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				//Clear old Inputs
				txtCheat.Clear();
				txtDescription.Clear();
			}
			catch (Exception ex)
			{
				MessageBox.Show("An Error occured:" + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			//Clear old Inputs
			txtCheat.Clear();
			txtDescription.Clear();
		}
	}
}
