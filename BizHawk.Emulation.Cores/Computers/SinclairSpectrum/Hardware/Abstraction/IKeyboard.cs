using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents a spectrum keyboard
    /// </summary>
    public interface IKeyboard : IPortIODevice
    {
        /// <summary>
        /// The calling spectrumbase class
        /// </summary>
        SpectrumBase _machine { get; }

        /// <summary>
        /// The keyboard matrix for a particular spectrum model
        /// </summary>
        string[] KeyboardMatrix { get; set; }

        /// <summary>
        /// Other keyboard keys that are not in the matrix
        /// (usually keys derived from key combos)
        /// </summary>
        string[] NonMatrixKeys { get; set; }

        /// <summary>
        /// Represents the spectrum key state
        /// </summary>
        int[] KeyLine { get; set; }

        /// <summary>
        /// Resets the line status
        /// </summary>
        void ResetLineStatus();

        /// <summary>
        /// There are some slight differences in how PortIN and PortOUT functions
        /// between Issue2 and Issue3 keyboards (16k/48k spectrum only)
        /// It is possible that some very old games require Issue2 emulation
        /// </summary>
        bool IsIssue2Keyboard { get; set; }

        /// <summary>
        /// Sets the spectrum key status
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isPressed"></param>
        void SetKeyStatus(string key, bool isPressed);

        /// <summary>
        /// Gets the status of a spectrum key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool GetKeyStatus(string key);

        /// <summary>
        /// Returns the query byte
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        byte GetLineStatus(byte lines);

        /// <summary>
        /// Reads a keyboard byte
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        byte ReadKeyboardByte(ushort addr);

        /// <summary>
        /// Looks up a key in the keyboard matrix and returns the relevent byte value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte GetByteFromKeyMatrix(string key);

        void SyncState(Serializer ser);
    }
}
