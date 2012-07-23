using System;
using System.Collections;
using System.Collections.Generic;
using BizHawk.MultiClient;

namespace BizHawk
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
		string WriterDescription();
		/// <summary>
		/// what default extension this writer would like to put on its output
		/// </summary>
		string DesiredExtension();
		/// <summary>
		/// name that command line parameters can refer to
		/// </summary>
		/// <returns></returns>
		string ShortName();
    }

	/// <summary>
	/// contains methods to find all IVideoWriter
	/// </summary>
	public static class VideoWriterInventory
	{
		public static IEnumerable<IVideoWriter> GetAllVideoWriters()
		{
			var ret = new IVideoWriter[]
			{ 
				new AviWriter(),
				new JMDWriter(),
				new WavWriterV(),
				new FFmpegWriter(),
				new NutWriter()
			};
			return ret;
		}

		/// <summary>
		/// find an IVideoWriter by its short name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static IVideoWriter GetVideoWriter(string name)
		{
			IVideoWriter ret = null;

			var vws = GetAllVideoWriters();

			foreach (var vw in vws)
				if (vw.ShortName() == name)
					ret = vw;

			foreach (var vw in vws)
				if (vw != ret)
					vw.Dispose();
			return ret;
		}
	}
}
