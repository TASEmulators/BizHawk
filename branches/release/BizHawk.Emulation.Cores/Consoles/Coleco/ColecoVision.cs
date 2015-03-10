using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components;
using BizHawk.Emulation.Cores.Components.Z80;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	[CoreAttributes(
		"ColecoHawk",
		"Vecna",
		isPorted: false,
		isReleased: true
		)]
	[ServiceNotApplicable(typeof(ISaveRam), typeof(IDriveLight))]
	public sealed partial class ColecoVision : IEmulator, IDebuggable, IInputPollable, IStatable, ISettable<object, ColecoVision.ColecoSyncSettings>
	{
		// ROM
		public byte[] RomData;
		public int RomLength;

		public byte[] BiosRom;

		// Machine
		public Z80A Cpu;
		public TMS9918A VDP;
		public SN76489 PSG;
		public byte[] Ram = new byte[1024];

		[CoreConstructor("Coleco")]
		public ColecoVision(CoreComm comm, GameInfo game, byte[] rom, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			MemoryCallbacks = new MemoryCallbackSystem();
			CoreComm = comm;
			_syncSettings = (ColecoSyncSettings)SyncSettings ?? new ColecoSyncSettings();
			bool skipbios = this._syncSettings.SkipBiosIntro;

			Cpu = new Z80A();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.ReadHardware = ReadPort;
			Cpu.WriteHardware = WritePort;
			Cpu.MemoryCallbacks = MemoryCallbacks;

			VDP = new TMS9918A(Cpu);
			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(VDP);
			PSG = new SN76489();

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			BiosRom = CoreComm.CoreFileProvider.GetFirmware("Coleco", "Bios", true, "Coleco BIOS file is required.");

			// gamedb can overwrite the syncsettings; this is ok
			if (game["NoSkip"])
				skipbios = false;
			LoadRom(rom, skipbios);
			this.game = game;
			SetupMemoryDomains();
			(ServiceProvider as BasicServiceProvider).Register<IDisassemblable>(new Disassembler());
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		const ushort RamSizeMask = 0x03FF;

		public void FrameAdvance(bool render, bool renderSound)
		{
			Frame++;
			_isLag = true;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			VDP.ExecuteFrame();
			PSG.EndFrame(Cpu.TotalExecutedCycles);

			if (_isLag)
			{
				LagCount++;
			}
		}

		void LoadRom(byte[] rom, bool skipbios)
		{
			RomData = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
				RomData[i] = rom[i % rom.Length];

			// hack to skip colecovision title screen
			if (skipbios)
			{
				RomData[0] = 0x55;
				RomData[1] = 0xAA;
			}
		}

		byte ReadPort(ushort port)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
					return VDP.ReadData();
				return VDP.ReadVdpStatus();
			}

			if (port >= 0xE0)
			{
				if ((port & 1) == 0)
					return ReadController1();
				return ReadController2();
			}

			return 0xFF;
		}

		void WritePort(ushort port, byte value)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port <= 0xBF)
			{
				if ((port & 1) == 0)
					VDP.WriteVdpData(value);
				else
					VDP.WriteVdpControl(value);
				return;
			}

			if (port >= 0x80 && port <= 0x9F)
			{
				InputPortSelection = InputPortMode.Right;
				return;
			}

			if (port >= 0xC0 && port <= 0xDF)
			{
				InputPortSelection = InputPortMode.Left;
				return;
			}

			if (port >= 0xE0)
			{
				PSG.WritePsgData(value, Cpu.TotalExecutedCycles);
				return;
			}
		}

		public bool DeterministicEmulation { get { return true; } }

		public void Dispose() { }
		public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public string SystemId { get { return "Coleco"; } }
		public GameInfo game;
		public CoreComm CoreComm { get; private set; }
		public ISoundProvider SoundProvider { get { return PSG; } }

		public string BoardName { get { return null; } }

		public ISyncSoundProvider SyncSoundProvider { get { return null; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }
	}
}