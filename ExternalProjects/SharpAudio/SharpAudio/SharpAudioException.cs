using System;

namespace SharpAudio
{
    /// <summary>
    /// Represents errors that occur in the Veldrid library.
    /// </summary>
    public class SharpAudioException : Exception
    {
        /// <summary>
        /// Constructs a new VeldridException.
        /// </summary>
        public SharpAudioException()
        {
        }

        /// <summary>
        /// Constructs a new Veldridexception with the given message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public SharpAudioException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a new Veldridexception with the given message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SharpAudioException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
