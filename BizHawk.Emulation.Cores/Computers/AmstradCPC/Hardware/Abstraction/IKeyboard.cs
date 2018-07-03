using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Represents a spectrum keyboard
    /// </summary>
    public interface IKeyboard
    {
        /// <summary>
        /// The calling spectrumbase class
        /// </summary>
        CPCBase _machine { get; }

        /// <summary>
        /// The keyboard matrix for a particular CPC model
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
        bool[] KeyStatus { get; set; }

        /// <summary>
        /// The currently selected line
        /// </summary>
        int CurrentLine { get; set; }

        /// <summary>
        /// Reads the current line status
        /// </summary>
        /// <returns></returns>
        byte ReadCurrentLine();

        /// <summary>
        /// Sets the CPC key status
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isPressed"></param>
        void SetKeyStatus(string key, bool isPressed);

        /// <summary>
        /// Gets the status of a CPC key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool GetKeyStatus(string key);

        void SyncState(Serializer ser);
    }
}
