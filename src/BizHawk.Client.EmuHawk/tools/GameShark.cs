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
namespace BizHawk.Client.EmuHawk
{
	[Tool(true, new[] { "GB", "GBA", "GEN", "N64", "NES", "PSX", "SAT", "SMS", "SNES" }, new[] { "Snes9x" })]
	public partial class GameShark : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		private IMemoryDomains MemoryDomains { get; set; }

		[RequiredService]
		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		private IEmulator Emulator { get; set; }

		protected override string WindowTitleStatic => "Cheat Code Converter";

		public GameShark()
		{
			InitializeComponent();
			Icon = Properties.Resources.SharkIcon;
		}

		private void Go_Click(object sender, EventArgs e)
		{
			foreach (var l in txtCheat.Lines)
			{
				try
				{
					var code = l.ToUpper();
					var decoder = new GameSharkDecoder(MemoryDomains, Emulator.SystemId);
					var result = decoder.Decode(code);
					var domain = decoder.CheatDomain();

					if (result.IsValid)
					{
						var description = !string.IsNullOrWhiteSpace(txtDescription.Text)
							? txtDescription.Text
							: code;
						MainForm.CheatList.Add(result.ToCheat(domain, description));
					}
					else
					{
						MessageBox.Show(result.Error, "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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