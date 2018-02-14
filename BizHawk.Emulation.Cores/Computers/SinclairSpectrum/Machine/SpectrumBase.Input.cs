
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Handles all ZX-level input
    /// </summary>
    public abstract partial class SpectrumBase
    {
        string Play = "Play Tape";
        string Stop = "Stop Tape";
        string RTZ = "RTZ Tape";
        string Record = "Record Tape";

        public void PollInput()
        {
            Spectrum.InputCallbacks.Call();

            lock (this)
            {
                // parse single keyboard matrix keys
                for (var i = 0; i < KeyboardDevice.KeyboardMatrix.Length; i++)
                {
                    

                    string key = KeyboardDevice.KeyboardMatrix[i];
                    bool prevState = KeyboardDevice.GetKeyStatus(key);
                    bool currState = Spectrum._controller.IsPressed(key);

                    if (currState != prevState)
                        KeyboardDevice.SetKeyStatus(key, currState);
                }

                // non matrix keys
                foreach (string k in KeyboardDevice.NonMatrixKeys)
                {
                    if (!k.StartsWith("Key"))
                        continue;

                    bool currState = Spectrum._controller.IsPressed(k);

                    KeyboardDevice.SetKeyStatus(k, currState);
                }
            }

            // J1
            foreach (string j in KempstonDevice._bitPos)
            {
                bool prevState = KempstonDevice.GetJoyInput(j);
                bool currState = Spectrum._controller.IsPressed(j);

                if (currState != prevState)
                    KempstonDevice.SetJoyInput(j, currState);
            }

            // Tape control
            if (Spectrum._controller.IsPressed(Play))
            {
                TapeDevice.Play();
            }
            if (Spectrum._controller.IsPressed(Stop))
            {
                TapeDevice.Stop();
            }
            if (Spectrum._controller.IsPressed(RTZ))
            {
                TapeDevice.RTZ();
            }
            if (Spectrum._controller.IsPressed(Record))
            {

            }
        }
    }
}

