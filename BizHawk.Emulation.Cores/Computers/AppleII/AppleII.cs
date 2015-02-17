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
	public partial class AppleII : IEmulator, IVideoProvider
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
			 _machine.Pause();
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
			_machine.Unpause();
			while (!_machine.Video.IsVBlank)
			{
				// Do nothing
			}

			while (_machine.Video.IsVBlank)
			{
				// Do nothing
			}

			_machine.Pause();

			Frame++;
		}

		#region IVideoProvider

		public int VirtualWidth { get { return 256; } }
		public int VirtualHeight { get { return 192; } }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }
		public int BackgroundColor { get { return 0; } }

		public int[] GetVideoBuffer()
		{
			//_machine.Video // Uh, yeah, something
			return new int[BufferWidth * BufferHeight];
		}

		#endregion

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
