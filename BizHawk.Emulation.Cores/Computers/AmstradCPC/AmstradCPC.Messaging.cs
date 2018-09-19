using System;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * Handles all messaging (OSD) operations *
    /// </summary>
    public partial class AmstradCPC
    {
        /// <summary>
        /// Writes a message to the OSD
        /// </summary>
        /// <param name="message"></param>
        /// <param name="category"></param>
        public void SendMessage(string message, MessageCategory category)
        {
            if (!CheckMessageSettings(category))
                return;

            StringBuilder sb = new StringBuilder();

            switch (category)
            {
                case MessageCategory.Tape:
                    sb.Append("DATACORDER: ");
                    sb.Append(message);
                    break;
                case MessageCategory.Input:
                    sb.Append("INPUT DETECTED: ");
                    sb.Append(message);
                    break;
                case MessageCategory.Disk:
                    sb.Append("DISK DRIVE: ");
                    sb.Append(message);
                    break;
                case MessageCategory.Emulator:
                case MessageCategory.Misc:
                    sb.Append("CPCHAWK: ");
                    sb.Append(message);
                    break;
            }

            CoreComm.Notify(sb.ToString());
        }

        #region Input Message Methods

        /// <summary>
        /// Called when certain input presses are detected
        /// </summary>
        /// <param name="input"></param>
        public void OSD_FireInputMessage(string input)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(input);
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Input);
        }

        #endregion

        #region DiskDevice Message Methods

        /// <summary>
        /// Disk message that is fired on core init
        /// </summary>
        public void OSD_DiskInit()
        {
            StringBuilder sb = new StringBuilder();
            if (_machine.diskImages != null && _machine.UPDDiskDevice != null)
            {
                sb.Append("Disk Media Imported (count: " + _machine.diskImages.Count() + ")");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Emulator);
            }
        }

        /// <summary>
        /// Disk message that is fired when a new disk is inserted into the drive
        /// </summary>
        public void OSD_DiskInserted()
        {
            StringBuilder sb = new StringBuilder();

            if (_machine.UPDDiskDevice == null)
            {
                sb.Append("No Drive Present");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                return;
            }

            sb.Append("DISK INSERTED (" + _machine.DiskMediaIndex + ": " + _diskInfo[_machine.DiskMediaIndex].Name + ")");
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
        }

        /// <summary>
        /// Tape message that prints the current status of the tape device
        /// </summary>
        public void OSD_ShowDiskStatus()
        {
            StringBuilder sb = new StringBuilder();

            if (_machine.UPDDiskDevice == null)
            {
                sb.Append("No Drive Present");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                return;
            }

            if (_diskInfo.Count == 0)
            {
                sb.Append("No Disk Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                return;
            }

            if (_machine.UPDDiskDevice != null)
            {
                if (_machine.UPDDiskDevice.DiskPointer == null)
                {
                    sb.Append("No Disk Loaded");
                    SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                    return;
                }

                sb.Append("Disk: " + _machine.DiskMediaIndex + ": " + _diskInfo[_machine.DiskMediaIndex].Name);
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                sb.Clear();
                /*
                string protection = "None";
                protection = Enum.GetName(typeof(ProtectionType), _machine.UPDDiskDevice.DiskPointer.Protection);
                if (protection == "None")
                    protection += " (OR UNKNOWN)";

                sb.Append("Detected Protection: " + protection);
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                sb.Clear();
                */

                sb.Append("Status: ");

                if (_machine.UPDDiskDevice.DriveLight)
                    sb.Append("READING/WRITING DATA");
                else
                    sb.Append("UNKNOWN");

                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Disk);
                sb.Clear();
            }
        }

        #endregion

        #region TapeDevice Message Methods

        /// <summary>
        /// Tape message that is fired on core init
        /// </summary>
        public void OSD_TapeInit()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("Tape Media Imported (count: " + _tapeInfo.Count() + ")");
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Emulator);
        }

        /// <summary>
        /// Tape message that is fired when tape is playing
        /// </summary>
        public void OSD_TapeMotorActive()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("MOTOR ON (" + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when tape is playing
        /// </summary>
        public void OSD_TapeMotorInactive()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("MOTOR OFF (" + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when tape is playing
        /// </summary>
        public void OSD_TapePlaying()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("PLAYING MANUAL (" + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when tape is stopped
        /// </summary>
        public void OSD_TapeStopped()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("STOPPED MANUAL (" + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when tape is rewound
        /// </summary>
        public void OSD_TapeRTZ()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("REWOUND (" + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a new tape is inserted into the datacorder
        /// </summary>
        public void OSD_TapeInserted()
        {
            if (_tapeInfo.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append("TAPE INSERTED (" + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }


        /// <summary>
        /// Tape message that is fired when a tape is stopped automatically
        /// </summary>
        public void OSD_TapeStoppedAuto()
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("STOPPED (Auto Tape Trap Detected)");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a tape is started automatically
        /// </summary>
        public void OSD_TapePlayingAuto()
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("PLAYING (Auto Tape Trap Detected)");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a new block starts playing
        /// </summary>
        public void OSD_TapePlayingBlockInfo(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("...Starting Block " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a tape block is skipped (because it is empty)
        /// </summary>
        public void OSD_TapePlayingSkipBlockInfo(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("...Skipping Empty Block " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a tape is started automatically
        /// </summary>
        public void OSD_TapeEndDetected(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("...Skipping Empty Block " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when user has manually skipped to the next block
        /// </summary>
        public void OSD_TapeNextBlock(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("Manual Skip Next " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when user has manually skipped to the next block
        /// </summary>
        public void OSD_TapePrevBlock(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }


            sb.Append("Manual Skip Prev " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that prints the current status of the tape device
        /// </summary>
        public void OSD_ShowTapeStatus()
        {
            StringBuilder sb = new StringBuilder();

            if (_tapeInfo.Count == 0)
            {
                sb.Append("No Tape Loaded");
                SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
                return;
            }

            sb.Append("Status: ");

            if (_machine.TapeDevice.TapeIsPlaying)
                sb.Append("PLAYING");
            else
                sb.Append("STOPPED");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
            sb.Clear();

            sb.Append("Tape: " + _machine.TapeMediaIndex + ": " + _tapeInfo[_machine.TapeMediaIndex].Name);
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
            sb.Clear();

            sb.Append("Block: ");
            sb.Append("(" + (_machine.TapeDevice.CurrentDataBlockIndex + 1) +
                " of " + _machine.TapeDevice.DataBlocks.Count() + ") " +
                _machine.TapeDevice.DataBlocks[_machine.TapeDevice.CurrentDataBlockIndex].BlockDescription);
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
            sb.Clear();

            sb.Append("Block Pos: ");

            int pos = _machine.TapeDevice.Position;
            int end = _machine.TapeDevice.DataBlocks[_machine.TapeDevice.CurrentDataBlockIndex].DataPeriods.Count;
            double p = 0;
            if (end != 0)
                p = ((double)pos / (double)end) * (double)100;

            sb.Append(p.ToString("N0") + "%");
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
            sb.Clear();

            // get position within the tape itself
            sb.Append("Tape Pos: ");
            var ind = _machine.TapeDevice.CurrentDataBlockIndex;
            int cnt = 0;
            for (int i = 0; i < ind; i++)
            {
                cnt += _machine.TapeDevice.DataBlocks[i].DataPeriods.Count;
            }
            // now we are at our current block
            int ourPos = cnt + pos;
            cnt += end;
            // count periods in the remaining blocks
            for (int i = ind + 1; i < _machine.TapeDevice.DataBlocks.Count; i++)
            {
                cnt += _machine.TapeDevice.DataBlocks[i].DataPeriods.Count;
            }
            // work out overall position within the tape
            p = 0;
            p = ((double)ourPos / (double)cnt) * (double)100;
            sb.Append(p.ToString("N0") + "%");
            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        #endregion

        /// <summary>
        /// Checks whether message category is allowed to be sent
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public bool CheckMessageSettings(MessageCategory category)
        {
            switch (Settings.OSDMessageVerbosity)
            {
                case OSDVerbosity.Full:
                    return true;
                case OSDVerbosity.None:
                    return false;
                case OSDVerbosity.Medium:
                    switch (category)
                    {
                        case MessageCategory.Disk:
                        case MessageCategory.Emulator:
                        case MessageCategory.Tape:
                        case MessageCategory.Misc:
                            return true;
                        default:
                            return false;
                    }
                default:
                    return true;
            }
        }

        /// <summary>
        /// Defines the different message categories
        /// </summary>
        public enum MessageCategory
        {
            /// <summary>
            /// No defined category as such
            /// </summary>
            Misc,
            /// <summary>
            /// User generated input messages (at the moment only tape/disk controls)
            /// </summary>
            Input,
            /// <summary>
            /// Tape device generated messages
            /// </summary>
            Tape,
            /// <summary>
            /// Disk device generated messages
            /// </summary>
            Disk,
            /// <summary>
            /// Emulator generated messages
            /// </summary>
            Emulator
        }
    }
}
