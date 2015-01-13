using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	[CoreAttributes("Handy", "K. Wilkins", true, true, "mednafen 0-9-34-1", "http://mednafen.sourceforge.net/")]
	[ServiceNotApplicable(typeof(ISettable<,>), typeof(IDriveLight))]
	public class Lynx : IEmulator, IVideoProvider, ISyncSoundProvider, IMemoryDomains, ISaveRam, IStatable, IInputPollable
	{
		IntPtr Core;

		[CoreConstructor("Lynx")]
		public Lynx(byte[] file, GameInfo game, CoreComm comm)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			byte[] bios = CoreComm.CoreFileProvider.GetFirmware("Lynx", "Boot", true, "Boot rom is required");
			if (bios.Length != 512)
				throw new MissingFirmwareException("Lynx Bootrom must be 512 bytes!");

			int pagesize0 = 0;
			int pagesize1 = 0;
			byte[] realfile = null;

			{
				var ms = new MemoryStream(file, false);
				var br = new BinaryReader(ms);
				string header = Encoding.ASCII.GetString(br.ReadBytes(4));
				int p0 = br.ReadUInt16();
				int p1 = br.ReadUInt16();
				int ver = br.ReadUInt16();
				string cname = Encoding.ASCII.GetString(br.ReadBytes(32)).Trim();
				string mname = Encoding.ASCII.GetString(br.ReadBytes(16)).Trim();
				int rot = br.ReadByte();

				ms.Position = 6;
				string bs93 = Encoding.ASCII.GetString(br.ReadBytes(6));
				if (bs93 == "BS93")
					throw new InvalidOperationException("Unsupported BS93 Lynx ram image");

				if (header == "LYNX" && (ver & 255) == 1)
				{
					Console.WriteLine("Processing Handy-Lynx header");
					pagesize0 = p0;
					pagesize1 = p1;
					Console.WriteLine("TODO: Rotate {0}", rot);
					Console.WriteLine("Cart: {0} Manufacturer: {1}", cname, mname);
					realfile = new byte[file.Length - 64];
					Buffer.BlockCopy(file, 64, realfile, 0, realfile.Length);
					Console.WriteLine("Header Listed banking: {0} {1}", p0, p1);
				}
				else
				{
					Console.WriteLine("No Handy-Lynx header found!  Assuming raw rom image.");
					realfile = file;
				}

			}

			if (game.OptionPresent("pagesize0"))
			{
				pagesize0 = int.Parse(game.OptionValue("pagesize0"));
				pagesize1 = int.Parse(game.OptionValue("pagesize1"));
				Console.WriteLine("Loading banking options {0} {1} from gamedb", pagesize0, pagesize1);
			}

			if (pagesize0 == 0 && pagesize1 == 0)
			{
				switch (realfile.Length)
				{
					case 0x10000: pagesize0 = 0x100; break;
					case 0x20000: pagesize0 = 0x200; break; //
					case 0x40000: pagesize0 = 0x400; break; // all known good dumps fall in one of these three categories
					case 0x80000: pagesize0 = 0x800; break; //

					case 0x30000: pagesize0 = 0x200; pagesize1 = 0x100; break;
					case 0x50000: pagesize0 = 0x400; pagesize1 = 0x100; break;
					case 0x60000: pagesize0 = 0x400; pagesize1 = 0x200; break;
					case 0x90000: pagesize0 = 0x800; pagesize1 = 0x100; break;
					case 0xa0000: pagesize0 = 0x800; pagesize1 = 0x200; break;
					case 0xc0000: pagesize0 = 0x800; pagesize1 = 0x400; break;
					case 0x100000: pagesize0 = 0x800; pagesize1 = 0x800; break;
				}
				Console.WriteLine("Auto-guessed banking options {0} {1}", pagesize0, pagesize1);
			}

			Core = LibLynx.Create(realfile, realfile.Length, bios, bios.Length, pagesize0, pagesize1, false);
			try
			{
				CoreComm.VsyncNum = 16000000; // 16.00 mhz refclock
				CoreComm.VsyncDen = 16 * 105 * 159;

				savebuff = new byte[LibLynx.BinStateSize(Core)];
				savebuff2 = new byte[savebuff.Length + 13];

				int rot = game.OptionPresent("rotate") ? int.Parse(game.OptionValue("rotate")) : 0;
				LibLynx.SetRotation(Core, rot);
				if ((rot & 1) != 0)
				{
					BufferWidth = HEIGHT;
					BufferHeight = WIDTH;
				}
				else
				{
					BufferWidth = WIDTH;
					BufferHeight = HEIGHT;
				}
				SetupMemoryDomains();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			if (Controller["Power"])
				LibLynx.Reset(Core);

			int samples = soundbuff.Length;
			IsLagFrame = LibLynx.Advance(Core, GetButtons(), videobuff, soundbuff, ref samples);
			numsamp = samples / 2; // sound provider wants number of sample pairs
			if (IsLagFrame)
				LagCount++;
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; private set; }

		// TODO
		public IInputCallbackSystem InputCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		public string SystemId { get { return "Lynx"; } }

		public bool DeterministicEmulation { get { return true; } }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				LibLynx.Destroy(Core);
				Core = IntPtr.Zero;
			}
		}

		#region Controller

		private static readonly ControllerDefinition LynxTroller = new ControllerDefinition
		{
			Name = "Lynx Controller",
			BoolButtons = { "Up", "Down", "Left", "Right", "A", "B", "Option 1", "Option 2", "Pause", "Power" },
		};

		public ControllerDefinition ControllerDefinition { get { return LynxTroller; } }
		public IController Controller { get; set; }

		LibLynx.Buttons GetButtons()
		{
			LibLynx.Buttons ret = 0;
			if (Controller["A"]) ret |= LibLynx.Buttons.A;
			if (Controller["B"]) ret |= LibLynx.Buttons.B;
			if (Controller["Up"]) ret |= LibLynx.Buttons.Up;
			if (Controller["Down"]) ret |= LibLynx.Buttons.Down;
			if (Controller["Left"]) ret |= LibLynx.Buttons.Left;
			if (Controller["Right"]) ret |= LibLynx.Buttons.Right;
			if (Controller["Pause"]) ret |= LibLynx.Buttons.Pause;
			if (Controller["Option 1"]) ret |= LibLynx.Buttons.Option_1;
			if (Controller["Option 2"]) ret |= LibLynx.Buttons.Option_2;

			return ret;
		}

		#endregion

		#region savestates

		JsonSerializer ser = new JsonSerializer() { Formatting = Formatting.Indented };
		byte[] savebuff;
		byte[] savebuff2;

		class TextStateData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
		}

		public void SaveStateText(TextWriter writer)
		{
			var s = new TextState<TextStateData>();
			s.Prepare();
			var ff = s.GetFunctionPointersSave();
			LibLynx.TxtStateSave(Core, ref ff);
			s.ExtraData.IsLagFrame = IsLagFrame;
			s.ExtraData.LagCount = LagCount;
			s.ExtraData.Frame = Frame;

			ser.Serialize(writer, s);
			// write extra copy of stuff we don't use
			writer.WriteLine();
			writer.WriteLine("Frame {0}", Frame);

			//Console.WriteLine(BizHawk.Common.BufferExtensions.BufferExtensions.HashSHA1(SaveStateBinary()));
		}

		public void LoadStateText(TextReader reader)
		{
			var s = (TextState<TextStateData>)ser.Deserialize(reader, typeof(TextState<TextStateData>));
			s.Prepare();
			var ff = s.GetFunctionPointersLoad();
			LibLynx.TxtStateLoad(Core, ref ff);
			IsLagFrame = s.ExtraData.IsLagFrame;
			LagCount = s.ExtraData.LagCount;
			Frame = s.ExtraData.Frame;
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			if (!LibLynx.BinStateSave(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("Core's BinStateSave() returned false!");
			writer.Write(savebuff.Length);
			writer.Write(savebuff);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != savebuff.Length)
				throw new InvalidOperationException("Save buffer size mismatch!");
			reader.Read(savebuff, 0, length);
			if (!LibLynx.BinStateLoad(Core, savebuff, savebuff.Length))
				throw new InvalidOperationException("Core's BinStateLoad() returned false!");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream(savebuff2, true);
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			if (ms.Position != savebuff2.Length)
				throw new InvalidOperationException();
			ms.Close();
			return savebuff2;
		}

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		#endregion

		#region saveram

		public byte[] CloneSaveRam()
		{
			int size;
			IntPtr data;
			if (!LibLynx.GetSaveRamPtr(Core, out size, out data))
				return null;
			byte[] ret = new byte[size];
			Marshal.Copy(data, ret, 0, size);
			return ret;
		}

		public void StoreSaveRam(byte[] srcdata)
		{
			int size;
			IntPtr data;
			if (!LibLynx.GetSaveRamPtr(Core, out size, out data))
				throw new InvalidOperationException();
			if (size != srcdata.Length)
				throw new ArgumentOutOfRangeException();
			Marshal.Copy(srcdata, 0, data, size);
		}

		public bool SaveRamModified
		{
			get
			{
				int unused;
				IntPtr unused2;
				return LibLynx.GetSaveRamPtr(Core, out unused, out unused2);
			}
		}

		#endregion

		#region VideoProvider

		const int WIDTH = 160;
		const int HEIGHT = 102;

		int[] videobuff = new int[WIDTH * HEIGHT];

		public IVideoProvider VideoProvider { get { return this; } }
		public int[] GetVideoBuffer() { return videobuff; }
		public int VirtualWidth { get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion

		#region SoundProvider

		short[] soundbuff = new short[2048];
		int numsamp;

		public ISoundProvider SoundProvider { get { return null; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return false; }
		public void EndAsyncSound() { }

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = soundbuff;
			nsamp = numsamp;
		}

		public void DiscardSamples()
		{
		}

		#endregion

		#region MemoryDomains

		private void SetupMemoryDomains()
		{
			var mms = new List<MemoryDomain>();
			mms.Add(MemoryDomain.FromIntPtr("RAM", 65536, MemoryDomain.Endian.Little, LibLynx.GetRamPointer(Core), true));

			IntPtr p;
			int s;
			if (LibLynx.GetSaveRamPtr(Core, out s, out p))
				mms.Add(MemoryDomain.FromIntPtr("Save RAM", s, MemoryDomain.Endian.Little, p, true));

			IntPtr p0, p1;
			int s0, s1;
			LibLynx.GetReadOnlyCartPtrs(Core, out s0, out p0, out s1, out p1);
			if (s0 > 0 && p0 != IntPtr.Zero)
				mms.Add(MemoryDomain.FromIntPtr("Cart A", s0, MemoryDomain.Endian.Little, p0, false));
			if (s1 > 0 && p1 != IntPtr.Zero)
				mms.Add(MemoryDomain.FromIntPtr("Cart B", s1, MemoryDomain.Endian.Little, p1, false));

			MemoryDomains = new MemoryDomainList(mms, 0);
		}


		public IMemoryDomainList MemoryDomains { get; private set; }

		#endregion
	}
}
