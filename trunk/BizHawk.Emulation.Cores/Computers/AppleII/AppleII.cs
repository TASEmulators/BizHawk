using System.IO;
using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	[CoreAttributes(
		"Virtu",
		"TODO",
		isPorted: true,
		isReleased: false
		)]
	public partial class AppleII : IEmulator
	{
		[CoreConstructor("AppleII")]
		public AppleII(CoreComm comm, GameInfo game, byte[] rom, object Settings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			CoreComm = comm;

			_disk1 = rom;

			// TODO: get from Firmware provider
			_appleIIRom = File.ReadAllBytes("C:\\apple\\AppleIIe.rom");
			_diskIIRom = File.ReadAllBytes("C:\\apple\\DiskII.rom");


			_machine = new Machine();
			
			var vidService = new BizVideoService(_machine);
			var gpService = new Jellyfish.Virtu.Services.GamePortService(_machine);
			var kbService = new BizKeyboardService(_machine);

			_machine.Services.AddService(typeof(Jellyfish.Virtu.Services.DebugService), new Jellyfish.Virtu.Services.DebugService(_machine));
			_machine.Services.AddService(typeof(Jellyfish.Virtu.Services.AudioService), new BizAudioService(_machine));
			_machine.Services.AddService(typeof(Jellyfish.Virtu.Services.VideoService), vidService);
			_machine.Services.AddService(typeof(Jellyfish.Virtu.Services.GamePortService), gpService);
			_machine.Services.AddService(typeof(Jellyfish.Virtu.Services.KeyboardService), kbService);
			_machine.BizInitialize();

			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(vidService);

			//make a writeable memory stream cloned from the rom.
			//for junk.dsk the .dsk is important because it determines the format from that
			var ms = new MemoryStream();
			ms.Write(rom,0,rom.Length);
			ms.Position = 0;
			bool writeProtected = false; //!!!!!!!!!!!!!!!!!!!
			Jellyfish.Virtu.Services.StorageService.LoadFile(ms, stream => _machine.BootDiskII.Drives[0].InsertDisk("junk.dsk", stream, writeProtected));
		}

		class BizKeyboardService : Jellyfish.Virtu.Services.KeyboardService
		{
			public BizKeyboardService(Machine _machine) : base(_machine) { }
			public override bool IsKeyDown(int key)
			{
				return false; //TODO! lol
			}
		}

		class BizAudioService : Jellyfish.Virtu.Services.AudioService
		{
			public BizAudioService(Machine _machine) : base(_machine) { }
			public override void SetVolume(float volume)
			{
				
			}
		}

		public class BizVideoService : Jellyfish.Virtu.Services.VideoService, IVideoProvider
		{
			public int[] fb;

			int[] IVideoProvider.GetVideoBuffer() { return fb; }

			// put together, these describe a metric on the screen
			// they should define the smallest size that the buffer can be placed inside such that:
			// 1. no actual pixel data is lost
			// 2. aspect ratio is accurate
			int IVideoProvider.VirtualWidth { get { return 560; } }
			int IVideoProvider.VirtualHeight { get { return 384; } }

			int IVideoProvider.BufferWidth { get { return 560; } }
			int IVideoProvider.BufferHeight { get { return 384; } }
			int IVideoProvider.BackgroundColor { get { return 0; } }

			public BizVideoService(Machine machine) :
				base(machine)
			{
				fb = new int[560*384];
			}

			public override void SetFullScreen(bool isFullScreen)
			{
			}

			public override void SetPixel(int x, int y, uint color)
			{
				fb[560 * y + x] = (int)color;
			}
			public override void Update()
			{
			}
		}
			 

		private readonly Machine _machine;
		private readonly byte[] _disk1;
		private readonly byte[] _appleIIRom;
		private readonly byte[] _diskIIRom;

		private static readonly ControllerDefinition AppleIIController =
			new ControllerDefinition
			{
				Name = "Apple II Keyboard",
				BoolButtons =
				{
					"Up", "Down", "Left", "Right"
				}
			};

		private void FrameAdv(bool render, bool rendersound)
		{
			_machine.BizFrameAdvance();
			Frame++;
		}

		#region IEmulator

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		[FeatureNotImplemented]
		public ISoundProvider SoundProvider
		{
			get { return NullSound.SilenceProvider; }
		}

		[FeatureNotImplemented]
		public ISyncSoundProvider SyncSoundProvider
		{
			get { return new FakeSyncSound(NullSound.SilenceProvider, 735); }
		}

		[FeatureNotImplemented]
		public bool StartAsyncSound()
		{
			return true;
		}

		[FeatureNotImplemented]
		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition
		{
			get { return AppleIIController; }
		}

		public IController Controller { get; set; }

		public int Frame { get; set; }

		[FeatureNotImplemented]
		public void FrameAdvance(bool render, bool rendersound)
		{
			FrameAdv(render, rendersound);
		}

		public string SystemId { get { return "AppleII"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			_machine.Dispose();
		}

		#endregion
		
	}
}
