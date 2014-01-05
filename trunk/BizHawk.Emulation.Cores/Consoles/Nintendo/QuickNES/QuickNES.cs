using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public class QuickNES : IEmulator, IVideoProvider, ISyncSoundProvider
	{
		static QuickNES()
		{
			LibQuickNES.qn_setup_mappers();
		}

		public QuickNES(CoreComm nextComm, byte[] Rom)
		{
			CoreComm = nextComm;

			Context = LibQuickNES.qn_new();
			if (Context == IntPtr.Zero)
				throw new InvalidOperationException("qn_new() returned NULL");
			try
			{
				LibQuickNES.ThrowStringError(LibQuickNES.qn_loadines(Context, Rom, Rom.Length));

				InitSaveRamBuff();
				InitSaveStateBuff();
				InitVideo();
				InitAudio();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		#region Controller

		public ControllerDefinition ControllerDefinition { get { return Emulation.Cores.Nintendo.NES.NES.NESController; } }
		public IController Controller { get; set; }

		void SetPads(out int j1, out int j2)
		{
			j1 = 0;
			j2 = 0;
			if (Controller["P1 A"])
				j1 |= 1;
			if (Controller["P1 B"])
				j1 |= 2;
			if (Controller["P1 Select"])
				j1 |= 4;
			if (Controller["P1 Start"])
				j1 |= 8;
			if (Controller["P1 Up"])
				j1 |= 16;
			if (Controller["P1 Down"])
				j1 |= 32;
			if (Controller["P1 Left"])
				j1 |= 64;
			if (Controller["P1 Right"])
				j1 |= 128;
			if (Controller["P2 A"])
				j2 |= 1;
			if (Controller["P2 B"])
				j2 |= 2;
			if (Controller["P2 Select"])
				j2 |= 4;
			if (Controller["P2 Start"])
				j2 |= 8;
			if (Controller["P2 Up"])
				j2 |= 16;
			if (Controller["P2 Down"])
				j2 |= 32;
			if (Controller["P2 Left"])
				j2 |= 64;
			if (Controller["P2 Right"])
				j2 |= 128;
		}

		#endregion

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			if (Controller["Power"])
				LibQuickNES.qn_reset(Context, true);
			if (Controller["Reset"])
				LibQuickNES.qn_reset(Context, false);

			int j1, j2;
			SetPads(out j1, out j2);

			Frame++;
			LibQuickNES.ThrowStringError(LibQuickNES.qn_emulate_frame(Context, j1, j2));
			IsLagFrame = LibQuickNES.qn_get_joypad_read_count(Context) == 0;
			if (IsLagFrame)
				LagCount++;

			Blit();
			DrainAudio();
		}

		#region state

		IntPtr Context;

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		#endregion

		public string SystemId { get { return "NES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public string BoardName { get { return null; } } // TODO

		#region saveram

		byte[] SaveRamBuff;

		void InitSaveRamBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_size(Context, ref size));
			SaveRamBuff = new byte[size];
		}

		public byte[] ReadSaveRam()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_save(Context, SaveRamBuff, SaveRamBuff.Length));
			return SaveRamBuff;
		}

		public void StoreSaveRam(byte[] data)
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_load(Context, data, data.Length));
		}

		public void ClearSaveRam()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_battery_ram_clear(Context));
		}

		public bool SaveRamModified
		{
			get
			{
				return LibQuickNES.qn_has_battery_ram(Context);
			}
			set
			{
				throw new Exception();
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

		byte[] SaveStateBuff;
		byte[] SaveStateBuff2;

		void InitSaveStateBuff()
		{
			int size = 0;
			LibQuickNES.ThrowStringError(LibQuickNES.qn_state_size(Context, ref size));
			SaveStateBuff = new byte[size];
			SaveStateBuff2 = new byte[size + 13];
		}

		public void SaveStateText(System.IO.TextWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateText(System.IO.TextReader reader)
		{
			throw new NotImplementedException();
		}

		public void SaveStateBinary(System.IO.BinaryWriter writer)
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_state_save(Context, SaveStateBuff, SaveStateBuff.Length));
			writer.Write(SaveStateBuff.Length);
			writer.Write(SaveStateBuff);
			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(System.IO.BinaryReader reader)
		{
			int len = reader.ReadInt32();
			if (len != SaveStateBuff.Length)
				throw new InvalidOperationException("Unexpected savestate buffer length!");
			reader.Read(SaveStateBuff, 0, SaveStateBuff.Length);
			LibQuickNES.ThrowStringError(LibQuickNES.qn_state_load(Context, SaveStateBuff, SaveStateBuff.Length));
			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new System.IO.MemoryStream(SaveStateBuff2, true);
			var bw = new System.IO.BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != SaveStateBuff2.Length)
				throw new InvalidOperationException("Unexpected savestate length!");
			bw.Close();
			return SaveStateBuff2;
		}

		public bool BinarySaveStatesPreferred { get { return true; } }

		#endregion

		public CoreComm CoreComm
		{
			get;
			private set;
		}

		public MemoryDomainList MemoryDomains
		{
			get { return MemoryDomainList.GetDummyList(); }
		}

		public List<KeyValuePair<string, int>> GetCpuFlagsAndRegisters()
		{
			return new List<KeyValuePair<string, int>>();
		}

		#region settings

		public object GetSettings()
		{
			return null;
		}

		public object GetSyncSettings()
		{
			return null;
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(object o)
		{
			return false;
		}

		#endregion

		public void Dispose()
		{
			if (Context != IntPtr.Zero)
			{
				LibQuickNES.qn_delete(Context);
				Context = IntPtr.Zero;
			}
			if (VideoInput != null)
			{
				VideoInputH.Free();
				VideoInput = null;
			}
			if (VideoOutput != null)
			{
				VideoOutputH.Free();
				VideoOutput = null;
			}
		}

		#region VideoProvider

		int[] VideoOutput;
		byte[] VideoInput;
		GCHandle VideoInputH;
		GCHandle VideoOutputH;

		void InitVideo()
		{
			int w = 0, h = 0;
			LibQuickNES.qn_get_image_dimensions(Context, ref w, ref h);
			VideoInput = new byte[w * h];
			VideoInputH = GCHandle.Alloc(VideoInput, GCHandleType.Pinned);
			LibQuickNES.qn_set_pixels(Context, VideoInputH.AddrOfPinnedObject(), w);
			VideoOutput = new int[256 * 240];
			VideoOutputH = GCHandle.Alloc(VideoOutput, GCHandleType.Pinned);
		}

		void Blit()
		{
			LibQuickNES.qn_blit(Context, VideoOutputH.AddrOfPinnedObject());
		}

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return VideoOutput; }
		public int VirtualWidth { get { return 292; } } // probably different on pal
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 240; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region SoundProvider

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		void InitAudio()
		{
			LibQuickNES.ThrowStringError(LibQuickNES.qn_set_sample_rate(Context, 44100));
		}

		void DrainAudio()
		{
			NumSamples = LibQuickNES.qn_read_audio(Context, MonoBuff, 1024);
			unsafe
			{
				fixed (short *_src = &MonoBuff[0], _dst = &StereoBuff[0])
				{
					short* src = _src;
					short* dst = _dst;
					for (int i = 0; i < NumSamples; i++)
					{
						*dst++ = *src;
						*dst++ = *src++;
					}
				}
			}			
		}

		short[] MonoBuff = new short[1024];
		short[] StereoBuff = new short[2048];
		int NumSamples = 0;

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = StereoBuff;
			nsamp = NumSamples;
		}

		public void DiscardSamples()
		{
		}

		#endregion
	}
}
