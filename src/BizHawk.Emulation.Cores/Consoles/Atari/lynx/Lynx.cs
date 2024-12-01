using System.IO;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
		[PortedCore(CoreNames.Handy, "K. Wilkins, Mednafen Team", "0.9.34.1", "https://mednafen.github.io/releases/")]
	[ServiceNotApplicable(typeof(IRegionable), typeof(ISettable<,>))]
	public partial class Lynx : IEmulator, IVideoProvider, ISoundProvider, ISaveRam, IStatable, IInputPollable
	{
		private static readonly LibLynx LibLynx;

		static Lynx()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libbizlynx.dll.so" : "bizlynx.dll", hasLimitedLifetime: false);
			LibLynx = BizInvoker.GetInvoker<LibLynx>(resolver, CallingConventionAdapters.Native);
		}

		[CoreConstructor(VSystemID.Raw.Lynx)]
		public Lynx(byte[] file, GameInfo game, CoreComm comm)
		{
			ServiceProvider = new BasicServiceProvider(this);

			var bios = comm.CoreFileProvider.GetFirmwareOrThrow(new("Lynx", "Boot"), "Boot rom is required");
			if (bios.Length != 512)
			{
				throw new MissingFirmwareException("Lynx Bootrom must be 512 bytes!");
			}

			int pagesize0 = 0;
			int pagesize1 = 0;
			byte[] realfile = null;

			{
				using var ms = new MemoryStream(file, false);
				using var br = new BinaryReader(ms);
				string header = Encoding.ASCII.GetString(br.ReadBytes(4));
				int p0 = br.ReadUInt16();
				int p1 = br.ReadUInt16();
				int ver = br.ReadUInt16();
				string cname = Encoding.ASCII.GetString(br.ReadBytes(32)).SubstringBefore('\0').Trim();
				string mname = Encoding.ASCII.GetString(br.ReadBytes(16)).SubstringBefore('\0').Trim();
				int rot = br.ReadByte();

				ms.Position = 6;
				string bs93 = Encoding.ASCII.GetString(br.ReadBytes(4));
				if (bs93 == "BS93")
				{
					throw new InvalidOperationException("Unsupported BS93 Lynx ram image");
				}

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
				_saveBuff = new byte[LibLynx.BinStateSize(Core)];

				int rot = game.OptionPresent("rotate") ? int.Parse(game.OptionValue("rotate")) : 0;
				LibLynx.SetRotation(Core, rot);
				if ((rot & 1) != 0)
				{
					BufferWidth = Height;
					BufferHeight = Width;
				}
				else
				{
					BufferWidth = Width;
					BufferHeight = Height;
				}
				SetupMemoryDomains();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private IntPtr Core;

		public IEmulatorServiceProvider ServiceProvider { get; }

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			if (controller.IsPressed("Power"))
			{
				LibLynx.Reset(Core);
			}

			int samples = _soundBuff.Length;
			IsLagFrame = LibLynx.Advance(Core, GetButtons(controller), _videoBuff, _soundBuff, ref samples);
			_numSamp = samples / 2; // sound provider wants number of sample pairs
			if (IsLagFrame)
			{
				LagCount++;
			}

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.Lynx;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				LibLynx.Destroy(Core);
				Core = IntPtr.Zero;
			}
		}

		private static readonly ControllerDefinition LynxTroller = new ControllerDefinition("Lynx Controller")
		{
			BoolButtons = { "Up", "Down", "Left", "Right", "A", "B", "Option 1", "Option 2", "Pause", "Power" },
		}.MakeImmutable();

		public ControllerDefinition ControllerDefinition => LynxTroller;

		private LibLynx.Buttons GetButtons(IController controller)
		{
			LibLynx.Buttons ret = 0;
			if (controller.IsPressed("A")) ret |= LibLynx.Buttons.A;
			if (controller.IsPressed("B")) ret |= LibLynx.Buttons.B;
			if (controller.IsPressed("Up")) ret |= LibLynx.Buttons.Up;
			if (controller.IsPressed("Down")) ret |= LibLynx.Buttons.Down;
			if (controller.IsPressed("Left")) ret |= LibLynx.Buttons.Left;
			if (controller.IsPressed("Right")) ret |= LibLynx.Buttons.Right;
			if (controller.IsPressed("Pause")) ret |= LibLynx.Buttons.Pause;
			if (controller.IsPressed("Option 1")) ret |= LibLynx.Buttons.Option_1;
			if (controller.IsPressed("Option 2")) ret |= LibLynx.Buttons.Option_2;

			return ret;
		}
	}
}
