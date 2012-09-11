using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.GB
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	public class Gameboy : IEmulator, IVideoProvider, ISoundProvider
	{
		/// <summary>
		/// internal gambatte state
		/// </summary>
		IntPtr GambatteState = IntPtr.Zero;

		/// <summary>
		/// keep a copy of the input callback delegate so it doesn't get GCed
		/// </summary>
		LibGambatte.InputGetter InputCallback;

		/// <summary>
		/// whatever keys are currently depressed
		/// </summary>
		LibGambatte.Buttons CurrentButtons = 0;

		public Gameboy(byte[] romdata)
		{
			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
				throw new Exception("gambatte_create() returned null???");

			if (LibGambatte.gambatte_load(GambatteState, romdata, (uint)romdata.Length, 0) != 0)
				throw new Exception("gambatte_load() returned non-zero (is this not a gb or gbc rom?)");

			InitSound();

			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;

			InputCallback = new LibGambatte.InputGetter(ControllerCallback);

			LibGambatte.gambatte_setinputgetter(GambatteState, InputCallback);
		}

		public static readonly ControllerDefinition GbController = new ControllerDefinition
		{
			Name = "Gameboy Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "A", "B", "Select", "Start"
			}
		};

		public ControllerDefinition ControllerDefinition
		{
			get { return GbController; }
		}

		public IController Controller { get; set; }

		// can when this is called (or not called) be used to give information about lagged frames?
		LibGambatte.Buttons ControllerCallback()
		{
			IsLagFrame = false;
			return CurrentButtons;
		}



		public void FrameAdvance(bool render)
		{
			uint nsamp = 35112; // according to gambatte docs, this is the nominal length of a frame in 2mhz clocks

			Controller.UpdateControls(Frame++);

			// update our local copy of the controller data
			CurrentButtons = 0;

			if (Controller["Up"])
				CurrentButtons |= LibGambatte.Buttons.UP;
			if (Controller["Down"])
				CurrentButtons |= LibGambatte.Buttons.DOWN;
			if (Controller["Left"])
				CurrentButtons |= LibGambatte.Buttons.LEFT;
			if (Controller["Right"])
				CurrentButtons |= LibGambatte.Buttons.RIGHT;
			if (Controller["A"])
				CurrentButtons |= LibGambatte.Buttons.A;
			if (Controller["B"])
				CurrentButtons |= LibGambatte.Buttons.B;
			if (Controller["Select"])
				CurrentButtons |= LibGambatte.Buttons.SELECT;
			if (Controller["Start"])
				CurrentButtons |= LibGambatte.Buttons.START;

			// the controller callback will set this to false if it actually gets called during the frame
			IsLagFrame = true;

			LibGambatte.gambatte_runfor(GambatteState, VideoBuffer, 160, soundbuff, ref nsamp);

			soundbuffcontains = (int)nsamp;

			if (IsLagFrame)
				LagCount++;

		}




		public int Frame { get; set; }

		public int LagCount { get; set; }

		public bool IsLagFrame { get; private set; }

		public string SystemId
		{
			get { return "GB"; }
		}

		public bool DeterministicEmulation { get; set; }

		public byte[] ReadSaveRam
		{
			get
			{
				int length = LibGambatte.gambatte_savesavedatalength(GambatteState);

				if (length > 0)
				{
					byte[] ret = new byte[length];
					LibGambatte.gambatte_savesavedata(GambatteState, ret);
					return ret;
				}
				else
					return new byte[0];
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length != LibGambatte.gambatte_savesavedatalength(GambatteState))
				throw new ArgumentException("Size of saveram data does not match expected!");
			LibGambatte.gambatte_loadsavedata(GambatteState, data);
		}


		public bool SaveRamModified
		{
			get
			{
				if (LibGambatte.gambatte_savesavedatalength(GambatteState) == 0)
					return false;
				else
					return true; // need to wire more stuff into the core to actually know this
			}
			set { }
		}

		public void ResetFrameCounter()
		{
			// is this right?
			Frame = 0;
			LagCount = 0;
		}

		#region savestates

		public void SaveStateText(System.IO.TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHex(writer);
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHex(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			uint nlen = 0;
			IntPtr ndata = IntPtr.Zero;

			if (!LibGambatte.gambatte_savestate(GambatteState, VideoBuffer, 160, ref ndata, ref nlen))
				throw new Exception("Gambatte failed to save the savestate!");

			if (nlen == 0)
				throw new Exception("Gambatte returned a 0-length savestate?");

			byte[] data = new byte[nlen];
			System.Runtime.InteropServices.Marshal.Copy(ndata, data, 0, (int)nlen);
			LibGambatte.gambatte_savestate_destroy(ndata);

			writer.Write((int)nlen);
			writer.Write(data);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int length = reader.ReadInt32();
			byte[] data = reader.ReadBytes(length);

			if (!LibGambatte.gambatte_loadstate(GambatteState, data, (uint)length))
				throw new Exception("Gambatte failed to load the savestate!");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
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

		CoreOutputComm GbOutputComm = new CoreOutputComm
		{
			VsyncNum = 262144,
			VsyncDen = 4389,
			RomStatusAnnotation = "Bizwhackin it up",
			RomStatusDetails = "LEVAR BURTON"
		};

		public CoreOutputComm CoreOutputComm
		{
			get { return GbOutputComm; }
		}

		public IList<MemoryDomain> MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		public MemoryDomain MainMemory
		{
			get { throw new NotImplementedException(); }
		}

		public void Dispose()
		{
			LibGambatte.gambatte_destroy(GambatteState);
			GambatteState = IntPtr.Zero;
			DisposeSound();
		}

		#region IVideoProvider

		public IVideoProvider VideoProvider
		{
			get { return this; }
		}

		/// <summary>
		/// stored image of most recent frame
		/// </summary>
		int[] VideoBuffer = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		public int VirtualWidth
		{
			// only sgb changes this, which we don't emulate here
			get { return 160; }
		}

		public int BufferWidth
		{
			get { return 160; }
		}

		public int BufferHeight
		{
			get { return 144; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}

		#endregion

		#region ISoundProvider

		public ISoundProvider SoundProvider
		{
			get { return this; }
		}

		/// <summary>
		/// sample pairs before resampling
		/// </summary>
		short[] soundbuff = new short[(35112 + 2064) * 2];
		/// <summary>
		/// how many sample pairs are in soundbuff
		/// </summary>
		int soundbuffcontains = 0;

		Sound.Utilities.SpeexResampler resampler;
		Sound.MetaspuSoundProvider metaspu;

		void InitSound()
		{
			metaspu = new Sound.MetaspuSoundProvider(Sound.ESynchMethod.ESynchMethod_V);
			resampler = new Sound.Utilities.SpeexResampler(2, 2097152, 44100, 2097152, 44100, metaspu.buffer.enqueue_samples);
		}

		void DisposeSound()
		{
			resampler.Dispose();
			resampler = null;
		}

		public void GetSamples(short[] samples)
		{
			resampler.EnqueueSamples(soundbuff, soundbuffcontains);
			soundbuffcontains = 0;
			resampler.Flush();
			metaspu.GetSamples(samples);
		}

		public void DiscardSamples()
		{
			metaspu.DiscardSamples();
		}

		public int MaxVolume { get; set; }
		#endregion

	}
}
