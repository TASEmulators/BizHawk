using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Emulation.Consoles.Nintendo.GBA
{
	public class GBA : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		public static readonly ControllerDefinition GBAController =
		new ControllerDefinition
		{
			Name = "GBA Controller",
			BoolButtons =
			{					
				"Up", "Down", "Left", "Right", "Select", "Start", "B", "A", "L", "R"//, "Reset", "Power",		
			}
		};
		public ControllerDefinition ControllerDefinition { get { return GBAController; } }
		public IController Controller { get; set; }

		public void Load(byte[] rom)
		{
			Init();
			LibMeteor.libmeteor_reset();
			LibMeteor.libmeteor_loadbios(File.ReadAllBytes("gbabios.rom"), 16384);
			LibMeteor.libmeteor_loadrom(rom, (uint)rom.Length);
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Controller.UpdateControls(Frame++);
			IsLagFrame = true;
			LibMeteor.libmeteor_frameadvance();
			if (IsLagFrame)
				LagCount++;
		}

		public int Frame
		{
			get;
			private set;
		}

		public int LagCount
		{
			get;
			set;
		}

		public bool IsLagFrame
		{
			get;
			private set;
		}

		public string SystemId
		{
			get { return "GBA"; }
		}

		public bool DeterministicEmulation
		{
			get { return true; }
		}

		public byte[] ReadSaveRam()
		{
			return new byte[0];
		}

		public void StoreSaveRam(byte[] data)
		{
		}

		public void ClearSaveRam()
		{
		}

		public bool SaveRamModified { get { return false; } set { } }

		public void ResetFrameCounter()
		{
			Frame = 0;
			LagCount = 0;
		}

		#region savestates

		public void SaveStateText(System.IO.TextWriter writer)
		{
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		#endregion

		public CoreInputComm CoreInputComm { get; set; }

		CoreOutputComm _CoreOutputComm = new CoreOutputComm
		{
			VsyncNum = 262144,
			VsyncDen = 4389
		};

		public CoreOutputComm CoreOutputComm { get { return _CoreOutputComm; } }

		public IList<MemoryDomain> MemoryDomains
		{
			get { return null; }
		}

		public MemoryDomain MainMemory
		{
			get { return null; }
		}

		static GBA attachedcore;
		LibMeteor.MessageCallback messagecallback;
		LibMeteor.InputCallback inputcallback;

		LibMeteor.Buttons GetInput()
		{
			IsLagFrame = false;
			LibMeteor.Buttons ret = 0;
			if (Controller["Up"]) ret |= LibMeteor.Buttons.BTN_UP;
			if (Controller["Down"]) ret |= LibMeteor.Buttons.BTN_DOWN;
			if (Controller["Left"]) ret |= LibMeteor.Buttons.BTN_LEFT;
			if (Controller["Right"]) ret |= LibMeteor.Buttons.BTN_RIGHT;
			if (Controller["Select"]) ret |= LibMeteor.Buttons.BTN_SELECT;
			if (Controller["Start"]) ret |= LibMeteor.Buttons.BTN_START;
			if (Controller["B"]) ret |= LibMeteor.Buttons.BTN_B;
			if (Controller["A"]) ret |= LibMeteor.Buttons.BTN_A;
			if (Controller["L"]) ret |= LibMeteor.Buttons.BTN_L;
			if (Controller["R"]) ret |= LibMeteor.Buttons.BTN_R;
			return ret;
		}


		void Init()
		{
			if (attachedcore != null)
				attachedcore.Dispose();

			messagecallback = (str) => Console.Write(str.Replace("\n","\r\n"));
			inputcallback = GetInput;
			LibMeteor.libmeteor_setmessagecallback(messagecallback);
			LibMeteor.libmeteor_setkeycallback(inputcallback);

			LibMeteor.libmeteor_init();
			videobuffer = new int[240 * 160];
			videohandle = GCHandle.Alloc(videobuffer, GCHandleType.Pinned);
			soundbuffer = new short[2048]; // nominal length of one frame is something like 1480 shorts?
			soundhandle = GCHandle.Alloc(soundbuffer, GCHandleType.Pinned);

			if (!LibMeteor.libmeteor_setbuffers
				(videohandle.AddrOfPinnedObject(), (uint)(sizeof(int) * videobuffer.Length),
				soundhandle.AddrOfPinnedObject(), (uint)(sizeof(short) * soundbuffer.Length)))
				throw new Exception("libmeteor_setbuffers() returned false??");

			attachedcore = this;
		}

		bool disposed = false;
		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				videohandle.Free();
				soundhandle.Free();
			}
		}

		#region IVideoProvider

		public IVideoProvider VideoProvider { get { return this; } }

		int[] videobuffer;
		GCHandle videohandle;

		public int[] GetVideoBuffer() { return videobuffer; }
		public int VirtualWidth { get { return 240; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region ISoundProvider

		short[] soundbuffer;
		GCHandle soundhandle;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			uint nbytes = LibMeteor.libmeteor_emptysound();
			samples = soundbuffer;
			nsamp = (int)(nbytes / 4);

		}

		public void DiscardSamples()
		{
			LibMeteor.libmeteor_emptysound();
		}

		#endregion
	}
}
