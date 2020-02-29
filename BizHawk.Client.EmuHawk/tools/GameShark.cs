using System;
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

		#region IToolForm

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

		#endregion

		private void Go_Click(object sender, EventArgs e)
		{
			foreach (var l in txtCheat.Lines)
			{
				try
				{
					var code = l.ToUpper();
					switch (Emulator.SystemId)
					{
						case "GB":
							GameBoy(code);
							break;
						case "GBA":
							GBA(code);
							break;
						case "GEN":
							Gen(code);
							break;
						case "N64":
							N64(code);
							break;
						case "NES":
							Nes(code);
							break;
						case "PSX":
							Psx(code);
							break;
						case "SAT":
							Saturn(code);
							break;
						case "SMS":
							Sms(code);
							break;
						case "SNES":
							Snes(code);
							break;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"An Error occured: {ex.GetType()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}

			txtCheat.Clear();
			txtDescription.Clear();
		}

		private void GameBoy(string code)
		{
			// Game Genie
			if (code.LastIndexOf("-") == 7 && code.IndexOf("-") == 3)
			{
				var result = GbGgGameGenieDecoder.Decode(code);
				if (result.IsValid)
				{
					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}

			// Game Shark codes
			if (code.Length == 8 && !code.Contains("-"))
			{
				var result = GbGameSharkDecoder.Decode(code);
				if (result.IsValid)
				{
					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}

			InputError($"Unknown code type: {code}");
		}

		private void GBA(string code)
		{
			if (code.Length == 12)
			{
				InputError("Codebreaker/GameShark SP/Xploder codes are not yet supported by this tool.");
			}

			var result = GbaGameSharkDecoder.Decode(code);
			if (result.IsValid)
			{
				var description = Description(code);
				Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
			}
			else
			{
				InputError(result.Error);
			}
		}

		private void Gen(string code)
		{
			// Game Genie only
			if (code.Length == 9 && code.Contains("-"))
			{
				var result = GenesisGameGenieDecoder.Decode(code);
				if (result.IsValid)
				{
					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}

			// Action Replay?
			if (code.Contains(":"))
			{
				var result = GenesisActionReplayDecoder.Decode(code);
				if (result.IsValid)
				{
					// Problem: I don't know what the Non-FF Style codes are.
					// TODO: Fix that.
					if (code.StartsWith("FF") == false)
					{
						MessageBox.Show("This Action Replay Code, is not understood by this tool.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}

					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}

			InputError($"Unknown code type: {code}");
		}

		private void N64(string cheat)
		{
			// These codes, more or less work without Needing much work.
			if (cheat.IndexOf(" ") != 8)
			{
				MessageBox.Show("All N64 GameShark Codes need to contain a space after the eighth character", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (cheat.Length != 13)
			{
				MessageBox.Show("All N64 GameShark Codes need to be 13 characters in length.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			switch (cheat.Remove(0, 2))
			{
				case "80":
				case "81":
				case "A0":
				case "A1":
				case "88":
				case "89":
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
					InputError("The code you entered is for Disabling the Expansion Pak.  This is not allowed by this tool.");
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
					InputError("The code you entered is not needed by Bizhawk.");
					return;
				// TODO: Make Patch Code (5000XXYY) work.
				case "50":
					MessageBox.Show("The code you entered is not supported by this tool.  Please Submit the Game's Name, Cheat/Code and Purpose to the BizHawk forums.", "Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				default:
					InputError("The GameShark code entered is not a recognized format.");
					return;
			}

			var decoder = new N64GameSharkDecoder(cheat);
			var watch = Watch.GenerateWatch(MemoryDomains["RDRAM"], decoder.Address, decoder.Size, Common.DisplayType.Hex, true, txtDescription.Text);
			Global.CheatList.Add(new Cheat(watch, decoder.Value));
		}

		private void Nes(string code)
		{
			var result = NesGameGenieDecoder.Decode(code);
			if (result.IsValid)
			{
				var description = Description(code);
				Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
			}
			else
			{
				InputError(result.Error);
			}
		}

		private void Psx(string code)
		{
			var result = PsxGameSharkDecoder.Decode(code);
			if (result.IsValid)
			{
				var description = Description(code);
				Global.CheatList.Add(result.ToCheat(MemoryDomains["MainRAM"], description));
			}
			else
			{
				InputError(result.Error);
			}
		}

		private void Saturn(string code)
		{
			var result = SaturnGameSharkDecoder.Decode(code);
			if (result.IsValid)
			{
				var description = Description(code);

				// Is Work RAM High may be incorrect?
				Global.CheatList.Add(result.ToCheat(MemoryDomains["Work Ram High"], description));
			}
			else
			{
				InputError(result.Error);
			}
		}

		// Note: this also handles Game Gear due to shared hardware
		private void Sms(string code)
		{
			// Game Genie
			if (code.LastIndexOf("-") == 7 && code.IndexOf("-") == 3)
			{
				var result = GbGgGameGenieDecoder.Decode(code);
				if (result.IsValid)
				{
					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}

			// Action Replay
			else if (code.IndexOf("-") == 3 && code.Length == 9)
			{
				var result = SmsActionReplayDecoder.Decode(code);
				if (result.IsValid)
				{
					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}

			InputError($"Unknown code type: {code}");
		}

		private void Snes(string code)
		{
			if (code.Contains("-") && code.Length == 9)
			{
				MessageBox.Show("Game genie codes are not currently supported for SNES", "SNES Game Genie not supported", MessageBoxButtons.OK, MessageBoxIcon.Error);
				//var result = SnesGameGenieDecoder.Decode(code);
				//if (result.IsValid)
				//{
				//	var description = Description(code);
				//	Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				//}
				//else
				//{
				//	InputError(result.Error);
				//}
			}
			else if (code.Length == 8)
			{
				var result = GbGameSharkDecoder.Decode(code);
				if (result.IsValid)
				{
					var description = Description(code);
					Global.CheatList.Add(result.ToCheat(MemoryDomains.SystemBus, description));
				}
				else
				{
					InputError(result.Error);
				}
			}
			
			InputError($"Unknown code type: {code}");
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

		private void InputError(string message)
		{
			MessageBox.Show(message, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private string Description(string cheat)
		{
			return !string.IsNullOrWhiteSpace(txtDescription.Text)
				? txtDescription.Text
				: cheat;
		}
	}
}