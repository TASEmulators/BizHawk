using System;
using System.Collections.Generic;
using System.IO;
using EMU7800.Core;

namespace BizHawk
{
	public partial class Atari7800 : IEmulator
	{
		public string SystemId { get { return "A78"; } } //TODO: are we going to allow this core to do 2600 games?
		public GameInfo game;

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;
			_islag = true;

			//TODO
			FrameBuffer fb = new FrameBuffer(262, 320); //TODO: 262 is NTSC
			theMachine.ComputeNextFrame(fb);

			
			if (_islag)
			{
				LagCount++;
			}

			videoProvider.FillFrameBuffer();
		}

		/* TODO */
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public ISyncSoundProvider SyncSoundProvider { get { return null; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }
		public bool DeterministicEmulation { get; set; }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter bw) { }
		public void LoadStateBinary(BinaryReader br) { }
		private IList<MemoryDomain> memoryDomains;
		public IList<MemoryDomain> MemoryDomains { get { return null; } }
		public MemoryDomain MainMemory { get { return null; } }
		/********************/

		public int Frame { get { return _frame; } set { _frame = value; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return _islag; } }
		private bool _islag = true;
		private int _lagcount = 0;
		private int _frame = 0;

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }
		public void Dispose() { }
		public IVideoProvider VideoProvider { get { return videoProvider; } }
		public ISoundProvider SoundProvider { get { return soundProvider; } }
		

		public void ResetFrameCounter()
		{
			_frame = 0;
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public ControllerDefinition ControllerDefinition { get { return Atari7800ControllerDefinition; } }
		public IController Controller { get; set; }
		public static readonly ControllerDefinition Atari7800ControllerDefinition = new ControllerDefinition
		{
			Name = "Atari 7800 Basic Controller", //TODO
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Button", 
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Button", 
				"Reset", "Select"
			}
		};

		public Atari7800(GameInfo game, byte[] rom, byte[] ntsc_bios, byte[] pal_bios, byte[] highscoreBIOS)
		{
			//TODO: store both the ntsc bios and the pal bios
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 1, Endian.Little, addr => 0xFF, null)); //TODO
			memoryDomains = domains.AsReadOnly();
			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
			this.rom = rom;
			this.game = game;
			this.hsbios = highscoreBIOS;
			NTSC_BIOS = new Bios7800(ntsc_bios);
			PAL_BIOS = new Bios7800(pal_bios);
			videoProvider = new MyVideoProvider(this);
			soundProvider = new MySoundProvider(this); //TODO
			HardReset();
		}

		public void HardReset()
		{
			_lagcount = 0;
			// show mapper class on romstatusdetails
			CoreOutputComm.RomStatusDetails =
						string.Format("{0}\r\nSHA1:{1}\r\nMD5:{2}\r\nMapper Impl \"{3}\"",
						game.Name,
						Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(rom)),
						Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(rom)),
						"TODO");

			cart = new Cart7848(rom); //TODO: mapper selection system
			
			int[] bob = new int[] { 0, 0, 0 };

			FileStream fs = new FileStream("C:\\dummy", FileMode.Create, FileAccess.ReadWrite); //TODO: I don't see what this context is used for, see if it can be whacked or pass in a null
			BinaryReader blah = new BinaryReader(fs);
			DeserializationContext george = new DeserializationContext(blah);
			NullLogger logger = new NullLogger();
			HSC7800 hsc7800 = new HSC7800(hsbios, new byte[4096]); //TODO: why should I have to feed it ram? how much?
			theMachine = new Machine7800NTSC(cart, NTSC_BIOS, hsc7800, logger);
			//TODO: clean up, the hs and bios are passed in, the bios has an object AND byte array in the core, and naming is inconsistent
		}

		void SyncState(Serializer ser) //TODO
		{
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
		}

		private void SoftReset() //TOOD: hook this up
		{
			theMachine.Reset();
		}

		MyVideoProvider videoProvider;

		class MyVideoProvider : IVideoProvider
		{
			public int top = 0; //TODO: I should delete these probably
			public int bottom = 262;
			public int left = 0;
			public int right = 320;

			Atari7800 emu;
			public MyVideoProvider(Atari7800 emu)
			{
				this.emu = emu;
			}

			int[] buffer = new int[262 * 320]; //TODO: use videobuffer values for this if there's a logical way

			public void FillFrameBuffer() //TODO: don't recalculate consantly, fill this on frame advance instead
			{
				FrameBuffer fb = emu.theMachine.CreateFrameBuffer();

				for (int i = 0; i < 262; i++)
				{
					for (int j = 0; j < 320; j++)
					{
						buffer[(i * fb.VisiblePitch) + j] = fb.VideoBuffer[i][j];
					}
				}
			}

			public int[] GetVideoBuffer()
			{
				return buffer;
			}

			public int VirtualWidth { get { return BufferWidth; } }
			public int BufferWidth { get { return right - left + 1; } }
			public int BufferHeight { get { return bottom - top + 1; } }
			public int BackgroundColor { get { return 0; } }
		}

		MySoundProvider soundProvider;

		class MySoundProvider : ISoundProvider
		{
			Atari7800 emu;
			public MySoundProvider(Atari7800 emu)
			{
				this.emu = emu;
			}
			public int MaxVolume { get { return 0; } set { } }
			public void DiscardSamples()
			{
			}

			public void GetSamples(short[] samples)
			{
			}
		}

	}
}
