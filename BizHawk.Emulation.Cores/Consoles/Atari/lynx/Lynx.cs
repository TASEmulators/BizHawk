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
	[ServiceNotApplicable(typeof(ISettable<,>), typeof(IDriveLight), typeof(IRegionable))]
	public partial class Lynx : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable
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
			if (Controller.IsPressed("Power"))
			{
				LibLynx.Reset(Core);
			}

			int samples = soundbuff.Length;
			IsLagFrame = LibLynx.Advance(Core, GetButtons(), videobuff, soundbuff, ref samples);
			numsamp = samples / 2; // sound provider wants number of sample pairs
			if (IsLagFrame)
				LagCount++;
		}

		public int Frame { get; private set; }

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
			if (Controller.IsPressed("A")) ret |= LibLynx.Buttons.A;
			if (Controller.IsPressed("B")) ret |= LibLynx.Buttons.B;
			if (Controller.IsPressed("Up")) ret |= LibLynx.Buttons.Up;
			if (Controller.IsPressed("Down")) ret |= LibLynx.Buttons.Down;
			if (Controller.IsPressed("Left")) ret |= LibLynx.Buttons.Left;
			if (Controller.IsPressed("Right")) ret |= LibLynx.Buttons.Right;
			if (Controller.IsPressed("Pause")) ret |= LibLynx.Buttons.Pause;
			if (Controller.IsPressed("Option 1")) ret |= LibLynx.Buttons.Option_1;
			if (Controller.IsPressed("Option 2")) ret |= LibLynx.Buttons.Option_2;

			return ret;
		}

		#endregion
	}
}
