
using System.Collections.Generic;
using System.Linq;

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

                // J1
                foreach (string j in JoystickCollection[0].ButtonCollection)
                {
                    bool prevState = JoystickCollection[0].GetJoyInput(j);
                    bool currState = Spectrum._controller.IsPressed(j);

                    if (currState != prevState)
                        JoystickCollection[0].SetJoyInput(j, currState);
                }

                // J2
                foreach (string j in JoystickCollection[1].ButtonCollection)
                {
                    bool prevState = JoystickCollection[1].GetJoyInput(j);
                    bool currState = Spectrum._controller.IsPressed(j);

                    if (currState != prevState)
                        JoystickCollection[1].SetJoyInput(j, currState);
                }

                // J3
                foreach (string j in JoystickCollection[2].ButtonCollection)
                {
                    bool prevState = JoystickCollection[2].GetJoyInput(j);
                    bool currState = Spectrum._controller.IsPressed(j);

                    if (currState != prevState)
                        JoystickCollection[2].SetJoyInput(j, currState);
                }
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

        /// <summary>
        /// Instantiates the joysticks array
        /// </summary>
        /// <param name="joys"></param>
        protected void InitJoysticks(List<JoystickType> joys)
        {
            List<IJoystick> jCollection = new List<IJoystick>();

            for (int i = 0; i < joys.Count(); i++)
            {
                jCollection.Add(InstantiateJoystick(joys[i], i + 1));
            }

            JoystickCollection = jCollection.ToArray();

            for (int i = 0; i < JoystickCollection.Length; i++)
            {
                Spectrum.OSD_FireInputMessage("Joystick " + (i + 1) + ": " + JoystickCollection[i].JoyType.ToString());
            }
        }

        /// <summary>
        /// Instantiates a new IJoystick object
        /// </summary>
        /// <param name="type"></param>
        /// <param name="playerNumber"></param>
        /// <returns></returns>
        public IJoystick InstantiateJoystick(JoystickType type, int playerNumber)
        {
            switch (type)
            {
                case JoystickType.Kempston:
                    return new KempstonJoystick(this, playerNumber);
                case JoystickType.Cursor:
                    return new CursorJoystick(this, playerNumber);
                case JoystickType.SinclairLEFT:
                    return new SinclairJoystick1(this, playerNumber);
                case JoystickType.SinclairRIGHT:
                    return new SinclairJoystick2(this, playerNumber);
                case JoystickType.NULL:
                    return new NullJoystick(this, playerNumber);
            }

            return null;
        }

        /// <summary>
        /// Returns a IJoystick object depending on the type (or null if not found)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected IJoystick LocateUniqueJoystick(JoystickType type)
        {
            return JoystickCollection.Where(a => a.JoyType == type).FirstOrDefault();
        }

        /// <summary>
        /// Signs whether input read has been requested
        /// </summary>
        private bool inputRead;
        public bool InputRead
        {
            get { return inputRead; }
            set { inputRead = value; }
        }

    }
}

