
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Handles all ZX-level input
    /// </summary>
    public abstract partial class SpectrumBase
    {
        private readonly bool[] _keyboardPressed = new bool[64];
        int _pollIndex;
        private bool _restorePressed;

        
        public void PollInput()
        {
            Spectrum.InputCallbacks.Call();

            // scan keyboard
            _pollIndex = 0;

            for (var i = 0; i < KeyboardDevice.KeyboardMatrix.Length; i++)
            {                
                string key = KeyboardDevice.KeyboardMatrix[i];
                bool prevState = KeyboardDevice.GetKeyStatus(key);
                bool currState = Spectrum._controller.IsPressed(key);

                //if (currState != prevState)
                    KeyboardDevice.SetKeyStatus(key, currState);
            }
        }
    }
}
