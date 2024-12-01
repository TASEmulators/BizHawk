using System.Drawing;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.Common.cheats;
using BizHawk.Emulation.Cores;

// TODO:
// Add Support/Handling for The Following Systems and Devices:
// GBA: Code Breaker (That uses unique Encryption keys)
// NES: Pro Action Rocky  (When someone asks)
// SNES: GoldFinger (Action Replay II) Support?
namespace BizHawk.Client.EmuHawk
{
	[Tool(
		released: true,
		supportedSystems: new[] { VSystemID.Raw.GB, VSystemID.Raw.GBA, VSystemID.Raw.GEN, VSystemID.Raw.N64, VSystemID.Raw.NES, VSystemID.Raw.PSX, VSystemID.Raw.SAT, VSystemID.Raw.SGB, VSystemID.Raw.SMS, VSystemID.Raw.SNES },
		unsupportedCores: new[] { CoreNames.Snes9X })]
	public partial class GameShark : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.SharkIcon;

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
			Icon = ToolIcon;
		}

		private void Go_Click(object sender, EventArgs e)
		{
			foreach (var l in txtCheat.Lines)
			{
				try
				{
					var code = l.ToUpperInvariant().Trim();
					var decoder = new GameSharkDecoder(MemoryDomains, Emulator.SystemId);
					var result = decoder.Decode(code);
					var domain = decoder.CheatDomain();

					if (result.IsValid(out var valid))
					{
						var description = !string.IsNullOrWhiteSpace(txtDescription.Text)
							? txtDescription.Text
							: code;
						MainForm.CheatList.Add(valid.ToCheat(domain, description));
					}
					else
					{
						DialogController.ShowMessageBox(result.Error, "Input Error", EMsgBoxIcon.Error);
					}
				}
				catch (Exception ex)
				{
					DialogController.ShowMessageBox($"An Error occured: {ex.GetType()}", "Error", EMsgBoxIcon.Error);
				}
			}

			txtCheat.Clear();
			txtDescription.Clear();
		}

		private void BtnClear_Click(object sender, EventArgs e)
		{
			// Clear old Inputs
			var result = DialogController.ShowMessageBox2("Are you sure you want to clear this form?", "Clear Form", EMsgBoxIcon.Question);
			if (result)
			{
				txtDescription.Clear();
				txtCheat.Clear();
			}
		}
	}
}
