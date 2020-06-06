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
	public partial class SynclessRecordingTools : Form
	{
		public SynclessRecordingTools()
		{
			InitializeComponent();
		}

		private void GetPaths(int index, out string png, out string wav)
		{
			string subPath = SynclessRecorder.GetPathFragmentForFrameNum(index);
			string path = _mFramesDirectory;
			path = Path.Combine(path, subPath);
			png = $"{path}.png";
			wav = $"{path}.wav";
		}

		private string _mSynclessConfigFile;
		private string _mFramesDirectory;

		public void Run()
		{
			var ofd = new OpenFileDialog
			{
				FileName = $"{Global.Game.FilesystemSafeName()}.syncless.txt",
				InitialDirectory = GlobalWin.Config.PathEntries.AvAbsolutePath()
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
					pngPath = png,
					wavPath = wav
				});
				
				frame++;
			}

			ShowDialog();
		}

		private readonly List<FrameInfo> _mFrameInfos = new List<FrameInfo>();

		struct FrameInfo
		{
			public string wavPath, pngPath;
		}


		private void btnExport_Click(object sender, EventArgs e)
		{
			if (_mFrameInfos.Count == 0)
			{
				return;
			}

			int width, height;
			using(var bmp = new Bitmap(_mFrameInfos[0].pngPath))
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

			using var avw = new AviWriter();
			avw.SetAudioParameters(44100, 2, 16); // hacky
			avw.SetMovieParameters(60, 1); // hacky
			avw.SetVideoParameters(width, height);
			var token = avw.AcquireVideoCodecToken(this);
			avw.SetVideoCodecToken(token);
			avw.OpenFile(sfd.FileName);
			foreach (var fi in _mFrameInfos)
			{
				using (var bb = new BitmapBuffer(fi.pngPath, new BitmapLoadOptions()))
				{
					var bbvp = new BitmapBufferVideoProvider(bb);
					avw.AddFrame(bbvp);
				}

				// offset = 44 dec
				var wavBytes = File.ReadAllBytes(fi.wavPath);
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
