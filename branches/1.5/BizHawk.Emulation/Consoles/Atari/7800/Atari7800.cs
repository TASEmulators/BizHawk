using System;
using System.Collections.Generic;
using System.IO;
using EMU7800.Core;

namespace BizHawk.Emulation
{
	public partial class Atari7800 : IEmulator
	{
		// TODO:
		// some things don't work when you try to plug in a 2600 game

		static Atari7800()
		{
			// add alpha bits to palette tables
			for (int i = 0; i < TIATables.NTSCPalette.Length; i++)
				TIATables.NTSCPalette[i] |= unchecked((int)0xff000000);
			for (int i = 0; i < TIATables.PALPalette.Length; i++)
				TIATables.PALPalette[i] |= unchecked((int)0xff000000);
		}

		public string SystemId { get { return "A78"; } } // TODO 2600?
		public GameInfo game;

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;

			if (Controller["Power"])
			{
				// it seems that theMachine.Reset() doesn't clear ram, etc
				// this should leave hsram intact but clear most other things
				HardReset();
			}

			ControlAdapter.Convert(Controller, theMachine.InputState);
			theMachine.ComputeNextFrame(avProvider.framebuffer);

			_islag = theMachine.InputState.Lagged;

			if (_islag)
			{
				LagCount++;
			}

			avProvider.FillFrameBuffer();

		}

		public CoreComm CoreComm { get; private set; }
		public bool DeterministicEmulation { get; set; }
		private List<MemoryDomain> _MemoryDomains;
		public IList<MemoryDomain> MemoryDomains { get; private set; }
		public MemoryDomain MainMemory { get { return MemoryDomains[0]; } }

