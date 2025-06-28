using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IVideoProvider
	{
		private int[] _vidBuff = [ ];
		public int VirtualWidth => BufferWidth;
		public int VirtualHeight { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator { get; }
		public int VsyncDenominator { get; }
		public int[] GetVideoBuffer() => _vidBuff;

		private void InitVideo()
		{
			var renderInfo = new LibDSDA.PackedRenderInfo()
			{
				SfxVolume = _settings.SfxVolume,
				MusicVolume = _settings.MusicVolume,
				Gamma = _settings.Gamma,
				HeadsUpMode = (int) _settings.HeadsUpMode,
				MapDetails = (int) _settings.MapDetails,
				MapOverlay = (int) _settings.MapOverlay,
				RenderVideo = 1,
				RenderAudio = 1,
				ShowMessages = Convert.ToInt32(_settings.ShowMessages),
				ReportSecrets = Convert.ToInt32(_settings.ReportSecrets),
				DsdaExHud = Convert.ToInt32(_settings.DsdaExHud),
				DisplayCoordinates = Convert.ToInt32(_settings.DisplayCoordinates),
				DisplayCommands = Convert.ToInt32(_settings.DisplayCommands),
				MapTotals = Convert.ToInt32(_settings.MapTotals),
				MapTime = Convert.ToInt32(_settings.MapTime),
				MapCoordinates = Convert.ToInt32(_settings.MapCoordinates),
				PlayerPointOfView = _settings.DisplayPlayer - 1,
			};

			_core.dsda_init_video(ref renderInfo);
			_vidBuff = new int[BufferWidth * BufferHeight];
		}

		private unsafe void UpdateVideo(bool init = false)
		{
			using (_elf.EnterExit())
			{
				_core.dsda_get_video(out var vi);

				int[] _palBuffer = [ ];
				var videoBuffer = (byte*)vi.VideoBuffer.ToPointer();
				var paletteBuffer = (int*)vi.PaletteBuffer.ToPointer();
				BufferWidth = vi.Width;
				BufferHeight = vi.Height;

				// Handling pallette buffer
				if (_palBuffer.Length < vi.PaletteSize)
				{
					_palBuffer = new int[vi.PaletteSize];
				}
				for (var i = 0; i < _palBuffer.Length; i++)
				{
					_palBuffer[i] = paletteBuffer[i];
				}

				// Handling video buffer
				if (_vidBuff.Length < BufferWidth * BufferHeight)
				{
					_vidBuff = new int[BufferWidth * BufferHeight];
				}
				for (var i = 0; i < _vidBuff.Length; i++)
				{
					_vidBuff[i] = _palBuffer[videoBuffer[i]];
				}
			}
		}
	}
}
