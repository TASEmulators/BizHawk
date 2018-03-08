using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Handles all messaging (OSD) operations
    /// </summary>
    public partial class ZXSpectrum
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
                    sb.Append("ZXHAWK: ");
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

        #region TapeDevice Message Methods

        /// <summary>
        /// Tape message that is fired on core init
        /// </summary>
        public void OSD_TapeInit()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Tape Media Imported (count: " + _gameInfo.Count() + ")");
            sb.Append("\n");
            for (int i = 0; i < _gameInfo.Count(); i++)
                sb.Append(i.ToString() + ": " + _gameInfo[i].Name + "\n");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Emulator);
        }

        /// <summary>
        /// Tape message that is fired when tape is playing
        /// </summary>
        public void OSD_TapePlaying()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PLAYING (" + _machine.TapeMediaIndex + ": " + _gameInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when tape is stopped
        /// </summary>
        public void OSD_TapeStopped()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("STOPPED (" + _machine.TapeMediaIndex + ": " + _gameInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when tape is rewound
        /// </summary>
        public void OSD_TapeRTZ()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("REWOUND (" + _machine.TapeMediaIndex + ": " + _gameInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a new tape is inserted into the datacorder
        /// </summary>
        public void OSD_TapeInserted()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TAPE INSERTED (" + _machine.TapeMediaIndex + ": " + _gameInfo[_machine.TapeMediaIndex].Name + ")");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }


        /// <summary>
        /// Tape message that is fired when a tape is stopped automatically
        /// </summary>
        public void OSD_TapeStoppedAuto()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("STOPPED (Auto Tape Trap Detected)");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a tape is started automatically
        /// </summary>
        public void OSD_TapePlayingAuto()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PLAYING (Auto Tape Trap Detected)");

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a new block starts playing
        /// </summary>
        public void OSD_TapePlayingBlockInfo(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("...Starting Block "+ blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a tape block is skipped (because it is empty)
        /// </summary>
        public void OSD_TapePlayingSkipBlockInfo(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("...Skipping Empty Block " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when a tape is started automatically
        /// </summary>
        public void OSD_TapeEndDetected(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("...Skipping Empty Block " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when user has manually skipped to the next block
        /// </summary>
        public void OSD_TapeNextBlock(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Manual Skip Next " + blockinfo);

            SendMessage(sb.ToString().TrimEnd('\n'), MessageCategory.Tape);
        }

        /// <summary>
        /// Tape message that is fired when user has manually skipped to the next block
        /// </summary>
        public void OSD_TapePrevBlock(string blockinfo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Manual Skip Prev " + blockinfo);

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
