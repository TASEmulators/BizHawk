using System;
using System.Collections.Generic;
using System.IO;
using EMU7800.Core;

namespace BizHawk
{
	public partial class Atari7800 : IEmulator
	{
		static Atari7800()
		{
			// add alpha bits to palette tables
			for (int i = 0; i < TIATables.NTSCPalette.Length; i++)
				TIATables.NTSCPalette[i] |= unchecked((int)0xff000000);
			for (int i = 0; i < TIATables.PALPalette.Length; i++)
				TIATables.PALPalette[i] |= unchecked((int)0xff000000);
		}

		public string SystemId { get { return "A78"; } } //TODO: are we going to allow this core to do 2600 games?
		public GameInfo game;

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;
			_islag = true;

			theMachine.ComputeNextFrame(avProvider.framebuffer);

			if (_islag)
			{
				LagCount++;
			}

			avProvider.FillFrameBuffer();
		}

		/* TODO */
		public CoreComm CoreComm { get; private set; }
		public ISyncSoundProvider SyncSoundProvider { get { return avProvider; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }
		public bool DeterministicEmulation { get; set; }
		public void SaveStateText(TextWriter writer) { SyncState(new Serializer(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(new Serializer(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(new Serializer(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(new Serializer(br)); }
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

		public byte[] ReadSaveRam()
		{
			return (byte[])hsram.Clone();
		}
		public void StoreSaveRam(byte[] data)
		{
			Buffer.BlockCopy(data, 0, hsram, 0, data.Length);
		}
		public void ClearSaveRam()
		{
			for (int i = 0; i < hsram.Length; i++)
				hsram[i] = 0;
		}
		public bool SaveRamModified
		{
			get
			{
				return GameInfo.MachineType == MachineType.A7800PAL || GameInfo.MachineType == MachineType.A7800NTSC;
			}
			set
			{
				throw new Exception("No one ever uses this, and it won't work with the way MainForm is set up.");
			}
		}

		public void Dispose()
		{
			if (avProvider != null)
			{
				avProvider.Dispose();
				avProvider = null;
			}
		}
		public IVideoProvider VideoProvider { get { return avProvider; } }
		public ISoundProvider SoundProvider { get { return null; } }


		public void ResetFrameCounter()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
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

		class ConsoleLogger : ILogger
		{
			public void WriteLine(string format, params object[] args)
			{
				Console.WriteLine(format, args);
			}

			public void WriteLine(object value)
			{
				Console.WriteLine(value);
			}

			public void Write(string format, params object[] args)
			{
				Console.Write(format, args);
			}

			public void Write(object value)
			{
				Console.Write(value);
			}
		}

		public Atari7800(CoreComm comm, GameInfo game, byte[] rom, byte[] ntsc_bios, byte[] pal_bios, byte[] highscoreBIOS, string GameDBfn)
		{
			CoreComm = comm;

			if (EMU7800.Win.GameProgramLibrary.EMU7800DB == null)
			{
				EMU7800.Win.GameProgramLibrary.EMU7800DB = new EMU7800.Win.GameProgramLibrary(new StreamReader(GameDBfn));
			}
			GameInfo = EMU7800.Win.GameProgramLibrary.EMU7800DB.TryRecognizeRom(rom);
			CoreComm.RomStatusDetails = GameInfo.ToString();
			Console.WriteLine("Rom Determiniation from 7800DB:");
			Console.WriteLine(GameInfo.ToString());

			//TODO: store both the ntsc bios and the pal bios
			var domains = new List<MemoryDomain>(1);
			domains.Add(new MemoryDomain("Main RAM", 1, Endian.Little, addr => 0xFF, null)); //TODO
			memoryDomains = domains.AsReadOnly();
			this.rom = rom;
			this.game = game;
			this.hsbios = highscoreBIOS;
			this.bios = GameInfo.MachineType == MachineType.A7800PAL ? pal_bios : ntsc_bios;
			HardReset();
		}

		public void HardReset()
		{
			_lagcount = 0;
			// show mapper class on romstatusdetails
			/*
			CoreComm.RomStatusDetails =
						string.Format("{0}\r\nSHA1:{1}\r\nMD5:{2}\r\nMapper Impl \"{3}\"",
						game.Name,
						Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(rom)),
						Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(rom)),
						"TODO");*/

			cart = Cart.Create(rom, GameInfo.CartType);

			//int[] bob = new int[] { 0, 0, 0 };
			//FileStream fs = new FileStream("C:\\dummy", FileMode.Create, FileAccess.ReadWrite); //TODO: I don't see what this context is used for, see if it can be whacked or pass in a null
			//BinaryReader blah = new BinaryReader(fs);
			//DeserializationContext george = new DeserializationContext(blah);

			ILogger logger = new ConsoleLogger();
			HSC7800 hsc7800 = new HSC7800(hsbios, hsram);
			Bios7800 bios7800 = new Bios7800(bios);
			theMachine = MachineBase.Create
				(GameInfo.MachineType,
				cart,
				bios7800,
				hsc7800,
				GameInfo.LController,
				GameInfo.RController,
				logger);

			//theMachine = new Machine7800NTSC(cart, null, null, logger);
			//TODO: clean up, the hs and bios are passed in, the bios has an object AND byte array in the core, and naming is inconsistent
			theMachine.Reset();
			if (avProvider != null)
				avProvider.Dispose();
			avProvider.ConnectToMachine(theMachine);
			// to sync exactly with audio as this emulator creates and times it, the frame rate should be exactly 60:1 or 50:1
			CoreComm.VsyncNum = theMachine.FrameHZ;
			CoreComm.VsyncDen = 1;
		}

		void SyncState(Serializer ser) //TODO
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				theMachine.Serialize(new BinaryWriter(ms));
				ms.Close();
				core = ms.ToArray();
			}
			ser.BeginSection("Atari7800");
			ser.Sync("core", ref core, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.EndSection();
			if (ser.IsReader)
			{
				theMachine = MachineBase.Deserialize(new BinaryReader(new MemoryStream(core, false)));
				avProvider.ConnectToMachine(theMachine);
			}
		}

		private void SoftReset() //TOOD: hook this up
		{
			theMachine.Reset();
		}

		MyAVProvider avProvider = new MyAVProvider();

		class MyAVProvider : IVideoProvider, ISyncSoundProvider, IDisposable
		{
			public FrameBuffer framebuffer { get; private set; }
			public void ConnectToMachine(MachineBase m)
			{
				framebuffer = m.CreateFrameBuffer();
				BufferWidth = framebuffer.VisiblePitch;
				BufferHeight = framebuffer.Scanlines;
				vidbuffer = new int[BufferWidth * BufferHeight];

				uint samplerate = (uint)m.SoundSampleFrequency;
				if (resampler != null)
					resampler.Dispose();
				resampler = new Emulation.Sound.Utilities.SpeexResampler(3, samplerate, 44100, samplerate, 44100, null, null);
				dcfilter = Emulation.Sound.Utilities.DCFilter.DetatchedMode(256);
			}

			int[] vidbuffer;
			Emulation.Sound.Utilities.SpeexResampler resampler;
			Emulation.Sound.Utilities.DCFilter dcfilter;

			public void FillFrameBuffer()
			{
				unsafe
				{
					fixed (BufferElement* src_ = framebuffer.VideoBuffer)
					{
						fixed (int* dst_ = vidbuffer)
						{
							fixed (int* pal = TIATables.NTSCPalette)
							{
								byte* src = (byte*)src_;
								int* dst = dst_;
								for (int i = 0; i < vidbuffer.Length; i++)
								{
									*dst++ = pal[*src++];
								}
							}
						}
					}
				}
			}

			public int[] GetVideoBuffer()
			{
				return vidbuffer;
			}

			public int VirtualWidth { get { return BufferWidth; } }
			public int BufferWidth { get; private set; }
			public int BufferHeight { get; private set; }
			public int BackgroundColor { get { return unchecked((int)0xff000000); } }

			public void GetSamples(out short[] samples, out int nsamp)
			{
				int nsampin = framebuffer.SoundBufferByteLength;
				unsafe
				{
					fixed (BufferElement* src_ = framebuffer.SoundBuffer)
					{
						byte* src = (byte*)src_;
						for (int i = 0; i < nsampin; i++)
						{
							short s = (short)(src[i] * 200 - 25500);
							resampler.EnqueueSample(s, s);
						}

					}
				}
				resampler.GetSamples(out samples, out nsamp);
				dcfilter.PushThroughSamples(samples, nsamp * 2);
			}

			public void DiscardSamples()
			{
				resampler.DiscardSamples();
			}

			public void Dispose()
			{
				if (resampler != null)
				{
					resampler.Dispose();
					resampler = null;
				}
			}
		}

	}
}
