

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Represents a spectrum keyboard
    /// </summary>
    public interface IKeyboard
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
        /// For 16/48k models
        /// </summary>
        bool Issue2 { get; set; }

        /// <summary>
        /// The current keyboard line status
        /// </summary>
        //byte[] LineStatus { get; set; }

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
    }
}
