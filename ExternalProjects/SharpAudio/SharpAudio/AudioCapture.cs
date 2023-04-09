using System;
using System.Runtime.InteropServices;

namespace SharpAudio
{
    /// <summary>
    /// Represents an abstract audio capture device, capable of creating device resources and executing commands.
    /// </summary>
    public abstract class AudioCapture : IDisposable
    {
        /// <summary>
        /// Gets a value identifying the specific graphics API used by this instance.
        /// </summary>
        public abstract AudioBackend BackendType { get; }

        /// <summary>
        /// Creates a new <see cref="AudioCapture"/> using OpenAL.
        /// </summary>
        /// <returns>A new <see cref="AudioCapture"/> using the OpenAL API.</returns>
        public static AudioCapture CreateOpenAL()
        {
            return CreateOpenAL(new AudioCaptureOptions());
        }

        /// <summary>
        /// Creates a new <see cref="AudioCapture"/> using OpenAL.
        /// </summary>
        /// <param name="options">the settings for this audio capture device</param>
        /// <returns>A new <see cref="AudioCapture"/> using the OpenAL API. If not possible returns null</returns>
        public static AudioCapture CreateOpenAL(AudioCaptureOptions options)
        {
            try
            {
                return new AL.ALCapture(options);
            }
            catch (TypeInitializationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new <see cref="AudioCapture"/> using MediaFoundation.
        /// </summary>
        /// <returns>A new <see cref="AudioCapture"/> using the MediaFoundation API.</returns>
        public static AudioCapture CreateMediaFoundation()
        {
            return CreateMediaFoundation(new AudioCaptureOptions());
        }

        /// <summary>
        /// Creates a new <see cref="AudioCapture"/> using MediaFoundation.
        /// </summary>
        /// <param name="options">the settings for this audio capture device</param>
        /// <returns>A new <see cref="AudioCapture"/> using the MediaFoundation API. If not possible returns null</returns>
        public static AudioCapture CreateMediaFoundation(AudioCaptureOptions options)
        {
            try
            {
                return new MF.MFCapture(options);
            }
            catch (TypeInitializationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new <see cref="AudioCapture"/> using OpenAL.
        /// </summary>
        /// <returns>A new <see cref="AudioCapture"/> using the openal API.</returns>
        public static AudioCapture CreateDefault()
        {
            return CreateDefault(new AudioCaptureOptions());
        }

        /// <summary>
        /// Create the default backend for the current operating system
        /// </summary>
        /// <param name="options">the settings for this audio capture device</param>
        /// <returns></returns>
        public static AudioCapture CreateDefault(AudioCaptureOptions options)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return CreateMediaFoundation(options); //TODO
            else
                return CreateOpenAL(options);
        }

        /// <summary>
        /// Performs API-specific disposal of resources controlled by this instance.
        /// </summary>
        protected abstract void PlatformDispose();

        /// <summary>
        /// Free this instance
        /// </summary>
        public void Dispose()
        {
            PlatformDispose();
        }
    }
}
