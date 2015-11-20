using System;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.Globalization;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{

	//TODO:
	//Add Support/Handling for The Following Systems and Devices:
	//GB/GBC: Pro Action Replay
	//GBA: GameShark, Action Replay (Same?), Code Breaker
	//GameGear: Game Genie, Pro Action Replay
	//NES: Game Genie, Pro Action Replay
	//PSX: Code Breaker, Action Replay, Game Busters (What is that?!)
	//SMS: Pro Action Replay
	//SNES: Game Genie, Pro Action Replay
	//Saturn: Pro Action Replay  (Is it the same as GameShark?  Appears to be so?)



	[ToolAttributes(released: true, supportedSystems: new[] { "GB", "GBA", "GEN", "N64", "PSX", "SAT", "SMS", "SNES" })]
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

		//My Variables
		string parseString = null;
		string RAMAddress = null;
		string RAMValue = null;
		int byteSize = 0;
		string testo = null;
		private void btnGo_Click(object sender, EventArgs e)
		{
			//Reset Variables
			parseString = null;
			RAMAddress = null;
			RAMValue = null;
			byteSize = 0;
			//We want Upper Case.
			txtCheat.Text = txtCheat.Text.ToUpper();
			//What System are we running?
			switch (Emulator.SystemId)
			{
				case "GB":
					GB();
                    break;
				case "GBA":
					GBA();
					break;
				case "GEN":
					GEN();
					break;
				case "N64":
					//This determies what kind of Code we have
					testo = txtCheat.Text.Remove(2, 11);
					N64();
					break;
				case "PSX":
					//This determies what kind of Code we have
					testo = txtCheat.Text.Remove(2, 11);
					PSX();
					break;
				case "SAT":
					//This determies what kind of Code we have
					testo = txtCheat.Text.Remove(2, 11);
					SAT();
                    break;
				case "SMS":
					SMS();
					break;
				case "SNES":
					//Currently only does Action Replay
					SNES();
					break;
				default:
					//This should NEVER happen
					break;
			}
		}
		private void GB()
		{
			//This Check ONLY applies to GB/GBC codes.
			if (txtCheat.Text.Length != 8)
			{
				MessageBox.Show("All GameShark Codes need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			testo = txtCheat.Text.Remove(2, 6);
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
		}
		private void GBA()
		{
			//Nothing, yet
			//Sample of Decryption Code from mGBA
			//const uint32_t GBACheatGameSharkSeeds[4] = { 0x09F4FBBD, 0x9681884A, 0x352027E9, 0xF3DEE5A7 };
			/* void GBACheatDecryptGameShark(uint32_t* op1, uint32_t* op2, const uint32_t* seeds) {
				uint32_t sum = 0xC6EF3720;
				int i;
				for (i = 0; i < 32; ++i)
				{
					*op2 -= ((*op1 << 4) + seeds[2]) ^ (*op1 + sum) ^ ((*op1 >> 5) + seeds[3]);
					*op1 -= ((*op2 << 4) + seeds[0]) ^ (*op2 + sum) ^ ((*op2 >> 5) + seeds[1]);
					sum -= 0x9E3779B9;
				}
			}
			*/

		}
		//This applies to the Genesis Game Genie.
		private readonly Dictionary<char, int> _GENgameGenieTable = new Dictionary<char, int>
		{
			{ 'A', 0 },
			{ 'B', 1 },
			{ 'C', 2 },
			{ 'D', 3 },
			{ 'E', 4 },
			{ 'F', 5 },
			{ 'G', 6 },
			{ 'H', 7 },
			{ 'J', 8 },
			{ 'K', 9 },
			{ 'L', 10 },
			{ 'M', 11 },
			{ 'N', 12 },
			{ 'P', 13 },
			{ 'R', 14 },
			{ 'S', 15 },
			{ 'T', 16 },
			{ 'V', 17 },
			{ 'W', 18 },
			{ 'X', 19 },
			{ 'Y', 20 },
			{ 'Z', 21 },
			{ '0', 22 },
			{ '1', 23 },
			{ '2', 24 },
			{ '3', 25 },
			{ '4', 26 },
			{ '5', 27 },
			{ '6', 28 },
			{ '7', 29 },
			{ '8', 30 },
			{ '9', 31 }
		};
		private void GEN()
		{
			//Game Genie only, for now.
			//This applies to the Game Genie
			if (txtCheat.Text.Length == 9 && txtCheat.Text.Contains("-"))
			{
				if (txtCheat.Text.IndexOf("-") != 5)
				{
					MessageBox.Show("All Genesis Game Genie Codes need to contain a dash after the fourth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				if (txtCheat.Text.Contains("I") == true | txtCheat.Text.Contains("O") == true | txtCheat.Text.Contains("Q") == true | txtCheat.Text.Contains("U") == true)
				{
					MessageBox.Show("All Genesis Game Genie Codes do not use I, O, Q or U.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				//This is taken from the GenGameGenie.CS file.
				string code = txtCheat.Text;
				int val = 0;
				int add = 0;
				string address = null;
				string value = null;
				//Remove the -
				code = code.Remove(4, 1);
				long hexcode = 0;

				// convert code to a long binary string
				foreach (var t in code)
				{
					hexcode <<= 5;
					int y;
					_GENgameGenieTable.TryGetValue(t, out y);
					hexcode |= y;
				}
				long decoded = (hexcode & 0xFF00000000) >> 32;
				decoded |= hexcode & 0x00FF000000;
				decoded |= (hexcode & 0x0000FF0000) << 16;
				decoded |= (hexcode & 0x00000000700) << 5;
				decoded |= (hexcode & 0x000000F800) >> 3;
				decoded |= (hexcode & 0x00000000FF) << 16;

				val = (int)(decoded & 0x000000FFFF);
				add = (int)((decoded & 0xFFFFFF0000) >> 16);
				//Make our Strings get the Hex Values.
				address = add.ToString("X6");
				value = val.ToString("X4");
				//Game Geneie, modifies the "ROM" which is why it says, "MD CART"
				var watch = Watch.GenerateWatch(MemoryDomains["MD CART"], long.Parse(address, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, true);
				//Add Cheat
				Global.CheatList.Add(new Cheat(watch, val));
			}
			//Action Replay?
			if (txtCheat.Text.Contains(":"))
			{
				//We start from Zero.
				if (txtCheat.Text.IndexOf(":") != 6)
				{
					MessageBox.Show("All Genesis Action Replay/Pro Action Replay Codes need to contain a colon after the sixth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				//Problem: I don't know what the Non-FF Style codes are.
				//TODO: Fix that.
				if (txtCheat.Text.StartsWith("FF") == false)
				{
					MessageBox.Show("This Action Replay Code, is not understood by this tool.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				//Now to do some work.
				//Determine Length, to Determine Byte Size

				parseString = txtCheat.Text.Remove(0, 2);
				switch (txtCheat.Text.Length)
				{
					case 9:
						//Sample Code of 1-Byte:
						//FFF761:64
						//Becomes:
						//Address: F761
						//Value: 64
						RAMAddress = parseString.Remove(4, 3);
						RAMValue = parseString.Remove(0, 5);
						byteSize = 1;
                        break;
					case 11:
						//Sample Code of 2-Byte:
						//FFF761:6411
						//Becomes:
						//Address: F761
						//Value: 6411
						RAMAddress = parseString.Remove(4, 5);
						RAMValue = parseString.Remove(0, 5);
						byteSize = 2;
						break;
					default:
						//We could have checked above but here is fine, since it's a quick check due to one of three possibilities.
						MessageBox.Show("All Genesis Action Replay/Pro Action Replay Codes need to be either 9 or 11 characters in length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
				}
				//Try and add.
				try
				{
					//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
					//System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description, Big Endian.
					if (byteSize == 1)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["68K RAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, false);
						//Take Watch, Add our Value we want, and it should be active when addded?
						Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
						//Clear old Inputs
						txtCheat.Clear();
						txtDescription.Clear();
					}
					if (byteSize == 2)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["68K RAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, true);
						//Take Watch, Add our Value we want, and it should be active when addded?
						Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
						//Clear old Inputs
						txtCheat.Clear();
						txtDescription.Clear();
					}

				}
				//Someone broke the world?
				catch (Exception ex)
				{
					MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
		private void N64()
		{
			//These codes, more or less work without Needing much work.
			if (txtCheat.Text.IndexOf(" ") != 8)
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
				case "3C":
				case "FF":
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
			}		
			//Now to get clever.
			//Sample Input for N64:
			//8133B21E 08FF
			//Becomes:
			//Address 33B21E
			//Value 08FF

			//Note, 80XXXXXX 00YY
			//Is Byte, not Word
			//Remove the 80 Octect
			parseString = txtCheat.Text.Remove(0, 2);
			//Get RAM Address
			RAMAddress = parseString.Remove(6, 5);
			//Get RAM Value
			RAMValue = parseString.Remove(0, 7);
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description, Big Endian.
				if (byteSize == 8)
				{
					//We have a Byte sized value
					var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, true);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 16)
				{
					//We have a Word (Double Byte) sized Value
					var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, true);
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
		}
		private void PSX()
		{
			//These codes, more or less work without Needing much work.
			if (txtCheat.Text.IndexOf(" ") != 8)
			{
				MessageBox.Show("All PSX GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				case "E1":
				//E1 byteSize = 8;
				case "E2":
				//E2 byteSize = 8;
				case "D0":
				//D0 byteSize = 16;
				case "D1":
				//D1 byteSize = 16;
				case "D2":
				//D2 byteSize = 16;
				case "D3":
				//D3 byteSize = 16;
				case "D4":
				//D4 byteSize = 16;
				case "D5":
				//D5 byteSize = 16;
				case "D6":
				//D6 byteSize = 16;

				//Increment/Decrement Codes
				case "10":
				//10 byteSize = 16;
				case "11":
				//11 byteSize = 16;
				case "20":
				//20 byteSize = 8
				case "21":
				//21 byteSize = 8
					MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				case "C0":
				case "C1":
				//Slow-Mo
				case "40":
					MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				case "C2":
				case "50":
					//Word?
					MessageBox.Show("The code you entered is not supported by this tool.  Please Submit the Game's Name, Cheat/Code and Purpose to the BizHawk forums.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				//Something wrong with their input.
				default:
					MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					//Leave this Method, before someone gets hurt.
					return;
			}
			//Sample Input for PSX:
			//800D10BA 0009
			//Address: 0D10BA
			//Value:  0009
			//Remove first two octets
			parseString = txtCheat.Text.Remove(0, 2);
			//Get RAM Address
			RAMAddress = parseString.Remove(6, 5);
			//Get RAM Value
			RAMValue = parseString.Remove(0, 7);
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.

				//My Consern is that Work RAM High may be incorrect?
				if (byteSize == 8)
				{
					//We have a Byte sized value
					var watch = Watch.GenerateWatch(MemoryDomains["MainRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, false);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 16)
				{
					//We have a Word (Double Byte) sized Value
					var watch = Watch.GenerateWatch(MemoryDomains["MainRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, false);
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

		}
		private void SAT()
		{
			//Not yet.
			if (txtCheat.Text.IndexOf(" ") != 8)
			{
				MessageBox.Show("All Saturn GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (txtCheat.Text.Length != 13)
			{
				MessageBox.Show("All Saturn GameShark Cheats need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//This is a special test.  Only the first character really matters?  16 or 36?
			testo = testo.Remove(1, 1);
			switch (testo)
			{
				case "1":
					byteSize = 16;
					break;
				case "3":
					byteSize = 8;
					break;
				//0 writes once.
				case "0":
				//D is RAM Equal To Activator, do Next Value
				case "D":
					MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				case "F":
					MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				default:
					MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					//Leave this Method, before someone gets hurt.
					return;
			}
			//Sample Input for Saturn:
			//160949FC 0090
			//Address: 0949FC
			//Value:  90
			//Note, 3XXXXXXX are Big Endian
			//Remove first two octets
			parseString = txtCheat.Text.Remove(0, 2);
			//Get RAM Address
			RAMAddress = parseString.Remove(6, 5);
			//Get RAM Value
			RAMValue = parseString.Remove(0, 7);
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.

				//My Consern is that Work RAM High may be incorrect?
				if (byteSize == 8)
				{
					//We have a Byte sized value
					var watch = Watch.GenerateWatch(MemoryDomains["Work Ram High"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Word, Watch.DisplayType.Hex, txtDescription.Text, true);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 16)
				{
					//We have a Word (Double Byte) sized Value
					var watch = Watch.GenerateWatch(MemoryDomains["Work Ram High"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, true);
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
		}
		private void SMS()
		{
			//This is FUN!
			if (txtCheat.Text.IndexOf("-") != 4)
			{
				MessageBox.Show("All Master System Action Replay Codes need to contain a dash after the fourth character.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (txtCheat.Text.Length != 9)
			{
				MessageBox.Show("All Master System Action Replay Codes need to nine charaters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			parseString = txtCheat.Text;
			parseString = parseString.Remove(0, 2);
			//MessageBox.Show(parseString);
			RAMAddress = parseString.Remove(4, 2);
			//MessageBox.Show(RAMAddress);
			RAMAddress = RAMAddress.Replace("-", "");
			MessageBox.Show(RAMAddress);
			RAMValue = parseString.Remove(0, 5);
			MessageBox.Show(RAMValue);
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
				var watch = Watch.GenerateWatch(MemoryDomains["Main RAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), Watch.WatchSize.Byte, Watch.DisplayType.Hex, txtDescription.Text, false);
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
		}
		private void SNES()
		{
			//TODO: Sample Code and Get Smarter?
			//This ONLY applies to Action Replay.
			if (txtCheat.Text.Length != 8)
			{
				MessageBox.Show("All Action Replay Codes need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//The Action Replay, is odd.
			//Checking won't be done.
			//Remove first two octets
			RAMAddress = txtCheat.Text.Remove(6, 2);
			//Get RAM Value
			RAMValue = txtCheat.Text.Remove(0, 6);
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
