using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
	public partial class Atari2600 : IEmulator
	{
		public string SystemId { get { return "A26"; } }
		public GameInfo game;

		public CoreComm CoreComm { get; private set; }
		public IVideoProvider VideoProvider { get { return tia; } }
		public ISoundProvider SoundProvider { get { return dcfilter; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(dcfilter, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public Atari2600(CoreComm comm, GameInfo game, byte[] rom)
		{
			CoreComm = comm;
			var domains = new List<MemoryDomain>(1)
				{
					new MemoryDomain("Main RAM", 128, Endian.Little, addr => ram[addr & 127], (addr, value) => ram[addr & 127] = value),
					new MemoryDomain("TIA", 16, Endian.Little, addr => tia.ReadMemory((ushort) addr, true),
					                 (addr, value) => tia.WriteMemory((ushort) addr, value)),
					new MemoryDomain("PIA", 1024, Endian.Little, addr => m6532.ReadMemory((ushort) addr, true),
					                 (addr, value) => m6532.WriteMemory((ushort) addr, value)),
					new MemoryDomain("System Bus", 8192, Endian.Little, addr => mapper.PeekMemory((ushort) addr), (addr, value) => { })
				};
			memoryDomains = domains.AsReadOnly();
			CoreComm.CpuTraceAvailable = true;
			this.rom = rom;
			this.game = game;
			Console.WriteLine("Game uses mapper " + game.GetOptionsDict()["m"]);
			HardReset();
		}
		public void ResetFrameCounter()
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

		private readonly IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		public void Dispose() { }
	}

}