		public int Frame { get { return _frame; } set { _frame = value; } }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return _islag; } }
		private bool _islag = true;
		private int _lagcount = 0;
		private int _frame = 0;

		#region saveram
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
		#endregion

		public void Dispose()
		{
			if (avProvider != null)
			{
				avProvider.Dispose();
				avProvider = null;
			}
		}


		public void ResetFrameCounter()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		#region savestates
		public void SaveStateText(TextWriter writer) { SyncState(new Serializer(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(new Serializer(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(new Serializer(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(new Serializer(br)); }
		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		void SyncState(Serializer ser)
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
		#endregion

		Atari7800Control ControlAdapter;

		public ControllerDefinition ControllerDefinition { get; private set; }
		public IController Controller { get; set; }


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

			if (rom.Length % 1024 == 128)
			{
				Console.WriteLine("Trimming 128 byte .a78 header...");
				byte[] newrom = new byte[rom.Length - 128];
				Buffer.BlockCopy(rom, 128, newrom, 0, newrom.Length);
				rom = newrom;
			}
			GameInfo = EMU7800.Win.GameProgramLibrary.EMU7800DB.TryRecognizeRom(rom);
			CoreComm.RomStatusDetails = GameInfo.ToString();
			Console.WriteLine("Rom Determiniation from 7800DB:");
			Console.WriteLine(GameInfo.ToString());

			this.rom = rom;
			this.game = game;
			this.hsbios = highscoreBIOS;
			this.bios = GameInfo.MachineType == MachineType.A7800PAL ? pal_bios : ntsc_bios;
            if (bios == null)
            {
                throw new InvalidDataException("The BIOS corresponding to the region of the game you loaded is required to run Atari 7800 games.");
            }
			HardReset();
		}

		void HardReset()
		{
			cart = Cart.Create(rom, GameInfo.CartType);
			ILogger logger = new ConsoleLogger();

			HSC7800 hsc7800 = null;
			if (hsbios != null)
			{
				hsc7800 = new HSC7800(hsbios, hsram);
			}

			Bios7800 bios7800 = new Bios7800(bios);
			theMachine = MachineBase.Create
				(GameInfo.MachineType,
				cart,
				bios7800,
				hsc7800,
				GameInfo.LController,
				GameInfo.RController,
				logger);

			theMachine.Reset();

			ControlAdapter = new Atari7800Control(theMachine);
			if (ControlAdapter.ControlType.Name != "Atari 7800 ProLine Joystick Controller")
				throw new Exception("For now, only Atari 7800 ProLine Joystick games are supported.");
			ControllerDefinition = ControlAdapter.ControlType;

			avProvider.ConnectToMachine(theMachine);
			// to sync exactly with audio as this emulator creates and times it, the frame rate should be exactly 60:1 or 50:1
			CoreComm.VsyncNum = theMachine.FrameHZ;
			CoreComm.VsyncDen = 1;

			// reset memory domains
			if (_MemoryDomains == null)
			{
				_MemoryDomains = new List<MemoryDomain>();
				if (theMachine is Machine7800)
				{
					_MemoryDomains.Add(new MemoryDomain(
						"RAM1", 0x800, Endian.Unknown,
						delegate(int addr)
						{
							return ((Machine7800)theMachine).RAM1[(ushort)addr];
						},
						delegate(int addr, byte val)
						{
							((Machine7800)theMachine).RAM1[(ushort)addr] = val;
						}));
					_MemoryDomains.Add(new MemoryDomain(
						"RAM2", 0x800, Endian.Unknown,
						delegate(int addr)
						{
							return ((Machine7800)theMachine).RAM2[(ushort)addr];
						},
						delegate(int addr, byte val)
						{
							((Machine7800)theMachine).RAM2[(ushort)addr] = val;
						}));
					_MemoryDomains.Add(new MemoryDomain(
						"BIOS ROM", bios.Length, Endian.Unknown,
						delegate(int addr)
						{
							return bios[addr];
						},
						delegate(int addr, byte val)
						{
						}));
					if (hsc7800 != null)
					{
						_MemoryDomains.Add(new MemoryDomain(
							"HSC ROM", hsbios.Length, Endian.Unknown,
							delegate(int addr)
							{
								return hsbios[addr];
							},
							delegate(int addr, byte val)
							{
							}));
						_MemoryDomains.Add(new MemoryDomain(
							"HSC RAM", hsram.Length, Endian.Unknown,
							delegate(int addr)
							{
								return hsram[addr];
							},
							delegate(int addr, byte val)
							{
								hsram[addr] = val;
							}));
					}
					_MemoryDomains.Add(new MemoryDomain(
						"System Bus", 65536, Endian.Unknown,
						delegate(int addr)
						{
							return theMachine.Mem[(ushort)addr];
						},
						delegate(int addr, byte val)
						{
							theMachine.Mem[(ushort)addr] = val;
						}));
				}
				else // todo 2600?
				{
				}
				MemoryDomains = _MemoryDomains.AsReadOnly();
			}

		}

		#region audio\video

		public ISyncSoundProvider SyncSoundProvider { get { return avProvider; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }
		public IVideoProvider VideoProvider { get { return avProvider; } }
		public ISoundProvider SoundProvider { get { return null; } }

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

				uint newsamplerate = (uint)m.SoundSampleFrequency;
				if (newsamplerate != samplerate)
				{
					// really shouldn't happen (after init), but if it does, we're ready
					if (resampler != null)
						resampler.Dispose();
					resampler = new Emulation.Sound.Utilities.SpeexResampler(3, newsamplerate, 44100, newsamplerate, 44100, null, null);
					samplerate = newsamplerate;
					dcfilter = Emulation.Sound.Utilities.DCFilter.DetatchedMode(256);
				}				
			}

			uint samplerate;
			int[] vidbuffer;
			Emulation.Sound.Utilities.SpeexResampler resampler;
			Emulation.Sound.Utilities.DCFilter dcfilter;

			public void FillFrameBuffer()
			{
				unsafe
				{
					fixed (byte* src_ = framebuffer.VideoBuffer)
					fixed (int* dst_ = vidbuffer)
					fixed (int* pal = TIATables.NTSCPalette)
					{
						byte* src = src_;
						int* dst = dst_;
						for (int i = 0; i < vidbuffer.Length; i++)
						{
							*dst++ = pal[*src++];
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
					fixed (byte* src = framebuffer.SoundBuffer)
					{
						for (int i = 0; i < nsampin; i++)
						{
							// the buffer values don't really get very large at all,
							// so this doesn't overflow
							short s = (short)(src[i] * 200);
							resampler.EnqueueSample(s, s);
						}

					}
				}
				resampler.GetSamples(out samples, out nsamp);
				dcfilter.PushThroughSamples(samples, nsamp * 2);
			}

			public void DiscardSamples()
			{
				if (resampler != null)
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
		#endregion
	}
}
