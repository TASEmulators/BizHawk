using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SynclessRecordingTools : Form, IDialogParent
	{
		private readonly Config _config;

		private readonly IGameInfo _game;

#if AVI_SUPPORT
		private readonly List<FrameInfo> _mFrameInfos = new();

		private string _mFramesDirectory;

		private string _mSynclessConfigFile;
#endif

		public IDialogController DialogController { get; }

		public SynclessRecordingTools(Config config, IGameInfo game, IDialogController dialogController)
		{
			_config = config;
			DialogController = dialogController;
			_game = game;
			InitializeComponent();
		}

#if AVI_SUPPORT
		public void Run()
		{
			string result = this.ShowFileOpenDialog(
				initDir: _config.PathEntries.AvAbsolutePath(),
				initFileName: $"{_game.FilesystemSafeName()}.syncless.txt");
			if (result is null) return;

			_mSynclessConfigFile = result;

			//---- this is pretty crappy:
			string[] lines = File.ReadAllLines(_mSynclessConfigFile);

			string framesDir = "";
			foreach (string line in lines)
			{
				int idx = line.IndexOf('=');
				string key = line[..idx];
				string value = line[(idx + 1)..];
				if (key == "framesdir")
				{
					framesDir = value;
				}
			}

			_mFramesDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_mSynclessConfigFile)), framesDir);
			
			// scan frames directory
			int frame = 1; // hacky! skip frame 0, because we have a problem with dumping that frame somehow
			for (;;)
			{
				GetPaths(frame, out string png, out string wav);
				if (!File.Exists(png) || !File.Exists(wav))
				{
					break;
				}

				_mFrameInfos.Add(new FrameInfo
				{
					PngPath = png,
					WavPath = wav
				});
				
				frame++;
			}

			ShowDialog();
		}

		private void GetPaths(int index, out string png, out string wav)
		{
			string subPath = SynclessRecorder.GetPathFragmentForFrameNum(index);
			string path = _mFramesDirectory;
			path = Path.Combine(path, subPath);
			png = $"{path}.png";
			wav = $"{path}.wav";
		}

		private class FrameInfo
		{
			public string WavPath { get; set; }
			public string PngPath { get; set; }
		}
#endif

		private void BtnExport_Click(object sender, EventArgs e)
		{
#if AVI_SUPPORT
			if (_mFrameInfos.Count == 0)
			{
				return;
			}

			int width, height;
			using(Bitmap bmp = new Bitmap(_mFrameInfos[0].PngPath))
			{
				width = bmp.Width;
				height = bmp.Height;
			}

			string initFileName = Path.ChangeExtension(_mSynclessConfigFile, ".avi");
			string result = this.ShowFileSaveDialog(
				initDir: Path.GetDirectoryName(initFileName)!,
				initFileName: initFileName);
			if (result is null) return;

			using AviWriter avw = new AviWriter(this);
			avw.SetAudioParameters(44100, 2, 16); // hacky
			avw.SetMovieParameters(60, 1); // hacky
			avw.SetVideoParameters(width, height);
			var token = avw.AcquireVideoCodecToken(_config);
			avw.SetVideoCodecToken(token);
			avw.OpenFile(result);
			foreach (var fi in _mFrameInfos)
			{
				using (BitmapBuffer bb = new BitmapBuffer(fi.PngPath, new BitmapLoadOptions()))
				{
					BitmapBufferVideoProvider bbvp = new BitmapBufferVideoProvider(bb);
					avw.AddFrame(bbvp);
				}

				// offset = 44 dec
				byte[] wavBytes = File.ReadAllBytes(fi.WavPath);
				MemoryStream ms = new MemoryStream(wavBytes) { Position = 44 };
				BinaryReader br = new BinaryReader(ms);
				List<short> sampleData = new List<short>();
				while (br.BaseStream.Position != br.BaseStream.Length)
				{
					sampleData.Add(br.ReadInt16());
				}

				avw.AddSamples(sampleData.ToArray());
			}

			avw.CloseFile();
#endif
		}
	}
}
