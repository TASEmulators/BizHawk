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


		public Gameboy(byte[] romdata)
		{
			// use temp file until we hack up the libgambatte api to take data directly

			using (FileStream fs = new FileStream("gambattetmp.gb", FileMode.OpenOrCreate, FileAccess.Write))
			{
				fs.Write(romdata, 0, romdata.Length);
			}

			GambatteState = LibGambatte.gambatte_create();

			if (GambatteState == IntPtr.Zero)
				throw new Exception("gambatte_create() returned null???");

			if (LibGambatte.gambatte_load(GambatteState, "gambattetmp.gb", 0) != 0)
				throw new Exception("gambatte_load() returned non-zero (is this not a gb or gbc rom?)");


			InitSound();
		}

		public IVideoProvider VideoProvider
		{
			get { return this; }
		}

		public ISoundProvider SoundProvider
		{
			get { return this; }
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


		
		public void FrameAdvance(bool render)
		{
			uint nsamp = 35112;

			LibGambatte.gambatte_runfor(GambatteState, VideoBuffer, 160, soundbuff, ref nsamp);

			soundbuffcontains = (int)nsamp;

		}

		public int Frame
		{
			get { return 0; }
		}

		public int LagCount { get; set; }

		public bool IsLagFrame
		{
			get { return false; }
		}

		public string SystemId
		{
			get { return "GB"; }
		}

		public bool DeterministicEmulation { get; set; }

		public byte[] ReadSaveRam
		{
			get { return new byte[0]; }
		}

		public bool SaveRamModified
		{
			get;
			set;
		}

		public void ResetFrameCounter()
		{
			throw new NotImplementedException();
		}

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
			return new byte[0];
		}


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
			// only sgb changes this
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
