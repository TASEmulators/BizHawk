using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

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
		void SetDefaultVideoCodecToken(Config config);

		/// <summary>
		/// Returns whether this VideoWriter dumps audio
		/// </summary>
		bool UsesAudio { get; }

		/// <summary>
		/// Returns whether this VideoWriter dumps video
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
		/// recommendation: try not to have the size or pacing of the audio chunks be too "weird"
		/// </summary>
		void AddSamples(short[] samples);

		/// <summary>
		/// obtain a set of recording compression parameters
		/// return null on user cancel
		/// </summary>
		/// <param name="hwnd">hwnd to attach to if the user is shown config dialog</param>
		/// <returns>codec token, dispose of it when you're done with it</returns>
		IDisposable AcquireVideoCodecToken(IDialogController dialogController, IWin32Window hwnd, Config config);

		/// <summary>
		/// set framerate to fpsNum/fpsDen (assumed to be unchanging over the life of the stream)
		/// </summary>
		void SetMovieParameters(int fpsNum, int fpsDen);

		/// <summary>
		/// set resolution parameters (width x height)
		/// must be set before file is opened
		/// can be changed in future
		/// should always match IVideoProvider
		/// </summary>
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
		/// <param name="lengthMs">Length of movie file in milliseconds</param>
		/// <param name="rerecords">Number of rerecords on movie file</param>
		void SetMetaData(string gameName, string authors, ulong lengthMs, ulong rerecords);

		/// <summary>
		/// what default extension this writer would like to put on its output
		/// </summary>
		string DesiredExtension();
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class VideoWriterAttribute : Attribute
	{
		public string ShortName { get; }
		public string Name { get; }
		public string Description { get; }

		public VideoWriterAttribute(string shortName, string name, string description)
		{
			ShortName = shortName;
			Name = name;
			Description = description;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class VideoWriterIgnoreAttribute : Attribute
	{
	}

	public class VideoWriterInfo
	{
		public VideoWriterAttribute Attribs { get; }
		private readonly Type _type;

		public VideoWriterInfo(VideoWriterAttribute attribs, Type type)
		{
			_type = type;
			Attribs = attribs;
		}

		public IVideoWriter Create() => (IVideoWriter)Activator.CreateInstance(_type);

		public override string ToString() => Attribs.Name;
	}

	/// <summary>
	/// contains methods to find all IVideoWriter
	/// </summary>
	public static class VideoWriterInventory
	{
		private static readonly Dictionary<string, VideoWriterInfo> VideoWriters = new Dictionary<string, VideoWriterInfo>();

		static VideoWriterInventory()
		{
			foreach (var t in EmuHawk.ReflectionCache.Types)
			{
				if (!t.IsInterface
					&& typeof(IVideoWriter).IsAssignableFrom(t)
					&& !t.IsAbstract
					&& t.GetCustomAttributes(typeof(VideoWriterIgnoreAttribute), false).Length == 0)
				{
					var a = (VideoWriterAttribute)t.GetCustomAttributes(typeof(VideoWriterAttribute), false)[0];
					VideoWriters.Add(a.ShortName, new VideoWriterInfo(a, t));
				}
			}
		}

		public static IEnumerable<VideoWriterInfo> GetAllWriters() => VideoWriters.Values;

		/// <summary>
		/// find an IVideoWriter by its short name
		/// </summary>
		public static IVideoWriter GetVideoWriter(string name)
		{
			return VideoWriters.TryGetValue(name, out var ret)
				? ret.Create()
				: null;
		}
	}
}
