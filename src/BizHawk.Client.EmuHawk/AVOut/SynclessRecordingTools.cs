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
		private readonly List<FrameInfo> _mFrameInfos = new List<FrameInfo>();
		private readonly Config _config;
		private readonly IGameInfo _game;

		private string _mSynclessConfigFile;
		private string _mFramesDirectory;

		public IDialogController DialogController { get; }

		public IWin32Window SelfAsHandle => this;

		public SynclessRecordingTools(Config config, IGameInfo game, IDialogController dialogController)
		{
			_config = config;
			DialogController = dialogController;
			_game = game;
			InitializeComponent();
		}

		public void Run()
		{
			var ofd = new OpenFileDialog
			{
				FileName = $"{_game.FilesystemSafeName()}.syncless.txt",
				InitialDirectory = _config.PathEntries.AvAbsolutePath()
			};

			if (ofd.ShowDialog() == DialogResult.Cancel)
			{
				return;
			}

			_mSynclessConfigFile = ofd.FileName;
			
			//---- this is pretty crappy:
			var lines = File.ReadAllLines(_mSynclessConfigFile);

			string framesDir = "";
			foreach (var line in lines)
			{
				int idx = line.IndexOf('=');
				string key = line.Substring(0, idx);
				string value = line.Substring(idx + 1, line.Length - (idx + 1));
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
				GetPaths(frame, out var png, out var wav);
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

		private void BtnExport_Click(object sender, EventArgs e)
		{
			if (_mFrameInfos.Count == 0)
			{
				return;
			}

			int width, height;
			using(var bmp = new Bitmap(_mFrameInfos[0].PngPath))
			{
				width = bmp.Width;
				height = bmp.Height;
			}

			var sfd = new SaveFileDialog
			{
				FileName = Path.ChangeExtension(_mSynclessConfigFile, ".avi")
			};
			sfd.InitialDirectory = Path.GetDirectoryName(sfd.FileName);
			if (sfd.ShowDialog() == DialogResult.Cancel)
			{
				return;
			}

			using var avw = new AviWriter(this);
			avw.SetAudioParameters(44100, 2, 16); // hacky
			avw.SetMovieParameters(60, 1); // hacky
			avw.SetVideoParameters(width, height);
			var token = avw.AcquireVideoCodecToken(_config);
			avw.SetVideoCodecToken(token);
			avw.OpenFile(sfd.FileName);
			foreach (var fi in _mFrameInfos)
			{
				using (var bb = new BitmapBuffer(fi.PngPath, new BitmapLoadOptions()))
				{
					var bbvp = new BitmapBufferVideoProvider(bb);
					avw.AddFrame(bbvp);
				}

				// offset = 44 dec
				var wavBytes = File.ReadAllBytes(fi.WavPath);
				var ms = new MemoryStream(wavBytes) { Position = 44 };
				var br = new BinaryReader(ms);
				var sampleData = new List<short>();
				while (br.BaseStream.Position != br.BaseStream.Length)
				{
					sampleData.Add(br.ReadInt16());
				}

				avw.AddSamples(sampleData.ToArray());
			}

			avw.CloseFile();
		}
	}
}
