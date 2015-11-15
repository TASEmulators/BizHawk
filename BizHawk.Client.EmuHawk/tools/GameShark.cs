using System;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.Globalization;

namespace BizHawk.Client.EmuHawk
{
	[ToolAttributes(released: true, supportedSystems: new[] { "GB", "N64" })]
	public partial class GameShark : Form, IToolForm, IToolFormAutoConfig
	{
		//We are using Memory Domains, so we NEED this.
		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }
		[RequiredService]
		private IEmulator Emulator { get; set; }
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
			string parseString = null;
			string RAMAddress = null;
			string RAMValue = null;
			//What System are we running?
			switch (Emulator.SystemId)
			{
				case "GB":
					//This Check ONLY applies to GB/GBC codes.
					if (txtCheat.Text.Length != 8)
					{
						MessageBox.Show("All GameShark cheats need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//Sample Input for GB/GBC:
					//010FF6C1
					//Becomes:
					//Address C1F6
					//Value 0F

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
					//Someone broke the world?
					catch (Exception ex)
					{
						MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					break;
				case "N64":
					//N64 Cheats are going be more, limited/restricted.  I am NOT going to support the non-8XXXXXXX YYYY style of codes.  That's too much work/hassle.
					//TODO: Find someone to impliment the Non-8XXXXXXX YYYY style of codes  Or Ignore them all together?
					//I think they can in theory work with straight conversion as written?
					if (txtCheat.Text.Contains(" ") == false)
					{
						MessageBox.Show("All N64 GameShark Cheats need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//Big Endian is USED constantly here.  The question is, how do we determine if it's one or two bytes?
					if (txtCheat.Text.StartsWith("8") == false)
					{
						MessageBox.Show("All N64 GameShark Cheats need to start with the number 8.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					if (txtCheat.Text.Length != 13)
					{
						MessageBox.Show("All N64 GameShark Cheats need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//Now to get clever.
					//Sample Input for N64:
					//8133B21E 08FF
					//Becomes:
					//Address 33B21E
					//Value 08FF

					//Note, 8XXXXXXX 00YY
					//Is Byte, not Word
					//Remove the 8X Octect
					parseString = txtCheat.Text.Remove(0, 2);
					//Get RAM Address
					RAMAddress = parseString.Remove(6, 5);
					//Get RAM Value
					RAMValue = parseString.Remove(0, 7);
					//I need to determine if this is a Byte or Word.
					//TODO: Make this suck less?  I feel it's sloppy as is and it may be false-postive.
					Boolean isByte = false;
					string firstTwo = null;
					firstTwo = RAMValue.Remove(2, 2);
					//MessageBox.Show(firstTwo);
					if (firstTwo == "00")
					{
						isByte = true;
                    }
					try
					{
						//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
						//System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.
						if (isByte == false)
						{
							//We have a Word (Double Byte) sized Value
							var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, true);
							//Take Watch, Add our Value we want, and it should be active when addded?
							Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
						}
						if (isByte == true)
						{
							//We have a Byte sized value
							var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, true);
							//Take Watch, Add our Value we want, and it should be active when addded?
							Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
						}
						//Clear old Inputs
						txtCheat.Clear();
						txtDescription.Clear();
					}
					//Someone broke the world?
					catch (Exception ex)
					{
						MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					break;
				default:
					//This should NEVER happen
					break;
			}
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			//Clear old Inputs
			txtCheat.Clear();
			txtDescription.Clear();
		}

		private void GameShark_Load(object sender, EventArgs e)
		{
			//Welp, let's determine what System we got.
			//NOTE: This is a sloppy Debugger/testing code.  For Bad Development usage.  DO NOT release with that line uncommented
			//MessageBox.Show(Emulator.SystemId);
		}
	}
}
