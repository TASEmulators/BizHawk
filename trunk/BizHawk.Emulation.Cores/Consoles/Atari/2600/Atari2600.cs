using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IEmulator
	{
		private readonly GameInfo _game;
		private bool _islag = true;
		private int _lagcount;
		private int _frame;

		public Atari2600(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			Ram = new byte[128];
			CoreComm = comm;
			Settings = (A2600Settings)settings ?? A2600Settings.GetDefaults();
			SyncSettings = (A2600SyncSettings)syncSettings ?? A2600SyncSettings.GetDefaults();

			var domains = new List<MemoryDomain>
			{
				new MemoryDomain(
					"Main RAM",
					128,
					MemoryDomain.Endian.Little,
					addr => Ram[addr],
					(addr, value) => Ram[addr] = value),
				new MemoryDomain(
					"TIA",
					16,
					MemoryDomain.Endian.Little,
					addr => _tia.ReadMemory((ushort)addr, true),
					(addr, value) => this._tia.WriteMemory((ushort)addr, value)),
				new MemoryDomain(
					"PIA",
					1024,
					MemoryDomain.Endian.Little,
					addr => M6532.ReadMemory((ushort)addr, true),
					(addr, value) => M6532.WriteMemory((ushort)addr, value)),
				new MemoryDomain(
					"System Bus",
					8192,
					MemoryDomain.Endian.Little,
					addr => _mapper.PeekMemory((ushort) addr),
					(addr, value) => { })
			};

			CoreComm.CpuTraceAvailable = true;
			Rom = rom;
			_game = game;

			if (!game.GetOptionsDict().ContainsKey("m"))
			{
				game.AddOption("m", DetectMapper(rom));
			}

			Console.WriteLine("Game uses mapper " + game.GetOptionsDict()["m"]);
			RebootCore();

			if (_mapper is mDPC) // TODO: also mDPCPlus
			{
				domains.Add(new MemoryDomain(
					"DPC",
					2048,
					MemoryDomain.Endian.Little,
					addr => (_mapper as mDPC).DspData[addr],
					(addr, value) => (_mapper as mDPC).DspData[addr] = value));
			}

			if (_mapper.HasCartRam)
			{
				domains.Add(new MemoryDomain(
					"Cart Ram",
					_mapper.CartRam.Len,
					MemoryDomain.Endian.Little,
					addr => _mapper.CartRam[addr],
					(addr, value) => _mapper.CartRam[addr] = value));
			}

			MemoryDomains = new MemoryDomainList(domains);
		}

		public string SystemId { get { return "A26"; } }

		public string BoardName { get { return _mapper.GetType().Name; } }

		public CoreComm CoreComm { get; private set; }

		public IVideoProvider VideoProvider { get { return _tia; } }

		public ISoundProvider SoundProvider { get { return _dcfilter; } }

		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(_dcfilter, 735); } }

		public ControllerDefinition ControllerDefinition { get { return Atari2600ControllerDefinition; } }

		public IController Controller { get; set; }

		public int Frame { get { return _frame; } set { _frame = value; } }

		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }

		public bool IsLagFrame { get { return _islag; } }

		public bool SaveRamModified { get; set; }

		public bool DeterministicEmulation { get; set; }

		public bool BinarySaveStatesPreferred { get { return false; } }

		public A2600Settings Settings { get; private set; }

		public A2600SyncSettings SyncSettings { get; private set; }

		public MemoryDomainList MemoryDomains { get; private set; }

		public static readonly ControllerDefinition Atari2600ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 2600 Basic Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button", 
				"Reset", "Select", "Power"
			}
		};

		public int CurrentScanLine
		{
			get { return _tia.CurrentScanLine; }
		}

		public bool IsVsync
		{
			get { return _tia.IsVSync; }
		}

		public bool IsVBlank
		{
			get { return _tia.IsVBlank; }
		}

		public CompactGameInfo GenerateGameDbEntry()
		{
			return new CompactGameInfo
			{
				Name = _game.Name,
				System = "A26",
				MetaData = "m=" + _mapper.GetType().ToString().Split('.').ToList().Last(),
				Hash = Util.Hash_SHA1(Rom),
				Region = _game.Region,
				Status = RomStatus.Unknown
			};
		}

		// TODO: rewrite GetCpuFlagsAndRegisters this way
		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", Cpu.A },
				{ "X", Cpu.X },
				{ "Y", Cpu.Y },
				{ "S", Cpu.S },
				{ "PC", Cpu.PC },

				{ "Flag C", Cpu.FlagC ? 1 : 0 },
				{ "Flag Z", Cpu.FlagZ ? 1 : 0 },
				{ "Flag I", Cpu.FlagI ? 1 : 0 },
				{ "Flag D", Cpu.FlagD ? 1 : 0 },

				{ "Flag B", Cpu.FlagB ? 1 : 0 },
				{ "Flag V", Cpu.FlagV ? 1 : 0 },
				{ "Flag N", Cpu.FlagN ? 1 : 0 },
				{ "Flag T", Cpu.FlagT ? 1 : 0 }
			};
		}

		public bool StartAsyncSound() { return true; }

		public void EndAsyncSound() { }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("A2600");
			Cpu.SyncState(ser);
			ser.Sync("ram", ref this.Ram, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			_tia.SyncState(ser);
			M6532.SyncState(ser);
			ser.BeginSection("Mapper");
			_mapper.SyncState(ser);
			ser.EndSection();
			ser.EndSection();
		}

		public byte[] ReadSaveRam()
		{
			return null;
		}

		public void StoreSaveRam(byte[] data) { }
		
		public void ClearSaveRam() { }

		public void SaveStateText(TextWriter writer)
		{
			SyncState(Serializer.CreateTextWriter(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(Serializer.CreateTextReader(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(Serializer.CreateBinaryReader(br));
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public void Dispose() { }

		public object GetSettings()
		{
			return Settings.Clone();
		}

		public object GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			Settings = (A2600Settings)o;
			return false;
		}

		public bool PutSyncSettings(object o)
		{
			SyncSettings = (A2600SyncSettings)o;
			return false;
		}

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
