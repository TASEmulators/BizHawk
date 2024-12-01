using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	[Core(CoreNames.ColecoHawk, "Vecna")]
	[ServiceNotApplicable(typeof(ISaveRam))]
	public sealed partial class ColecoVision : IEmulator, IDebuggable, IInputPollable, ISettable<ColecoVision.ColecoSettings, ColecoVision.ColecoSyncSettings>
	{
		[CoreConstructor(VSystemID.Raw.Coleco)]
		public ColecoVision(CoreComm comm, GameInfo game, byte[] rom,
			ColecoSettings settings,
			ColecoSyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			_syncSettings = syncSettings ?? new ColecoSyncSettings();
			bool skipBios = _syncSettings.SkipBiosIntro;

			_cpu = new Z80A<CpuLink>(new CpuLink(this));

			PSG = new SN76489col();
			SGM_sound = new AY_3_8910_SGM();
			_blip.SetRates(3579545, 44100);

			ControllerDeck = new ColecoVisionControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			_vdp = new TMS9918A(_cpu);
			ser.Register<IVideoProvider>(_vdp);
			ser.Register<IStatable>(new StateSerializer(SyncState));

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			_biosRom = comm.CoreFileProvider.GetFirmwareOrThrow(new("Coleco", "Bios"), "Coleco BIOS file is required.");

			// gamedb can overwrite the SyncSettings; this is ok
			if (game["NoSkip"])
			{
				skipBios = false;
			}

			use_SGM = _syncSettings.UseSGM;

			if (use_SGM)
			{
				Console.WriteLine("Using the Super Game Module");
			}
			
			LoadRom(rom, skipBios);
			SetupMemoryDomains();

			ser.Register<IDisassemblable>(_cpu);
			ser.Register<ITraceable>(_tracer = new TraceBuffer(_cpu.TraceHeader));
		}

		private readonly Z80A<CpuLink> _cpu;
		private readonly TMS9918A _vdp;
		private readonly byte[] _biosRom;
		private readonly TraceBuffer _tracer;

		private byte[] _romData;
		private byte[] _ram = new byte[1024];
		public byte[] SGM_high_RAM = new byte[0x6000];
		public byte[] SGM_low_RAM = new byte[0x2000];

		public bool temp_1_prev, temp_2_prev;

		private int _frame;
		private IController _controller = NullController.Instance;

		private enum InputPortMode
		{
			Left, Right
		}

		private InputPortMode _inputPortSelection;

		public ColecoVisionControllerDeck ControllerDeck { get; }

		private void LoadRom(byte[] rom, bool skipbios)
		{
			if (rom.Length <= 32768)
			{
				_romData = new byte[0x8000];
				for (int i = 0; i < 0x8000; i++)
				{
					_romData[i] = rom[i % rom.Length];
				}
			}
			else
			{
				// all original ColecoVision games had 32k or less of ROM
				// so, if we have more then that, we must be using a MegaCart mapper
				is_MC = true;

				_romData = rom;
			}

			// hack to skip colecovision title screen
			if (skipbios)
			{
				_romData[0] = 0x55;
				_romData[1] = 0xAA;
			}
		}

		private byte ReadPort(ushort port)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
				{
					return _vdp.ReadData();
				}

				return _vdp.ReadVdpStatus();
			}

			if (port >= 0xE0)
			{
				if ((port & 1) == 0)
				{
					return ReadController1();
				}

				return ReadController2();
			}

			if (use_SGM)
			{
				if (port == 0x50)
				{
					return SGM_sound.port_sel;
				}

				if (port == 0x52)
				{
					return SGM_sound.ReadReg();
				}

				if (port == 0x53)
				{
					return port_0x53;
				}

				if (port == 0x7F)
				{
					return port_0x7F;
				}
			}

			return 0xFF;
		}

		private void WritePort(ushort port, byte value)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port <= 0xBF)
			{
				if ((port & 1) == 0)
				{
					_vdp.WriteVdpData(value);
				}
				else
				{
					_vdp.WriteVdpControl(value);
				}

				return;
			}

			if (port >= 0x80 && port <= 0x9F)
			{
				_inputPortSelection = InputPortMode.Right;
				return;
			}

			if (port >= 0xC0 && port <= 0xDF)
			{
				_inputPortSelection = InputPortMode.Left;
				return;
			}

			if (port >= 0xE0)
			{
				PSG.WriteReg(value);
			}

			if (use_SGM)
			{
				if (port == 0x50)
				{
					SGM_sound.port_sel = (byte)(value & 0xF);
				}

				if (port == 0x51)
				{
					SGM_sound.WriteReg(value);
				}

				if (port == 0x53)
				{
					if ((value & 1) > 0)
					{
						enable_SGM_high = true;
					}
					else
					{
						// NOTE: the documentation states that you shouldn't turn RAM back off once enabling it
						// so we won't do anything here
					}

					port_0x53 = value;
				}

				if (port == 0x7F)
				{
					if (value == 0xF)
					{
						enable_SGM_low = false;
					}
					else if (value == 0xD)
					{
						enable_SGM_low = true;
					}

					port_0x7F = value;
				}
			}
		}

		private byte ReadController1()
		{
			_isLag = false;
			byte retval;
			if (_inputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort1(_controller, true, true);
				return retval;
			}

			if (_inputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort1(_controller, false, true);
				return retval;
			}

			return 0x7F;
		}

		private byte ReadController2()
		{
			_isLag = false;
			byte retval;
			if (_inputPortSelection == InputPortMode.Left)
			{
				retval = ControllerDeck.ReadPort2(_controller, true, true);
				return retval;
			}

			if (_inputPortSelection == InputPortMode.Right)
			{
				retval = ControllerDeck.ReadPort2(_controller, false, true);
				return retval;
			}

			return 0x7F;
		}

		private byte ReadMemory(ushort addr)
		{
			if (addr >= 0x8000)
			{
				if (!is_MC)
				{
					return _romData[addr & 0x7FFF];
				}
				else
				{
					// reading from 0xFFC0 to 0xFFFF triggers bank switching
					// I don't know if it happens before or after the read though

					if (addr >= 0xFFC0)
					{
						MC_bank = (addr - 0xFFC0) & (_romData.Length / 0x4000 - 1);
					}
					
					// the first 16K of the map is always the last 16k of the ROM
					if (addr < 0xC000)
					{
						return _romData[_romData.Length - 0x4000 + (addr - 0x8000)];
					}
					else
					{
						return _romData[MC_bank * 0x4000 + (addr - 0xC000)];
					}
				}
			}

			if (!enable_SGM_high)
			{
				if (addr >= 0x6000)
				{
					return _ram[addr & 1023];
				}
			}
			else
			{
				if (addr >= 0x2000)
				{
					return SGM_high_RAM[addr - 0x2000];
				}
			}

			if (addr < 0x2000)
			{
				if (!enable_SGM_low)
				{
					return _biosRom[addr];
				}
				else
				{
					return SGM_low_RAM[addr];
				}
			}

			////Console.WriteLine("Unhandled read at {0:X4}", addr);
			return 0xFF;
		}

		private void WriteMemory(ushort addr, byte value)
		{
			if (!enable_SGM_high)
			{
				if (addr >= 0x6000 && addr < 0x8000)
				{
					_ram[addr & 1023] = value;
				}
			}
			else
			{
				if (addr >= 0x2000 && addr < 0x8000)
				{
					SGM_high_RAM[addr - 0x2000] = value;
				}
			}

			if (addr < 0x2000)
			{
				if (enable_SGM_low)
				{
					SGM_low_RAM[addr] = value;
				}
			}
			////Console.WriteLine("Unhandled write at {0:X4}:{1:X2}", addr, value);
		}

		private void HardReset()
		{
			PSG.Reset();
			_cpu.Reset();
		}

		private void SoftReset()
		{
			PSG.Reset();
			_cpu.Reset();
		}
	}
}
