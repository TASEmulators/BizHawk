using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

using System.Runtime.InteropServices;


namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public class GPGX : IEmulator, ISyncSoundProvider, IVideoProvider
	{
		static GPGX AttachedCore = null;

		byte[] romfile;

		bool disposed = false;

		LibGPGX.load_archive_cb LoadCallback = null;

		public GPGX(CoreComm NextComm, byte[] romfile, string romextension)
		{
			try
			{
				CoreComm = NextComm;
				if (AttachedCore != null)
				{
					AttachedCore.Dispose();
					AttachedCore = null;
				}
				AttachedCore = this;

				MemoryDomains = MemoryDomainList.GetDummyList();

				LoadCallback = new LibGPGX.load_archive_cb(load_archive);

				this.romfile = romfile;

				if (!LibGPGX.gpgx_init(romextension, LoadCallback))
					throw new Exception("gpgx_init() failed");
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		/// <summary>
		/// core callback for file loading
		/// </summary>
		/// <param name="filename">string identifying file to be loaded</param>
		/// <param name="buffer">buffer to load file to</param>
		/// <param name="maxsize">maximum length buffer can hold</param>
		/// <returns>actual size loaded, or 0 on failure</returns>
		int load_archive(string filename, IntPtr buffer, int maxsize)
		{
			byte[] srcdata = null;

			if (buffer == IntPtr.Zero)
			{
				Console.WriteLine("Couldn't satisfy firmware request {0} because buffer == NULL", filename);
				return 0;
			}

			if (filename == "PRIMARY_ROM")
				srcdata = romfile;
			else
			{
				// use corecomm for firmware requests
			}

			if (srcdata != null)
			{
				if (srcdata.Length > maxsize)
				{
					Console.WriteLine("Couldn't satisfy firmware request {0} because {1} > {2}", filename, srcdata.Length, maxsize);
					return 0;
				}
				else
				{
					Marshal.Copy(srcdata, 0, buffer, srcdata.Length);
					Console.WriteLine("Firmware request {0} satisfied at size {1}", filename, srcdata.Length);
					return srcdata.Length;
				}
			}
			else
			{
				Console.WriteLine("Couldn't satisfy firmware request {0} for unknown reasons", filename);
				return 0;
			}

		}

		#region controller

		public ControllerDefinition ControllerDefinition { get { return NullEmulator.NullController; } }
		public IController Controller { get; set; }

		#endregion

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			IsLagFrame = true;
			Frame++;
			LibGPGX.gpgx_advance();
			update_video();
			update_audio();
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		public string SystemId { get { return "GEN"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }

		#region saveram

		public byte[] ReadSaveRam()
		{
			throw new NotImplementedException();
		}

		public void StoreSaveRam(byte[] data)
		{
			throw new NotImplementedException();
		}

		public void ClearSaveRam()
		{
			throw new NotImplementedException();
		}

		public bool SaveRamModified
		{
			get
			{
				return false;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
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
			return new byte[0];
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		public MemoryDomainList MemoryDomains { get; private set; }

		public List<KeyValuePair<string, int>> GetCpuFlagsAndRegisters()
		{
			return new List<KeyValuePair<string, int>>();
		}

		public void Dispose()
		{
			if (!disposed)
			{
				if (AttachedCore != this)
					throw new Exception();
				AttachedCore = null;
			}
		}

		#region SoundProvider

		short[] samples = new short[4096];
		int nsamp = 0;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = this.nsamp;
			samples = this.samples;
			this.nsamp = 0;
		}

		public void DiscardSamples()
		{
			this.nsamp = 0;
		}

		void update_audio()
		{
			IntPtr src = IntPtr.Zero;
			LibGPGX.gpgx_get_audio(ref nsamp, ref src);
			if (src != IntPtr.Zero)
			{
				Marshal.Copy(src, samples, 0, nsamp * 2);
			}
		}

		#endregion

		#region VideoProvider

		public IVideoProvider VideoProvider { get { return this; } }

		int[] vidbuff = new int[0];
		int vwidth;
		int vheight;
		public int[] GetVideoBuffer() { return vidbuff; }
		public int VirtualWidth { get { return BufferWidth; } } // TODO
		public int BufferWidth { get { return vwidth; } }
		public int BufferHeight { get { return vheight; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		unsafe void update_video()
		{
			int pitch = 0;
			IntPtr src = IntPtr.Zero;

			LibGPGX.gpgx_get_video(ref vwidth, ref vheight, ref pitch, ref src);

			if (vidbuff.Length < vwidth * vheight)
				vidbuff = new int[vwidth * vheight];

			int rinc = (pitch / 4) - vwidth;
			fixed (int* pdst_ = &vidbuff[0])
			{
				int* pdst = pdst_;
				int* psrc = (int*)src;

				for (int j = 0; j < vheight; j++)
				{
					for (int i = 0; i < vwidth; i++)
						*pdst++ = *psrc++ | unchecked((int)0xff000000);
					psrc += rinc;
				}
			}
		}

		#endregion
	}
}
