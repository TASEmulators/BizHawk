using System;
using System.Globalization;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.Common.cheats;

// TODO:
// Add Support/Handling for The Following Systems and Devices:
// GBA: Code Breaker (That uses unique Encryption keys)
// NES: Pro Action Rocky  (When someone asks)
// SNES: GoldFinger (Action Replay II) Support?

// Clean up the checks to be more robust/less "hacky"
// They work but feel bad

// Verify all wording in the error reports
namespace BizHawk.Client.EmuHawk
{
	[Tool(true, new[] { "GB", "GBA", "GEN", "N64", "NES", "PSX", "SAT", "SMS", "SNES" }, new[] { "Snes9x" })]
	public partial class GameShark : Form, IToolForm, IToolFormAutoConfig
	{
		[RequiredService]
		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		private IEmulator Emulator { get; set; }

		public GameShark()
		{
			InitializeComponent();
		}

		public bool UpdateBefore => true;
		public bool AskSaveChanges() => true;

		public void FastUpdate()
		{
		}

		public void Restart()
		{
		}

		public void NewUpdate(ToolFormUpdateType type)
		{
		}

		public void UpdateValues()
		{
		}

		private string _parseString;
		private string _ramAddress;
		private string _ramValue;
		private int _byteSize;
		private string _testo;
		private string _singleCheat;
		private int _loopValue;

		private void Go_Click(object sender, EventArgs e)
		{
			// Reset Variables
			_parseString = null;
			_ramAddress = null;
			_ramValue = null;
			_byteSize = 0;

			_singleCheat = null;
			for (int i = 0; i < txtCheat.Lines.Length; i++)
			{
				_loopValue = i;
				_singleCheat = txtCheat.Lines[i].ToUpper();

				switch (Emulator.SystemId)
				{
					case "GB":
						GameBoy();
						break;
					case "GBA":
						GBA();
						break;
					case "GEN":
						Gen();
						break;
					case "N64":
						// This determines what kind of Code we have
						_testo = _singleCheat.Remove(2, 11);
						N64();
						break;
					case "NES":
						Nes();
						break;
					case "PSX":
						// This determines what kind of Code we have
						_testo = _singleCheat.Remove(2, 11);
						Psx();
						break;
					case "SAT":
						// This determines what kind of Code we have
						_testo = _singleCheat.Remove(2, 11);
						Saturn();
						break;
					case "SMS":
						Sms();
						break;
					case "SNES":
						Snes();
						break;
				}
			}

			txtCheat.Clear();
			txtDescription.Clear();
		}

