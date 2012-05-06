using System;
using System.Collections;
using System.Collections.Generic;

namespace BizHawk
{
    public interface IVideoWriter : IDisposable
    {
        /// <summary>
        /// sets the codec token to be used for video compression
        /// </summary>
        void SetVideoCodecToken(IDisposable token);


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
        /// </summary>
        void AddSamples(short[] samples);

        /// <summary>
        /// obtain a set of recording compression parameters
        /// </summary>
        /// <param name="hwnd">hwnd to attach to if the user is shown config dialog</param>
        /// <returns>codec token, dispose of it when you're done with it</returns>
        IDisposable AcquireVideoCodecToken(IntPtr hwnd);

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

    }
}
