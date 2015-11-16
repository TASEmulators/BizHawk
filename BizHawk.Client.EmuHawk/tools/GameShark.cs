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
			//We want Upper Case.
			int byteSize = 0;
			txtCheat.Text = txtCheat.Text.ToUpper();
			string testo = txtCheat.Text.Remove(2, 11);
			switch (Emulator.SystemId)
			{
				case "GB":
					//This Check ONLY applies to GB/GBC codes.
					if (txtCheat.Text.Length != 8)
					{
						MessageBox.Show("All GameShark Codes need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//Let's make sure we start with zero.  We have a good length, and a good starting zero, we should be good.  Hopefully.
					switch (testo)
					{
						//Is this 00 or 01?
						case "00":
						case "01":
							//Good.
							break;
						default:
							//No.
							MessageBox.Show("All GameShark Codes for GameBoy need to start with 00 or 01", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
					//These codes, more or less work without Needing much work.
					if (txtCheat.Text.Contains(" ") == false)
					{
						MessageBox.Show("All N64 GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					if (txtCheat.Text.Length != 13)
					{
						MessageBox.Show("All N64 GameShark Codes need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//We need to determine what kind of cheat this is.
					//I need to determine if this is a Byte or Word.
					switch (testo)
					{
						//80 and 81 are the most common, so let's not get all worried.
						case "80":
							//Byte
							byteSize = 8;
                            break;
						case "81":
							//Word
							byteSize = 16;
							break;
						//Case A0 and A1 means "Write to Uncached address.
						case "A0":
							//Byte
							byteSize = 8;
							break;
						case "A1":
							//Word
							byteSize = 16;
							break;
						//Do we support the GameShark Button?  No.  But these cheats, can be toggled.  Which "Counts"
						//<Ocean_Prince> Consequences be damned!
						case "88":
							//Byte
							byteSize = 8;
							break;
						case "89":
							//Word
							byteSize = 16;
							break;
						//These are compare Address X to Value Y, then apply Value B to Address A
						//This is not supported, yet
						//TODO: When BizHawk supports a compare RAM Address's value is true then apply a value to another address, make it a thing.
						case "D0":
							//Byte
						case "D1":
							//Word
                        case "D2":
							//Byte
						case "D3":
							//Word
							MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						//These codes are for Disabling the Expansion Pak.  that's a bad thing?  Assuming bad codes, until told otherwise.
						case "EE":
						case "DD":
						case "CC":
							MessageBox.Show("The code you entered is for Disabling the Expansion Pak.  This is not allowed by this tool.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						//Enable Code
						//Not Necessary?  Think so?
						case "DE":
						//Single Write ON-Boot code.
						//Not Necessary?  Think so?
						case "F0":
						case "F1":
						case "2A":
							MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						//TODO: Make Patch Code (5000XXYY) work.
						case "50":
							//Word?
							MessageBox.Show("The code you entered is not supported by this tool.  Please Submit the Game's Name, Cheat/Code and Purpose to the BizHawk forums.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						//I hope this isn't a thing.
						default:
							MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							//Leave this Method, before someone gets hurt.
							return;
							//break;
					}
					//Big Endian is USED constantly here.  The question is, how do we determine if it's one or two bytes?					
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

					try
					{
						//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
						//System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.
						if (byteSize == 8)
						{
							//We have a Word (Double Byte) sized Value
							var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, true);
							//Take Watch, Add our Value we want, and it should be active when addded?
							Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
						}
						if (byteSize == 16)
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
				case "PSX":
					//Not yet.
					//These codes, more or less work without Needing much work.
					if (txtCheat.Text.Contains(" ") == false)
					{
						MessageBox.Show("All PSX GameShark Cheats need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					if (txtCheat.Text.Length != 13)
					{
						MessageBox.Show("All PSX GameShark Cheats need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//We need to determine what kind of cheat this is.
					//I need to determine if this is a Byte or Word.
					switch (testo)
					{
						//30 80 Cheats mean, "Write, don't care otherwise."
						case "30":
							byteSize = 8;
							break;
						case "80":
							byteSize = 16;
                            break;
						//When value hits YYYY, make the next cheat go off
						case "E0":
						//E0 byteSize = 8;
						case "D0":
							//D0 byteSize = 16;
							MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						//Something wrong with their input.
						default:
							MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
							//Leave this Method, before someone gets hurt.
							return;
					}

					//MainRAM
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
