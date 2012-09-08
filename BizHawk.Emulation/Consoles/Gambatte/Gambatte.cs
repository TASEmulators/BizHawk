using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Consoles.Gambatte
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C++ libgambatte
	/// </summary>
	public class Gambatte : IEmulator, IVideoProvider
	{
		/// <summary>
		/// internal gambatte state
		/// </summary>
		IntPtr GambatteState = IntPtr.Zero;


		public Gambatte(byte[] romdata)
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

		}

		public IVideoProvider VideoProvider
		{
			get { return this; }
		}

		public ISoundProvider SoundProvider
		{
			get { return new NullSound(); }
		}



		static readonly ControllerDefinition GbController = new ControllerDefinition
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


		short[] soundscratch = new short[(35112 + 2064) * 2];
		uint[] videoscratch = new uint[160 * 144];
		public void FrameAdvance(bool render)
		{
			uint nsamp = 35112;

			LibGambatte.gambatte_runfor(GambatteState, videoscratch, 160, soundscratch, ref nsamp);

			// can't convert uint[] to int[], so we do this instead
			// TODO: lie in the p/invoke layer and claim unsigned* is really int*

			Buffer.BlockCopy(videoscratch, 0, VideoBuffer, 0, VideoBuffer.Length * sizeof(int));
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
			VsyncNum = 60,
			VsyncDen = 1,
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
	}
}
