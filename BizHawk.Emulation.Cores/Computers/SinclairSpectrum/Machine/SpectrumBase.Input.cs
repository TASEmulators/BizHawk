
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
        string NextTape = "Insert Next Tape";
        string PrevTape = "Insert Previous Tape";

        bool pressed_Play = false;
        bool pressed_Stop = false;
        bool pressed_RTZ = false;
        bool pressed_NextTape = false;
        bool pressed_PrevTape = false;

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
                if (!pressed_Play)
                {
                    Spectrum.OSD_FireInputMessage(Play);
                    TapeDevice.Play();
                    pressed_Play = true;
                }
            }
            else
                pressed_Play = false;

            if (Spectrum._controller.IsPressed(Stop))
            {
                if (!pressed_Stop)
                {
                    Spectrum.OSD_FireInputMessage(Stop);
                    TapeDevice.Stop();
                    pressed_Stop = true;
                }
            }
            else
                pressed_Stop = false;

            if (Spectrum._controller.IsPressed(RTZ))
            {
                if (!pressed_RTZ)
                {
                    Spectrum.OSD_FireInputMessage(RTZ);
                    TapeDevice.RTZ();
                    pressed_RTZ = true;
                }
            }
            else
                pressed_RTZ = false;

            if (Spectrum._controller.IsPressed(Record))
            {

            }
            if (Spectrum._controller.IsPressed(NextTape))
            {
                if (!pressed_NextTape)
                {
                    Spectrum.OSD_FireInputMessage(NextTape);
                    TapeMediaIndex++;
                    pressed_NextTape = true;
                }
            }
            else
                pressed_NextTape = false;

            if (Spectrum._controller.IsPressed(PrevTape))
            {
                if (!pressed_PrevTape)
                {
                    Spectrum.OSD_FireInputMessage(PrevTape);
                    TapeMediaIndex--;
                    pressed_PrevTape = true;
                }
            }
            else
                pressed_PrevTape = false;
        }
    }
}

