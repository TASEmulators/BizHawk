using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IEmulator
	{
		public string SystemId { get { return "A26"; } }
		public GameInfo game;

		public string BoardName { get { return mapper.GetType().Name; } }

		public CoreComm CoreComm { get; private set; }
		public IVideoProvider VideoProvider { get { return tia; } }
		public ISoundProvider SoundProvider { get { return dcfilter; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(dcfilter, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public Atari2600(CoreComm comm, GameInfo game, byte[] rom, object Settings, object SyncSettings)
		{
			CoreComm = comm;
			this.Settings = (A2600Settings)Settings ?? A2600Settings.GetDefaults();
			this.SyncSettings = (A2600SyncSettings)SyncSettings ?? A2600SyncSettings.GetDefaults();

			var domains = new List<MemoryDomain>(1)
				{
					new MemoryDomain("Main RAM", 128, MemoryDomain.Endian.Little, addr => ram[addr], (addr, value) => ram[addr] = value),
					new MemoryDomain("TIA", 16, MemoryDomain.Endian.Little, addr => tia.ReadMemory((ushort) addr, true),
					                 (addr, value) => tia.WriteMemory((ushort) addr, value)),
					new MemoryDomain("PIA", 1024, MemoryDomain.Endian.Little, addr => m6532.ReadMemory((ushort) addr, true),
					                 (addr, value) => m6532.WriteMemory((ushort) addr, value)),
					new MemoryDomain("System Bus", 8192, MemoryDomain.Endian.Little, addr => mapper.PeekMemory((ushort) addr), (addr, value) => { })
				};
			memoryDomains = new MemoryDomainList(domains);
			CoreComm.CpuTraceAvailable = true;
			this.rom = rom;
			this.game = game;

			if (!game.GetOptionsDict().ContainsKey("m"))
				DetectMapper();

			Console.WriteLine("Game uses mapper " + game.GetOptionsDict()["m"]);
			HardReset();
		}

		void DetectMapper()
		{
			string m = "UNKNOWN";
			switch (rom.Length)
			{
				case 2048: m = "2K"; break;
				case 4096: m = "4K"; break;
			}
			game.AddOption("m", m);
		}


		public List<KeyValuePair<string, int>> GetCpuFlagsAndRegisters()
		{
			return new List<KeyValuePair<string, int>>
			{
				new KeyValuePair<string, int>("A", cpu.A),
				new KeyValuePair<string, int>("X", cpu.X),
				new KeyValuePair<string, int>("Y", cpu.Y),
				new KeyValuePair<string, int>("S", cpu.S),
				new KeyValuePair<string, int>("PC", cpu.PC),
				new KeyValuePair<string, int>("Flag C", cpu.FlagC ? 1 : 0),
				new KeyValuePair<string, int>("Flag Z", cpu.FlagZ ? 1 : 0),
				new KeyValuePair<string, int>("Flag I", cpu.FlagI ? 1 : 0),
				new KeyValuePair<string, int>("Flag D", cpu.FlagD ? 1 : 0),
				new KeyValuePair<string, int>("Flag B", cpu.FlagB ? 1 : 0),
				new KeyValuePair<string, int>("Flag V", cpu.FlagV ? 1 : 0),
				new KeyValuePair<string, int>("Flag N", cpu.FlagN ? 1 : 0),
				new KeyValuePair<string, int>("Flag T", cpu.FlagT ? 1 : 0)
			};
		}

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public static readonly ControllerDefinition Atari2600ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 2600 Basic Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button", 
				"Reset", "Select"
			}
		};

		void SyncState(Serializer ser)
		{
			ser.BeginSection("A2600");
			cpu.SyncState(ser);
			ser.Sync("ram", ref ram, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			tia.SyncState(ser);
			m6532.SyncState(ser);
			ser.BeginSection("Mapper");
			mapper.SyncState(ser);
			ser.EndSection();
			ser.EndSection();
		}

		public ControllerDefinition ControllerDefinition { get { return Atari2600ControllerDefinition; } }
		public IController Controller { get; set; }

		public int Frame { get { return _frame; } set { _frame = value; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return _islag; } }
		private bool _islag = true;
		private int _lagcount;
		private int _frame;

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }

		public bool DeterministicEmulation { get; set; }
		public void SaveStateText(TextWriter writer) { SyncState(Serializer.CreateTextWriter(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(Serializer.CreateTextReader(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public bool BinarySaveStatesPreferred { get { return false; } }

		private readonly MemoryDomainList memoryDomains;
		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }
		public void Dispose() { }

		public object GetSettings() { return Settings.Clone(); }
		public object GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(object o) { Settings = (A2600Settings)o; return false; }
		public bool PutSyncSettings(object o) { SyncSettings = (A2600SyncSettings)o; return false; }

		public A2600Settings Settings { get; private set; }
		public A2600SyncSettings SyncSettings { get; private set; }

		public class A2600Settings
		{
			// todo: descriptions
			public bool ShowBG { get; set; }
			public bool ShowPlayer1 { get; set; }
			public bool ShowPlayer2 { get; set; }
			public bool ShowMissle1 { get; set; }
			public bool ShowMissle2 { get; set; }
			public bool ShowBall { get; set; }
			public bool ShowPlayfield { get; set; }

			public A2600Settings Clone()
			{
				return (A2600Settings)MemberwiseClone();
			}
			public static A2600Settings GetDefaults()
			{
				return new A2600Settings
				{
					ShowBG = true,
					ShowPlayer1 = true,
					ShowPlayer2 = true,
					ShowMissle1 = true,
					ShowMissle2 = true,
					ShowBall = true,
					ShowPlayfield = true
				};
			}
		}

		public class A2600SyncSettings
		{
			[Description("Set the TV Type switch on the console to B&W or Color")]
			public bool BW { get; set; }
			[Description("Set the Left Difficulty switch on the console")]
			public bool LeftDifficulty { get; set; }
			[Description("Set the Right Difficulty switch on the console")]
			public bool RightDifficulty { get; set; }

			public A2600SyncSettings Clone()
			{
				return (A2600SyncSettings)MemberwiseClone();
			}
			public static A2600SyncSettings GetDefaults()
			{
				return new A2600SyncSettings
				{
					BW = false,
					LeftDifficulty = true,
					RightDifficulty = true
				};
			}
		}
	}

}