		private void GameBoy()
		{
			string ramCompare = null;

			// Game Genie
			if (_singleCheat.LastIndexOf("-") == 7 && _singleCheat.IndexOf("-") == 3)
			{
				var decoder = new GbGgGameGenieDecoder(_singleCheat);
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], decoder.Address, WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(decoder.Compare.HasValue
					? new Cheat(watch, decoder.Value, decoder.Compare)
					: new Cheat(watch, decoder.Value));
			}
			else if (_singleCheat.Contains("-") && _singleCheat.LastIndexOf("-") != 7 && _singleCheat.IndexOf("-") != 3)
			{
				MessageBox.Show("All GameBoy Game Genie Codes need to have a dash after the third character and seventh character.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Game Shark codes
			if (_singleCheat.Length != 8 && _singleCheat.Contains("-") == false)
			{
				MessageBox.Show("All GameShark Codes need to be Eight characters in Length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (_singleCheat.Length == 8 && _singleCheat.Contains("-") == false)
			{
				_testo = _singleCheat.Remove(2, 6);
				switch (_testo)
				{
					case "00":
					case "01":
						break;
					default:
						MessageBox.Show("All GameShark Codes for GameBoy need to start with 00 or 01", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
				}

				// Sample Input for GB/GBC:
				// 010FF6C1
				// Becomes:
				// Address C1F6
				// Value 0F
				_parseString = _singleCheat.Remove(0, 2);

				// Now we need to break it down a little more.
				_ramValue = _parseString.Remove(2, 4);
				_parseString = _parseString.Remove(0, 2);

				// The issue is Endian...  Time to get ultra clever.  And Regret it.
				// First Half
				_ramAddress = _parseString.Remove(0, 2);
				_ramAddress = _ramAddress + _parseString.Remove(2, 2);
			}

			// This part, is annoying...
			try
			{
				// A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				// System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				
				// Take Watch, Add our Value we want, and it should be active when added?
				Global.CheatList.Add(ramCompare == null
					? new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber))
					: new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber), int.Parse(ramCompare, NumberStyles.HexNumber)));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		// Provided by mGBA and endrift
		private readonly uint[] _gbaGameSharkSeeds = { 0x09F4FBBDU, 0x9681884AU, 0x352027E9U, 0xF3DEE5A7U };
		private readonly uint[] _gbaProActionReplaySeeds = { 0x7AA9648FU, 0x7FAE6994U, 0xC0EFAAD5U, 0x42712C57U };

		// blnEncrypted is used to see if the previous line for Slide code was encrypted or not.
		private bool _blnEncrypted;

		// blnGameShark means, "This is a Game Shark/Action Replay (Not MAX) code."
		private bool _blnGameShark;

		// blnActionReplayMax means "This is an Action Replay MAX code."
		private bool _blnActionReplayMax;

		// blnCodeBreaker means "This is a CodeBreaker code."
		// Boolean blnCodeBreaker = false;
		// blnUnhandled means "BizHawk can't do this one or the tool can't."
		private bool _blnUnhandled;

		// blnUnneeded means "You don't need this code."
		private bool _blnUnneeded;

		private void GBA()
		{
			// Super Ultra Mega HD BizHawk GameShark/Action Replay/Code Breaker Final Hyper Edition Arcade Remix EX + α GBA Code detection method.
			// Seriously, it's that complex.
			_blnEncrypted = false;

			// Check Game Shark/Action Replay (Not Max) Codes
			if (_singleCheat.Length == 17 && _singleCheat.IndexOf(" ") == 8)
			{
				// blnNoCode = true;
				// Super Ultra Mega HD BizHawk GameShark/Action Replay/Code Breaker Final Hyper Edition Arcade Remix EX + α GBA Code detection method.
				// Seriously, it's that complex.

				// Check Game Shark/Action Replay (Not Max) Codes
				if (_singleCheat.Length == 17 && _singleCheat.IndexOf(" ") == 8)
				{
					// These are for the Decryption Values for GameShark and Action Replay MAX.
					uint op1;
					uint op2;
					uint sum = 0xC6EF3720;

					// Let's get the stuff separated.
					_ramAddress = _singleCheat.Remove(8, 9);
					_ramValue = _singleCheat.Remove(0, 9);

					// Let's see if this code matches the GameShark.
					GBAGameShark();

					if (_blnUnneeded)
					{
						return;
					}

					if (_blnUnhandled)
					{
						return;
					}

					if (!_blnGameShark)
					{
						// We don't have a GameShark code, or we have an encrypted code?
						// Further testing required.
						// GameShark Decryption Method
						_parseString = _singleCheat;

						op1 = uint.Parse(_parseString.Remove(8, 9), NumberStyles.HexNumber);
						op2 = uint.Parse(_parseString.Remove(0, 9), NumberStyles.HexNumber);

						// Tiny Encryption Algorithm
						for (int i = 0; i < 32; ++i)
						{
							op2 -= ((op1 << 4) + _gbaGameSharkSeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + _gbaGameSharkSeeds[3]);
							op1 -= ((op2 << 4) + _gbaGameSharkSeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + _gbaGameSharkSeeds[1]);
							sum -= 0x9E3779B9;
						}

						// op1 has the Address
						// op2 has the Value
						// Sum, is pointless?
						_ramAddress = $"{op1:X8}";
						_ramValue = $"{op2:X8}";
						GBAGameShark();
					}

					// We don't do Else If after the if here because it won't allow us to verify the second code check.
					if (_blnGameShark)
					{
						// We got a Valid GameShark Code.  Hopefully.
						AddGBA();
						return;
					}

					// We are going to assume that we got an Action Replay MAX code, or at least try to guess that we did.
					GbaActionReplay();

					if (_blnActionReplayMax == false)
					{
						// Action Replay Max decryption Method
						_parseString = _singleCheat;
						sum = 0xC6EF3720;
						op1 = uint.Parse(_parseString.Remove(8, 9), NumberStyles.HexNumber);
						op2 = uint.Parse(_parseString.Remove(0, 9), NumberStyles.HexNumber);

						// Tiny Encryption Algorithm
						int j;
						for (j = 0; j < 32; ++j)
						{
							op2 -= ((op1 << 4) + _gbaProActionReplaySeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + _gbaProActionReplaySeeds[3]);
							op1 -= ((op2 << 4) + _gbaProActionReplaySeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + _gbaProActionReplaySeeds[1]);
							sum -= 0x9E3779B9;
						}

						// op1 has the Address
						// op2 has the Value
						// Sum, is pointless?
						_ramAddress = $"{op1:X8}";
						_ramValue = $"{op2:X8}";
						_blnEncrypted = true;
						GbaActionReplay();
					}

					// MessageBox.Show(blnActionReplayMax.ToString());
					// We don't do Else If after the if here because it won't allow us to verify the second code check.
					if (_blnActionReplayMax)
					{
						// We got a Valid Action Replay Max Code.  Hopefully.
						AddGBA();
						return;
					}
				}

				// Detect CodeBreaker/GameShark SP/Xploder codes
				if (_singleCheat.Length == 12 && _singleCheat.IndexOf(" ") != 8)
				{
					MessageBox.Show("Codebreaker/GameShark SP/Xploder codes are not supported by this tool.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;

#if false
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
					//GBACodeBreaker();

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
						RAMAddress = $"{op1:X8}";
						//RAMAddress = RAMAddress.Remove(0, 1);
						RAMValue = $"{op2:X8}";
						// && RAMAddress[6] == '0'
					}

					if (blnCodeBreaker)
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
#endif
				}
			}
		}

		public void GBAGameShark()
		{
			// This is for the Game Shark/Action Replay (Not Max)
			if (_ramAddress.StartsWith("0") && _ramValue.StartsWith("000000"))
			{
				// 0aaaaaaaa 000000xx  1 Byte Constant Write
				// 1 Byte Size Value
				_byteSize = 8;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("1") && _ramValue.StartsWith("0000"))
			{
				// 1aaaaaaaa 0000xxxx  2 Byte Constant Write
				// 2 Byte Size Value
				_byteSize = 16;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("2"))
			{
				// 2aaaaaaaa xxxxxxxx  4 Byte Constant Write
				// 4 Byte Size Value
				_byteSize = 32;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("3000"))
			{
				// 3000cccc xxxxxxxx aaaaaaaa  4 Byte Group Write  What?  Is this like a Slide Code
				// 4 Byte Size Value
				// Sample
				// 30000004 01010101 03001FF0 03001FF4 03001FF8 00000000
				// write 01010101 to 3 addresses - 01010101, 03001FF0, 03001FF4, and 03001FF8. '00000000' is used for padding, to ensure the last code encrypts correctly.
				// Note: The device improperly writes the Value, to the address.  We should ignore that.
				MessageBox.Show("Sorry, this tool does not support 3000XXXX codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("6") && _ramValue.StartsWith("0000"))
			{
				// 6aaaaaaa 0000xxxx  2 Byte ROM patch
				_byteSize = 16;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("6") && _ramValue.StartsWith("1000"))
			{
				// 6aaaaaaa 1000xxxx  4 Byte ROM patch
				_byteSize = 32;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("6") && _ramValue.StartsWith("2000"))
			{
				// 6aaaaaaa 2000xxxx  8 Byte ROM patch
				_byteSize = 32;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("8") && _ramAddress[2] == '1' && _ramValue.StartsWith("00"))
			{
				// 8a1aaaaa 000000xx  1 Byte Write when GS Button Pushed
				// Treat as Constant Write.
				_byteSize = 8;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("8") && _ramAddress[2] == '2' && _ramValue.StartsWith("00"))
			{
				// 8a2aaaaa 000000xx  2 Byte Write when GS Button Pushed
				// Treat as Constant Write.
				_byteSize = 16;
				_blnGameShark = true;
			}
			else if (_ramAddress.StartsWith("80F00000"))
			{
				// 80F00000 0000xxxx  Slow down when GS Button Pushed.
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
			}
			else if (_ramAddress.StartsWith("D") && !_ramAddress.StartsWith("DEADFACE") && _ramValue.StartsWith("0000"))
			{
				// Daaaaaaa 0000xxxx  2 Byte If Equal, Activate next code
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("E0"))
			{
				// E0zzxxxx aaaaaaaa  2 Byte if Equal, Activate ZZ Lines.
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("F") && _ramValue.StartsWith("0000"))
			{
				// Faaaaaaa 0000xxxx  Hook Routine.  Probably not necessary?
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
			}
			else if (_ramAddress.StartsWith("001DC0DE"))
			{
				// xxxxxxxx 001DC0DE  Auto-Detect Game.  Useless for Us.
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
			}
			else if (_ramAddress.StartsWith("DEADFACE"))
			{
				// DEADFACE 0000xxxx  Change Encryption Seeds.  Unsure how this works.
				MessageBox.Show("Sorry, this tool does not support DEADFACE codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else
			{
				_blnGameShark = false;
			}

			// It's not a GameShark/Action Replay (Not MAX) code, so we say False.
		}

		public void GbaActionReplay()
		{
			// These checks are done on the Decrypted code, not the Encrypted one.
			// ARM Code Types
			// 1) Normal RAM Write Codes
			if (_ramAddress.StartsWith("002"))
			{
				// 0022 Should be Changed to 020
				// Fill area (XXXXXXXX) to (XXXXXXXX+YYYYYY) with Byte ZZ.
				// XXXXXXXX
				// YYYYYYZZ
				_ramAddress = _ramAddress.Replace("002", "020");
				_blnActionReplayMax = true;
				_byteSize = 8;
			}
			else if (_ramAddress.StartsWith("022"))
			{
				// 0222 Should be Changed to 020
				// Fill area (XXXXXXXX) to (XXXXXXXX+YYYY*2) with Halfword ZZZZ.
				// XXXXXXXX
				// YYYYZZZZ
				_ramAddress = _ramAddress.Replace("022", "020");
				_blnActionReplayMax = true;
				_byteSize = 16;
			}
			else if (_ramAddress.StartsWith("042"))
			{
				// 0422 Should be Changed to 020
				// Write the Word ZZZZZZZZ to address XXXXXXXX.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("042", "020");
				_blnActionReplayMax = true;
				_byteSize = 32;
			}
			// 2) Pointer RAM Write Codes
			else if (_ramAddress.StartsWith("402"))
			{
				// 4022 Should be Changed to 020
				// Writes Byte ZZ to ([the address kept in XXXXXXXX]+[YYYYYY]).
				// XXXXXXXX
				// YYYYYYZZ
				_ramAddress = _ramAddress.Replace("402", "020");
				_blnActionReplayMax = true;
				_byteSize = 8;
			}
			else if (_ramAddress.StartsWith("420"))
			{
				// 4202 Should be Changed to 020
				// Writes Halfword ZZZZ ([the address kept in XXXXXXXX]+[YYYY*2]).
				// XXXXXXXX
				// YYYYZZZZ
				_ramAddress = _ramAddress.Replace("420", "020");
				_blnActionReplayMax = true;
				_byteSize = 16;
			}
			else if (_ramAddress.StartsWith("422"))
			{
				// 442 Should be Changed to 020
				// Writes the Word ZZZZZZZZ to [the address kept in XXXXXXXX].
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("422", "020");
				_blnActionReplayMax = true;
				_byteSize = 32;
			}
			// 3) Add Codes
			else if (_ramAddress.StartsWith("802"))
			{
				// 802 Should be Changed to 020
				// Add the Byte ZZ to the Byte stored in XXXXXXXX.
				// XXXXXXXX
				// 000000ZZ
				//RAMAddress = RAMAddress.Replace("8022", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("822") && !_ramAddress.StartsWith("8222"))
			{
				// 822 Should be Changed to 020
				// Add the Halfword ZZZZ to the Halfword stored in XXXXXXXX.
				// XXXXXXXX
				// 0000ZZZZ
				//RAMAddress = RAMAddress.Replace("822", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("842"))
			{
				// NOTE!
				// This is duplicated below with different function results.
				// 842 Should be Changed to 020
				// Add the Word ZZZZ to the Halfword stored in XXXXXXXX.
				// XXXXXXXX
				// ZZZZZZZZ
				//RAMAddress = RAMAddress.Replace("842", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}

			// 4) Write to $4000000 (IO Registers!)
			else if (_ramAddress.StartsWith("C6000130"))
			{
				// C6000130 Should be Changed to 00000130
				// Write the Halfword ZZZZ to the address $4XXXXXX
				// 00XXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("C6000130", "00000130");
				_blnActionReplayMax = true;
				_byteSize = 16;
			}
			else if (_ramAddress.StartsWith("C7000130"))
			{
				// C7000130 Should be Changed to 00000130
				// Write the Word ZZZZZZZZ to the address $4XXXXXX
				// 00XXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("C7000130", "00000130");
				_blnActionReplayMax = true;
				_byteSize = 32;
			}
			// 5) If Equal Code (= Joker Code)
			else if (_ramAddress.StartsWith("082"))
			{
				// 082 Should be Changed to 020
				// If Byte at XXXXXXXX = ZZ then execute next code.
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("082", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("482"))
			{
				// 482 Should be Changed to 020
				// If Byte at XXXXXXXX = ZZ then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("482", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("882"))
			{
				// 882 Should be Changed to 020
				// If Byte at XXXXXXXX = ZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("882", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("C82"))
			{
				// C82 Should be Changed to 020
				// While Byte at XXXXXXXX <> ZZ turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("C82", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("0A2"))
			{
				// 0A2 Should be Changed to 020
				// If Halfword at XXXXXXXX = ZZZZ then execute next code.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("0A2", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("4A2"))
			{
				// 4A2 Should be Changed to 020
				// If Halfword at XXXXXXXX = ZZZZ then execute next 2 codes.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("4A2", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("8A2"))
			{
				// 8A2 Should be Changed to 020
				// If Halfword at XXXXXXXX = ZZZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("8A2", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("CA2"))
			{
				// CA2 Should be Changed to 020
				// While Halfword at XXXXXXXX <> ZZZZ turn off all codes.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("CA2", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("0C2"))
			{
				// 0C2 Should be Changed to 020
				// If Word at XXXXXXXX = ZZZZZZZZ then execute next code.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("0C2", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("4C2"))
			{
				// 4C2 Should be Changed to 020
				// If Word at XXXXXXXX = ZZZZZZZZ then execute next 2 codes.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("4C2", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("8C2"))
			{
				// 8C2 Should be Changed to 020
				// If Word at XXXXXXXX = ZZZZZZZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("8C2", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("CC2"))
			{
				// CC2 Should be Changed to 020
				// While Word at XXXXXXXX <> ZZZZZZZZ turn off all codes.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("CC2", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 6) If Different Code
			else if (_ramAddress.StartsWith("102"))
			{
				// 102 Should be Changed to 020
				// If Byte at XXXXXXXX <> ZZ then execute next code.
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("102", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("502"))
			{
				// 502 Should be Changed to 020
				// If Byte at XXXXXXXX <> ZZ then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("502", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("902"))
			{
				// 902 Should be Changed to 020
				// If Byte at XXXXXXXX <> ZZ execute all the codes below this one in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("902", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("D02"))
			{
				// D02 Should be Changed to 020
				// While Byte at XXXXXXXX = ZZ turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				_ramAddress = _ramAddress.Replace("D02", "020");
				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("122"))
			{
				// 122 Should be Changed to 020
				// If Halfword at XXXXXXXX <> ZZZZ then execute next code.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("122", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("522"))
			{
				// 522 Should be Changed to 020
				// If Halfword at XXXXXXXX <> ZZZZ then execute next 2 codes.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("522", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("922"))
			{
				// 922 Should be Changed to 020
				// If Halfword at XXXXXXXX <> ZZZZ disable all the codes below this one.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("922", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("D22"))
			{
				// D22 Should be Changed to 020
				// While Halfword at XXXXXXXX = ZZZZ turn off all codes.
				// XXXXXXXX
				// 0000ZZZZ
				_ramAddress = _ramAddress.Replace("D22", "020");
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("142"))
			{
				// 142 Should be Changed to 020
				// If Word at XXXXXXXX <> ZZZZZZZZ then execute next code.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("142", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("542"))
			{
				// 542 Should be Changed to 020
				// If Word at XXXXXXXX <> ZZZZZZZZ then execute next 2 codes.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("542", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("942"))
			{
				// 942 Should be Changed to 020
				// If Word at XXXXXXXX <> ZZZZZZZZ disable all the codes below this one.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("942", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("D42"))
			{
				// D42 Should be Changed to 020
				// While Word at XXXXXXXX = ZZZZZZZZ turn off all codes.
				// XXXXXXXX
				// ZZZZZZZZ
				_ramAddress = _ramAddress.Replace("D42", "020");
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 7)
			// [If Byte at address XXXXXXXX is lower than ZZ] (signed) Code
			// Signed means : For bytes : values go from -128 to +127. For Halfword : values go from -32768/+32767. For Words : values go from -2147483648 to 2147483647. For exemple, for the Byte comparison, 7F (127) will be > to FF (-1).
			else if (_ramAddress.StartsWith("182"))
			{
				// 182 Should be Changed to 020
				// If ZZ > Byte at XXXXXXXX then execute next code.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("182"))
				{
					_ramAddress = _ramAddress.Replace("182", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("582"))
			{
				// 582 Should be Changed to 020
				// If ZZ > Byte at XXXXXXXX then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("582"))
				{
					_ramAddress = _ramAddress.Replace("582", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("982"))
			{
				// 982 Should be Changed to 020
				// If ZZ > Byte at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("982"))
				{
					_ramAddress = _ramAddress.Replace("982", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("D82") || _ramAddress.StartsWith("E82"))
			{
				// D82 or E82 Should be Changed to 020
				// While ZZ <= Byte at XXXXXXXX turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("D82"))
				{
					_ramAddress = _ramAddress.Replace("D82", "020");
				}
				else if (_ramAddress.StartsWith("E82"))
				{
					_ramAddress = _ramAddress.Replace("E82", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("1A2"))
			{
				// 1A2 Should be Changed to 020
				// If ZZZZ > Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("1A2"))
				{
					_ramAddress = _ramAddress.Replace("1A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("5A2"))
			{
				// 5A2  Should be Changed to 020
				// If ZZZZ > Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("5A2"))
				{
					_ramAddress = _ramAddress.Replace("5A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("9A2"))
			{
				// 9A2 Should be Changed to 020
				// If ZZZZ > Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX 0000ZZZZ 
				if (_ramAddress.StartsWith("9A2"))
				{
					_ramAddress = _ramAddress.Replace("9A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("DA2"))
			{
				// DA2 Should be Changed to 020
				// While ZZZZ <= Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("DA2"))
				{
					_ramAddress = _ramAddress.Replace("DA2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("1C2"))
			{
				// 1C2 or Should be Changed to 020
				// If ZZZZ > Word at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("1C2"))
				{
					_ramAddress = _ramAddress.Replace("1C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("5C2"))
			{
				// 5C2 Should be Changed to 020
				// If ZZZZ > Word at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("5C2"))
				{
					_ramAddress = _ramAddress.Replace("5C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("9C2"))
			{
				// 9C2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("9C2"))
				{
					_ramAddress = _ramAddress.Replace("9C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("DC2"))
			{
				// DC2 Should be Changed to 020
				// While ZZZZZZZZ <= Word at XXXXXXXX turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("DC2"))
				{
					_ramAddress = _ramAddress.Replace("DC2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 8)
			// [If Byte at address XXXXXXXX is higher than ZZ] (signed) Code
			// Signed means : For bytes : values go from -128 to +127. For Halfword : values go from -32768/+32767. For Words : values go from -2147483648 to 2147483647. For exemple, for the Byte comparison, 7F (127) will be > to FF (-1).
			else if (_ramAddress.StartsWith("202"))
			{
				// 202 Should be Changed to 020
				// If ZZ < Byte at XXXXXXXX then execute next code.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("202"))
				{
					_ramAddress = _ramAddress.Replace("202", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("602"))
			{
				// 602 Should be Changed to 020
				// If ZZ < Byte at XXXXXXXX then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("602"))
				{
					_ramAddress = _ramAddress.Replace("602", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("A02"))
			{
				// A02 Should be Changed to 020
				// If ZZ < Byte at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("A02"))
				{
					_ramAddress = _ramAddress.Replace("A02", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("E02"))
			{
				// E02 Should be Changed to 020
				// While ZZ => Byte at XXXXXXXX turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("E02"))
				{
					_ramAddress = _ramAddress.Replace("E02", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("222"))
			{
				// 222 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("222"))
				{
					_ramAddress = _ramAddress.Replace("222", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("622"))
			{
				// 622 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("622"))
				{
					_ramAddress = _ramAddress.Replace("622", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("A22"))
			{
				// A22 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("A22"))
				{
					_ramAddress = _ramAddress.Replace("A22", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("E22"))
			{
				// E22 Should be Changed to 020
				// While ZZZZ => Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("E22"))
				{
					_ramAddress = _ramAddress.Replace("E22", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("242"))
			{
				// 242 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("242"))
				{
					_ramAddress = _ramAddress.Replace("242", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("642"))
			{
				// 642 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("642"))
				{
					_ramAddress = _ramAddress.Replace("642", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("A42"))
			{
				// A42 Should be Changed to 020
				// If ZZZZ < Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("A42"))
				{
					_ramAddress = _ramAddress.Replace("A42", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("E42"))
			{
				// E42 Should be Changed to 020
				// While ZZZZ => Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX 0000ZZZZ
				if (_ramAddress.StartsWith("E42"))
				{
					_ramAddress = _ramAddress.Replace("E42", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 9)
			// [If Value at adress XXXXXXXX is lower than...] (unsigned) Code
			// Unsigned means : For bytes : values go from 0 to +255. For Halfword : values go from 0 to +65535. For Words : values go from 0 to 4294967295. For exemple, for the Byte comparison, 7F (127) will be < to FF (255).
			else if (_ramAddress.StartsWith("282"))
			{
				// 282 Should be Changed to 020
				// If ZZZZZZZZ > Byte at XXXXXXXX then execute next line.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("282"))
				{
					_ramAddress = _ramAddress.Replace("282", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("682"))
			{
				// 682 Should be Changed to 020
				// If ZZZZZZZZ > Byte at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("682"))
				{
					_ramAddress = _ramAddress.Replace("682", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("A82"))
			{
				// A82 Should be Changed to 020
				// If ZZZZZZZZ > Byte at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("A82"))
				{
					_ramAddress = _ramAddress.Replace("A82", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("2A2"))
			{
				// 2A2 Should be Changed to 020
				// If ZZZZZZZZ > Halfword at XXXXXXXX then execute next line.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("2A2"))
				{
					_ramAddress = _ramAddress.Replace("2A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("6A2"))
			{
				// 6A2 Should be Changed to 020
				// If ZZZZZZZZ > Halfword at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("6A2"))
				{
					_ramAddress = _ramAddress.Replace("6A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("AA2"))
			{
				// AA2 Should be Changed to 020
				// If ZZZZZZZZ > Halfword at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("AA2"))
				{
					_ramAddress = _ramAddress.Replace("AA2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("EA2"))
			{
				// EA2 Should be Changed to 020
				// While ZZZZZZZZ <= Halfword at XXXXXXXX turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("EA2"))
				{
					_ramAddress = _ramAddress.Replace("EA2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("2C2"))
			{
				// 2C2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute next line.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("2C2"))
				{
					_ramAddress = _ramAddress.Replace("2C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("6C2"))
			{
				// 6C2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("6C2"))
				{
					_ramAddress = _ramAddress.Replace("6C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("AC2"))
			{
				// AC2 Should be Changed to 020
				// If ZZZZZZZZ > Word at XXXXXXXX then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("AC2"))
				{
					_ramAddress = _ramAddress.Replace("AC2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("EC2"))
			{
				// EC2 Should be Changed to 020
				// While ZZZZZZZZ <= Word at XXXXXXXX turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("EC2"))
				{
					_ramAddress = _ramAddress.Replace("EC2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 10)
			// [If Value at address XXXXXXXX is higher than...] (unsigned) Code
			// Unsigned means For bytes : values go from 0 to +255. For Halfword : values go from 0 to +65535. For Words : values go from 0 to 4294967295. For exemple, for the Byte comparison, 7F (127) will be < to FF (255).
			else if (_ramAddress.StartsWith("302"))
			{
				// 302 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("302"))
				{
					_ramAddress = _ramAddress.Replace("302", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("702"))
			{
				// 702 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("702"))
				{
					_ramAddress = _ramAddress.Replace("702", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("B02"))
			{
				// B02 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("B02"))
				{
					_ramAddress = _ramAddress.Replace("B02", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("F02"))
			{
				// F02 Should be Changed to 020
				// If ZZZZZZZZ < Byte at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("F02"))
				{
					_ramAddress = _ramAddress.Replace("F02", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("322"))
			{
				// 322 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line.  
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("322"))
				{
					_ramAddress = _ramAddress.Replace("322", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("722"))
			{
				// 722 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("722"))
				{
					_ramAddress = _ramAddress.Replace("722", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("B22"))
			{
				// B22 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("B22"))
				{
					_ramAddress = _ramAddress.Replace("B22", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("F22"))
			{
				// F22 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("F22"))
				{
					_ramAddress = _ramAddress.Replace("F22", "020");
				}
				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("342"))
			{
				// 342 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line.  
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("342"))
				{
					_ramAddress = _ramAddress.Replace("342", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("742"))
			{
				// 742 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("742"))
				{
					_ramAddress = _ramAddress.Replace("742", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("B42"))
			{
				// B42 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("B42"))
				{
					_ramAddress = _ramAddress.Replace("B42", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("F42"))
			{
				// F42 Should be Changed to 020
				// If ZZZZZZZZ < Halfword at XXXXXXXX then execute next line..
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("F42"))
				{
					_ramAddress = _ramAddress.Replace("F42", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 11) If AND Code
			else if (_ramAddress.StartsWith("382"))
			{
				// 382 Should be Changed to 020
				// If ZZ AND Byte at XXXXXXXX <> 0 (= True) then execute next code.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("382"))
				{
					_ramAddress = _ramAddress.Replace("382", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("782"))
			{
				// 782 Should be Changed to 020
				// If ZZ AND Byte at XXXXXXXX <> 0 (= True) then execute next 2 codes.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("782"))
				{
					_ramAddress = _ramAddress.Replace("782", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("B82"))
			{
				// B82 Should be Changed to 020
				// If ZZ AND Byte at XXXXXXXX <> 0 (= True) then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("B82"))
				{
					_ramAddress = _ramAddress.Replace("B82", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("F82"))
			{
				// F82 Should be Changed to 020
				// While ZZ AND Byte at XXXXXXXX = 0 (= False) then turn off all codes.
				// XXXXXXXX
				// 000000ZZ
				if (_ramAddress.StartsWith("F82"))
				{
					_ramAddress = _ramAddress.Replace("F82", "020");
				}

				_byteSize = 8;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("3A2"))
			{
				// 3A2 Should be Changed to 020
				// If ZZZZ AND Halfword at XXXXXXXX <> 0 (= True) then execute next code.
				// XXXXXXXX
				// 0000ZZZZ
				if (_ramAddress.StartsWith("3A2"))
				{
					_ramAddress = _ramAddress.Replace("3A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("7A2"))
			{
				// 7A2 Should be Changed to 020
				// If ZZZZ AND Halfword at XXXXXXXX <> 0 (= True) then execute next 2 codes.
				// XXXXXXXX
				// 0000ZZZZ
				if (_ramAddress.StartsWith("7A2"))
				{
					_ramAddress = _ramAddress.Replace("7A2", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("3C2"))
			{
				// 3C2 Should be Changed to 020
				// If ZZZZZZZZ AND Word at XXXXXXXX <> 0 (= True) then execute next code.
				// XXXXXXXX
				// ZZZZZZZZ
				if (_ramAddress.StartsWith("3C2"))
				{
					_ramAddress = _ramAddress.Replace("3C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("7C2"))
			{
				// 7C2 Should be Changed to 020
				// If ZZZZZZZZ AND Word at XXXXXXXX <> 0 (= True) then execute next 2 codes.
				// XXXXXXXX
				// ZZZZZZZZ
				if (_ramAddress.StartsWith("7C2"))
				{
					_ramAddress = _ramAddress.Replace("7C2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("BC2"))
			{
				// BC2 Should be Changed to 020
				// If ZZZZZZZZ AND Word at XXXXXXXX <> 0 (= True) then execute all following codes in the same row (else execute none of the codes below).
				// XXXXXXXX
				// ZZZZZZZZ
				if (_ramAddress.StartsWith("BC2"))
				{
					_ramAddress = _ramAddress.Replace("BC2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("FC2"))
			{
				// FC2 Should be Changed to 020
				// While ZZZZZZZZ AND Word at XXXXXXXX = 0 (= False) then turn off all codes.
				// XXXXXXXX
				// ZZZZZZZZ
				if (_ramAddress.StartsWith("FC2"))
				{
					_ramAddress = _ramAddress.Replace("FC2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 12) "Always..." Codes
			// For the "Always..." codes: -XXXXXXXX can be any authorized address BUT 00000000 (use 02000000 if you don't know what to choose). -ZZZZZZZZ can be anything. -The "y" in the code data must be in the [1-7] range (which means not 0).
			else if (_ramAddress.StartsWith("0E2"))
			{
				// 0E2 Should be Changed to 020
				// Always skip next line.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("0E2"))
				{
					_ramAddress = _ramAddress.Replace("0E2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("4E2"))
			{
				// 4E2 Should be Changed to 020
				// Always skip next 2 lines.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("4E2"))
				{
					_ramAddress = _ramAddress.Replace("4E2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("8E2"))
			{
				// 8E2 Should be Changed to 020
				// Always Stops executing all the codes below.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("8E2"))
				{
					_ramAddress = _ramAddress.Replace("8E2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("CE2"))
			{
				// CE2 Should be Changed to 020
				// Always turn off all codes.
				// XXXXXXXX ZZZZZZZZ
				if (_ramAddress.StartsWith("CE2"))
				{
					_ramAddress = _ramAddress.Replace("CE2", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			// 13) 1 Line Special Codes (= starting with "00000000")
			// Special Slide Code Detection Method
			else if (_ramAddress.StartsWith("00000000") && _ramValue.StartsWith("8"))
			{
				// Code Sample
				// 00000000 82222C96
				// 00000063 0032000C

				// This says:
				// Write to Address 2222C96
				// The value 63
				// Fifty (50) times (32 in Hex)
				// Increment the Address by 12 (Hex)

				// Sample Compare:
				// 00222696 00000063  Unit 1 HP
				// 002226A2 00000063  Unit 2 HP
				// Goes up by C (12)

				// Parse and go
				// We reset RAM Address to the RAM Value, because the Value holds the address on the first line.
				string realAddress = _ramValue.Remove(0, 1);

				// We need the next line
				try
				{
					_loopValue += 1;
					_singleCheat = txtCheat.Lines[_loopValue].ToUpper();

					// We need to parse now.
					if (_singleCheat.Length == 17 && _singleCheat.IndexOf(" ") == 8)
					{
						if (_blnEncrypted)
						{
							// The code was Encrypted
							// Decrypt before we do stuff.
							// Action Replay Max decryption Method
							_parseString = _singleCheat;
							var sum = 0xC6EF3720;
							var op1 = uint.Parse(_parseString.Remove(8, 9), NumberStyles.HexNumber);
							var op2 = uint.Parse(_parseString.Remove(0, 9), NumberStyles.HexNumber);
							
							// Tiny Encryption Algorithm
							int j;
							for (j = 0; j < 32; ++j)
							{
								op2 -= ((op1 << 4) + _gbaProActionReplaySeeds[2]) ^ (op1 + sum) ^ ((op1 >> 5) + _gbaProActionReplaySeeds[3]);
								op1 -= ((op2 << 4) + _gbaProActionReplaySeeds[0]) ^ (op2 + sum) ^ ((op2 >> 5) + _gbaProActionReplaySeeds[1]);
								sum -= 0x9E3779B9;
							}

							// op1 has the Address
							// op2 has the Value
							// Sum, is pointless?
							_ramAddress = $"{op1:X8}";
							_ramValue = $"{op2:X8}";
						}
						else if (_blnEncrypted == false)
						{
							_ramAddress = _singleCheat.Remove(8, 9);
							_ramValue = _singleCheat.Remove(0, 9);
						}

						// We need to determine the Byte Size.
						// Determine how many leading Zeros there are.
						string realValue = _ramAddress;
						if (realValue.StartsWith("000000"))
						{
							_byteSize = 8;
						}
						else if (realValue.StartsWith("0000"))
						{
							_byteSize = 16;
						}
						else
						{
							_byteSize = 32;
						}

						var increment = int.Parse(_ramValue.Remove(0, 4), NumberStyles.HexNumber);
						var looper = int.Parse(_ramValue.Remove(4, 4), NumberStyles.HexNumber);

						// We set the RAMAddress to our RealAddress
						_ramAddress = realAddress;

						// We set the RAMValue to our RealValue
						_ramValue = realValue;
						for (int i = 0; i < looper; i++)
						{
							// We need to Bulk Add codes
							// Add our Cheat
							AddGBA();
							_ramAddress = (int.Parse(_ramAddress) + increment).ToString();
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else if (_ramAddress.StartsWith("080"))
			{
				// End of the code list (even if you put values in the 2nd line).
				// 00000000
				// Let's ignore the user's input on this one?
				if (_ramAddress.StartsWith("080"))
				{
					_ramAddress = _ramAddress.Replace("080", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
			}
			else if (_ramAddress.StartsWith("0800") && _ramAddress[6] == '0' && _ramAddress[7] == '0')
			{
				// AR Slowdown : loops the AR XX times
				// 0800XX00
				if (_ramAddress.StartsWith("0800") && _ramAddress[6] == '0' && _ramAddress[7] == '0')
				{
					_ramAddress = _ramAddress.Replace("0800", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
			}
			// 14) 2 Lines Special Codes (= starting with '00000000' and padded (if needed) with "00000000")
			// Note: You have to add the 0es manually, after clicking the "create" button.
			// Ocean Prince's note:
			// Several of these appear to be conflicted with above detections.
			else if (_ramAddress.StartsWith("1E2"))
			{
				// 1E2 Should be Changed to 020
				// Patches ROM address (XXXXXXXX << 1) with Halfword ZZZZ. Does not work on V1/2 upgraded to V3. Only for a real V3 Hardware?
				// XXXXXXXX
				// 0000ZZZZ
				if (_ramAddress.StartsWith("1E2"))
				{
					_ramAddress = _ramAddress.Replace("1E2", "020");
				}

				_byteSize = 16;
				_blnActionReplayMax = true;
			}
			else if (_ramAddress.StartsWith("40000000"))
			{
				// 40000000 Should be Changed to 00000000
				// (SP = 0) (means : stops the "then execute all following codes in the same row" and stops the "else execute none of the codes below)".
				// 00000000
				// 00000000
				if (_ramAddress.StartsWith("40000000"))
				{
					_ramAddress = _ramAddress.Replace("40000000", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("60000000"))
			{
				// 60000000 Should be Changed to 00000000
				// (SP = 1) (means : start to execute all codes until end of codes or SP = 0). (bypass the number of codes to executes set by the master code). Should be Changed to (If SP <> 2)
				// 00000000
				// 00000000
				if (_ramAddress.StartsWith("60000000"))
				{
					_ramAddress = _ramAddress.Replace("60000000", "020");
				}

				_byteSize = 16;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}

			// TODO:
			// Figure out how these work.
			// NOTE:
			// This is a Three Line Checker
			else if (_ramAddress.StartsWith("8022"))
			{
				// 802 Should be Changed to 020
				// Writes Byte YY at address XXXXXXXX. Then makes YY = YY + Z1, XXXXXXXX = XXXXXXXX + Z3Z3, Z2 = Z2 - 1, and repeats until Z2 < 0.
				// XXXXXXXX
				// 000000YY
				// Z1Z2Z3Z3
				if (_ramAddress.StartsWith("8022"))
				{
					_ramAddress = _ramAddress.Replace("8022", "0200");
				}
				_byteSize = 8;
				MessageBox.Show("Sorry, this tool does not support 8022 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			// I Don't get what this is doing.
			else if (_ramAddress.StartsWith("8222"))
			{
				// 822 Should be Changed to 020
				// Writes Halfword YYYY at address XXXXXXXX. Then makes YYYY = YYYY + Z1, XXXXXXXX = XXXXXXXX + Z3Z3*2,
				// XXXXXXXX
				// 0000YYYY
				// Z1Z2Z3Z3
				if (_ramAddress.StartsWith("8222"))
				{
					_ramAddress = _ramAddress.Replace("8222", "0200");
				}
				_byteSize = 16;
				MessageBox.Show("Sorry, this tool does not support 8222 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("842"))
			{
				// 842 Should be Changed to 020
				// Writes Word YYYYYYYY at address XXXXXXXX. Then makes YYYYYYYY = YYYYYYYY + Z1, XXXXXXXX = XXXXXXXX + Z3Z3*4, Z2 = Z2 - 1, and repeats until Z2<0.
				// XXXXXXXX
				// YYYYYYYY
				// Z1Z2Z3Z3
				// WARNING: There is a BUG on the REAL AR (v2 upgraded to v3, and maybe on real v3) with the 32Bits Increment Slide code. You HAVE to add a code (best choice is 80000000 00000000 : add 0 to value at address 0) right after it, else the AR will erase the 2 last 8 digits lines of the 32 Bits Inc. Slide code when you enter it !!!
				if (_ramAddress.StartsWith("842"))
				{
					_ramAddress = _ramAddress.Replace("842", "020");
				}

				_byteSize = 32;
				MessageBox.Show("Sorry, this tool does not support 8222 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			// (end three line)
			// 15) Special Codes
			// -Master Code-
			// address to patch AND $1FFFFFE Should be Changed to address to patch
			// Master Code settings.
			// XXXXXXXX
			// 0000YYYY
			else if (_ramValue.StartsWith("001DC0DE"))
			{
				// -ID Code-
				// Word at address 080000AC
				// Must always be 001DC0DE
				// XXXXXXXX
				// 001DC0DE
				if (_ramValue.StartsWith("001DC0DE"))
				{
					_ramValue = _ramValue.Replace("001DC0DE", "020");
				}

				_byteSize = 32;
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
			}
			else if (_ramAddress.StartsWith("DEADFACE"))
			{
				// -DEADFACE-
				// New Encryption seed.
				// DEADFACE
				// 0000XXXX
				MessageBox.Show("Sorry, this tool does not support DEADFACE codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_blnActionReplayMax == false)
			{
				// Is it a bad code?  Check the others.
				_blnActionReplayMax = false;
			}
		}

		public void GBACodeBreaker()
		{
			// These checks are done on the Decrypted code, not the Encrypted one.
			if (_ramAddress.StartsWith("0000") && _ramValue.StartsWith("0008") || _ramAddress.StartsWith("0000") && _ramValue.StartsWith("0002"))
			{
				// Master Code #1
				// 0000xxxx yyyy

				// xxxx is the CRC value (the "Game ID" converted to hex)
				// Flags("yyyy"):
				// 0008 - CRC Exists(CRC is used to autodetect the inserted game)
				// 0002 - Disable Interrupts
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
				return;
			}

			if (_ramAddress.StartsWith("1") && _ramValue.StartsWith("1000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("2000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("3000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("4000") || _ramAddress.StartsWith("1") && _ramValue.StartsWith("0020"))
			{
				// Master Code #2
				// 1aaaaaaa xxxy
				// 'y' is the CBA Code Handler Store Address(0 - 7)[address = ((d << 0x16) + 0x08000100)]

				// 1000 - 32 - bit Long - Branch Type(Thumb)
				// 2000 - 32 - bit Long - Branch Type(ARM)
				// 3000 - 8 - bit(?) Long - Branch Type(Thumb)
				// 4000 - 8 - bit(?) Long - Branch Type(ARM)
				// 0020 - Unknown(Odd Effect)
				MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnneeded = true;
				return;
			}
			
			if (_ramAddress.StartsWith("3"))
			{
				// 8 - Bit Constant RAM Write
				// 3aaaaaaa 00yy
				// Continuosly writes the 8 - Bit value specified by 'yy' to address aaaaaaa.
				_ramAddress = _ramAddress.Remove(0, 1);
				_byteSize = 16;
			}
			else if (_ramAddress.StartsWith("4"))
			{
				// Slide Code
				// 4aaaaaaa yyyy
				// xxxxxxxx iiii
				// This is one of those two - line codes.The "yyyy" set is the data to store at the address (aaaaaaa), with xxxxxxxx being the number of addresses to store to, and iiii being the value to increment the addresses by.  The codetype is usually use to fill memory with a certain value.
				_ramAddress = _ramAddress.Remove(0, 1);
				_byteSize = 32;
				MessageBox.Show("Sorry, this tool does not support 4 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("6"))
			{
				// 16 - Bit Logical AND
				// 6aaaaaaa yyyy
				// Performs the AND function on the address provided with the value provided. I'm not going to explain what AND does, so if you'd like to know I suggest you see the instruction manual for a graphing calculator.
				// This is another advanced code type you'll probably never need to use.  

				// Ocean Prince's note:
				// AND means "If ALL conditions are True then Do"
				// I don't understand how this would be applied/works.  Samples are requested.
				MessageBox.Show("Sorry, this tool does not support 6 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("7"))
			{
				// 16 - Bit 'If Equal To' Activator
				// 7aaaaaaa yyyy
				// If the value at the specified RAM address(aaaaaaa) is equal to yyyy value, active the code on the next line.
				_byteSize = 32;
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("8"))
			{
				// 16 - Bit Constant RAM Write
				// 8aaaaaaa yyyy
				// Continuously writes yyyy values to the specified RAM address(aaaaaaa).
				// Continuously writes the 8 - Bit value specified by 'yy' to address aaaaaaa.
				_ramAddress = _ramAddress.Remove(0, 1);
				_byteSize = 32;
			}
			else if (_ramAddress.StartsWith("9"))
			{
				// Change Encryption Seeds
				// 9yyyyyyy yyyy
				// (When 1st Code Only!)
				// Works like the DEADFACE on GSA.Changes the encryption seeds used for the rest of the codes.
				MessageBox.Show("Sorry, this tool does not support 9 codes.", "Tool error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				_byteSize = 32;
				_blnUnhandled = true;
			}
			else if (_ramAddress.StartsWith("A"))
			{
				// 16 - Bit 'If Not Equal' Activator
				// Axxxxxxx yyyy
				// Basically the opposite of an 'If Equal To' Activator.Activates the code on the next line if address xxxxxxx is NOT equal to yyyy
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
				_byteSize = 32;

			}
			else if (_ramAddress.StartsWith("D00000"))
			{
				// 16 - Bit Conditional RAM Write
				// D00000xx yyyy
				// No Description available at this time.
				MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
				_blnUnhandled = true;
				_byteSize = 32;
			}
		}
		
		public void AddGBA()
		{
			if (_byteSize == 8)
			{
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
			}
			else if (_byteSize == 16)
			{
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
			}
			else if (_byteSize == 32)
			{
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
			}
		}

		private void Gen()
		{
			// Game Genie only
			if (_singleCheat.Length == 9 && _singleCheat.Contains("-"))
			{
				if (_singleCheat.IndexOf("-") != 4)
				{
					MessageBox.Show("All Genesis Game Genie Codes need to contain a dash after the fourth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				if (_singleCheat.Contains("I") | _singleCheat.Contains("O") | _singleCheat.Contains("Q") | _singleCheat.Contains("U"))
				{
					MessageBox.Show("All Genesis Game Genie Codes do not use I, O, Q or U.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				var decoder = new GenesisGameGenieDecoder(_singleCheat);

				// Game Genie, modifies the "ROM" which is why it says, "MD CART"
				var watch = Watch.GenerateWatch(MemoryDomains["M68K BUS"], decoder.Address, WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
				Global.CheatList.Add(new Cheat(watch, decoder.Value));
			}

			// Action Replay?
			if (_singleCheat.Contains(":"))
			{
				// We start from Zero.
				if (_singleCheat.IndexOf(":") != 6)
				{
					MessageBox.Show("All Genesis Action Replay/Pro Action Replay Codes need to contain a colon after the sixth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				// Problem: I don't know what the Non-FF Style codes are.
				// TODO: Fix that.
				if (_singleCheat.StartsWith("FF") == false)
				{
					MessageBox.Show("This Action Replay Code, is not understood by this tool.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}

				// Now to do some work.
				// Determine Length, to Determine Byte Size
				_parseString = _singleCheat.Remove(0, 2);
				switch (_singleCheat.Length)
				{
					case 9:
						// Sample Code of 1-Byte:
						// FFF761:64
						// Becomes:
						// Address: F761
						// Value: 64
						_ramAddress = _parseString.Remove(4, 3);
						_ramValue = _parseString.Remove(0, 5);
						_byteSize = 1;
						break;
					case 11:
						// Sample Code of 2-Byte:
						// FFF761:6411
						// Becomes:
						// Address: F761
						// Value: 6411
						_ramAddress = _parseString.Remove(4, 5);
						_ramValue = _parseString.Remove(0, 5);
						_byteSize = 2;
						break;
					default:
						// We could have checked above but here is fine, since it's a quick check due to one of three possibilities.
						MessageBox.Show("All Genesis Action Replay/Pro Action Replay Codes need to be either 9 or 11 characters in length", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
				}

				try
				{
					if (_byteSize == 1)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["68K RAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
						Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
					}
					else if (_byteSize == 2)
					{
						var watch = Watch.GenerateWatch(MemoryDomains["68K RAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
						Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void N64()
		{
			// These codes, more or less work without Needing much work.
			if (_singleCheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All N64 GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (_singleCheat.Length != 13)
			{
				MessageBox.Show("All N64 GameShark Codes need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// We need to determine what kind of cheat this is.
			// I need to determine if this is a Byte or Word.
			switch (_testo)
			{
				// 80 and 81 are the most common, so let's not get all worried.
				case "80":
					_byteSize = 8;
					break;
				case "81":
					_byteSize = 16;
					break;
				// Case A0 and A1 means "Write to Uncached address.
				case "A0":
					_byteSize = 8;
					break;
				case "A1":
					_byteSize = 16;
					break;
				// Do we support the GameShark Button?  No.  But these cheats, can be toggled.  Which "Counts"
				// <Ocean_Prince> Consequences be damned!
				case "88":
					_byteSize = 8;
					break;
				case "89":
					_byteSize = 16;
					break;
				// These are compare Address X to Value Y, then apply Value B to Address A
				// This is not supported, yet
				// TODO: When BizHawk supports a compare RAM Address's value is true then apply a value to another address, make it a thing.
				case "D0":
				case "D1":
				case "D2":
				case "D3":
					MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				// These codes are for Disabling the Expansion Pak.  that's a bad thing?  Assuming bad codes, until told otherwise.
				case "EE":
				case "DD":
				case "CC":
					MessageBox.Show("The code you entered is for Disabling the Expansion Pak.  This is not allowed by this tool.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				// Enable Code
				// Not Necessary?  Think so?
				case "DE":
				// Single Write ON-Boot code.
				// Not Necessary?  Think so?
				case "F0":
				case "F1":
				case "2A":
				case "3C":
				case "FF":
					MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				// TODO: Make Patch Code (5000XXYY) work.
				case "50":
					MessageBox.Show("The code you entered is not supported by this tool.  Please Submit the Game's Name, Cheat/Code and Purpose to the BizHawk forums.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				default:
					MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
			}

			// Sample Input for N64:
			// 8133B21E 08FF
			// Becomes:
			// Address 33B21E
			// Value 08FF

			// Note, 80XXXXXX 00YY
			// Is Byte, not Word
			// Remove the 80 Octect
			_parseString = _singleCheat.Remove(0, 2);

			// Get RAM Address
			_ramAddress = _parseString.Remove(6, 5);

			// Get RAM Value
			_ramValue = _parseString.Remove(0, 7);
			try
			{
				if (_byteSize == 8)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, true, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
				else if (_byteSize == 16)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Nes()
		{
			if (_singleCheat.Length != 6 && _singleCheat.Length != 8)
			{
				MessageBox.Show("Game Genie codes need to be six or eight characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			var decoder = new NesGameGenieDecoder(_singleCheat);

			try
			{
				var description = _singleCheat;
				if (!string.IsNullOrWhiteSpace(txtDescription.Text))
				{
					description = txtDescription.Text;
				}

				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], decoder.Address, WatchSize.Byte, Common.DisplayType.Hex, false, description);
				Global.CheatList.Add(
					decoder.Compare.HasValue
						? new Cheat(watch, decoder.Value, decoder.Compare.Value, true, Cheat.CompareType.Equal)
						: new Cheat(watch, decoder.Value));

			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Psx()
		{
			// These codes, more or less work without Needing much work.
			if (_singleCheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All PSX GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (_singleCheat.Length != 13)
			{
				MessageBox.Show("All PSX GameShark Cheats need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			switch (_testo)
			{
				// 30 80 Cheats mean, "Write, don't care otherwise."
				case "30":
					_byteSize = 8;
					break;
				case "80":
					_byteSize = 16;
					break;
				// When value hits YYYY, make the next cheat go off
				case "E0":
				// E0 byteSize = 8;
				case "E1":
				// E1 byteSize = 8;
				case "E2":
				// E2 byteSize = 8;
				case "D0":
				// D0 byteSize = 16;
				case "D1":
				// D1 byteSize = 16;
				case "D2":
				// D2 byteSize = 16;
				case "D3":
				// D3 byteSize = 16;
				case "D4":
				// D4 byteSize = 16;
				case "D5":
				// D5 byteSize = 16;
				case "D6":
				// D6 byteSize = 16;

				// Increment/Decrement Codes
				case "10":
				// 10 byteSize = 16;
				case "11":
				// 11 byteSize = 16;
				case "20":
				// 20 byteSize = 8
				case "21":
					// 21 byteSize = 8
					MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				case "C0":
				case "C1":
				// Slow-Mo
				case "40":
					MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				case "C2":
				case "50":
					MessageBox.Show("The code you entered is not supported by this tool.  Please Submit the Game's Name, Cheat/Code and Purpose to the BizHawk forums.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				default:
					MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
			}

			// Sample Input for PSX:
			// 800D10BA 0009
			// Address: 0D10BA
			// Value:  0009
			// Remove first two octets
			_parseString = _singleCheat.Remove(0, 2);
			
			// Get RAM Address
			_ramAddress = _parseString.Remove(6, 5);
			
			// Get RAM Value
			_ramValue = _parseString.Remove(0, 7);
			try
			{
				// A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				// System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.
				// My Concern is that Work RAM High may be incorrect?
				if (_byteSize == 8)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["MainRAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
				else if (_byteSize == 16)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["MainRAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Saturn()
		{
			if (_singleCheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All Saturn GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (_singleCheat.Length != 13)
			{
				MessageBox.Show("All Saturn GameShark Cheats need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// This is a special test.  Only the first character really matters?  16 or 36?
			_testo = _testo.Remove(1, 1);
			switch (_testo)
			{
				case "1":
					_byteSize = 16;
					break;
				case "3":
					_byteSize = 8;
					break;
				// 0 writes once.
				case "0":
				// D is RAM Equal To Activator, do Next Value
				case "D":
					MessageBox.Show("The code you entered is not supported by BizHawk.", "Emulator Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				case "F":
					MessageBox.Show("The code you entered is not needed by Bizhawk.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				default:
					MessageBox.Show("The GameShark code entered is not a recognized format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
			}

			// Sample Input for Saturn:
			// 160949FC 0090
			// Address: 0949FC
			// Value:  90
			// Note, 3XXXXXXX are Big Endian
			// Remove first two octets
			_parseString = _singleCheat.Remove(0, 2);

			// Get RAM Address
			_ramAddress = _parseString.Remove(6, 5);

			// Get RAM Value
			_ramValue = _parseString.Remove(0, 7);
			try
			{
				// A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				// System Bus Domain, The Address to Watch, Byte size (Word), Hex Display, Description.  Big Endian.
				// My Concern is that Work RAM High may be incorrect?
				if (_byteSize == 8)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["Work Ram High"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, true, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
				else if (_byteSize == 16)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["Work Ram High"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, true, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		// This also handles Game Gear due to shared hardware. Go figure.
		private void Sms()
		{
			string ramCompare = null;

			// Game Genie
			if (_singleCheat.LastIndexOf("-") == 7 && _singleCheat.IndexOf("-") == 3)
			{
				var decoder = new GbGgGameGenieDecoder(_singleCheat);
				var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], decoder.Address, WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
				Global.CheatList.Add(decoder.Compare.HasValue
					? new Cheat(watch, decoder.Value, decoder.Compare)
					: new Cheat(watch, decoder.Value));
			}

			// Action Replay
			else if (_singleCheat.IndexOf("-") == 3 && _singleCheat.Length == 9)
			{
				_parseString = _singleCheat;
				_parseString = _parseString.Remove(0, 2);
				_ramAddress = _parseString.Remove(4, 2);
				_ramAddress = _ramAddress.Replace("-", "");
				_ramValue = _parseString.Remove(0, 5);
			}

			// It's an Action Replay
			if (_singleCheat.Length != 9 && _singleCheat.LastIndexOf("-") != 7)
			{
				MessageBox.Show("All Master System Action Replay Codes need to be nine characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Game Genie
			if (_singleCheat.LastIndexOf("-") != 7 && _singleCheat.IndexOf("-") != 3)
			{
				MessageBox.Show("All Master System Game Genie Codes need to have a dash after the third character and seventh character.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				// A Watch needs to be generated so we can make a cheat out of that.  This is due to how the Cheat engine works.
				// System Bus Domain, The Address to Watch, Byte size (Byte), Hex Display, Description.  Not Big Endian.
				var watch = Watch.GenerateWatch(MemoryDomains["Main RAM"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
				
				// Take Watch, Add our Value we want, and it should be active when added
				Global.CheatList.Add(ramCompare == null
					? new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber))
					: new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber), int.Parse(ramCompare, NumberStyles.HexNumber)));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Snes()
		{
			if (_singleCheat.Contains("-") && _singleCheat.Length == 9)
			{
				MessageBox.Show("Game genie codes are not currently supported for SNES", "SNES Game Genie not supported", MessageBoxButtons.OK, MessageBoxIcon.Error);
				////var decoder = new SnesGameGenieDecoder(_singleCheat);
				////var watch = Watch.GenerateWatch(MemoryDomains["CARTROM"], decoder.Address, WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
				////Global.CheatList.Add(new Cheat(watch, decoder.Value));
			}

			// This ONLY applies to Action Replay.
			if (_singleCheat.Length == 8)
			{
				// Sample Code:
				// 7E18A428
				// Address: 7E18A4
				// Value: 28
				// Remove last two octets
				_ramAddress = _singleCheat.Remove(6, 2);

				// Get RAM Value
				_ramValue = _singleCheat.Remove(0, 6);

				// Note, it's a Word.  However, we are using this to keep from repeating code.
				_byteSize = 16;
			}

			if (_singleCheat.Contains("-") && _singleCheat.Length != 9)
			{
				MessageBox.Show("Game Genie Codes need to be nine characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			if (_singleCheat.Length != 9 && _singleCheat.Length != 8)
			{
				MessageBox.Show("Pro Action Replay Codes need to be eight characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			try
			{
				// Action Replay
				if (_byteSize == 16)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Word, Common.DisplayType.Hex, false, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
				else if (_byteSize == 8)
				{
					var watch = Watch.GenerateWatch(MemoryDomains["System Bus"], long.Parse(_ramAddress, NumberStyles.HexNumber), WatchSize.Byte, Common.DisplayType.Hex, false, txtDescription.Text);
					Global.CheatList.Add(new Cheat(watch, int.Parse(_ramValue, NumberStyles.HexNumber)));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnClear_Click(object sender, EventArgs e)
		{
			// Clear old Inputs
			var result = MessageBox.Show("Are you sure you want to clear this form?", "Clear Form", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				txtDescription.Clear();
				txtCheat.Clear();
			}
		}
	}
}