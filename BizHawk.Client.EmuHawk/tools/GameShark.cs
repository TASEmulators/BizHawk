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
	//GBA: Code Breaker (That uses unique Encryption keys)
	//NES: Pro Action Rocky  (When someone asks)
	//SNES: GoldFinger (Action Replay II) Support?

	//Clean up the checks to be more robust/less "hacky"
	//They work but feel bad

	//Verify all wording in the error reports
	

	[ToolAttributes(released: true, supportedSystems: new[] { "GB", "GBA", "GEN", "N64", "NES", "PSX", "SAT", "SMS", "SNES" })]
	public partial class GameShark : Form, IToolForm, IToolFormAutoConfig
	{
		#region " Game Genie Dictionary "
		private readonly Dictionary<char, int> _GBGGgameGenieTable = new Dictionary<char, int>
		{
			{'0', 0 },
			{'1', 1 },
			{'2', 2 },
			{'3', 3 },
			{'4', 4 },
			{'5', 5 },
			{'6', 6 },
			{'7', 7 },
			{'8', 8 },
			{'9', 9 },
			{'A', 10 },
			{'B', 11 },
			{'C', 12 },
			{'D', 13 },
			{'E', 14 },
			{'F', 15 }
		};
		private readonly Dictionary<char, long> _GENgameGenieTable = new Dictionary<char, long>
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
		//This only applies to the NES
		private readonly Dictionary<char, int> _NESgameGenieTable = new Dictionary<char, int>
			{
				{ 'A', 0 },  // 0000
				{ 'P', 1 },  // 0001
				{ 'Z', 2 },  // 0010
				{ 'L', 3 },  // 0011
				{ 'G', 4 },  // 0100
				{ 'I', 5 },  // 0101
				{ 'T', 6 },  // 0110
				{ 'Y', 7 },  // 0111
				{ 'E', 8 },  // 1000
				{ 'O', 9 },  // 1001
				{ 'X', 10 }, // 1010
				{ 'U', 11 }, // 1011
				{ 'K', 12 }, // 1100
				{ 'S', 13 }, // 1101
				{ 'V', 14 }, // 1110
				{ 'N', 15 }, // 1111
			};
		// including transposition
		// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
		// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
		//This only applies to the SNES
		private readonly Dictionary<char, int> _SNESgameGenieTable = new Dictionary<char, int>
		{
			{ 'D', 0 },  // 0000
			{ 'F', 1 },  // 0001
			{ '4', 2 },  // 0010
			{ '7', 3 },  // 0011
			{ '0', 4 },  // 0100
			{ '9', 5 },  // 0101
			{ '1', 6 },  // 0110
			{ '5', 7 },  // 0111
			{ '6', 8 },  // 1000
			{ 'B', 9 },  // 1001
			{ 'C', 10 }, // 1010 
			{ '8', 11 }, // 1011 
			{ 'A', 12 }, // 1100 
			{ '2', 13 }, // 1101 
			{ '3', 14 }, // 1110 
			{ 'E', 15 }  // 1111 
		};
		#endregion

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

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			
		}

		//My Variables
		string parseString = null;
		string RAMAddress = null;
		string RAMValue = null;
		int byteSize = 0;
		string testo = null;
		string SingleCheat = null;
		int loopValue = 0;
		private void btnGo_Click(object sender, EventArgs e)
		{
			//Reset Variables
			parseString = null;
			RAMAddress = null;
			RAMValue = null;
			byteSize = 0;

			SingleCheat = null;
			for (int i = 0; i < txtCheat.Lines.Length; i++)
			{
				loopValue = i;
				SingleCheat = txtCheat.Lines[i].ToUpper();
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
						testo = SingleCheat.Remove(2, 11);
						N64();
						break;
					case "NES":
						NES();
						break;
					case "PSX":
						//This determies what kind of Code we have
						testo = SingleCheat.Remove(2, 11);
						PSX();
						break;
					case "SAT":
						//This determies what kind of Code we have
						testo = SingleCheat.Remove(2, 11);
						SAT();
						break;
					case "SMS":
						SMS();
						break;
					case "SNES":
						SNES();
						break;
					default:
						//This should NEVER happen
						break;
				}
			}
			//We did the lines.  Let's clear.
			txtCheat.Clear();
			txtDescription.Clear();
		}
		//Original Code by adelikat
		//Decodes GameGear and GameBoy
		public void GBGGDecode(string code, ref int val, ref int add, ref int cmp)
		{
			// No cypher on value
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|      Value    |A|B|C|D|E|F|G|H|I|J|K|L|XOR 0xF|a|b|c|c|NotUsed|e|f|g|h|
			// proper |      Value    |XOR 0xF|A|B|C|D|E|F|G|H|I|J|K|L|g|h|a|b|Nothing|c|d|e|f|
			int x;
			// Getting Value
			if (code.Length > 0)
			{
				_GBGGgameGenieTable.TryGetValue(code[0], out x);
				val = x << 4;
			}

			if (code.Length > 1)
			{
				_GBGGgameGenieTable.TryGetValue(code[1], out x);
				val |= x;
			}
			// Address
			if (code.Length > 2)
			{
				_GBGGgameGenieTable.TryGetValue(code[2], out x);
				add = x << 8;
			}
			else
			{
				add = -1;
			}

			if (code.Length > 3)
			{
				_GBGGgameGenieTable.TryGetValue(code[3], out x);
				add |= x << 4;
			}

			if (code.Length > 4)
			{
				_GBGGgameGenieTable.TryGetValue(code[4], out x);
				add |= x;
			}

			if (code.Length > 5)
			{
				_GBGGgameGenieTable.TryGetValue(code[5], out x);
				add |= (x ^ 0xF) << 12;
			}

			// compare need to be full
			if (code.Length > 8)
			{
				int comp = 0;
				_GBGGgameGenieTable.TryGetValue(code[6], out x);
				comp = x << 2;

				// 8th character ignored
				_GBGGgameGenieTable.TryGetValue(code[8], out x);
				comp |= (x & 0xC) >> 2;
				comp |= (x & 0x3) << 6;
				cmp = comp ^ 0xBA;
			}
			else
			{
				cmp = -1;
			}
		}
		private void GB()
		{
			string RAMCompare = null;

			//Game Genie
			if (SingleCheat.LastIndexOf("-") == 7 && SingleCheat.IndexOf("-") == 3)
			{
				int val = 0;
				int add = 0;
				int cmp = 0;
				parseString = SingleCheat.Replace("-", "");
				GBGGDecode(parseString, ref val, ref add, ref cmp);
				RAMAddress = string.Format("{0:X4}", add);
				RAMValue = string.Format("{0:X2}", val);
				RAMCompare = string.Format("{0:X2}", cmp);
			}
			//Game Genie
			else if (SingleCheat.Contains("-") == true && SingleCheat.LastIndexOf("-") != 7 && SingleCheat.IndexOf("-") != 3)
			{
				MessageBox.Show("All GameBoy Game Geneie Codes need to have a dash after the third character and seventh character.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			//Game Shark codes
			if (SingleCheat.Length != 8 && SingleCheat.Contains("-") == false)
			{
				MessageBox.Show("All GameShark Codes need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			else if (SingleCheat.Length == 8 && SingleCheat.Contains("-") == false)
			{
				testo = SingleCheat.Remove(2, 6);
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

				parseString = SingleCheat.Remove(0, 2);
				//Now we need to break it down a little more.
				RAMValue = parseString.Remove(2, 4);
				parseString = parseString.Remove(0, 2);
				//The issue is Endian...  Time to get ultra clever.  And Regret it.
				//First Half
				RAMAddress = parseString.Remove(0, 2);
				RAMAddress = RAMAddress + parseString.Remove(2, 2);
				//We now have our values.

			}

			//This part, is annoying...
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				//Take Watch, Add our Value we want, and it should be active when addded?
				if (RAMCompare == null)
				{
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				else if (RAMCompare != null)
				{
					//We have a Compare
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber), int.Parse(RAMCompare, NumberStyles.HexNumber)));
				}
			}
			//Someone broke the world?
			catch (Exception ex)
			{
				MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		//Provided by mGBA and endrift
		UInt32[] GBAGameSharkSeeds = { UInt32.Parse("09F4FBBD", NumberStyles.HexNumber), UInt32.Parse("9681884A", NumberStyles.HexNumber), UInt32.Parse("352027E9", NumberStyles.HexNumber), UInt32.Parse("F3DEE5A7", NumberStyles.HexNumber) };
		UInt32[] GBAProActionReplaySeeds = { UInt32.Parse("7AA9648F", NumberStyles.HexNumber), UInt32.Parse("7FAE6994", NumberStyles.HexNumber), UInt32.Parse("C0EFAAD5", NumberStyles.HexNumber), UInt32.Parse("42712C57", NumberStyles.HexNumber) };
		//blnEncrypted is used to see if the previous line for Slide code was encrypted or not.
		Boolean blnEncrypted = false;
		//blnGameShark means, "This is a Game Shark/Action Replay (Not MAX) code."
		Boolean blnGameShark = false;
		//blnActionReplayMax means "This is an Action Replay MAX code."
		Boolean blnActionReplayMax = false;
		//blnCodeBreaker means "This is a CodeBreaker code."
		Boolean blnCodeBreaker = false;
		//blnUnhandled means "BizHawk can't do this one or the tool can't."
		Boolean blnUnhandled = false;
		//blnUnneeded means "You don't need this code."
		Boolean blnUnneeded = false;
		private void GBA()
		{
			blnEncrypted = false;
			bool blnNoCode = false;
			//Super Ultra Mega HD BizHawk GameShark/Action Replay/Code Breaker Final Hyper Edition Arcade Remix EX + α GBA Code detection method.
			//Seriously, it's that complex.

			//Check Game Shark/Action Replay (Not Max) Codes
			if (SingleCheat.Length == 17 && SingleCheat.IndexOf(" ") == 8)
			{
				blnNoCode = true;
				//Super Ultra Mega HD BizHawk GameShark/Action Replay/Code Breaker Final Hyper Edition Arcade Remix EX + α GBA Code detection method.
				//Seriously, it's that complex.

				//Check Game Shark/Action Replay (Not Max) Codes
				if (SingleCheat.Length == 17 && SingleCheat.IndexOf(" ") == 8)
				{
					//These are for the Decyption Values for GameShark and Action Replay MAX.
					UInt32 op1 = 0;
					UInt32 op2 = 0;
					UInt32 sum = 0xC6EF3720;

					//Let's get the stuff seperated.
					RAMAddress = SingleCheat.Remove(8, 9);
					RAMValue = SingleCheat.Remove(0, 9);
					//Let's see if this code matches the GameShark.
					GBAGameShark();
					//We got an Un-Needed code.
					if (blnUnneeded == true)
					{
						return;
					}
					//We got an Unhandled code.
					else if (blnUnhandled == true)
					{
						return;
					}
					if (blnGameShark == false)
					{
						//We don't have a GameShark code, or we have an encrypted code?
						//Further testing required.
						//GameShark Decryption Method
						parseString = SingleCheat;

						op1 = UInt32.Parse(parseString.Remove(8, 9), NumberStyles.HexNumber);
						op2 = UInt32.Parse(parseString.Remove(0, 9), NumberStyles.HexNumber);
						//Tiny Encryption Algorithm
						int i;
						for (i = 0; i < 32; ++i)
						{
							op2 -= ((op1 << 4) + GBAGameSharkSeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + GBAGameSharkSeeds[3]);
							op1 -= ((op2 << 4) + GBAGameSharkSeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + GBAGameSharkSeeds[1]);
							sum -= 0x9E3779B9;
						}
						//op1 has the Address
						//op2 has the Value
						//Sum, is pointless?
						RAMAddress = string.Format("{0:X8}", op1);
						RAMValue = string.Format("{0:X8}", op2);
						GBAGameShark();
					}
					//We don't do Else If after the if here because it won't allow us to verify the second code check.
					if (blnGameShark == true)
					{
						//We got a Valid GameShark Code.  Hopefully.
						AddGBA();
						return;
					}

					//We are going to assume that we got an Action Replay MAX code, or at least try to guess that we did.
					GBAActionReplay();

					if (blnActionReplayMax == false)
					{
						//Action Replay Max decryption Method
						parseString = SingleCheat;
						op1 = 0;
						op2 = 0;
						sum = 0xC6EF3720;
						op1 = UInt32.Parse(parseString.Remove(8, 9), NumberStyles.HexNumber);
						op2 = UInt32.Parse(parseString.Remove(0, 9), NumberStyles.HexNumber);
						//Tiny Encryption Algorithm
						int j;
						for (j = 0; j < 32; ++j)
						{
							op2 -= ((op1 << 4) + GBAProActionReplaySeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + GBAProActionReplaySeeds[3]);
							op1 -= ((op2 << 4) + GBAProActionReplaySeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + GBAProActionReplaySeeds[1]);
							sum -= 0x9E3779B9;
						}
						//op1 has the Address
						//op2 has the Value
						//Sum, is pointless?
						RAMAddress = string.Format("{0:X8}", op1);
						RAMValue = string.Format("{0:X8}", op2);
						blnEncrypted = true;
						GBAActionReplay();
					}
					//MessageBox.Show(blnActionReplayMax.ToString());
					//We don't do Else If after the if here because it won't allow us to verify the second code check.
					if (blnActionReplayMax == true)
					{
						//We got a Valid Action Replay Max Code.  Hopefully.
						AddGBA();
						//MessageBox.Show("ARM");
						return;
					}
				}
				//Detect CodeBreaker/GameShark SP/Xploder codes
				if (SingleCheat.Length == 12 && SingleCheat.IndexOf(" ") != 8)
				{
					MessageBox.Show("Codebreaker/GameShark SP/Xploder codes are not supported by this tool.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;

					//WARNING!!
					//This code is NOT ready yet.
					//The GameShark Key
					//09F4FBBD
					//9681884A
					//352027E9
					//F3DEE5A7

					//The CodeBreaker Key, for Advance Wars 2 (USA)
					//911B9B36
					//BC7C46FC
					//CE58668D
					//5C453661
					//Four sets, and this guy...
					//9D6E

					//Sample Code
					//Encyped:		6C2A1F51C2C0
					//Decrypted:	82028048 FFFFFFFF
					GBACodeBreaker();

					if (blnCodeBreaker == false)
					{
						parseString = SingleCheat;
						UInt32 op1 = 0;
						UInt32 op2 = 0;
						UInt32 sum = 0xC6EF3720;
						string test1;
						string test2;
						test1 = parseString.Remove(5, 6);
						test2 = parseString.Remove(0, 6);
						MessageBox.Show(test1.ToString());
						MessageBox.Show(test2.ToString());
						op1 = UInt32.Parse(parseString.Remove(5, 6), NumberStyles.HexNumber);
						op2 = UInt32.Parse(parseString.Remove(0, 6), NumberStyles.HexNumber);

						//Tiny Encryption Algorithm
						int i;
						for (i = 0; i < 32; ++i)
						{
							op2 -= ((op1 << 4) + GBAGameSharkSeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + GBAGameSharkSeeds[3]);
							op1 -= ((op2 << 4) + GBAGameSharkSeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + GBAGameSharkSeeds[1]);
							sum -= 0x9E3779B9;
						}
						//op1 has the Address
						//op2 has the Value
						//Sum, is pointless?
						RAMAddress = string.Format("{0:X8}", op1);
						//RAMAddress = RAMAddress.Remove(0, 1);
						RAMValue = string.Format("{0:X8}", op2);
						// && RAMAddress[6] == '0'
					}

					if (blnCodeBreaker == true)
					{
						//We got a Valid Code Breaker Code.  Hopefully.
						AddGBA();
						return;
					}

					if (SingleCheat.IndexOf(" ") != 8 && SingleCheat.Length != 12)
					{
						MessageBox.Show("ALL Codes for Action Replay, Action Replay MAX, Codebreaker, GameShark Advance, GameShark SP, Xploder have a Space after the 8th character.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					//We have a code
					if (blnNoCode == false)
					{
					}

				}
			}
		}
		public void GBAGameShark()
		{
			//This is for the Game Shark/Action Replay (Not Max)
			if (RAMAddress.StartsWith("0") == true && RAMValue.StartsWith("000000") == true)
			{
				//0aaaaaaaa 000000xx  1 Byte Constant Write
				//1 Byte Size Value
				byteSize = 8;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("1") == true && RAMValue.StartsWith("0000") == true)
			{
				//1aaaaaaaa 0000xxxx  2 Byte Constant Write
				//2 Byte Size Value
				byteSize = 16;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("2") == true)
			{
				//2aaaaaaaa xxxxxxxx  4 Byte Constant Write
				//4 Byte Size Value
				byteSize = 32;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("3000") == true)
			{
				//3000cccc xxxxxxxx aaaaaaaa  4 Byte Group Write  What?  Is this like a Slide Code
				//4 Byte Size Value
				//Sample
				//30000004 01010101 03001FF0 03001FF4 03001FF8 00000000
				//write 01010101 to 3 addresses - 01010101, 03001FF0, 03001FF4, and 03001FF8. '00000000' is used for padding, to ensure the last code encrypts correctly.
				//Note: The device improperly writes the Value, to the address.  We should ignore that.
				MessageBox.Show("Sorry, this tool does not support 3000XXXX codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("6") == true && RAMValue.StartsWith("0000") == true)
			{
				//6aaaaaaa 0000xxxx  2 Byte ROM patch
				byteSize = 16;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("6") == true && RAMValue.StartsWith("1000") == true)
			{
				//6aaaaaaa 1000xxxx  4 Byte ROM patch
				byteSize = 32;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("6") == true && RAMValue.StartsWith("2000") == true)
			{
				//6aaaaaaa 2000xxxx  8 Byte ROM patch
				byteSize = 32;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("8") == true && RAMAddress[2] == '1' == true && RAMValue.StartsWith("00") == true)
			{
				//8a1aaaaa 000000xx  1 Byte Write when GS Button Pushed
				//Treat as Constant Write.
				byteSize = 8;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("8") == true && RAMAddress[2] == '2' == true && RAMValue.StartsWith("00") == true)
			{
				//8a2aaaaa 000000xx  2 Byte Write when GS Button Pushed
				//Treat as Constant Write.
				byteSize = 16;
				blnGameShark = true;
			}
			else if (RAMAddress.StartsWith("80F00000") == true)
			{
				//80F00000 0000xxxx  Slow down when GS Button Pushed.
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("D") == true && RAMAddress.StartsWith("DEADFACE") == false && RAMValue.StartsWith("0000") == true)
			{
				//Daaaaaaa 0000xxxx  2 Byte If Equal, Activate next code
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("E0") == true)
			{
				//E0zzxxxx aaaaaaaa  2 Byte if Equal, Activate ZZ Lines.
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("F") == true && RAMValue.StartsWith("0000") == true)
			{
				//Faaaaaaa 0000xxxx  Hook Routine.  Probably not necessary?
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("001DC0DE") == true)
			{
				//xxxxxxxx 001DC0DE  Auto-Detect Game.  Useless for Us.
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("DEADFACE") == true)
			{
				//DEADFACE 0000xxxx  Change Encryption Seeds.  Unsure how this works.
				MessageBox.Show("Sorry, this tool does not support DEADFACE codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			else
			{
				blnGameShark = false;
			}
			//It's not a GameShark/Action Replay (Not MAX) code, so we say False.

			return;
		}
		public void GBAActionReplay()
		{
			//These checks are done on the DECYPTED code, not the Encrypted one.
			//ARM Code Types
			// 1) Normal RAM Write Codes
			if (RAMAddress.StartsWith("002") == true)
			{
				// 0022 Should be Changed to 020
				// Fill area (XXXXXXXX) to (XXXXXXXX+YYYYYY) with Byte ZZ.
				// XXXXXXXX
				// YYYYYYZZ
				//This corrects the value
				RAMAddress = RAMAddress.Replace("002", "020");
				blnActionReplayMax = true;
				byteSize = 8;
			}
			else if (RAMAddress.StartsWith("022") == true)
			{
				// 0222 Should be Changed to 020
				// Fill area (XXXXXXXX) to (XXXXXXXX+YYYY*2) with Halfword ZZZZ.
				// XXXXXXXX
				// YYYYZZZZ
				//This corrects the value
				RAMAddress = RAMAddress.Replace("022", "020");
				blnActionReplayMax = true;
				byteSize = 16;
			}

			else if (RAMAddress.StartsWith("042") == true)
			{
				// 0422 Should be Changed to 020
				// Write the Word ZZZZZZZZ to address XXXXXXXX.
				// XXXXXXXX
				// ZZZZZZZZ
				//This corrects the value
				RAMAddress = RAMAddress.Replace("042", "020");
				blnActionReplayMax = true;
				byteSize = 32;
			}
			// 2) Pointer RAM Write Codes
			else if (RAMAddress.StartsWith("402") == true)
			{
				// 4022 Should be Changed to 020
				// Writes Byte ZZ to ([the address kept in XXXXXXXX]+[YYYYYY]).
				// XXXXXXXX
				// YYYYYYZZ
				//This corrects the value
				RAMAddress = RAMAddress.Replace("402", "020");
				blnActionReplayMax = true;
				byteSize = 8;
			}
			else if (RAMAddress.StartsWith("420") == true)
			{
				// 4202 Should be Changed to 020
				// Writes Halfword ZZZZ ([the address kept in XXXXXXXX]+[YYYY*2]).
				// XXXXXXXX
				// YYYYZZZZ
				//This corrects the value
				RAMAddress = RAMAddress.Replace("420", "020");
				blnActionReplayMax = true;
				byteSize = 16;
			}
			else if (RAMAddress.StartsWith("422") == true)
			{
				// 442 Should be Changed to 020
				// Writes the Word ZZZZZZZZ to [the address kept in XXXXXXXX].
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("422", "020");
				blnActionReplayMax = true;
				byteSize = 32;
			}
			// 3) Add Codes
			else if (RAMAddress.StartsWith("802") == true)
			{
				// 802 Should be Changed to 020
				// Add the Byte ZZ to the Byte stored in XXXXXXXX.
				// XXXXXXXX
				// 000000ZZ
				//RAMAddress = RAMAddress.Replace("8022", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("822") == true & RAMAddress.StartsWith("8222") == false)
			{
				// 822 Should be Changed to 020
				// Add the Halfword ZZZZ to the Halfword stored in XXXXXXXX.
				// XXXXXXXX
				// 0000ZZZZ
				//RAMAddress = RAMAddress.Replace("822", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("842") == true)
			{
				//NOTE!
				//This is duplicated below with different function results.
				// 842 Should be Changed to 020
				// Add the Word ZZZZ to the Halfword stored in XXXXXXXX.
				// XXXXXXXX
				// ZZZZZZZZ
				//RAMAddress = RAMAddress.Replace("842", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}

			// 4) Write to $4000000 (IO Registers!)
			else if (RAMAddress.StartsWith("C6000130") == true)
			{
				// C6000130 Should be Changed to 00000130
				// Write the Halfword ZZZZ to the address $4XXXXXX
				// 00XXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("C6000130", "00000130");
				blnActionReplayMax = true;
				byteSize = 16;
			}
			else if (RAMAddress.StartsWith("C7000130") == true)
			{
				// C7000130 Should be Changed to 00000130
				// Write the Word ZZZZZZZZ to the address $4XXXXXX
				// 00XXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("C7000130", "00000130");
				blnActionReplayMax = true;
				byteSize = 32;
			}
			// 5) If Equal Code (= Joker Code)
			else if (RAMAddress.StartsWith("082") == true)
			{
				// 082 Should be Changed to 020
				// If Byte at XXXXXXXX = ZZ then execute next code.
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("082", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("482") == true)
			{
				// 482 Should be Changed to 020
				// If Byte at XXXXXXXX = ZZ then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("482", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("882") == true)
			{
				// 882 Should be Changed to 020
				// If Byte at XXXXXXXX = ZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("882", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("C82") == true)
			{
				// C82 Should be Changed to 020
				// While Byte at XXXXXXXX <> ZZ turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("C82", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("0A2") == true)
			{
				// 0A2 Should be Changed to 020
				// If Halfword at XXXXXXXX = ZZZZ then execute next code.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("0A2", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("4A2") == true)
			{
				// 4A2 Should be Changed to 020
				// If Halfword at XXXXXXXX = ZZZZ then execute next 2 codes.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("4A2", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("8A2") == true)
			{
				// 8A2 Should be Changed to 020
				// If Halfword at XXXXXXXX = ZZZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("8A2", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("CA2") == true)
			{
				// CA2 Should be Changed to 020
				// While Halfword at XXXXXXXX <> ZZZZ turn off all codes.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("CA2", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("0C2") == true)
			{
				// 0C2 Should be Changed to 020
				// If Word at XXXXXXXX = ZZZZZZZZ then execute next code.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("0C2", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("4C2") == true)
			{
				// 4C2 Should be Changed to 020
				// If Word at XXXXXXXX = ZZZZZZZZ then execute next 2 codes.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("4C2", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("8C2") == true)
			{
				// 8C2 Should be Changed to 020
				// If Word at XXXXXXXX = ZZZZZZZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("8C2", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("CC2") == true)
			{
				// CC2 Should be Changed to 020
				// While Word at XXXXXXXX <> ZZZZZZZZ turn off all codes.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("CC2", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 6) If Different Code
			else if (RAMAddress.StartsWith("102") == true)
			{
				// 102 Should be Changed to 020
				// If Byte at XXXXXXXX <> ZZ then execute next code.
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("102", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("502") == true)
			{
				// 502 Should be Changed to 020
				// If Byte at XXXXXXXX <> ZZ then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("502", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("902") == true)
			{
				// 902 Should be Changed to 020
				// If Byte at XXXXXXXX <> ZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("902", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("D02") == true)
			{
				// D02 Should be Changed to 020
				// While Byte at XXXXXXXX = ZZ turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				RAMAddress = RAMAddress.Replace("D02", "020");
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("122") == true)
			{
				// 122 Should be Changed to 020
				// If Halfword at XXXXXXXX <> ZZZZ then execute next code.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("122", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("522") == true)
			{
				// 522 Should be Changed to 020
				// If Halfword at XXXXXXXX <> ZZZZ then execute next 2 codes.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("522", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("922") == true)
			{
				// 922 Should be Changed to 020
				// If Halfword at XXXXXXXX <> ZZZZ disable all the codes below this one.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("922", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("D22") == true)
			{
				// D22 Should be Changed to 020
				// While Halfword at XXXXXXXX = ZZZZ turn off all codes.
				// XXXXXXXX
				// 0000ZZZZ
				RAMAddress = RAMAddress.Replace("D22", "020");
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("142") == true)
			{
				// 142 Should be Changed to 020
				// If Word at XXXXXXXX <> ZZZZZZZZ then execute next code.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("142", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("542") == true)
			{
				// 542 Should be Changed to 020
				// If Word at XXXXXXXX <> ZZZZZZZZ then execute next 2 codes.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("542", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("942") == true)
			{
				// 942 Should be Changed to 020
				// If Word at XXXXXXXX <> ZZZZZZZZ disable all the codes below this one.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("942", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("D42") == true)
			{
				// D42 Should be Changed to 020
				// While Word at XXXXXXXX = ZZZZZZZZ turn off all codes.
				// XXXXXXXX
				// ZZZZZZZZ
				RAMAddress = RAMAddress.Replace("D42", "020");
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 7)
			// [If Byte at address XXXXXXXX is lower than ZZ] (signed) Code
			// Signed means : For bytes : values go from -128 to +127. For Halfword : values go from -32768/+32767. For Words : values go from -2147483648 to 2147483647. For exemple, for the Byte comparison, 7F (127) will be > to FF (-1).
			// 
			// 
			else if (RAMAddress.StartsWith("182") == true)
			{
				// 182 Should be Changed to 020
				// If ZZ > Byte at XXXXXXXX then execute next code.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("182") == true)
				{
					RAMAddress = RAMAddress.Replace("182", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("582") == true)
			{
				// 582 Should be Changed to 020
				// If ZZ > Byte at XXXXXXXX then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("582") == true)
				{
					RAMAddress = RAMAddress.Replace("582", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("982") == true)
			{
				// 982 Should be Changed to 020
				// If ZZ > Byte at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("982") == true)
				{
					RAMAddress = RAMAddress.Replace("982", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("D82") == true || (RAMAddress.StartsWith("E82") == true))
			{
				// D82 or E82 Should be Changed to 020
				// While ZZ <= Byte at XXXXXXXX turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("D82") == true)
				{
					RAMAddress = RAMAddress.Replace("D82", "020");
				}
				else if ((RAMAddress.StartsWith("E82") == true))
				{
					RAMAddress = RAMAddress.Replace("E82", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("1A2") == true)
			{
				// 1A2 Should be Changed to 020
				// If ZZZZ > Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("1A2") == true)
				{
					RAMAddress = RAMAddress.Replace("1A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("5A2") == true)
			{
				// 5A2  Should be Changed to 020
				// If ZZZZ > Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("5A2") == true)
				{
					RAMAddress = RAMAddress.Replace("5A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("9A2") == true)
			{
				// 9A2 Should be Changed to 020
				// If ZZZZ > Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX 0000ZZZZ 
				if (RAMAddress.StartsWith("9A2") == true)
				{
					RAMAddress = RAMAddress.Replace("9A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("DA2") == true)
			{
				// DA2 Should be Changed to 020
				// While ZZZZ <= Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("DA2") == true)
				{
					RAMAddress = RAMAddress.Replace("DA2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("1C2") == true)
			{
				// 1C2 or Should be Changed to 020
				// If ZZZZ > Word at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("1C2") == true)
				{
					RAMAddress = RAMAddress.Replace("1C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("5C2") == true)
			{
				// 5C2 Should be Changed to 020
				// If ZZZZ > Word at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("5C2") == true)
				{
					RAMAddress = RAMAddress.Replace("5C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("9C2") == true)
			{
				// 9C2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("9C2") == true)
				{
					RAMAddress = RAMAddress.Replace("9C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("DC2") == true)
			{
				// DC2 Should be Changed to 020
				// While ZZZZZZZZ <= Word at XXXXXXXX turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("DC2") == true)
				{
					RAMAddress = RAMAddress.Replace("DC2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 8)
			// [If Byte at address XXXXXXXX is higher than ZZ] (signed) Code
			// Signed means : For bytes : values go from -128 to +127. For Halfword : values go from -32768/+32767. For Words : values go from -2147483648 to 2147483647. For exemple, for the Byte comparison, 7F (127) will be > to FF (-1).
			//
			else if (RAMAddress.StartsWith("202") == true)
			{
				// 202 Should be Changed to 020
				// If ZZ < Byte at XXXXXXXX then execute next code.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("202") == true)
				{
					RAMAddress = RAMAddress.Replace("202", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("602") == true)
			{
				// 602 Should be Changed to 020
				// If ZZ < Byte at XXXXXXXX then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("602") == true)
				{
					RAMAddress = RAMAddress.Replace("602", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("A02") == true)
			{
				// A02 Should be Changed to 020
				// If ZZ < Byte at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("A02") == true)
				{
					RAMAddress = RAMAddress.Replace("A02", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("E02") == true)
			{
				// E02 Should be Changed to 020
				// While ZZ => Byte at XXXXXXXX turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("E02") == true)
				{
					RAMAddress = RAMAddress.Replace("E02", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("222") == true)
			{
				// 222 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("222") == true)
				{
					RAMAddress = RAMAddress.Replace("222", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("622") == true)
			{
				// 622 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("622") == true)
				{
					RAMAddress = RAMAddress.Replace("622", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("A22") == true)
			{
				// A22 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("A22") == true)
				{
					RAMAddress = RAMAddress.Replace("A22", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("E22") == true)
			{
				// E22 Should be Changed to 020
				// While ZZZZ => Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("E22") == true)
				{
					RAMAddress = RAMAddress.Replace("E22", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("242") == true)
			{
				// 242 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("242") == true)
				{
					RAMAddress = RAMAddress.Replace("242", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("642") == true)
			{
				// 642 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("642") == true)
				{
					RAMAddress = RAMAddress.Replace("642", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("A42") == true)
			{
				// A42 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("A42") == true)
				{
					RAMAddress = RAMAddress.Replace("A42", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("E42") == true)
			{
				// E42 Should be Changed to 020
				// While ZZZZ => Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX 0000ZZZZ
				if (RAMAddress.StartsWith("E42") == true)
				{
					RAMAddress = RAMAddress.Replace("E42", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 9)
			// [If Value at adress XXXXXXXX is lower than...] (unsigned) Code
			// Unsigned means : For bytes : values go from 0 to +255. For Halfword : values go from 0 to +65535. For Words : values go from 0 to 4294967295. For exemple, for the Byte comparison, 7F (127) will be < to FF (255).
			// 
			// 
			else if (RAMAddress.StartsWith("282") == true)
			{
				// 282 Should be Changed to 020
				// If ZZZZZZZZ > Byte at XXXXXXXX then execute next line.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("282") == true)
				{
					RAMAddress = RAMAddress.Replace("282", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("682") == true)
			{
				// 682 Should be Changed to 020
				// If ZZZZZZZZ > Byte at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("682") == true)
				{
					RAMAddress = RAMAddress.Replace("682", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("A82") == true)
			{
				// A82 Should be Changed to 020
				// If ZZZZZZZZ > Byte at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("A82") == true)
				{
					RAMAddress = RAMAddress.Replace("A82", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("2A2") == true)
			{
				// 2A2 Should be Changed to 020
				// If ZZZZZZZZ > Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("2A2") == true)
				{
					RAMAddress = RAMAddress.Replace("2A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("6A2") == true)
			{
				// 6A2 Should be Changed to 020
				// If ZZZZZZZZ > Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("6A2") == true)
				{
					RAMAddress = RAMAddress.Replace("6A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("AA2") == true)
			{
				// AA2 Should be Changed to 020
				// If ZZZZZZZZ > Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("AA2") == true)
				{
					RAMAddress = RAMAddress.Replace("AA2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("EA2") == true)
			{
				// EA2 Should be Changed to 020
				// While ZZZZZZZZ <= Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("EA2") == true)
				{
					RAMAddress = RAMAddress.Replace("EA2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("2C2") == true)
			{
				// 2C2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute next line.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("2C2") == true)
				{
					RAMAddress = RAMAddress.Replace("2C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("6C2") == true)
			{
				// 6C2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("6C2") == true)
				{
					RAMAddress = RAMAddress.Replace("6C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("AC2") == true)
			{
				// AC2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("AC2") == true)
				{
					RAMAddress = RAMAddress.Replace("AC2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("EC2") == true)
			{
				// EC2 Should be Changed to 020
				// While ZZZZZZZZ <= Word at XXXXXXXX turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("EC2") == true)
				{
					RAMAddress = RAMAddress.Replace("EC2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 10)
			// [If Value at adress XXXXXXXX is higher than...] (unsigned) Code
			// Unsigned means For bytes : values go from 0 to +255. For Halfword : values go from 0 to +65535. For Words : values go from 0 to 4294967295. For exemple, for the Byte comparison, 7F (127) will be < to FF (255).
			else if (RAMAddress.StartsWith("302") == true)
			{
				// 302 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("302") == true)
				{
					RAMAddress = RAMAddress.Replace("302", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("702") == true)
			{
				// 702 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("702") == true)
				{
					RAMAddress = RAMAddress.Replace("702", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("B02") == true)
			{
				// B02 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("B02") == true)
				{
					RAMAddress = RAMAddress.Replace("B02", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("F02") == true)
			{
				// F02 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("F02") == true)
				{
					RAMAddress = RAMAddress.Replace("F02", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("322") == true)
			{
				// 322 Should be Changed to 020
				//If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line.  
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("322") == true)
				{
					RAMAddress = RAMAddress.Replace("322", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("722") == true)
			{
				// 722 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("722") == true)
				{
					RAMAddress = RAMAddress.Replace("722", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("B22") == true)
			{
				// B22 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("B22") == true)
				{
					RAMAddress = RAMAddress.Replace("B22", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("F22") == true)
			{
				// F22 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("F22") == true)
				{
					RAMAddress = RAMAddress.Replace("F22", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}

			else if (RAMAddress.StartsWith("342") == true)
			{
				// 342 Should be Changed to 020
				//If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line.  
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("342") == true)
				{
					RAMAddress = RAMAddress.Replace("342", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("742") == true)
			{
				// 742 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("742") == true)
				{
					RAMAddress = RAMAddress.Replace("742", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("B42") == true)
			{
				// B42 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("B42") == true)
				{
					RAMAddress = RAMAddress.Replace("B42", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("F42") == true)
			{
				// F42 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("F42") == true)
				{
					RAMAddress = RAMAddress.Replace("F42", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 11) If AND Code
			else if (RAMAddress.StartsWith("382") == true)
			{
				// 382 Should be Changed to 020
				// If ZZ AND Byte at XXXXXXXX <> 0 (= True) then execute next code.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("382") == true)
				{
					RAMAddress = RAMAddress.Replace("382", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("782") == true)
			{
				// 782 Should be Changed to 020
				// If ZZ AND Byte at XXXXXXXX <> 0 (= True) then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("782") == true)
				{
					RAMAddress = RAMAddress.Replace("782", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("B82") == true)
			{
				// B82 Should be Changed to 020
				// If ZZ AND Byte at XXXXXXXX <> 0 (= True) then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("B82") == true)
				{
					RAMAddress = RAMAddress.Replace("B82", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("F82") == true)
			{
				// F82 Should be Changed to 020
				// While ZZ AND Byte at XXXXXXXX = 0 (= False) then turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				if (RAMAddress.StartsWith("F82") == true)
				{
					RAMAddress = RAMAddress.Replace("F82", "020");
				}
				byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("3A2") == true)
			{
				// 3A2 Should be Changed to 020
				// If ZZZZ AND Halfword at XXXXXXXX <> 0 (= True) then execute next code.
				// XXXXXXXX
				// 0000ZZZZ
				if (RAMAddress.StartsWith("3A2") == true)
				{
					RAMAddress = RAMAddress.Replace("3A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("7A2") == true)
			{
				// 7A2 Should be Changed to 020
				// If ZZZZ AND Halfword at XXXXXXXX <> 0 (= True) then execute next 2 codes.
				// XXXXXXXX
				// 0000ZZZZ
				if (RAMAddress.StartsWith("7A2") == true)
				{
					RAMAddress = RAMAddress.Replace("7A2", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("3C2") == true)
			{
				// 3C2 Should be Changed to 020
				// If ZZZZZZZZ AND Word at XXXXXXXX <> 0 (= True) then execute next code.
				// XXXXXXXX
				// ZZZZZZZZ
				if (RAMAddress.StartsWith("3C2") == true)
				{
					RAMAddress = RAMAddress.Replace("3C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("7C2") == true)
			{
				// 7C2 Should be Changed to 020
				// If ZZZZZZZZ AND Word at XXXXXXXX <> 0 (= True) then execute next 2 codes.
				// XXXXXXXX
				// ZZZZZZZZ
				if (RAMAddress.StartsWith("7C2") == true)
				{
					RAMAddress = RAMAddress.Replace("7C2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("BC2") == true)
			{
				// BC2 Should be Changed to 020
				// If ZZZZZZZZ AND Word at XXXXXXXX <> 0 (= True) then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// ZZZZZZZZ
				if (RAMAddress.StartsWith("BC2") == true)
				{
					RAMAddress = RAMAddress.Replace("BC2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("FC2") == true)
			{
				// FC2 Should be Changed to 020
				// While ZZZZZZZZ AND Word at XXXXXXXX = 0 (= False) then turn off all codes.
				// XXXXXXXX
				// ZZZZZZZZ
				if (RAMAddress.StartsWith("FC2") == true)
				{
					RAMAddress = RAMAddress.Replace("FC2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 12) "Always..." Codes
			// For the "Always..." codes: -XXXXXXXX can be any authorised address BUT 00000000 (use 02000000 if you don't know what to choose). -ZZZZZZZZ can be anything. -The "y" in the code data must be in the [1-7] range (which means not 0).
			// 
			// 
			else if (RAMAddress.StartsWith("0E2") == true)
			{
				// 0E2 Should be Changed to 020
				// Always skip next line.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("0E2") == true)
				{
					RAMAddress = RAMAddress.Replace("0E2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("4E2") == true)
			{
				// 4E2 Should be Changed to 020
				// Always skip next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("4E2") == true)
				{
					RAMAddress = RAMAddress.Replace("4E2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("8E2") == true)
			{
				// 8E2 Should be Changed to 020
				// Always Stops executing all the codes below.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("8E2") == true)
				{
					RAMAddress = RAMAddress.Replace("8E2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("CE2") == true)
			{
				// CE2 Should be Changed to 020
				// Always turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (RAMAddress.StartsWith("CE2") == true)
				{
					RAMAddress = RAMAddress.Replace("CE2", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			// 13) 1 Line Special Codes (= starting with "00000000")
			//Special Slide Code Detection Method
			else if (RAMAddress.StartsWith("00000000") == true && RAMValue.StartsWith("8"))
			{
				//Code Sample
				//00000000 82222C96
				//00000063 0032000C

				//This says:
				// Write to Address 2222C96
				// The value 63
				// Fifty (50) times (32 in Hex)
				// Incriment the Address by 12 (Hex)

				//Sample Compare:
				// 00222696 00000063  Unit 1 HP
				// 002226A2 00000063  Unit 2 HP
				// Goes up by C (12)

				//Parse and go
				//We reset RAM Address to the RAM Value, because the Value holds the address on the first line.

				int incriment;
				int looper;
				string RealAddress = null;
				string realValue = null;
				RealAddress = RAMValue.Remove(0, 1);
				//MessageBox.Show("Real Address: " + RealAddress);
				//We need the next line
				try
				{
					loopValue += 1;
					//MessageBox.Show("Loop Value: " + loopValue.ToString());
					SingleCheat = txtCheat.Lines[loopValue].ToUpper();
					//We need to parse now.
					if (SingleCheat.Length == 17 && SingleCheat.IndexOf(" ") == 8)
					{
						if (blnEncrypted == true)
						{
							//The code was Encrypted
							//Decrypt before we do stuff.
							//Action Replay Max decryption Method
							parseString = SingleCheat;
							UInt32 op1 = 0;
							UInt32 op2 = 0;
							UInt32 sum = 0xC6EF3720;
							op1 = 0;
							op2 = 0;
							sum = 0xC6EF3720;
							op1 = UInt32.Parse(parseString.Remove(8, 9), NumberStyles.HexNumber);
							op2 = UInt32.Parse(parseString.Remove(0, 9), NumberStyles.HexNumber);
							//Tiny Encryption Algorithm
							int j;
							for (j = 0; j < 32; ++j)
							{
								op2 -= ((op1 << 4) + GBAProActionReplaySeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + GBAProActionReplaySeeds[3]);
								op1 -= ((op2 << 4) + GBAProActionReplaySeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + GBAProActionReplaySeeds[1]);
								sum -= 0x9E3779B9;
							}
							//op1 has the Address
							//op2 has the Value
							//Sum, is pointless?
							RAMAddress = string.Format("{0:X8}", op1);
							RAMValue = string.Format("{0:X8}", op2);
						}
						else if (blnEncrypted == false)
						{
							RAMAddress = SingleCheat.Remove(8, 9);
							RAMValue = SingleCheat.Remove(0, 9);
						}
						//We need to determine the Byte Size.
						//Determine how many leading Zeros there are.
						realValue = RAMAddress;
						if (realValue.StartsWith("000000"))
						{
							//Byte
							byteSize = 8;
						}
						else if (realValue.StartsWith("0000"))
						{
							//2 Byte
							byteSize = 16;
						}
						else
						{
							//4 Byte
							byteSize = 32;
						}
						//I need the Incriment Value (Add to RAM Address)
						//I also need the Loop Value
						incriment = int.Parse(RAMValue.Remove(0, 4), NumberStyles.HexNumber);
						looper = int.Parse(RAMValue.Remove(4, 4), NumberStyles.HexNumber);
						//We set the RAMAddress to our RealAddress
						RAMAddress = RealAddress;
						//We set the RAMValue to our RealValue
						RAMValue = realValue;
						for (int i = 0; i < looper; i++)
						{
							//We need to Bulk Add codes
							//Add our Cheat
							AddGBA();
							//Time to add
							RAMAddress = (int.Parse(RAMAddress) + incriment).ToString();
						}
					}
				}
				catch (Exception ex)
				{
					//We should warn the user.
					MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else if (RAMAddress.StartsWith("080") == true)
			{
				// End of the code list (even if you put values in the 2nd line).
				// 00000000
				//Let's ignore the user's input on this one?
				if (RAMAddress.StartsWith("080") == true)
				{
					RAMAddress = RAMAddress.Replace("080", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("0800") == true && RAMAddress[6] == '0' == true && RAMAddress[7] == '0' == true)
			{
				// AR Slowdown : loops the AR XX times
				// 0800XX00
				if (RAMAddress.StartsWith("0800") == true && RAMAddress[6] == '0' == true && RAMAddress[7] == '0' == true)
				{
					RAMAddress = RAMAddress.Replace("0800", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			// 14) 2 Lines Special Codes (= starting with '00000000' and padded (if needed) with "00000000")
			// Note: You have to add the 0es manually, after clicking the "create" button.
			//Ocean Prince's note:
			//Several of these appear to be conflicted with above detections.
			else if (RAMAddress.StartsWith("1E2") == true)
			{
				// 1E2 Should be Changed to 020
				// Patches ROM address (XXXXXXXX << 1) with Halfword ZZZZ. Does not work on V1/2 upgraded to V3. Only for a real V3 Hardware?
				// XXXXXXXX
				// 0000ZZZZ
				if (RAMAddress.StartsWith("1E2") == true)
				{
					RAMAddress = RAMAddress.Replace("1E2", "020");
				}
				byteSize = 16;
				blnActionReplayMax = true;
			}
			else if (RAMAddress.StartsWith("40000000") == true)
			{
				// 40000000 Should be Changed to 00000000
				// (SP = 0) (means : stops the "then execute all following codes in the same row" and stops the "else execute none of the codes below)".
				// 00000000
				// 00000000
				if (RAMAddress.StartsWith("40000000") == true)
				{
					RAMAddress = RAMAddress.Replace("40000000", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("60000000") == true)
			{
				// 60000000 Should be Changed to 00000000
				// (SP = 1) (means : start to execute all codes until end of codes or SP = 0). (bypass the number of codes to executes set by the master code). Should be Changed to (If SP <> 2)
				// 00000000
				// 00000000
				if (RAMAddress.StartsWith("60000000") == true)
				{
					RAMAddress = RAMAddress.Replace("60000000", "020");
				}
				byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			//TODO:
			//Figure out how these work.
			//NOTE:
			//This is a Three Line Checker
			#region "The Three Line Adds"
			else if (RAMAddress.StartsWith("8022") == true)
			{
				// 802 Should be Changed to 020
				// Writes Byte YY at address XXXXXXXX. Then makes YY = YY + Z1, XXXXXXXX = XXXXXXXX + Z3Z3, Z2 = Z2 - 1, and repeats until Z2 < 0.
				// XXXXXXXX
				// 000000YY
				// Z1Z2Z3Z3
				if (RAMAddress.StartsWith("8022") == true)
				{
					RAMAddress = RAMAddress.Replace("8022", "0200");
				}
				byteSize = 8;
				MessageBox.Show("Sorry, this tool does not support 8022 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			//I Don't get what this is doing.
			else if (RAMAddress.StartsWith("8222") == true)
			{
				// 822 Should be Changed to 020
				// Writes Halfword YYYY at address XXXXXXXX. Then makes YYYY = YYYY + Z1, XXXXXXXX = XXXXXXXX + Z3Z3*2,
				// XXXXXXXX
				// 0000YYYY
				// Z1Z2Z3Z3
				if (RAMAddress.StartsWith("8222") == true)
				{
					RAMAddress = RAMAddress.Replace("8222", "0200");
				}
				byteSize = 16;
				MessageBox.Show("Sorry, this tool does not support 8222 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("842") == true)
			{
				// 842 Should be Changed to 020
				// Writes Word YYYYYYYY at address XXXXXXXX. Then makes YYYYYYYY = YYYYYYYY + Z1, XXXXXXXX = XXXXXXXX + Z3Z3*4, Z2 = Z2 - 1, and repeats until Z2<0.
				// XXXXXXXX
				// YYYYYYYY
				// Z1Z2Z3Z3
				// WARNING: There is a BUG on the REAL AR (v2 upgraded to v3, and maybe on real v3) with the 32Bits Increment Slide code. You HAVE to add a code (best choice is 80000000 00000000 : add 0 to value at address 0) right after it, else the AR will erase the 2 last 8 digits lines of the 32 Bits Inc. Slide code when you enter it !!!
				if (RAMAddress.StartsWith("842") == true)
				{
					RAMAddress = RAMAddress.Replace("842", "020");
				}
				byteSize = 32;
				MessageBox.Show("Sorry, this tool does not support 8222 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			#endregion
			// 15) Special Codes
			// -Master Code-
			// address to patch AND $1FFFFFE Should be Changed to address to patch
			// Master Code settings.
			// XXXXXXXX
			// 0000YYYY
			// 
			else if (RAMValue.StartsWith("001DC0DE") == true)
			{
				// -ID Code-
				// Word at address 080000AC
				// Must always be 001DC0DE
				// XXXXXXXX
				// 001DC0DE
				if (RAMValue.StartsWith("001DC0DE") == true)
				{
					RAMValue = RAMValue.Replace("001DC0DE", "020");
				}
				byteSize = 32;
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("DEADFACE") == true)
			{
				// -DEADFACE-
				// New Encryption seed.
				// DEADFACE
				// 0000XXXX
				MessageBox.Show("Sorry, this tool does not support DEADFACE codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			else if (blnActionReplayMax == false)
			{
				//Is it a bad code?  Check the others.
				blnActionReplayMax = false;
			}
			return;
		}
		public void GBACodeBreaker()
		{
			//These checks are done on the DECYPTED code, not the Encrypted one.
			if (RAMAddress.StartsWith("0000") == true && RAMValue.StartsWith("0008") == true || RAMAddress.StartsWith("0000") == true && RAMValue.StartsWith("0002") == true)
			{
				//Master Code #1
				//0000xxxx yyyy

				//xxxx is the CRC value (the "Game ID" converted to hex)
				//Flags("yyyy"):
				//0008 - CRC Exists(CRC is used to autodetect the inserted game)
				//0002 - Disable Interupts
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("1") == true && RAMValue.StartsWith("1000") == true || RAMAddress.StartsWith("1") == true && RAMValue.StartsWith("2000") == true || RAMAddress.StartsWith("1") == true && RAMValue.StartsWith("3000") == true || RAMAddress.StartsWith("1") == true && RAMValue.StartsWith("4000") == true || RAMAddress.StartsWith("1") == true && RAMValue.StartsWith("0020") == true)
			{
				//Master Code #2	 
				//1aaaaaaa xxxy
				//'y' is the CBA Code Handler Store Address(0 - 7)[address = ((d << 0x16) + 0x08000100)]

				//1000 - 32 - bit Long - Branch Type(Thumb)
				//2000 - 32 - bit Long - Branch Type(ARM)
				//3000 - 8 - bit(?) Long - Branch Type(Thumb)
				//4000 - 8 - bit(?) Long - Branch Type(ARM)
				//0020 - Unknown(Odd Effect)
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnneeded = true;
				return;
			}
			else if (RAMAddress.StartsWith("3") == true)
			{
				//8 - Bit Constant RAM Write
				//3aaaaaaa 00yy
				//Continuosly writes the 8 - Bit value specified by 'yy' to address aaaaaaa.
				RAMAddress = RAMAddress.Remove(0, 1);
				byteSize = 16;
			}
			else if (RAMAddress.StartsWith("4") == true)
			{
				//Slide Code
				//4aaaaaaa yyyy
				//xxxxxxxx iiii
				//This is one of those two - line codes.The "yyyy" set is the data to store at the address (aaaaaaa), with xxxxxxxx being the number of addresses to store to, and iiii being the value to increment the addresses by.  The codetype is usually use to fill memory with a certain value.
				RAMAddress = RAMAddress.Remove(0, 1);
				byteSize = 32;
				MessageBox.Show("Sorry, this tool does not support 4 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("6") == true)
			{
				//16 - Bit Logical AND
				//6aaaaaaa yyyy
				//Performs the AND function on the address provided with the value provided. I'm not going to explain what AND does, so if you'd like to know I suggest you see the instruction manual for a graphing calculator.
				//This is another advanced code type you'll probably never need to use.  

				//Ocean Prince's note:
				//AND means "If ALL conditions are True then Do"
				//I don't understand how this would be applied/works.  Samples are requested.
				MessageBox.Show("Sorry, this tool does not support 6 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("7") == true)
			{
				//16 - Bit 'If Equal To' Activator
				//7aaaaaaa yyyy
				//If the value at the specified RAM address(aaaaaaa) is equal to yyyy value, active the code on the next line.
				byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("8") == true)
			{
				//16 - Bit Constant RAM Write
				//8aaaaaaa yyyy
				//Continuosly writes yyyy values to the specified RAM address(aaaaaaa).
				//Continuosly writes the 8 - Bit value specified by 'yy' to address aaaaaaa.
				RAMAddress = RAMAddress.Remove(0, 1);
				byteSize = 32;
			}
			else if (RAMAddress.StartsWith("9") == true)
			{
				//Change Encryption Seeds
				//9yyyyyyy yyyy
				//(When 1st Code Only!)
				//Works like the DEADFACE on GSA.Changes the encryption seeds used for the rest of the codes.
				MessageBox.Show("Sorry, this tool does not support 9 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				byteSize = 32;
				blnUnhandled = true;
				return;
			}
			else if (RAMAddress.StartsWith("A") == true)
			{
				//16 - Bit 'If Not Equal' Activator
				//Axxxxxxx yyyy
				//Basicly the opposite of an 'If Equal To' Activator.Activates the code on the next line if address xxxxxxx is NOT equal to yyyy
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				byteSize = 32;
				return;

			}
			else if (RAMAddress.StartsWith("D00000") == true)
			{
				//16 - Bit Conditional RAM Write
				//D00000xx yyyy
				//No Description available at this time.
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				blnUnhandled = true;
				byteSize = 32;
				return;
			}
			return;
		}
		public void AddGBA()
		{
			if (byteSize == 8)
			{
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
			}
			else if (byteSize == 16)
			{
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
			}
			else if (byteSize == 32)
			{
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
			}
		}
		private void GEN()
		{
			//Game Genie only, for now.
			//This applies to the Game Genie
			if (SingleCheat.Length == 9 && SingleCheat.Contains("-"))
			{
				if (SingleCheat.IndexOf("-") != 4)
				{
					MessageBox.Show("All Genesis Game Genie Codes need to contain a dash after the fourth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				if (SingleCheat.Contains("I") == true | SingleCheat.Contains("O") == true | SingleCheat.Contains("Q") == true | SingleCheat.Contains("U") == true)
				{
					MessageBox.Show("All Genesis Game Genie Codes do not use I, O, Q or U.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				//This is taken from the GenGameGenie.CS file.
				string code = SingleCheat;
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
					long y;
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
				var watch = Watch.GenerateWatch(MemoryDomains["MD CART"], long.Parse(address, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
				//Add Cheat
				Global.CheatList.Add(new Cheat(watch, val));
			}
			//Action Replay?
			if (SingleCheat.Contains(":"))
			{
				//We start from Zero.
				if (SingleCheat.IndexOf(":") != 6)
				{
					MessageBox.Show("All Genesis Action Replay/Pro Action Replay Codes need to contain a colon after the sixth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				//Problem: I don't know what the Non-FF Style codes are.
				//TODO: Fix that.
				if (SingleCheat.StartsWith("FF") == false)
				{
					MessageBox.Show("This Action Replay Code, is not understood by this tool.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				//Now to do some work.
				//Determine Length, to Determine Byte Size

				parseString = SingleCheat.Remove(0, 2);
				switch (SingleCheat.Length)
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
						var watch = Watch.GenerateWatch(MemoryDomains["68K RAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
						//Take Watch, Add our Value we want, and it should be active when addded?
						Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
					}
					if (byteSize == 2)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["68K RAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
						//Take Watch, Add our Value we want, and it should be active when addded?
						Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
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
			if (SingleCheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All N64 GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (SingleCheat.Length != 13)
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
			parseString = SingleCheat.Remove(0, 2);
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
					var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, true, txtDescription.Text);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 16)
				{
					//We have a Word (Double Byte) sized Value
					var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
			}
			//Someone broke the world?
			catch (Exception ex)
			{
				MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void NES()
		{
			string strCompare = null;
			//Original code from adelikat
			if (SingleCheat.Length != 6 || SingleCheat.Length != 8)
			{
				int Value = 0;
				int Address = 0x8000;
				int x;
				int Compare = 0;
				// char 3 bit 3 denotes the code length.
				string code = SingleCheat;
				if (SingleCheat.Length == 6)
				{
					// Char # |   1   |   2   |   3   |   4   |   5   |   6   |
					// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
					// maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|5|E|F|G|

					_NESgameGenieTable.TryGetValue(code[0], out x);
					Value |= x & 0x07;
					Value |= (x & 0x08) << 4;

					_NESgameGenieTable.TryGetValue(code[1], out x);
					Value |= (x & 0x07) << 4;
					Address |= (x & 0x08) << 4;

					_NESgameGenieTable.TryGetValue(code[2], out x);
					Address |= (x & 0x07) << 4;

					_NESgameGenieTable.TryGetValue(code[3], out x);
					Address |= (x & 0x07) << 12;
					Address |= x & 0x08;

					_NESgameGenieTable.TryGetValue(code[4], out x);
					Address |= x & 0x07;
					Address |= (x & 0x08) << 8;

					_NESgameGenieTable.TryGetValue(code[5], out x);
					Address |= (x & 0x07) << 8;
					Value |= x & 0x08;
					RAMAddress = string.Format("{0:X4}", Address);
					RAMValue = string.Format("{0:X2}", Value);
					strCompare = string.Format("{0:X2}", Compare);
				}
				else if (SingleCheat.Length == 8)
				{
					// Char # |   1   |   2   |   3   |   4   |   5   |   6   |   7   |   8   |
					// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
					// maps to|1|6|7|8|H|2|3|4|-|I|J|K|L|A|B|C|D|M|N|O|%|E|F|G|!|^|&|*|5|@|#|$|
					Value = 0;
					Address = 0x8000;
					Compare = 0;


					_NESgameGenieTable.TryGetValue(code[0], out x);
					Value |= x & 0x07;
					Value |= (x & 0x08) << 4;

					_NESgameGenieTable.TryGetValue(code[1], out x);
					Value |= (x & 0x07) << 4;
					Address |= (x & 0x08) << 4;

					_NESgameGenieTable.TryGetValue(code[2], out x);
					Address |= (x & 0x07) << 4;

					_NESgameGenieTable.TryGetValue(code[3], out x);
					Address |= (x & 0x07) << 12;
					Address |= x & 0x08;

					_NESgameGenieTable.TryGetValue(code[4], out x);
					Address |= x & 0x07;
					Address |= (x & 0x08) << 8;

					_NESgameGenieTable.TryGetValue(code[5], out x);
					Address |= (x & 0x07) << 8;
					Compare |= x & 0x08;

					_NESgameGenieTable.TryGetValue(code[6], out x);
					Compare |= x & 0x07;
					Compare |= (x & 0x08) << 4;

					_NESgameGenieTable.TryGetValue(code[7], out x);
					Compare |= (x & 0x07) << 4;
					Value |= x & 0x08;
					RAMAddress = string.Format("{0:X4}", Address);
					RAMValue = string.Format("{0:X2}", Value);
					strCompare = string.Format("{0:X2}", Compare);
				}
			}
			if (SingleCheat.Length != 6 && SingleCheat.Length != 8)
			{
				//Not a proper Code
				MessageBox.Show("Game Genie codes need to be six or eight characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			try
			{

				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.
				//We have a Byte sized value

				var description = SingleCheat;
				if (!string.IsNullOrWhiteSpace(txtDescription.Text))
				{
					description = txtDescription.Text;
				}

				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, description);
				//Take Watch, Add our Value we want, and it should be active when addded?
				if (strCompare == "00")
				{
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (strCompare != "00")
				{
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber), int.Parse(strCompare, NumberStyles.HexNumber)));
				}
				//Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber), )
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
			if (SingleCheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All PSX GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (SingleCheat.Length != 13)
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
			parseString = SingleCheat.Remove(0, 2);
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
					var watch = Watch.GenerateWatch(MemoryDomains["MainRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 16)
				{
					//We have a Word (Double Byte) sized Value
					var watch = Watch.GenerateWatch(MemoryDomains["MainRAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
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
			if (SingleCheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All Saturn GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (SingleCheat.Length != 13)
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
			parseString = SingleCheat.Remove(0, 2);
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
					var watch = Watch.GenerateWatch(MemoryDomains["Work Ram High"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, true, txtDescription.Text);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 16)
				{
					//We have a Word (Double Byte) sized Value
					var watch = Watch.GenerateWatch(MemoryDomains["Work Ram High"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
					//Take Watch, Add our Value we want, and it should be active when addded?
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
			}
			//Someone broke the world?
			catch (Exception ex)
			{
				MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		//This also handles Game Gear due to shared hardware.  Go figure.
		private void SMS()
		{
			string RAMCompare = null;
			//Game Genie
			if (SingleCheat.LastIndexOf("-") == 7 && SingleCheat.IndexOf("-") == 3)
			{
				int val = 0;
				int add = 0;
				int cmp = 0;
				parseString = SingleCheat.Replace("-", "");
				GBGGDecode(parseString, ref val, ref add, ref cmp);
				RAMAddress = string.Format("{0:X4}", add);
				RAMValue = string.Format("{0:X2}", val);
				RAMCompare = string.Format("{0:X2}", cmp);
			}
			//Action Replay
			else if (SingleCheat.IndexOf("-") == 3 && SingleCheat.Length == 9)
			{
				parseString = SingleCheat;
				parseString = parseString.Remove(0, 2);
				RAMAddress = parseString.Remove(4, 2);
				RAMAddress = RAMAddress.Replace("-", "");
				RAMValue = parseString.Remove(0, 5);
			}
			//It's an Action Replay
			if (SingleCheat.Length != 9 && SingleCheat.LastIndexOf("-") != 7)
			{
				MessageBox.Show("All Master System Action Replay Codes need to be nine charaters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//Game Genie
			else if (SingleCheat.LastIndexOf("-") != 7 && SingleCheat.IndexOf("-") != 3)
			{
				MessageBox.Show("All Master System Game Geneie Codes need to have a dash after the third character and seventh character.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
				var watch = Watch.GenerateWatch(MemoryDomains["Main RAM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
				//Take Watch, Add our Value we want, and it should be active when addded

				if (RAMCompare == null)
				{
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				else if (RAMCompare != null)
				{
					//We have a Compare
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber), int.Parse(RAMCompare, NumberStyles.HexNumber)));
				}
			}
			//Someone broke the world?
			catch (Exception ex)
			{
				MessageBox.Show("An Error occured: " + ex.GetType().ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		//Original code from adelikat
		private void SnesGGDecode(string code, ref int val, ref int add)
		{
			// Code: D F 4 7 0 9 1 5 6 B C 8 A 2 3 E
			// Hex:  0 1 2 3 4 5 6 7 8 9 A B C D E F
			// XXYY-YYYY, where XX is the value, and YY-YYYY is the address.
			// Char # |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
			// Bit  # |3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|3|2|1|0|
			// maps to|     Value     |i|j|k|l|q|r|s|t|o|p|a|b|c|d|u|v|w|x|e|f|g|h|m|n|
			// order  |     Value     |a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|
			int x;

			// Getting Value
			if (code.Length > 0)
			{
				_SNESgameGenieTable.TryGetValue(code[0], out x);
				val = x << 4;
			}

			if (code.Length > 1)
			{
				_SNESgameGenieTable.TryGetValue(code[1], out x);
				val |= x;
			}

			// Address
			if (code.Length > 2)
			{
				_SNESgameGenieTable.TryGetValue(code[2], out x);
				add = x << 12;
			}

			if (code.Length > 3)
			{
				_SNESgameGenieTable.TryGetValue(code[3], out x);
				add |= x << 4;
			}

			if (code.Length > 4)
			{
				_SNESgameGenieTable.TryGetValue(code[4], out x);
				add |= (x & 0xC) << 6;
				add |= (x & 0x3) << 22;
			}

			if (code.Length > 5)
			{
				_SNESgameGenieTable.TryGetValue(code[5], out x);
				add |= (x & 0xC) << 18;
				add |= (x & 0x3) << 2;
			}

			if (code.Length > 6)
			{
				_SNESgameGenieTable.TryGetValue(code[6], out x);
				add |= (x & 0xC) >> 2;
				add |= (x & 0x3) << 18;
			}

			if (code.Length > 7)
			{
				_SNESgameGenieTable.TryGetValue(code[7], out x);
				add |= (x & 0xC) << 14;
				add |= (x & 0x3) << 10;
			}
		}
		private void SNES()
		{
			Boolean GameGenie = false;
			if (SingleCheat.Contains("-") && SingleCheat.Length == 9)
			{
				int val = 0, add = 0;
				string input;
				//We have to remove the - since it will cause issues later on.
				input = SingleCheat.Replace("-", "");
				SnesGGDecode(input, ref val, ref add);
				RAMAddress = string.Format("{0:X6}", add);
				RAMValue = string.Format("{0:X2}", val);
				//We trim the first value here to make it work.
				RAMAddress = RAMAddress.Remove(0, 1);
				//Note, it's not actually a byte, but a Word.  However, we are using this to keep from repeating code.
				byteSize = 8;
				GameGenie = true;

			}
			//This ONLY applies to Action Replay.
			if (SingleCheat.Length == 8)
			{
				//Sample Code:
				//7E18A428
				//Address: 7E18A4
				//Value: 28
				//Remove last two octets
				RAMAddress = SingleCheat.Remove(6, 2);
				//Get RAM Value
				RAMValue = SingleCheat.Remove(0, 6);
				//Note, it's a Word.  However, we are using this to keep from repeating code.
				byteSize = 16;
			}
			if (SingleCheat.Contains("-") && SingleCheat.Length != 9)
			{
				MessageBox.Show("Game Genie Codes need to be nine characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			if (SingleCheat.Length != 9 && SingleCheat.Length != 8)
			{
				MessageBox.Show("Pro Action Replay Codes need to be eight characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			try
			{
				//A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				//Action Replay
				if (byteSize == 16)
				{
					//System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
					var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
				}
				if (byteSize == 8)
				{
					//Is this correct?
					if (GameGenie == true)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["CARTROM"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
						Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
					}
					else if (GameGenie == false)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(RAMAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
						Global.CheatList.Add(new Cheat(watch, int.Parse(RAMValue, NumberStyles.HexNumber)));
					}
				}
				//Take Watch, Add our Value we want, and it should be active when addded?
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
			DialogResult result = MessageBox.Show("Are you sure you want to clear this form?", "Clear Form", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				txtDescription.Clear();
				txtCheat.Clear();
			}
		}

		private void GameShark_Load(object sender, EventArgs e)
		{
			//TODO?
			//Add special handling for cores that need special things?
			//GBA may need a special thing.
		}
	}
}