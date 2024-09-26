using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	[PortedCore(
		name: CoreNames.QuickNes,
		author: "SergioMartin86, kode54, Blargg",
		portedVersion: "1.0.0",
		portedUrl: "https://github.com/SergioMartin86/quickerNES")]
	public sealed partial class QuickNES : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IInputPollable,
		IBoardInfo, IVideoLogicalOffsets, IStatable, IDebuggable,
		ISettable<QuickNES.QuickNESSettings, QuickNES.QuickNESSyncSettings>, INESPPUViewable
	{
		static QuickNES()
		{
			var resolver = new DynamicLibraryImportResolver(
				$"libquicknes{(OSTailoredCode.IsUnixHost ? ".so" : ".dll")}", hasLimitedLifetime: false);
			QN = BizInvoker.GetInvoker<LibQuickNES>(resolver, CallingConventionAdapters.Native);
		}

		[CoreConstructor(VSystemID.Raw.NES)]
		public QuickNES(byte[] file, QuickNESSettings settings, QuickNESSyncSettings syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Context = QN.qn_new();
			if (Context == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(QN.qn_new)}() returned NULL");
			}

			try
			{
				file = FixInesHeader(file);
				LibQuickNES.ThrowStringError(QN.qn_loadines(Context, file, file.Length));

				InitSaveRamBuff();
				InitSaveStateBuff();
				InitAudio();
				InitMemoryDomains();

				int mapper = 0;
				string mappername = Marshal.PtrToStringAnsi(QN.qn_get_mapper(Context, ref mapper));
				Console.WriteLine($"{CoreNames.QuickNes}: Booted with Mapper #{mapper} \"{mappername}\"");
				BoardName = mappername;
				PutSettings(settings ?? new QuickNESSettings());

				_syncSettings = syncSettings ?? new QuickNESSyncSettings();
				_syncSettingsNext = _syncSettings.Clone();

				SetControllerDefinition();
				ComputeBootGod();

				ConnectTracer();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private static readonly LibQuickNES QN;

		public IEmulatorServiceProvider ServiceProvider { get; }

		int IVideoLogicalOffsets.ScreenX => _settings.ClipLeftAndRight ? 8 : 0;

		int IVideoLogicalOffsets.ScreenY => _settings.ClipTopAndBottom ? 8 : 0;

		public ControllerDefinition ControllerDefinition { get; private set; }

		private void SetControllerDefinition()
		{
			ControllerDefinition def = new("NES Controller");

			// Function to add gamepad buttons
			void AddButtons(IEnumerable<(string PrefixedName, uint Bitmask)> entries)
				=> def.BoolButtons.AddRange(entries.Select(static p => p.PrefixedName));

			// Parsing Port1 inputs

			switch (_syncSettings.Port1)
			{
				case Port1PeripheralOption.Gamepad:

					// Adding set of gamepad buttons (P1)
					AddButtons(GamepadButtons[0]);
					
					break;

				case Port1PeripheralOption.FourScore:

					// Adding set of gamepad buttons (P1)
					AddButtons(FourScoreButtons[0]);

					break;

				case Port1PeripheralOption.ArkanoidNES:

					// Adding Arkanoid Paddle potentiometer
					def.AddAxis("P2 Paddle", 0.RangeTo(160), 80);

					// Adding Arkanoid Fire button
					def.BoolButtons.Add("P2 Fire");

					break;

				case Port1PeripheralOption.ArkanoidFamicom:

					// Adding set of gamepad buttons (P1)
					AddButtons(GamepadButtons[0]);

					// Adding dummy set of P2 buttons (not yet supported)
					def.BoolButtons.Add("P2 Up");
					def.BoolButtons.Add("P2 Down");
					def.BoolButtons.Add("P2 Left");
					def.BoolButtons.Add("P2 Right");
					def.BoolButtons.Add("P2 B");
					def.BoolButtons.Add("P2 A");
					def.BoolButtons.Add("P2 M"); // Microphone

					// Adding Arkanoid Paddle potentiometer
					def.AddAxis("P3 Paddle", 0.RangeTo(160), 80);

					// Adding Arkanoid Fire button
					def.BoolButtons.Add("P3 Fire");

					break;
			}

			// Parsing Port2 inputs

			switch (_syncSettings.Port2)
			{
				case Port2PeripheralOption.Gamepad:

					// Adding set of gamepad buttons (P1)
					AddButtons(GamepadButtons[1]);

					break;

				case Port2PeripheralOption.FourScore2:

					// Adding set of gamepad buttons (P2)
					AddButtons(FourScoreButtons[1]);

					break;
			}

			// Adding console buttons
			def.BoolButtons.AddRange(new[] { "Reset", "Power" }); // console buttons

			ControllerDefinition = def.MakeImmutable();
		}

		private static readonly (string PrefixedName, uint Bitmask)[][] GamepadButtons = new[]
		{
			new[] {
				("P1 Up",     0b0000_0000_0000_0000_0000_0000_0001_0000u),
				("P1 Down",   0b0000_0000_0000_0000_0000_0000_0010_0000u),
				("P1 Left",   0b0000_0000_0000_0000_0000_0000_0100_0000u),
				("P1 Right",  0b0000_0000_0000_0000_0000_0000_1000_0000u),
				("P1 Start",  0b0000_0000_0000_0000_0000_0000_0000_1000u),
				("P1 Select", 0b0000_0000_0000_0000_0000_0000_0000_0100u),
				("P1 B",      0b0000_0000_0000_0000_0000_0000_0000_0010u),
				("P1 A",      0b0000_0000_0000_0000_0000_0000_0000_0001u),
			},					
			new[] {				
				("P2 Up",     0b0000_0000_0000_0000_0000_0000_0001_0000u),
				("P2 Down",   0b0000_0000_0000_0000_0000_0000_0010_0000u),
				("P2 Left",   0b0000_0000_0000_0000_0000_0000_0100_0000u),
				("P2 Right",  0b0000_0000_0000_0000_0000_0000_1000_0000u),
				("P2 Start",  0b0000_0000_0000_0000_0000_0000_0000_1000u),
				("P2 Select", 0b0000_0000_0000_0000_0000_0000_0000_0100u),
				("P2 B",      0b0000_0000_0000_0000_0000_0000_0000_0010u),
				("P2 A",      0b0000_0000_0000_0000_0000_0000_0000_0001u),
			},
		};

		private static readonly (string PrefixedName, uint Bitmask)[][] FourScoreButtons = new[]
		{
			new[] {
				("P1 Up",     0b0000_0000_0000_0000_0000_0000_0001_0000u),
				("P1 Down",   0b0000_0000_0000_0000_0000_0000_0010_0000u),
				("P1 Left",   0b0000_0000_0000_0000_0000_0000_0100_0000u),
				("P1 Right",  0b0000_0000_0000_0000_0000_0000_1000_0000u),
				("P1 Start",  0b0000_0000_0000_0000_0000_0000_0000_1000u),
				("P1 Select", 0b0000_0000_0000_0000_0000_0000_0000_0100u),
				("P1 B",      0b0000_0000_0000_0000_0000_0000_0000_0010u),
				("P1 A",      0b0000_0000_0000_0000_0000_0000_0000_0001u),

			    ("P3 Up",     0b0000_0000_0000_0000_0001_0000_0000_0000u),
				("P3 Down",   0b0000_0000_0000_0000_0010_0000_0000_0000u),
				("P3 Left",   0b0000_0000_0000_0000_0100_0000_0000_0000u),
				("P3 Right",  0b0000_0000_0000_0000_1000_0000_0000_0000u),
				("P3 Start",  0b0000_0000_0000_0000_0000_1000_0000_0000u),
				("P3 Select", 0b0000_0000_0000_0000_0000_0100_0000_0000u),
				("P3 B",      0b0000_0000_0000_0000_0000_0010_0000_0000u),
				("P3 A",      0b0000_0000_0000_0000_0000_0001_0000_0000u),
			},
			new[] {
				("P2 Up",     0b0000_0000_0000_0000_0000_0000_0001_0000u),
				("P2 Down",   0b0000_0000_0000_0000_0000_0000_0010_0000u),
				("P2 Left",   0b0000_0000_0000_0000_0000_0000_0100_0000u),
				("P2 Right",  0b0000_0000_0000_0000_0000_0000_1000_0000u),
				("P2 Start",  0b0000_0000_0000_0000_0000_0000_0000_1000u),
				("P2 Select", 0b0000_0000_0000_0000_0000_0000_0000_0100u),
				("P2 B",      0b0000_0000_0000_0000_0000_0000_0000_0010u),
				("P2 A",      0b0000_0000_0000_0000_0000_0000_0000_0001u),

				("P4 Up",     0b0000_0000_0000_0000_0001_0000_0000_0000u),
				("P4 Down",   0b0000_0000_0000_0000_0010_0000_0000_0000u),
				("P4 Left",   0b0000_0000_0000_0000_0100_0000_0000_0000u),
				("P4 Right",  0b0000_0000_0000_0000_1000_0000_0000_0000u),
				("P4 Start",  0b0000_0000_0000_0000_0000_1000_0000_0000u),
				("P4 Select", 0b0000_0000_0000_0000_0000_0100_0000_0000u),
				("P4 B",      0b0000_0000_0000_0000_0000_0010_0000_0000u),
				("P4 A",      0b0000_0000_0000_0000_0000_0001_0000_0000u),
			},
		};


		private void SetPads(IController controller, out uint j1, out uint j2)
		{
			static uint PackGamepadButtonsFor(int portNumber, IController controller)
			{
				uint ret = unchecked(0xFFFFFF00u);
				foreach (var (prefixedName, bitmask) in GamepadButtons[portNumber])
				{
					if (controller.IsPressed(prefixedName)) ret |= bitmask;
				}
				return ret;
			}

			static uint PackFourscoreButtonsFor(int portNumber, IController controller)
			{
				uint ret = 0;
				if (portNumber == 0) ret |= 0b1111_1111_0000_1000_0000_0000_0000_0000u;
				if (portNumber == 1) ret |= 0b1111_1111_0000_0100_0000_0000_0000_0000u;

				foreach (var (prefixedName, bitmask) in FourScoreButtons[portNumber])
				{
					if (controller.IsPressed(prefixedName)) ret |= bitmask;
				}
				return ret;
			}

			j1 = 0;
			j2 = 0;
			switch (_syncSettings.Port1)
			{
				case Port1PeripheralOption.Gamepad:
				case Port1PeripheralOption.ArkanoidFamicom:
					j1 = PackGamepadButtonsFor(0, controller);
					break;
				case Port1PeripheralOption.FourScore:
					j1 = PackFourscoreButtonsFor(0, controller);
					break;
			}
			switch (_syncSettings.Port2)
			{
				case Port2PeripheralOption.Gamepad:
					j2 = PackGamepadButtonsFor(1, controller);
					break;
				case Port2PeripheralOption.FourScore2:
					j2 = PackFourscoreButtonsFor(1, controller);
					break;
			}
		}

		public enum QuickerNESInternalControllerTypeEnumeration : byte
		{
			None = 0x0,
			Joypad = 0x1,
			ArkanoidNES = 0x2,
			ArkanoidFamicom = 0x3,
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			CheckDisposed();

			if (controller.IsPressed("Power"))
				QN.qn_reset(Context, true);
			if (controller.IsPressed("Reset"))
				QN.qn_reset(Context, false);

			SetPads(controller, out var j1, out var j2);

			QN.qn_set_tracecb(Context, Tracer.IsEnabled() ? _traceCb : null);

			// Getting correct internal controller type for QuickerNES
			QuickerNESInternalControllerTypeEnumeration internalQuickerNESControllerType = QuickerNESInternalControllerTypeEnumeration.None;

			// Handling Port2
			switch (_syncSettings.Port2)
			{
				case Port2PeripheralOption.Gamepad:
				case Port2PeripheralOption.FourScore2:
					internalQuickerNESControllerType = QuickerNESInternalControllerTypeEnumeration.Joypad; break;
			}

			// Handling Port1 -- Using Arkanoid overrides the selection for Port2
			switch (_syncSettings.Port1)
			{
				case Port1PeripheralOption.Gamepad:
				case Port1PeripheralOption.FourScore:
					internalQuickerNESControllerType = QuickerNESInternalControllerTypeEnumeration.Joypad; break;
				case Port1PeripheralOption.ArkanoidNES:
					internalQuickerNESControllerType = QuickerNESInternalControllerTypeEnumeration.ArkanoidNES; break;
				case Port1PeripheralOption.ArkanoidFamicom:
					internalQuickerNESControllerType = QuickerNESInternalControllerTypeEnumeration.ArkanoidFamicom; break;
			}

			// Parsing arkanoid inputs
			byte arkanoidPos = 0;
			byte arkanoidFire = 0;

			switch (_syncSettings.Port1)
			{
				case Port1PeripheralOption.ArkanoidNES:
					arkanoidPos = unchecked((byte)controller.AxisValue("P2 Paddle"));
					arkanoidFire = controller.IsPressed("P2 Fire") ? (byte) 1 : (byte) 0;
					break;

				case Port1PeripheralOption.ArkanoidFamicom:
					arkanoidPos = unchecked((byte)controller.AxisValue("P3 Paddle"));
					arkanoidFire = controller.IsPressed("P3 Fire") ? (byte) 1 : (byte) 0;
					break;
			}

			LibQuickNES.ThrowStringError(QN.qn_emulate_frame(Context, j1, j2, arkanoidPos, arkanoidFire, (uint) internalQuickerNESControllerType));
			IsLagFrame = QN.qn_get_joypad_read_count(Context) == 0;
			if (IsLagFrame)
				LagCount++;

			if (render)
				Blit();
			if (rendersound)
				DrainAudio();

			_callBack1?.Invoke();
			_callBack2?.Invoke();

			Frame++;

			return true;
		}

		private IntPtr Context;
		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.NES;
		public bool DeterministicEmulation => true;
		public string BoardName { get; }

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		public RomStatus? BootGodStatus { get; private set; }
		public string BootGodName { get; private set; }

		private void ComputeBootGod()
		{
			// inefficient, sloppy, etc etc
			var chrrom = _memoryDomains["CHR VROM"];
			var prgrom = _memoryDomains["PRG ROM"]!;

			var ms = new MemoryStream();
			for (int i = 0; i < prgrom.Size; i++)
				ms.WriteByte(prgrom.PeekByte(i));
			if (chrrom != null)
				for (int i = 0; i < chrrom.Size; i++)
					ms.WriteByte(chrrom.PeekByte(i));

			var sha1 = SHA1Checksum.ComputeDigestHex(ms.ToArray());
			Console.WriteLine("Hash for BootGod: {0}", sha1);

			// Bail out on ROM's known to not be playable by this core
			if (HashBlackList.Contains(sha1))
			{
				throw new UnsupportedGameException("Game known to not be playable in this core");
			}

			sha1 = $"{SHA1Checksum.PREFIX}:{sha1}";
			var carts = BootGodDb.Identify(sha1);

			if (carts.Count > 0)
			{
				Console.WriteLine("BootGod entry found: {0}", carts[0].Name);
				switch (carts[0].System)
				{
					case "NES-PAL":
					case "NES-PAL-A":
					case "NES-PAL-B":
					case "Dendy":
						Console.WriteLine("Bad region {0}! Failing over...", carts[0].System);
						throw new UnsupportedGameException("Unsupported region!");
				}

				BootGodStatus = RomStatus.GoodDump;
				BootGodName = carts[0].Name;
			}
			else
			{
				Console.WriteLine("No BootGod entry found.");
				BootGodStatus = null;
				BootGodName = null;
			}
		}

		public void Dispose()
		{
			if (Context != IntPtr.Zero)
			{
				QN.qn_delete(Context);
				Context = IntPtr.Zero;
			}
		}

		private void CheckDisposed()
		{
			if (Context == IntPtr.Zero)
				throw new ObjectDisposedException(nameof(QuickNES));
		}

		// Fix some incorrect ines header entries that QuickNES uses to load games.
		// we need to do this from the raw file since QuickNES hasn't had time to process it yet.
		private byte[] FixInesHeader(byte[] file)
		{
			var sha1 = SHA1Checksum.ComputeDigestHex(file);
			bool didSomething = false;

			Console.WriteLine(sha1);
			if (sha1== "93010514AA1300499ABC8F145D6ABCDBF3084090") // Ms. Pac Man (Tengen) [!]
			{
				file[6] &= 0xFE;
				didSomething = true;
			}

			if (didSomething)
			{
				Console.WriteLine("iNES header error detected, adjusting settings...");
				Console.WriteLine(sha1);
			}

			return file;
		}

		// These games are known to not work in quicknes but quicknes thinks it can run them, bail out if one of these is loaded
		private static readonly HashSet<string> HashBlackList = new HashSet<string>
		{
			"E39CA4477D3B96E1CE3A1C61D8055187EA5F1784", // Bill and Ted's Excellent Adventure
			"E8BC7E6BAE7032D571152F6834516535C34C68F0", // Bill and Ted's Excellent Adventure bad dump
			"401023BAE92A38B89F7D0C2E0F023E35F1FFEEFD", // Bill and Ted's Excellent Adventure bad dump
			"6270F9FF2BD0B32A23A45985D9D7FB2793E1CED3", // Bill and Ted's Excellent Adventure overdump dump
			"5E3C02A3A5F6CD4F2442311630F1C44A8E9DC7E2", // Paperboy
			"42A3920EF411E85CA6D8165B99A4FCD40B6038F3", // 6-in-1 (Game Star - GK-L01A) (Menu) [p1]
			"8271C26A03612CDFBC5B817C2452243AC48641A8", // 6-in-1 (Game Star - GK-L01A) (Menu) [p1][o1]
			"503C4F9F911E597D228DEA6AB7B37937FDED5243", // 54-in-1 (Game Star - GK-54) (Menu) [p1]
			"A4D9BE1F32173C849906EB33DC26D6AD13B2BD1A", // 54-in-1 (Game Star - GK-54) (Menu) [p1][o1]
			"C282476FD47797668FD165BD618554EB69D2718F", // 72-in-1 (Menu) [p1]
			"A8991144EC23CAF2F49775FADA0D861FC9E1CAB4", // 10000000-in-1 (Menu) [p1]
			"9C94B46C3F37F7888D8466E637D345608FBCF1E6", // A Ressha de Ikou (J) [b1]
			"20AA75A93B2909E79E5F292049D1894D9B89BD38", // A Ressha de Ikou (J) [b3]
			"67D8E0E135B8E38164D791F4846346D3AA5787C4", // Advanced Dungeons and Dragons - Hillsfar (J) [b1]
			"85DC8BF106CBE7E3359B30337DFB07ABB43A31B2", // Advanced Dungeons and Dragons - Hillsfar (J) [o1]
			"85DC8BF106CBE7E3359B30337DFB07ABB43A31B2", // Advanced Dungeons and Dragons - Hillsfar (J) [!]
			"B2662816D0367143D41A697B7B714F312E9AC125", // Advanced Dungeons and Dragons - Hillsfar (U) [!]
			"2462212CA9B3D2773EB0F36D806DACC20C7876AD", // Adventures of Lolo (U) [b3]
			"BF19A52458C5B773E9AC9AE1472E52DFF078E25B", // Akira (J) [hM04][t1]
			"1D5DA20A02E4AB7543BF13CFEBB622383162CE1A", // Akira (J) [hM04][t1][b2]
			"E8E1F3327ECFD81ECBEA96D5A58AF8CF6F0B481A", // America Oudan Ultra Quiz - Shijou Saidai no Tatakai (J) [b1]
			"D136CF62BD85A58994BD35AA8861C622655B0D8B", // America Oudan Ultra Quiz - Shijou Saidai no Tatakai (J) [o1]
			"6E5307B2AFF37D5D3A64AB43505C78F09804E8FC", // Atlantis no Nazo (J) [hM03][b5]
			"5C3B1873C21AA32FA1553F4C5CBEF5B16772FCA1", // Bad SMB1 (SMB1 Hack) [b1]
			"67E92A12177C861D62351FB67A5E9A490F6EABC7", // Bad SMB1 (SMB1 Hack) [b1][a1]
			"DA2C3D5CEB003AB1576996F1B2A64C72EA3E5136", // Bad SMB1 (SMB1 Hack) [b1][a3]
			"E601591E3B0183F2C5C1F4BF2D8302A8E1EBE683", // Bad SMB1 (SMB1 Hack) [b1][a5]
			"16BF1B6C9367B26E649511CCD52D62AB6FF6C06F", // Bad SMB1 (SMB1 Hack) [b1][a6]
			"AEE1B002A8C8AFCE533EB89F4A457430D8E4CEC4", // Bad SMB1 (SMB1 Hack) [b1][a7]
			"A9B4F80B4D137E767866BC5D7BBF79AEB1DA852C", // Bad SMB1 (SMB1 Hack) [b1][a8]
			"828271C30AA100CDA16499DC686D9F1ADC95EC9A", // Bad SMB1 (SMB1 Hack) [b1][a9]
			"9C69D57222BD2ED31C00EB222F2914633A5C09E5", // Bad SMB1 (SMB1 Hack) [b1][aa]
			"9401F010CF57E879E589BFDEAEFEF49FD2CA5F4C", // Bad SMB1 (SMB1 Hack) [b1][ab]
			"FEC5208BB1BC6B0C6342BD1B98E82F5C75D49886", // Bad SMB1 (SMB1 Hack) [b1][ac]
			"FFE45C6F1C02D126AB82CDC70B88203DE6EBE545", // Bad SMB1 (SMB1 Hack) [b1][ad]
			"A86CB96F1FAE9F6BC672465E28043E9F33DA4FAE", // Bakushou!! Jinsei Gekijou 3 (J) [p1][hM04]
			"FCE567F36BEC72FCC6AF4719ED4E67C107ED7E4D", // Bakushou!! Jinsei Gekijou 3 (J) [p1][hM04][b1]
			"073DCCE8E69F7FF8D96EDF1306E9802FFFFB1988", // Bakushou!! Jinsei Gekijou 3 (J) [p1][hM04][b3]
			"F9423CCC1AA711CB06B8F5C66E9304636F5E9B10", // Banana (J) [b1]
			"A419FB5749C74A8DE64140F0E51460DBAC31526B", // Baseball Stars II (U) [b4]
			"3A5341A47E72079FA85EFE5514A57D9D70C72107", // Bill World V0
			"424065D59A113833699C292E722675789E2A42FA", // Blaster Master Pimp Your Ride v1
			"D9F7FE3BAD6A25F8DC31389E15F5A5FBC0AE7446", // Boulder Dash (U) [b4]
			"DA8C226A7022A702492921E5CC8215FD02223C41", // Boulder Dash (U) [b5]
			"293B1E284ADA7677B7518FE4DC18E04BEBE14367", // Boulder Dash (U) [b6]
			"8B6AF34A4C705B17532BD4C80A121A4896EAA267", // Bugs Bunny Fun House (U) (Prototype) [b1]
			"D3D6C21E5E11AD325D66C49A6325DFE0B62D5C3E", // Burai Fighter (J)
			"EECC4BCC7697BFFA04C6425D2399E7F451175AD6", // Burai Fighter (U)
			"D2332E93093C5ACD2AF8E3F1380459DB09776329", // Business Wars (J)
			"998AED29B60F74A2F191F8A3480F8F60F55DBA2F", // Chibi Maruko-Chan - Uki Uki Shopping (J) [hM04]
			"F196DC527F16C172383B02FEFCEE66F3C490CF97", // Chibi Maruko-Chan - Uki Uki Shopping (J) [hM04][b2]
			"5CA9A644FBBDC97393E8BE9322CBCBEB05E1B4A5", // Chibi Maruko-Chan - Uki Uki Shopping (J) [hM04][b3]
			"F9984D4DB41A497C23B8E182B91088AF43EF3F00", // Chinese Character Demo (PD)
			"E79FC613112CC5AB0FC8B1150E182670FB042F4A", // Contra Fighter (Unl)
			"F49F55748D8F1139F26289C3D390A138AF627195", // CPUtime (PD)
			"A31B7F4BE478353442EED59EAAA71743A5C26C9D", // Crisis Force (J) [hM04]
			"D9A6384293002315B8663F8C5CD2CC9BB273BFB2", // Crisis Force (J) [hM04][b2]
			"DF3B2EC1EE818DA7C57672A82E76D9591C9D9DC1", // Cybernoid - The Fighting Machine
			"819C27583EA289301649BA3157709EB7C0E35800", // Demo 1 by zgh4000 (2006-03) (PD)
			"9EEA0CC3189B6A985C25D86B40D91CB6AFD87F89", // Demo 2 by zgh4000 (2006-03) (PD)
			"FF944D6D5A187834D4F796CD1C9FC91EA7BFADAC", // Demo 3 by zgh4000 (2006-07-26) (PD)
			"C2D136065E2FB92465EC061B6A73BB0ED97D51C2", // Devil Hunter Yohko Dithering Demo by Chris Covell (PD)
			"EB39B2E832AE07A9372B20E33EF380CFFD992C34", // Digital Devil Monogatari - Megami Tensei (J) [hM04][o1]
			"BCA548619ACE3D32A6F2543FA307AB4F6B4BCAAC", // Digital Devil Monogatari - Megami Tensei II (J) [hM04]
			"4C1C5E1890A1CF4C25C6D543A2B5CDEAAD2220DF", // Dragon Slayer 4 - Drasle Family (J) [!]
			"0CD9B2808C29F1236879E52D40188F4FA31332C0", // Dragon Spirit - Aratanaru Densetsu (J) [hM04]
			"1D64C56B161AE12195FDECD86D9E73627CB30729", // Dragon Unit (J) [p1]
			"24D87DE5789D19699EC1D01D21272E0BA1C96621", // Duck Hunt (W) [p1][T+Fre]
			"D4B221633548FEDDFF20185F28F82A3438A78BFD", // Duck Hunt (W) [p2]
			"593BCE6743E1743897BA1837F9738E14309563B5", // EarthWorm Jim 3 (Unl) [!]
			"33A9F3385238F778F85869CA687DFAC7BDCDD3A3", // EarthWorm Jim 3 (Unl) [a1]
			"17720AE1AFC6A3750384D6B082391C0C2F8A0699", // Family BASIC (J) (V2
			"8E90D9A6A6090307A7E408D1C1704D09BA8F94FC", // Family BASIC (J) (V2
			"E9CFA35A037CC218F01BFB4A1EB5D1D332EA2AA9", // Family BASIC (J) (V2
			"8904A8BF6F667ED977F2121AC887C7FE0CB969F0", // Family Boxing (J) [!]
			"EACFEEA2BD8887B044D0C06071FAF058C5DB137D", // Family Boxing (J) [b1]
			"8904A8BF6F667ED977F2121AC887C7FE0CB969F0", // Family Boxing (J) [b2]
			"6A87E0E0A880692C42E78813AD969D6C6CDACB83", // Family Computer - Othello (J) [!]
			"CA9257C01F6E190F7AE7998A3C1C681903EE0530", // Family School (J)
			"7708275B2C36B180D252FC9528843D89753BCA1F", // Family Trainer 4 - Jogging Race (J) [!]
			"EBB788D43F17F7603FF8DCE618D2C15CF66A469E", // FAU Screen Test (2003-09-23) (PD)
			"CAC7ED722CCA56B5B021F844E8614083C95E6760", // FAU Screen Test (PD)
			"A9DF3E38F8DEEEE5058F45172DCDE68A4FBD788D", // Full NES Palette Viewer - Optimized Version by Blargg (PD)
			"521AE7F44BC02F9CF5BD252172B3FDC610CD0529", // Full NES Palette Viewer 2 by Blargg (PD)
			"7E0047AD135D0DC49C0BE1A1D6B673F1D1189C62", // Full NES Palette Viewer by Blargg (PD)
			"06E03A9618A0A4D67CA770CF9557C2DE46C2B9A8", // Ganbare Goemon 2 (J) [hM04][b1]
			"50039187E64E5DB436DC3C56DC698A0B3B050D4F", // Ganbare Goemon 2 (J) [hM04][b2]
			"4C70634349D71631A31631C92FFAF3E461C2BEB1", // Ganbare Goemon 2 (J) [hM04][b3]
			"7AA10EB0C8763F092FE91763DE29A5374297A018", // Interlace Demo 1 by Chris Covell (PD)
			"4400F5811A0D6A27190FDD76898938E9D838E23C", // Interlace Demo 2 by Chris Covell (PD)
			"E0FB07CC22BDF5394F792259EC8C36E3CC06388B", // Interlace Demo 3 by Chris Covell (PD)
			"855ED5A83F31A33772E77E7960DC0F63B2B72F2A", // Interlace Demo 4 by Chris Covell (PD)
			"ED8ACE4BDA8DAA9832E12D59911A942B8C105A46", // Jajamaru Ninpou Chou (J) [hM04]
			"3EC73A1490D23AB25CDD9910FB1526658087FED6", // Jetsons, The - Cogswell's Caper! (E) [!]	NES
			"724B28125305C1103B17557DE1B930E5CBAC48E8", // Jetsons, The - Cogswell's Caper! (E) [t1]	NES
			"C95020E9E5f69EFCFA28EEDB5B4EBA955E20DDAC", // Jetsons, The - Cogswell's Caper! (J) [t1]	NES
			"F7AA0A3704E465F162dA5b1AEFF1B99C49FDD559", // Jetsons, The - Cogswell's Caper! (J)	NES
			"72FCD4A5AAA14B426AC8ABB8DB97E42BEDBBBE1D", // Jetsons, The - Cogswell's Caper! (U) [!]	NES
			"D328F4C44961B972551784364B0D622F007643F3", // Jetsons, The - Cogswell's Caper! (U) [b1]	NES
			"92DA3C931FF9BCE4EF3FDC14E74D35292FC4B6FF", // Jetsons, The - Cogswell's Caper! (U) [o1]	NES
			"7A7BCA9A30A9F1B8AD3B45FA7DD7C8C180F53640", // Jetsons, The - Cogswell's Caper! (U) [t1]	NES
			"123045D5E8CF038C2FD396BD266EEF96DAFF9BCD", // Jikuu Yuuden - Debias (J) [o1]
			"123045D5E8CF038C2FD396BD266EEF96DAFF9BCD", // Jikuu Yuuden - Debias (J) [!]
			"76DB18B90FB2B76FA685D6462846ED3A92F5CBD4", // Joe and Mac (U) [!] 
			"7E1C9F23BF9BECB7831459598339A4DC9A3CECFC", // Joe and Mac (E) [!]
			"A654DE12A59D07BAFF30DD6CB5E1AD05EB20B2D7", // Jumpy Demo by Rwin (PD)
			"DE42818873470458DF29F515A193F536A0642EA8", // Kamikaze Mario DX Plus V1
			"BFECB191CFD480B14B7169441DB3D389A4B634D2", // Kamikaze Mario DX Plus V1
			"BA2D68997B3580D59680B49BA71DF87159D41350", // Kamikaze Mario DX+ by79 (SMB1 Hack)
			"D17E19BB52E9C83D11D7A3362C4AAA733EFBD553", // Karate Champ (U) (REV0) [a1][!]
			"6A43DDDDF3668A7A57318BE0E8FBAB66547774A4", // Karnov (J) [b1][o1]
			"190BF6CEA6464C77C240DF3A4DAB65BA6B3CF625", // King Neptune's Adventure (Color Dreams) [!]
			"6BE670DFB4F49CB3F9024748AAEEBCD4499B5A9E", // Kiri5 Star (SMB1 Hack) [a1]
			"63F907C78BD1A8D0DB249EE447452186318B86CC", // Knight Rider (J) [b1]
			"5438D3F810767D07F5A7F2B39504ABDEF5E14346", // Knight Rider (J)
			"5AF88BE752FE06673874574A039AD03749C2BBA1", // Knight Rider (U) [!]
			"DE6437789335DC1EE92172D42A2A10A39ED7F648", // Knight Rider (U) [b1]
			"3D4CD96640ADB6336160BC72B3F5816991215FA6", // Knight Rider (U) [b2]
			"4EDBAC801F6185FADC882039ABB5123E482EE897", // Knight Rider (U) [b3]
			"39E36A5F14ACCCA95B7CC0BB68A96F769DF8DF13", // Knight Rider (U) [b4]
			"D94F0ABED2637D16E4C9613C427D4C55921B1A00", // Knight Rider (U) [b5]
			"5AF88BE752FE06673874574A039AD03749C2BBA1", // Knight Rider (U) [o1]
			"F0D4A36B8BAC7ED47978CE9C8A308AE0ABF0E768", // Kyoro Chan Land (J) [o2]
			"5F604D18935D69F1027C8ECBFCA46C1952F75953", // Kyuukyoku Harikiri Stadium (J) [hM04]
			"977A06EB9D191B287168AA3EF88CE992E78C13D2", // Lin Ze Xu Jin Yan (Ch) [f1] (NTSC by nfzxyd)
			"BFE87FACFA2222D9E4984B8A893E033BD5796A8E", // Magic Johnson's Fast Break (U) [b5]
			"5C3642576B73A92D63C4BC2DEA61337D6911424F", // Mario Adventure 2 by Krillian (SMB1 Hack)
			"E474EF05C1E1471768EA502F6427BA408BFB5168", // Mario Adventure by DahrkDaiz (SMB3 PRG1 Hack)
			"AC035F21428E9055C43FCD3E1119D15540D7FFFB", // Mario Different Levels (SMB1 Hack) [b3]
			"7B71EC3BC30998C3179190D5F1723F7BA784CDAE", // Mario Different Levels (SMB1 Hack)
			"E5F00271FE6799A089CF11F59B7418D347365737", // Mario in - Some Usual Day (SMB3 PRG1 Hack)
			"9AD449AADA74F1438F491CEC72591BD4F03FDCCD", // Mario Kai 2 (SMB1 Hack)
			"057D76406A98EE07224002132273D5FFB72447DC", // Mario MI41 (SMB1 Hack) [a1]
			"39AEE43C51B461002EEF2744DA2F312932839E44", // Mario Nasubi 3 (SMB1 Hack)
			"921E5D925CA16FB35462E0F1DED65B1CE3BA6FE6", // Mario's Adventure (SMB1 Hack)
			"E7937B33820AA3BC32682A64A8339BEAACED53F1", // Mario's Dream World by Darvon (SMB1 Hack)
			"34BECFEBBBAB586C952E73BDCB0550FCE2A56D10", // Megaman III Challenge Stage 1 (Hack)
			"CFD977F445E1492514BB987F14BDC52699028C8B", // Minelvaton Saga - Ragon no Fukkatsu (J) [hM04][b2]
			"00A315DF9B20EEC76D24CB00000C0D8875151A91", // MiniGame 2003 4-in-1 (PD)
			"98965784822A9CB4CD29EE63AC3DF256E9232E66", // Mushroom Dreams by Rage Games (V1
			"98965784822A9CB4CD29EE63AC3DF256E9232E66", // Mushroom Dreams by Rage Games (V1
			"7516140CF4814BF31E9E21489716364AAC60F995", // Namcot Mahjong 3 - Mahjong Tengoku (J) [hM04]
			"6D4ABE3415EC5A4FA8F53C80618D00DB506A6250", // Parodius da! (J) [p1][hM04]
			"FFC8409F6C37F23957F79093AD00E96B67DA6832", // Peach &amp; Daisy - The Royal Quest (Alpha) (SMB1 Hack) [a1]
			"3F1E4904938691D48BC858F85F9BE3AC8446077F", // Peach &amp; Daisy in The Ultimate Quest (SMB3 PRG0 Hack)
			"74D9D3C47B6D9C22B6B947FF33A794A24748058F", // Peach &amp; Daisy in The Ultimate Quest (SMB3 PRG1 Hack)
			"9E0D92EAAF32A3661B1F33AAAA2AFB5590F89B50", // Peach &amp; Daisy in The Ultimate Quest V2b (SMB3 PRG1 Hack)
			"1EBD8B27E8D4BA6B2A3C4A1C3E58AFF9B93870B8", // Peach's Nightmare - No Mercy (Beta) (SMB1 Hack)
			"029506CBEAA7B73CB622606D34BF8E7D07D82C3C", // Peach's Nightmare - No Mercy (SMB1 Hack)
			"60FC5FA5B5ACCAF3AEFEBA73FC8BFFD3C4DAE558", // Pegasus 5-in-1 (Golden Five) (Unl)
			"60FC5FA5B5ACCAF3AEFEBA73FC8BFFD3C4DAE558", // Pegasus 5-in-1 (Golden Five) (Unl) [o1]
			"841499E9E87E24AD0AFDC0C6A6F3152ABE4E8643", // Playbox BASIC (Prototype V0
			"406A0641D80F91C34ABA839E6978D250D3E3E611", // PPU Timing V2 by Kevin Horton (PD)
			"EE6554E05BCC447B9533AAC61B3841C491AD636D", // Predator - Schwarzenegger - Soon the Hunt Will Begin (Hack)
			"390443F9B8A69FEE3CFF5F234A3E92AAE8B48102", // Project Q (J)
			"C54C2C2E7F8FE4599570656FBFD2F3349A66B4BA", // Puzslot (J) [b1]
			"02C434FA365DAD5BA0DCCF789897E905FD60914E", // Return to Camelot by Castle Masters (PD) [a1]
			"CA74A7A9FE061CBD0AFFEF7BC358C789517A57B3", // Rocket Ranger (U) [!]
			"139C23F24EBDABAC86573C57390BBC720E7C9B1D", // Shuffle Fight (J) [b1]
			"E777176ABF8D118EBAB9B7A64AC69FF9F93DCC8A", // Shuffle Fight (J) [!]
			"5E4858A07330A7C1FE6EB9ADFDCE778043ADA5C6", // Shougi Meikan '92 (J) [!]
			"0A808A7EB907D1690927AD3468679CDD7A9158AD", // Shougi Meikan '93 (J) [!]
			"4F1B46185AE1E89DA2AB4DF54A1B07D5B553D204", // SMB1 with Mi22 (SMB1 Hack) [a1]
			"562DE00C418240552CEB6AAE82796F711CE8B5ED", // SMB1 with Mi22 (SMB1 Hack) [a2]
			"A0610726A7B9AECD0ECFFDF7CCAAC7AA021DE26D", // SMB1 with Mi22 (SMB1 Hack) [a3]
			"EB7E09BB47D4C5B22253555C8E0C4B71495ED0EA", // SMB1 with Mi22 (SMB1 Hack) [a4]
			"2433C04100E938CDA3EB0C461479AF16FA4E3945", // SMB1 with Mi22 (SMB1 Hack) [a5]
			"F455FD22DFE029039D328D8EAD88F87E08955833", // SMB1-155 (SMB1 Hack) [b1]
			"4184469BA7435429C87D95AB89E74D0D62BCAF78", // Snoopy's Silly Sports Spectacular (U) [b6]
			"AB30FD19583EF80C836F6DA8C21CCA878479DA00", // Snow Bros
			"44F2BA467EAE22E1D1133AFF56171A1B9C734D56", // Snow Bros
			"76E71F32551D60D3AF26EB4AD15F4BDE7C6CA29B", // Snow Bros
			"9D879BAFA963E283625B53C5514B4990D3641D35", // Sokoban by Johannes Holmberg (PD)
			"5A91F54A6FF44762D98FC8D8974909D298EB52A8", // Somari (SOMARI-P) (NT-616) (Unl) [!]
			"801E93C0D0E6A3DF01C1ADBA119D1B938C7FF377", // Space Boy (Unl)
			"57E44EF692FF5DD190A323ABEAD26855174A979E", // Spelunker (JP)
			"0BB2C29B35517E08E5DCBA6DB9AAE4B18055A84F", // Spelunker (US)
			"B6D1C372A38D196112AA98905C709AD844BD6627", // Super 35-in-1 (6-in-1 VT5201) [p1]
			"A0A70F6B8633E20648FBD2C2A0F9B8669F6F9337", // Super Balloon Fight Physics Mario (SMB1 Hack)
			"8E6C81992CFAD39621852B468A82656E068D3FAD", // Super Castlevania 2 V0
			"9B9CD214DC63BFA44E3D20B8E669E042BB93E900", // Super Koopa Bros by Mind_Bender (SMB1 Hack)
			"CF1A69448F2DDC3D542E20B1693DB349EC8343EC", // Super Mario Adventure (SMB1 Hack)
			"5EA2769F10567B4E2D049C8A891F370A2E3505DA", // Super Mario Bros 256w (SMB1 Hack) [a1]
			"CF2B9B4DE63C21BA0C555431A6C96A729B6E5E86", // Super Mario Bros 256w (SMB1 Hack)
			"A136ABC540F2A2281B378519F87C144B33CD1B27", // Super Mario Bros by RORAN (V051129) (SMB1 Hack)
			"FA3955983443CD56BF7DCEB2D76815E01FF49BCE", // Super Mario Bros Kuriboo (SMB1 Hack)
			"0A38A59635AD80B3F8D1084E410B904533F0E114", // Super Mario Bros SAB (SMB1 Hack)
			"3DB7F2C421CCB6BBADB3C65A69787575B8A7BAD4", // Super Mario Bros with Kanji Numbers (SMB1 Hack)
			"64C20BFF4F4C60F94C012ECF7EC59F8F06E4DA55", // Super Mario Bros Hack
			"794DC7F85B558C7A5AB8F9D475B5407CD1847713", // Super Mario Bros Hack
			"CE1B236ED0EAC133A3DE5411AEF57228220885EF", // Super Sonic 5 (1997) (Unl) [!]
			"AF725F26A418BB64A606E57718F7F42E98F1798B", // Super Sonic 5 (1997) (Unl) [f1]
			"92AE64DBEB8C287140F5D4395F6602682C267D63", // Super Yoshi (SMB1 Hack)
			"1EC9E1A4F7E30EF71BC236429FBB033C02E892E3", // Takeshi no Sengoku Fuuunji (J) [hM04]
			"B9F444FF60F60C177EEEC8671BEC3731B0F6FE49", // Tang Mu Li Xian Ji (Ch)
			"1DB0C2A5B03F27CDB15731E7A389E2CB4A33864A", // Tank Demo by Ian Bell (Mapper 0 PAL) (PD)
			"3B444F997A2DD961C491EBCC6404A5EFF6F3F91F", // Tank Demo by Ian Bell (Mapper 1 PAL) (PD)
			"7EF667D9BF107B6512565177B9C62081077558F5", // Tenchi wo Kurau II - Shokatsu Koumei Den (J) (PRG0) [T+Chi]
			"92CC033C1255F119B3A566EDCA10140C014FA479", // Tenchi wo Kurau II - Shokatsu Koumei Den (J) (PRG0) [T-Chi][a4]
			"EEF617A022B8E45E0BDE088FED654C89AD4FAABF", // Tenchi wo Kurau II - Shokatsu Koumei Den (J) (PRG0) [T-Chi][a2]
			"9A7D080AEADFAE8793E928E85C5A6A04D0F62F55", // Tetris Mario Bros (SMB1 Hack) [a1]
			"80410CB2D7EE2D7AD8BA02A53B12149376FAAB87", // Tetsuwan Atom (J) [hM04]
			"6A904B03EFDEE317736257CB78DD90BDA4E49268", // Tetsuwan Atom (J) [hM04][b1]
			"9D724154D2F5629157384715B3782D6C304AC957", // Tetsuwan Atom (J) [hM04][b2]
			"8B3CA684081CC60B40EA76AAB6B4E6F32B27F8A7", // Tiny Toon Adventures Cartoon Workshop (U) [!]
			"12F44FB720137ACEBCC609E8BA059E845D04A03A", // Tiny Toon Adventures Cartoon Workshop (U) [b1]
			"8B3CA684081CC60B40EA76AAB6B4E6F32B27F8A7", // Tiny Toon Adventures Cartoon Workshop (U) [o1]
			"4348469BF59233EB3AB68C005A422347C6708762", // Tower RE Mario Bros (SMB1 Hack)
			"179F2A9D5AFB6C78CE7346BB1C822EF48B18842A", // Yoshi's Quest (SMB1 Hack)
			"B2C0F095AD39F7BAF8B0D9CA7050DFD0A92BC69E", // Donald Land (J) [hM04]
			"8A91E213A653AB12027DE09603799C6B0819450A", // Duckwater by Overkil (PD)
			"A722F8076894207282A416187BAC19B7CE2D4087", // Fighting for Dignity (Easy) (Captain Tsubasa II Hack)
			"A03F18E46C8F04773D8AC0BE68828E8661BD0409", // Fighting for Dignity (Expert) (Captain Tsubasa II Hack)
			"0618A0E60DF8174A865192623BE4D1A70EEDC412", // Fighting for Dignity (Hard) (Captain Tsubasa II Hack)
			"40D614BDF3DC9C624AFD3495BBEC1CB0230CA9BC", // Fighting for Dignity (Normal) (Captain Tsubasa II Hack)
			"6E76825E4A9B7335D48937817B96E65DCB1FD8C5", // Flowing Palette by Blargg (PD)
			"2DD7126BE8147A021501DA7016A3A7AF25D00B10", // Hello World (PD)
			"25D4AE575CDAE6E4513310AAC632D37EAF49D019", // Improved 400+ Color Palette Demo by Blargg (PD) [a1]
			"2D15E2BF197AA7E682EA94767EA81C63ED73D33A", // Improved 400+ Color Palette Demo by Blargg (PD)
			"60AD2A26053DEAB6D0B6148B53A025FD89743035", // Mortal Kombat II (Unl) (REV.B) [!]
			"F3D43BCC7E75D78477F96ACB5544A9BA0FF7564D", // Mortal Kombat III Turbo - 18 People (Ch)
			"8BA9B8629AE755FB1A41E2FCB608F0FFB54B2902", // Mortal Kombat III Turbo - 18 People (NT-851) (Ch) [!]
			"F9E1C94C16AE4196BC8814AB2A32F6156AE46C82", // NEStronome by Ernesto Borio (2009) (PD)
			"B0F9A7BDE0A4AEAD2847679662D68F2471875EEB", // Parasol Stars - The Story of Bubble Bobble 3 (E) [t1]
			"004B1CCEBA54E4192EE8789B9A6AD131E56DD241", // rNES_demo by Ernesto Borio (2009) (PD)
			"986F02624DB41425D89D8C8632F77F2FFC860D04", // Radac Tailor-Made (J) (Sample)
			"BC1734BEE472D34F489A6F5F2530A019F28055B7", // 800-in-1 [p1][b1]
			"4543F0D7EB387793F6C92FC6A075AA776C07085A", // Atari RBI Baseball (VS) [b1]
			"B35C68AC81CC2D2B13237B6FF3927F3DFC852226", // Atari RBI Baseball (VS)
			"085ACDCB5E1FB136F74DC5265C85F0C45CFE98AA", // Balloon Fight (VS) (Player 2 Mode) [b1]
			"E1BB6A1858E57D83AC84784075D89D7EDFE066ED", // Baseball (VS) (Player 1 Mode) [b3]
			"6287B45DD16BD8366E9D58A6F135ECD81502A1B4", // Baseball (VS) (Player 1 Mode) [b2]
			"7AF6CBA6BF62A7D1B5A5EB310738645064FF9945", // Baseball (VS) (Player 1 Mode) [b1]
			"48F46D306CD2EFDCEF1B4066D7A5067AD5C57B34", // Baseball (VS) (Player 2 Mode) [b3]
			"C57B1F474D78357141918BF777C10C8E68D47546", // Baseball (VS) (Player 2 Mode) [b2]
			"E9C2F93FF3E7E9ACDCFFCCD0C1A1BDD0AE415B0C", // Baseball (VS) (Player 2 Mode) [b1]
			"6A01FB7F185A45BAA21CC1EEDEB945CACA1C4D92", // Battle City (VS) [p1][!]
			"6A01FB7F185A45BAA21CC1EEDEB945CACA1C4D92", // Battle City (VS) [p1][o1]
			"2191BC8619EF2EC4E242FFC42402E6764FB4A740", // Dr. Mario (VS)
			"77959F436F2A0D18249A44133FC4068B61029283", // Ice Climber (VS) (Player 1 Mode) [b1]
			"200E5B57CE68676E5B5159A551EBBE8EBFBA063F", // Ice Climber (VS) (Player 2 Mode) [b1]
			"A8548AE518289D276B93589A8BD0759134FEAEA4", // Mahjong (VS) (Player 1 Mode) [b1]
			"EE89B382CF21A2E4E1806059EA0FBB192435C1CA", // Mahjong (VS) (Player 2 Mode) [b1]
			"EE89B382CF21A2E4E1806059EA0FBB192435C1CA", // Mahjong (VS) (Player 2 Mode) [b1]
			"D7AB201265390588AB3CF3C2BB6B72BEA3F97137", // Super Xevious - Gump no Nazo (VS) [b1]
			"E291969D53D245E2E4F932D16E61B7DDE2E2570F", // Super Xevious - Gump no Nazo (VS) [b2]
			"3F9CB2322FBAD6671DF328A77D5B89FB8299F213", // VS. TKO Boxing (VS) [!]
			"8EC5D4DEED22E230020596993BB1C42AEB2215DA", // VS. TKO Boxing (VS) [a1]
			"6A01FB7F185A45BAA21CC1EEDEB945CACA1C4D92", // Battle City (VS) [p1][!]
			"D9B1B87204E025A637821A0168475E1209CE0C8A", // Top Gun (VS)
			"4D5C2BF0B8EAA1690182D692A02BE1CC871481F4", // Punch-Out!! (E) (VS)
		};
	}
}
