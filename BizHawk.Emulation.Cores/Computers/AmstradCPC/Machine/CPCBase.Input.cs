using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Input *
    /// </summary>
    public abstract partial class CPCBase
    {
        string Play = "Play Tape";
        string Stop = "Stop Tape";
        string RTZ = "RTZ Tape";
        string Record = "Record Tape";
        string NextTape = "Insert Next Tape";
        string PrevTape = "Insert Previous Tape";
        string NextBlock = "Next Tape Block";
        string PrevBlock = "Prev Tape Block";
        string TapeStatus = "Get Tape Status";

        string NextDisk = "Insert Next Disk";
        string PrevDisk = "Insert Previous Disk";
        string EjectDisk = "Eject Current Disk";
        string DiskStatus = "Get Disk Status";

        string HardResetStr = "Power";
        string SoftResetStr = "Reset";

        bool pressed_Play = false;
        bool pressed_Stop = false;
        bool pressed_RTZ = false;
        bool pressed_NextTape = false;
        bool pressed_PrevTape = false;
        bool pressed_NextBlock = false;
        bool pressed_PrevBlock = false;
        bool pressed_TapeStatus = false;
        bool pressed_NextDisk = false;
        bool pressed_PrevDisk = false;
        bool pressed_EjectDisk = false;
        bool pressed_DiskStatus = false;
        bool pressed_HardReset = false;
        bool pressed_SoftReset = false;

        /// <summary>
        /// Cycles through all the input callbacks
        /// This should be done once per frame
        /// </summary>
        public void PollInput()
        {
            CPC.InputCallbacks.Call();

            lock (this)
            {
                // parse single keyboard matrix keys.
                // J1 and J2 are scanned as part of the keyboard
                for (var i = 0; i < KeyboardDevice.KeyboardMatrix.Length; i++)
                {
                    string key = KeyboardDevice.KeyboardMatrix[i];
                    bool prevState = KeyboardDevice.GetKeyStatus(key);
                    bool currState = CPC._controller.IsPressed(key);

                    if (currState != prevState)
                        KeyboardDevice.SetKeyStatus(key, currState);
                }

                // non matrix keys (J2)
                foreach (string k in KeyboardDevice.NonMatrixKeys)
                {
                    if (!k.StartsWith("P2"))
                        continue;

                    bool currState = CPC._controller.IsPressed(k);

                    switch (k)
                    {
                        case "P2 Up":
                            if (currState)
                                KeyboardDevice.SetKeyStatus("Key 6", true);
                            else if (!KeyboardDevice.GetKeyStatus("Key 6"))
                                KeyboardDevice.SetKeyStatus("Key 6", false);
                            break;
                        case "P2 Down":
                            if (currState)
                                KeyboardDevice.SetKeyStatus("Key 5", true);
                            else if (!KeyboardDevice.GetKeyStatus("Key 5"))
                                KeyboardDevice.SetKeyStatus("Key 5", false);
                            break;
                        case "P2 Left":
                            if (currState)
                                KeyboardDevice.SetKeyStatus("Key R", true);
                            else if (!KeyboardDevice.GetKeyStatus("Key R"))
                                KeyboardDevice.SetKeyStatus("Key R", false);
                            break;
                        case "P2 Right":
                            if (currState)
                                KeyboardDevice.SetKeyStatus("Key T", true);
                            else if (!KeyboardDevice.GetKeyStatus("Key T"))
                                KeyboardDevice.SetKeyStatus("Key T", false);
                            break;
                        case "P2 Fire":
                            if (currState)
                                KeyboardDevice.SetKeyStatus("Key G", true);
                            else if (!KeyboardDevice.GetKeyStatus("Key G"))
                                KeyboardDevice.SetKeyStatus("Key G", false);
                            break;
                    }
                }
            }

            // Tape control
            if (CPC._controller.IsPressed(Play))
            {
                if (!pressed_Play)
                {
                    CPC.OSD_FireInputMessage(Play);
                    TapeDevice.Play();
                    pressed_Play = true;
                }
            }
            else
                pressed_Play = false;

            if (CPC._controller.IsPressed(Stop))
            {
                if (!pressed_Stop)
                {
                    CPC.OSD_FireInputMessage(Stop);
                    TapeDevice.Stop();
                    pressed_Stop = true;
                }
            }
            else
                pressed_Stop = false;

            if (CPC._controller.IsPressed(RTZ))
            {
                if (!pressed_RTZ)
                {
                    CPC.OSD_FireInputMessage(RTZ);
                    TapeDevice.RTZ();
                    pressed_RTZ = true;
                }
            }
            else
                pressed_RTZ = false;

            if (CPC._controller.IsPressed(Record))
            {

            }
            if (CPC._controller.IsPressed(NextTape))
            {
                if (!pressed_NextTape)
                {
                    CPC.OSD_FireInputMessage(NextTape);
                    TapeMediaIndex++;
                    pressed_NextTape = true;
                }
            }
            else
                pressed_NextTape = false;

            if (CPC._controller.IsPressed(PrevTape))
            {
                if (!pressed_PrevTape)
                {
                    CPC.OSD_FireInputMessage(PrevTape);
                    TapeMediaIndex--;
                    pressed_PrevTape = true;
                }
            }
            else
                pressed_PrevTape = false;

            if (CPC._controller.IsPressed(NextBlock))
            {
                if (!pressed_NextBlock)
                {
                    CPC.OSD_FireInputMessage(NextBlock);
                    TapeDevice.SkipBlock(true);
                    pressed_NextBlock = true;
                }
            }
            else
                pressed_NextBlock = false;

            if (CPC._controller.IsPressed(PrevBlock))
            {
                if (!pressed_PrevBlock)
                {
                    CPC.OSD_FireInputMessage(PrevBlock);
                    TapeDevice.SkipBlock(false);
                    pressed_PrevBlock = true;
                }
            }
            else
                pressed_PrevBlock = false;

            if (CPC._controller.IsPressed(TapeStatus))
            {
                if (!pressed_TapeStatus)
                {
                    //Spectrum.OSD_FireInputMessage(TapeStatus);
                    CPC.OSD_ShowTapeStatus();
                    pressed_TapeStatus = true;
                }
            }
            else
                pressed_TapeStatus = false;

            if (CPC._controller.IsPressed(HardResetStr))
            {
                if (!pressed_HardReset)
                {
                    HardReset();
                    pressed_HardReset = true;
                }
            }
            else
                pressed_HardReset = false;

            if (CPC._controller.IsPressed(SoftResetStr))
            {
                if (!pressed_SoftReset)
                {
                    SoftReset();
                    pressed_SoftReset = true;
                }
            }
            else
                pressed_SoftReset = false;

            // disk control
            if (CPC._controller.IsPressed(NextDisk))
            {
                if (!pressed_NextDisk)
                {
                    CPC.OSD_FireInputMessage(NextDisk);
                    DiskMediaIndex++;
                    pressed_NextDisk = true;
                }
            }
            else
                pressed_NextDisk = false;

            if (CPC._controller.IsPressed(PrevDisk))
            {
                if (!pressed_PrevDisk)
                {
                    CPC.OSD_FireInputMessage(PrevDisk);
                    DiskMediaIndex--;
                    pressed_PrevDisk = true;
                }
            }
            else
                pressed_PrevDisk = false;

            if (CPC._controller.IsPressed(EjectDisk))
            {
                if (!pressed_EjectDisk)
                {
                    CPC.OSD_FireInputMessage(EjectDisk);
                    //if (UPDDiskDevice != null)
                      //  UPDDiskDevice.FDD_EjectDisk();
                }
            }
            else
                pressed_EjectDisk = false;

            if (CPC._controller.IsPressed(DiskStatus))
            {
                if (!pressed_DiskStatus)
                {
                    //Spectrum.OSD_FireInputMessage(TapeStatus);
                    CPC.OSD_ShowDiskStatus();
                    pressed_DiskStatus = true;
                }
            }
            else
                pressed_DiskStatus = false;
        }

        /// <summary>
        /// Signs whether input read has been requested
        /// This forms part of the IEmulator LagFrame implementation
        /// </summary>
        private bool inputRead;
        public bool InputRead
        {
            get { return inputRead; }
            set { inputRead = value; }
        }
    }
}
