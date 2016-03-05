using System;
using System.Collections;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Common.ReflectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public interface IVideoWriter : IDisposable
	{
		/// <summary>
		/// sets the codec token to be used for video compression
		/// </summary>
		void SetVideoCodecToken(IDisposable token);

		/// <summary>
		/// sets to a default video codec token without calling any UI - for automated dumping
		/// </summary>
		void SetDefaultVideoCodecToken();

		/// <summary>
		/// Returns whether this videowriter dumps audio
		/// </summary>
		bool UsesAudio { get; }

		/// <summary>
		/// Returns whether this videowriter dumps video
		/// </summary>
		bool UsesVideo { get; }

		// why no OpenFile(IEnumerator<string>) ?
		// different video writers may have different ideas of how and why splitting is to occur
		/// <summary>
		/// opens a recording stream
		/// set a video codec token first.
		/// </summary>
		void OpenFile(string baseName);

		/// <summary>
		/// close recording stream
		/// </summary>
		void CloseFile();

		/// <summary>
		/// tells which emulation frame we're on. Happens before AddFrame() or AddSamples()
		/// </summary>
		void SetFrame(int frame);

		/// <summary>
		/// adds a frame to the stream
		/// </summary>
		void AddFrame(IVideoProvider source);

		/// <summary>
		/// adds audio samples to the stream
		/// no attempt is made to sync this to the video
		/// reccomendation: try not to have the size or pacing of the audio chunks be too "weird"
		/// </summary>
		void AddSamples(short[] samples);

		/// <summary>
		/// obtain a set of recording compression parameters
		/// return null on user cancel
		/// </summary>
		/// <param name="hwnd">hwnd to attach to if the user is shown config dialog</param>
		/// <returns>codec token, dispose of it when you're done with it</returns>
		IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd);

		/// <summary>
		/// set framerate to fpsnum/fpsden (assumed to be unchanging over the life of the stream)
		/// </summary>
		void SetMovieParameters(int fpsnum, int fpsden);

		/// <summary>
		/// set resolution parameters (width x height)
		/// must be set before file is opened
		/// can be changed in future
		/// should always match IVideoProvider
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		void SetVideoParameters(int width, int height);

		/// <summary>
		/// set audio parameters.  cannot change later
		/// </summary>
		void SetAudioParameters(int sampleRate, int channels, int bits);

		/// <summary>
		/// set metadata parameters; should be called before opening file
		/// ok to not set at all, if not applicable
		/// </summary>
		/// <param name="gameName">The name of the game loaded</param>
		/// <param name="authors">Authors on movie file</param>
		/// <param name="lengthMS">Length of movie file in milliseconds</param>
		/// <param name="rerecords">Number of rerecords on movie file</param>
		void SetMetaData(string gameName, string authors, UInt64 lengthMS, UInt64 rerecords);

		/// <summary>
		/// short description of this IVideoWriter
		/// </summary>
		// string WriterDescription();
		/// <summary>
		/// what default extension this writer would like to put on its output
		/// </summary>
		string DesiredExtension();
		/// <summary>
		/// name that command line parameters can refer to
		/// </summary>
		/// <returns></returns>
		// string ShortName();
	}

	public static class VideoWriterExtensions
	{
		public static string WriterDescription(this IVideoWriter w)
		{
			return w.GetAttribute<VideoWriterAttribute>().Description;
		}

		public static string ShortName(this IVideoWriter w)
		{
			return w.GetAttribute<VideoWriterAttribute>().ShortName;
		}

		public static string LongName(this IVideoWriter w)
		{
			return w.GetAttribute<VideoWriterAttribute>().Name;
		}
	}


	[AttributeUsage(AttributeTargets.Class)]
	public class VideoWriterAttribute : Attribute
	{
		public string ShortName { get; private set; }
		public string Name { get; private set; }
		public string Description { get; private set; }

		public VideoWriterAttribute(string ShortName, string Name, string Description)
		{
			this.ShortName = ShortName;
			this.Name = Name;
			this.Description = Description;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class VideoWriterIgnoreAttribute : Attribute
	{
	}

	public class VideoWriterInfo
	{
		public VideoWriterAttribute Attribs { get; private set; }
		private Type type;

		public VideoWriterInfo(VideoWriterAttribute Attribs, Type type)
		{
			this.type = type;
			this.Attribs = Attribs;
		}

		public IVideoWriter Create()
		{
			return (IVideoWriter)Activator.CreateInstance(type);
		}

		public override string ToString()
		{
			return Attribs.Name;
		}
	}

	/// <summary>
	/// contains methods to find all IVideoWriter
	/// </summary>
	public static class VideoWriterInventory
	{
		private static Dictionary<string, VideoWriterInfo> vws = new Dictionary<string, VideoWriterInfo>();

		static VideoWriterInventory()
		{
			foreach (Type t in typeof(VideoWriterInventory).Assembly.GetTypes())
			{
				if (!t.IsInterface
					&& typeof(IVideoWriter).IsAssignableFrom(t)
					&& !t.IsAbstract
					&& t.GetCustomAttributes(typeof(VideoWriterIgnoreAttribute), false).Length == 0)
				{
					var a = (VideoWriterAttribute)t.GetCustomAttributes(typeof(VideoWriterAttribute), false)[0];
					vws.Add(a.ShortName, new VideoWriterInfo(a, t));
				}
			}
		}

		public static IEnumerable<VideoWriterInfo> GetAllWriters()
		{
			return vws.Values;
		}

		/// <summary>
		/// find an IVideoWriter by its short name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static IVideoWriter GetVideoWriter(string name)
		{
			VideoWriterInfo ret;
			if (vws.TryGetValue(name, out ret))
				return ret.Create();
			else
				return null;
		}
	}
}
