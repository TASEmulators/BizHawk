using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.cheats
{
	public class GameSharkDecoder
	{
		private readonly IMemoryDomains _domains;
		private readonly string _systemId;

		public GameSharkDecoder(IMemoryDomains domains, string systemId)
		{
			_domains = domains;
			_systemId = systemId;
		}

		public IDecodeResult Decode(string code)
		{
			return _systemId switch
			{
				"GB" => GameBoy(code),
				"GBA" => Gba(code),
				"GEN" => Gen(code),
				"N64" => N64(code),
				"NES" => Nes(code),
				"PSX" => Psx(code),
				"SAT" => Saturn(code),
				"SMS" => Sms(code),
				"SNES" => Snes(code),
				_ => new InvalidCheatCode("Cheat codes not currently supported on this system")
			};
		}

		public MemoryDomain CheatDomain()
		{
			var domain = CheatDomainName();
			return CheatDomainName() == null
				? _domains.SystemBus
				: _domains[domain];
		}

		private string CheatDomainName() => _systemId switch
		{
			"N64" => "RDRAM",
			"PSX" => "MainRAM",
			"Saturn" => "Work Ram High", // Work RAM High may be incorrect?
			_ => null

		};

		private static IDecodeResult GameBoy(string code)
		{
			// Game Genie
			if (code.LastIndexOf("-") == 7 && code.IndexOf("-") == 3)
			{
				return GbGgGameGenieDecoder.Decode(code);
					
			}

			// Game Shark codes
			if (code.Length == 8 && !code.Contains("-"))
			{
				return GbGameSharkDecoder.Decode(code);
			}

			return new InvalidCheatCode($"Unknown code type: {code}");
		}

		private static IDecodeResult Gba(string code)
		{
			if (code.Length == 12)
			{
				return new InvalidCheatCode("Codebreaker/GameShark SP/Xploder codes are not yet supported.");
			}

			return GbaGameSharkDecoder.Decode(code);
		}

		private static IDecodeResult Gen(string code)
		{
			// Game Genie only
			if (code.Length == 9 && code.Contains("-"))
			{
				return GenesisGameGenieDecoder.Decode(code);
			}

			// Action Replay?
			if (code.Contains(":"))
			{
				// Problem: I don't know what the Non-FF Style codes are.
				// TODO: Fix that.
				if (code.StartsWith("FF") == false)
				{
					return new InvalidCheatCode("This Action Replay Code, is not yet supported.");
				}

				return GenesisActionReplayDecoder.Decode(code);
			}

			return new InvalidCheatCode($"Unknown code type: {code}");
		}

		private static IDecodeResult N64(string code) => N64GameSharkDecoder.Decode(code);

		private static IDecodeResult Nes(string code) => NesGameGenieDecoder.Decode(code);

		private static IDecodeResult Psx(string code) => PsxGameSharkDecoder.Decode(code);

		private static IDecodeResult Saturn(string code) => SaturnGameSharkDecoder.Decode(code);

		private static IDecodeResult Sms(string code)
		{
			// Game Genie
			if (code.LastIndexOf("-") == 7 && code.IndexOf("-") == 3)
			{
				return GbGgGameGenieDecoder.Decode(code);
			}

			// Action Replay
			if (code.IndexOf("-") == 3 && code.Length == 9)
			{
				return SmsActionReplayDecoder.Decode(code);
			}

			return new InvalidCheatCode($"Unknown code type: {code}");
		}

		private static IDecodeResult Snes(string code)
		{
			if (code.Contains("-") && code.Length == 9)
			{
				return new InvalidCheatCode("Game genie codes are not currently supported for SNES");
				////return SnesGameGenieDecoder.Decode(code);
			}

			if (code.Length == 8)
			{
				return GbGameSharkDecoder.Decode(code);
			}
			
			return new InvalidCheatCode($"Unknown code type: {code}");
		}
	}
}
