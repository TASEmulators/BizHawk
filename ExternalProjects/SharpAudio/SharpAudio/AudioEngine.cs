using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpAudio
{
    /// <summary>
    /// Represents an abstract audio device, capable of creating device resources and executing commands.
    /// </summary>
    public abstract class AudioEngine : IDisposable
    {
        /// <summary>
        /// Gets a value identifying the specific graphics API used by this instance.
        /// </summary>
        public abstract AudioBackend BackendType { get; }

        /// <summary>
        /// Creates a new <see cref="AudioEngine"/> using XAudio 2.
        /// </summary>
        /// <returns>A new <see cref="AudioEngine"/> using the XAudio 2 API.</returns>
        public static AudioEngine CreateXAudio()
        {
            return CreateXAudio(new AudioEngineOptions());
        }

        /// <summary>
        /// Creates a new <see cref="AudioEngine"/> using XAudio 2.
        /// </summary>
        /// <param name="options">the settings for this audio engine</param>
        /// <returns>A new <see cref="AudioEngine"/> using the XAudio 2 API.</returns>
        public static AudioEngine CreateXAudio(AudioEngineOptions options)
        {
            try
            {
                return new XA2.XA2Engine(options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new <see cref="AudioEngine"/> using OpenAL.
        /// </summary>
        /// <returns>A new <see cref="AudioEngine"/> using the openal API.</returns>
        public static AudioEngine CreateOpenAL()
        {
            return CreateOpenAL(new AudioEngineOptions());
        }

        /// <summary>
        /// Creates a new <see cref="AudioEngine"/> using OpenAL.
        /// </summary>
        /// <param name="options">the settings for this audio engine</param>
        /// <returns>A new <see cref="AudioEngine"/> using the openal API. If not possible returns null</returns>
        public static AudioEngine CreateOpenAL(AudioEngineOptions options)
        {
            try
            {
                return new AL.ALEngine(options);
            }
            catch (TypeInitializationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new <see cref="AudioEngine"/> using OpenAL.
        /// </summary>
        /// <returns>A new <see cref="AudioEngine"/> using the openal API.</returns>
        public static AudioEngine CreateDefault()
        {
            return CreateDefault(new AudioEngineOptions());
        }

        /// <summary>
        /// Create the default backend for the current operating system
        /// </summary>
        /// <param name="options">the settings for this audio engine</param>
        /// <returns></returns>
        public static AudioEngine CreateDefault(AudioEngineOptions options)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return CreateXAudio(options);
            else
                return CreateOpenAL(options);
        }

        /// <summary>
        /// Creates a new <see cref="AudioBuffer"/> with this engine.
        /// </summary>
        /// <returns>A new <see cref="AudioBuffer"/></returns>
        public abstract AudioBuffer CreateBuffer();

        /// <summary>
        /// Creates a new <see cref="AudioSource"/> with this engine.
        /// </summary>
        /// <param name="mixer">The mixer this sound will be added to</param>
        /// <returns>A new <see cref="AudioSource"/></returns>
        public abstract AudioSource CreateSource(Submixer mixer = null);

        /// <summary>
        /// Creates a new <see cref="Audio3DEngine"/> with this engine.
        /// </summary>
        /// <returns>A new <see cref="Audio3DEngine"/></returns>
        public abstract Audio3DEngine Create3DEngine();

        /// <summary>
        /// Creates a new <see cref="Submixer"/> with this engine.
        /// </summary>
        /// <returns>A new <see cref="Submixer"/></returns>
        public abstract Submixer CreateSubmixer();

        /// <summary>
        /// Acquires friendly names for the possible devices to be used
        /// </summary>
        /// <returns>An enumeration of names for all possible devices</returns>
        public static IEnumerable<string> GetDeviceNames(AudioBackend backend)
        {
	        return backend switch
	        {
		        AudioBackend.XAudio2 => XA2.XA2Engine.GetDeviceNames(),
		        AudioBackend.OpenAL => AL.ALEngine.GetDeviceNames(),
		        _ => Array.Empty<string>()
	        };
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
