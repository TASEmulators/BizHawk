//#define ARM_DEBUG

namespace GarboDev
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Timers;
	using System.Windows.Forms;
	using System.Collections.Generic;

	using BizHawk.Emulation;
	using BizHawk;

	public class GbaManager : IEmulator, ISoundProvider, IVideoProvider
	{
		public const int cpuFreq = 16 * 1024 * 1024;

		private int framesRendered;

		private Arm7Processor arm7 = null;
		private Memory memory = null;
		private VideoManager videoManager = null;
		private SoundManager soundManager = null;

		private bool skipBios = false;

		public delegate void CpuUpdateDelegate(Arm7Processor processor, Memory memory);
		private event CpuUpdateDelegate onCpuUpdate = null;

		public Arm7Processor Arm7
		{
			get { return this.arm7; }
		}

		public VideoManager VideoManager
		{
			get { return this.videoManager; }
		}

		public SoundManager SoundManager
		{
			get { return this.soundManager; }
		}

		public Memory Memory
		{
			get { return this.memory; }
		}

		public Dictionary<uint, bool> Breakpoints
		{
			get { return this.arm7.Breakpoints; }
		}

		public ushort KeyState
		{
			get
			{
				if (this.memory != null)
				{
					return this.memory.KeyState;
				}

				return 0x3FF;
			}

			set
			{
				this.arm7.KeyState = value;
			}
		}

		public int FramesRendered
		{
			get { return this.framesRendered; }
			set { this.framesRendered = value; }
		}

		public event CpuUpdateDelegate OnCpuUpdate
		{
			add
			{
				this.onCpuUpdate += value;
				this.onCpuUpdate(this.arm7, this.memory);
			}
			remove
			{
				this.onCpuUpdate -= value;
			}
		}

		public bool SkipBios
		{
			get { return this.skipBios; }
			set { this.skipBios = value; }
		}

		public GbaManager(CoreComm comm)
		{
			_corecomm = comm;

			this.memory = new Memory();
			this.arm7 = new Arm7Processor(this.memory);
			this.videoManager = new VideoManager(this);
			this.videoManager.Memory = this.memory;
			this.soundManager = new SoundManager(this.memory, 44100);
		
			this.framesRendered = 0;
			Renderer renderer = new Renderer();
			renderer.Initialize(null);
			VideoManager.Renderer = renderer;

			videoManager.Presenter = delegate(uint[] data)
			{
				Buffer.BlockCopy(data, 0, this.vbuf, 0, 240 * 160 * 4);
			};
		}

		public void Load(byte[] rom, byte[] bios)
		{
			LoadBios(bios);
			LoadRom(rom);
		}

		public void Reset()
		{
			//this.Halt();

			this.arm7.Reset(this.skipBios);
			this.memory.Reset();
			this.videoManager.Reset();
		}

		public void AudioMixerStereo(short[] buffer, int length)
		{
			// even = left, odd = right
			if (this.soundManager.SamplesMixed > Math.Max(500, length))
			{
				this.soundManager.GetSamples(buffer, length);
			}
		}

		public void LoadState(BinaryReader state)
		{
		}

		public void SaveState(BinaryWriter state)
		{
			state.Write("GARB");
		}

		public void LoadBios(byte[] biosRom)
		{
			this.memory.LoadBios(biosRom);

			if (this.onCpuUpdate != null)
			{
				this.onCpuUpdate(this.arm7, this.memory);
			}
		}

		public void LoadRom(byte[] cartRom)
		{
			//this.Halt();
			
			/*
			byte[] logo = new byte[]
                    {
            			0x24,0xff,0xae,0x51,0x69,0x9a,0xa2,0x21,
            			0x3d,0x84,0x82,0x0a,0x84,0xe4,0x09,0xad,
            			0x11,0x24,0x8b,0x98,0xc0,0x81,0x7f,0x21,
            			0xa3,0x52,0xbe,0x19,0x93,0x09,0xce,0x20,
	    	        	0x10,0x46,0x4a,0x4a,0xf8,0x27,0x31,0xec,
        	    		0x58,0xc7,0xe8,0x33,0x82,0xe3,0xce,0xbf,
	        	    	0x85,0xf4,0xdf,0x94,0xce,0x4b,0x09,0xc1,
		        	    0x94,0x56,0x8a,0xc0,0x13,0x72,0xa7,0xfc,
    		    	    0x9f,0x84,0x4d,0x73,0xa3,0xca,0x9a,0x61,
        		    	0x58,0x97,0xa3,0x27,0xfc,0x03,0x98,0x76,
	        		    0x23,0x1d,0xc7,0x61,0x03,0x04,0xae,0x56,
    		        	0xbf,0x38,0x84,0x00,0x40,0xa7,0x0e,0xfd,
	    		        0xff,0x52,0xfe,0x03,0x6f,0x95,0x30,0xf1,
            			0x97,0xfb,0xc0,0x85,0x60,0xd6,0x80,0x25,
	            		0xa9,0x63,0xbe,0x03,0x01,0x4e,0x38,0xe2,
		        	    0xf9,0xa2,0x34,0xff,0xbb,0x3e,0x03,0x44,
			            0x78,0x00,0x90,0xcb,0x88,0x11,0x3a,0x94,
            			0x65,0xc0,0x7c,0x63,0x87,0xf0,0x3c,0xaf,
	            		0xd6,0x25,0xe4,0x8b,0x38,0x0a,0xac,0x72,
		            	0x21,0xd4,0xf8,0x07
                    };

			Array.Copy(logo, 0, cartRom, 4, logo.Length);
			cartRom[0xB2] = 0x96;
			cartRom[0xBD] = 0;
			for (int i = 0xA0; i <= 0xBC; i++) cartRom[0xBD] = (byte)(cartRom[0xBD] - cartRom[i]);
			cartRom[0xBD] = (byte)((cartRom[0xBD] - 0x19) & 0xFF);
			*/
			this.memory.LoadCartridge(cartRom);

			this.Reset();

			if (this.onCpuUpdate != null)
			{
				this.onCpuUpdate(this.arm7, this.memory);
			}
		}

		public void Step()
		{
			//this.Halt();

			this.arm7.Step();

			if (this.onCpuUpdate != null)
			{
				this.onCpuUpdate(this.arm7, this.memory);
			}
		}

		public void StepScanline()
		{
			//this.Halt();

			this.arm7.Execute(960);
			this.videoManager.RenderLine();
			this.videoManager.EnterHBlank(this.arm7);
			this.arm7.Execute(272);
			this.videoManager.LeaveHBlank(this.arm7);

			if (this.onCpuUpdate != null)
			{
				this.onCpuUpdate(this.arm7, this.memory);
			}
		}

		void UpdateInputState()
		{
			ushort ret = 0;
			if (_controller["Up"]) ret |= 64;
			if (_controller["Down"]) ret |= 128;
			if (_controller["Left"]) ret |= 32;
			if (_controller["Right"]) ret |= 16;
			if (_controller["Select"]) ret |= 4;
			if (_controller["Start"]) ret |= 8;
			if (_controller["B"]) ret |= 2;
			if (_controller["A"]) ret |= 1;
			if (_controller["L"]) ret |= 512;
			if (_controller["R"]) ret |= 256;
			ret ^= 0x3ff;
			KeyState = ret;
		}

		private void StepFrame()
		{
			UpdateInputState();
			if (_controller["Power"])
				Reset();

			int vramCycles = 0;
			bool inHblank = false;

			//HighPerformanceTimer profileTimer = new HighPerformanceTimer();

			while (true)
			{


				const int cycleStep = 123;


				if (vramCycles <= 0)
				{
					if (inHblank)
					{
						vramCycles += 960;
						bool HitVBlank = this.videoManager.LeaveHBlank(this.arm7);
						inHblank = false;
						if (HitVBlank)
							break;
					}
					else
					{
						vramCycles += 272;
						this.videoManager.RenderLine();
						this.videoManager.EnterHBlank(this.arm7);
						inHblank = true;
					}
				}

				this.arm7.Execute(cycleStep);
#if ARM_DEBUG
                            if (this.arm7.BreakpointHit)
                            {
                                this.waitingToHalt = true;
                                Monitor.Wait(this);
                            }
#endif
				vramCycles -= cycleStep;
				this.arm7.FireIrq();
			}


		}

		IVideoProvider IEmulator.VideoProvider
		{
			get { return this; }
		}

		ISoundProvider IEmulator.SoundProvider
		{
			get { return this; }
		}

		ISyncSoundProvider IEmulator.SyncSoundProvider
		{
			get { return new FakeSyncSound(this, 735); }
		}

		bool IEmulator.StartAsyncSound()
		{
			return false;
		}

		void IEmulator.EndAsyncSound()
		{

		}

		ControllerDefinition IEmulator.ControllerDefinition
		{
			get { return BizHawk.Emulation.Consoles.Nintendo.GBA.GBA.GBAController; }
		}

		IController _controller;

		IController IEmulator.Controller
		{
			get { return _controller; }
			set { _controller = value; }
		}

		void IEmulator.FrameAdvance(bool render, bool rendersound)
		{
			StepFrame();
		}

		int IEmulator.Frame
		{
			get { return 0; }
		}

		int IEmulator.LagCount
		{
			get
			{
				return 0;
			}
			set
			{

			}
		}

		bool IEmulator.IsLagFrame
		{
			get { return false; }
		}

		string IEmulator.SystemId
		{
			get { return "GBA"; }
		}

		bool IEmulator.DeterministicEmulation
		{
			get { return true; }
		}

		byte[] IEmulator.ReadSaveRam()
		{
			return new byte[0];
		}

		void IEmulator.StoreSaveRam(byte[] data)
		{

		}

		void IEmulator.ClearSaveRam()
		{

		}

		bool IEmulator.SaveRamModified
		{
			get
			{
				return false;
			}
			set
			{

			}
		}

		void IEmulator.ResetFrameCounter()
		{

		}

		void IEmulator.SaveStateText(TextWriter writer)
		{

		}

		void IEmulator.LoadStateText(TextReader reader)
		{

		}

		void IEmulator.SaveStateBinary(BinaryWriter writer)
		{

		}

		void IEmulator.LoadStateBinary(BinaryReader reader)
		{

		}

		byte[] IEmulator.SaveStateBinary()
		{
			return new byte[16];
		}

		CoreComm _corecomm;

		CoreComm IEmulator.CoreComm
		{
			get { return _corecomm; }
		}

		IList<MemoryDomain> IEmulator.MemoryDomains
		{
			get { return new List<MemoryDomain>(); }
		}

		MemoryDomain IEmulator.MainMemory
		{
			get { return null; }
		}

		void IDisposable.Dispose()
		{

		}

		void ISoundProvider.GetSamples(short[] samples)
		{
			AudioMixerStereo(samples, samples.Length);
		}

		void ISoundProvider.DiscardSamples()
		{

		}

		int ISoundProvider.MaxVolume
		{
			get;
			set;
		}

		int[] vbuf = new int[240 * 160];
		int[] IVideoProvider.GetVideoBuffer() { return vbuf; }
		int IVideoProvider.VirtualWidth { get { return 240; } }
		int IVideoProvider.BufferWidth { get { return 240; } }
		int IVideoProvider.BufferHeight { get { return 160; } }
		int IVideoProvider.BackgroundColor { get { return unchecked((int)0xff000000); } }
	}
}
