using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Bizware;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SynclessRecordingTools : Form
	{
		public SynclessRecordingTools()
		{
			InitializeComponent();
		}

		void GetPaths(int index, out string png, out string wav)
		{
			string subpath = SynclessRecorder.GetPathFragmentForFrameNum(index);
			string path = mFramesDirectory;
			path = Path.Combine(path, subpath);
			png = path + ".png";
			wav = path + ".wav";
		}

		string mSynclessConfigFile;
		string mFramesDirectory;

		public void Run()
		{
			var ofd = new OpenFileDialog();
			ofd.FileName = PathManager.FilesystemSafeName(Global.Game) + ".syncless.txt";
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AvPathFragment, null);
			if (ofd.ShowDialog() == DialogResult.Cancel)
				return;

			mSynclessConfigFile = ofd.FileName;
			
			//---- this is pretty crappy:
			var lines = File.ReadAllLines(mSynclessConfigFile);

			string framesdir = "";
			foreach (var line in lines)
			{
				int idx = line.IndexOf('=');
				string key = line.Substring(0, idx);
				string value = line.Substring(idx + 1, line.Length - (idx + 1));
				if (key == "framesdir")
				{
					framesdir = value;
				}
			}

			mFramesDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(mSynclessConfigFile)), framesdir);
			
			//scan frames directory
			int frame = 1; //hacky! skip frame 0, because we have a problem with dumping that frame somehow
			for (; ; )
			{
				string wav, png;
				GetPaths(frame, out png, out wav);
				if (!File.Exists(png) || !File.Exists(wav))
					break;
				mFrameInfos.Add(new FrameInfo()
				{
					pngPath = png,
					wavPath = wav
				});
				
				frame++;
			}

			ShowDialog();
		}

		List<FrameInfo> mFrameInfos = new List<FrameInfo>();
		struct FrameInfo
		{
			public string wavPath, pngPath;
		}


		private void btnExport_Click(object sender, EventArgs e)
		{
			if(mFrameInfos.Count == 0) return;

			int width, height;
			using(var bmp = new Bitmap(mFrameInfos[0].pngPath))
			{
				width = bmp.Width;
				height = bmp.Height;
			}

			var sfd = new SaveFileDialog();
			sfd.FileName = Path.ChangeExtension(mSynclessConfigFile, ".avi");
			sfd.InitialDirectory = Path.GetDirectoryName(sfd.FileName);
			if (sfd.ShowDialog() == DialogResult.Cancel)
				return;

			using (AviWriter avw = new AviWriter())
			{
				avw.SetAudioParameters(44100, 2, 16); //hacky
				avw.SetMovieParameters(60, 1); //hacky
				avw.SetVideoParameters(width, height);
				var token = avw.AcquireVideoCodecToken(this);
				avw.SetVideoCodecToken(token);
				avw.OpenFile(sfd.FileName);
				foreach (var fi in mFrameInfos)
				{
					using (var bb = new BitmapBuffer(fi.pngPath, new BitmapLoadOptions()))
					{
						var bbvp = new BitmapBufferVideoProvider(bb);
						avw.AddFrame(bbvp);
					}
					//offset = 44 dec
					var wavBytes = File.ReadAllBytes(fi.wavPath);
					var ms = new MemoryStream(wavBytes);
					ms.Position = 44;
					var br = new BinaryReader(ms);
					List<short> sampledata = new List<short>();
					while (br.BaseStream.Position != br.BaseStream.Length)
					{
						sampledata.Add(br.ReadInt16());
					}
					avw.AddSamples(sampledata.ToArray());
				}
				avw.CloseFile();
			}

		}
	}
}
