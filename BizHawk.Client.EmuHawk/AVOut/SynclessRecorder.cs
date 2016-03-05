using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation;
using BizHawk.Emulation.Common;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	[VideoWriter("syncless", "Syncless Recording", "Writes each frame to a directory as a PNG and WAV pair, identified by frame number. The results can be exported into one video file.")]
	public class SynclessRecorder : IVideoWriter
	{
		public void Dispose() { }

		public void SetVideoCodecToken(IDisposable token) { }

		public void SetDefaultVideoCodecToken() { }

		public void SetFrame(int frame)
		{
			mCurrFrame = frame;
		}

		int mCurrFrame;
		string mBaseDirectory, mFramesDirectory;
		string mProjectFile;
		public void OpenFile(string projFile)
		{
			mProjectFile = projFile;
			mBaseDirectory = Path.GetDirectoryName(mProjectFile);
			string basename = Path.GetFileNameWithoutExtension(projFile);
			string framesDirFragment = basename + "_frames";
			mFramesDirectory = Path.Combine(mBaseDirectory, framesDirFragment);
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("version=1");
			sb.AppendLine("framesdir=" + framesDirFragment);
			File.WriteAllText(mProjectFile, sb.ToString());
		}

		public void CloseFile() { }

		public void AddFrame(IVideoProvider source)
		{
			using (var bb = new BitmapBuffer(source.BufferWidth, source.BufferHeight, source.GetVideoBuffer()))
			{
				string subpath = GetAndCreatePathForFrameNum(mCurrFrame);
				string path = subpath + ".png";
				bb.ToSysdrawingBitmap().Save(path, System.Drawing.Imaging.ImageFormat.Png);
			}
		}

		public void AddSamples(short[] samples)
		{
			string subpath = GetAndCreatePathForFrameNum(mCurrFrame);
			string path = subpath + ".wav";
			WavWriterV wwv = new WavWriterV();
			wwv.SetAudioParameters(paramSampleRate, paramChannels, paramBits);
			wwv.OpenFile(path);
			wwv.AddSamples(samples);
			wwv.CloseFile();
			wwv.Dispose();
		}

		public bool UsesAudio { get { return true; } }
		public bool UsesVideo { get { return true; } }

		class DummyDisposable : IDisposable { public void Dispose() { } }

		public IDisposable AcquireVideoCodecToken(IWin32Window hwnd) { return new DummyDisposable(); }

		public void SetMovieParameters(int fpsnum, int fpsden)
		{
			//should probably todo in here
		}

		public void SetVideoParameters(int width, int height)
		{
			//may want to todo
		}

		int paramSampleRate, paramChannels, paramBits;

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			paramSampleRate = sampleRate;
			paramChannels = channels;
			paramBits = bits;
		}

		public void SetMetaData(string gameName, string authors, UInt64 lengthMS, UInt64 rerecords)
		{
			//not needed
		}

		public string DesiredExtension() { return "syncless.txt"; }


		/// <summary>
		/// splits the string into chunks of length s
		/// </summary>
		static List<string> StringChunkSplit(string s, int len)
		{
			if (len == 0) throw new ArgumentException("Invalid len", "len");

			int numChunks = (s.Length + len - 1) / len;
			List<string> output = new List<string>(numChunks);
			for (int i = 0, j = 0; i < numChunks; i++, j += len)
			{
				int todo = len;
				int remain = s.Length - j;
				if (remain < todo) todo = remain;

				output.Add(s.Substring(j, todo));
			}
			return output;
		}

		string GetAndCreatePathForFrameNum(int index)
		{
			string subpath = GetPathFragmentForFrameNum(index);
			string path = mFramesDirectory;
			path = Path.Combine(path, subpath);
			string fpath = path + ".nothing";
			Directory.CreateDirectory(Path.GetDirectoryName(fpath));
			return path;
		}

		public static string GetPathFragmentForFrameNum(int index)
		{
			var chunks = StringChunkSplit(index.ToString(), 2);
			string subpath = string.Join("/", chunks);
			return subpath;
		}
	}


}
