using System;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// This class represents a TZX-related exception
    /// </summary>
    public class TzxException : Exception
    {
        /// <summary>
        /// Initializes the exception with the specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public TzxException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes the exception with the specified message
        /// and inner exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public TzxException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
