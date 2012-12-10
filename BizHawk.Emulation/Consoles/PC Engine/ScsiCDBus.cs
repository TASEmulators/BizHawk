using System;
using BizHawk.DiscSystem;
using BizHawk.Emulation.Sound;
using System.IO;
using System.Globalization;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    // TODO we can adjust this to have Think take the number of cycles and not require
    // a reference to Cpu.TotalExecutedCycles
    // which incidentally would allow us to put it back to an int from a long if we wanted to

    public sealed class ScsiCDBus
    {
        const int STATUS_GOOD               = 0;
        const int STATUS_CHECK_CONDITION    = 1;
        const int STATUS_CONDITION_MET      = 2;
        const int STATUS_BUSY               = 4;
        const int STATUS_INTERMEDIATE       = 8;

        const int SCSI_TEST_UNIT_READY      = 0x00;
        const int SCSI_REQUEST_SENSE        = 0x03;
        const int SCSI_READ                 = 0x08;
        const int SCSI_AUDIO_START_POS      = 0xD8;
        const int SCSI_AUDIO_END_POS        = 0xD9;
        const int SCSI_PAUSE                = 0xDA;
        const int SCSI_READ_SUBCODE_Q       = 0xDD;
        const int SCSI_READ_TOC             = 0xDE;

        bool bsy, sel, cd, io, msg, req, ack, atn, rst;
        bool signalsChanged;

        public bool BSY 
        {
            get { return bsy; } 
            set { 
                if (value != BSY) signalsChanged = true;
                bsy = value;
            } 
        }
        public bool SEL 
        {
            get { return sel; }
            set
            {
                if (value != SEL) signalsChanged = true;
                sel = value;
            }
        }
        public bool CD // CONTROL = true, DATA = false
        {
            get { return cd; }
            set
            {
                if (value != CD) signalsChanged = true;
                cd = value;
            }
        }
        public bool IO // INPUT = true, OUTPUT = false
        {
            get { return io; }
            set
            {
                if (value != IO) signalsChanged = true;
                io = value;
            }
        } 
        public bool MSG
        {
            get { return msg; }
            set
            {
                if (value != MSG) signalsChanged = true;
                msg = value;
            }
        }
        public bool REQ
        {
            get { return req; }
            set
            {
                if (value != REQ) signalsChanged = true;
                req = value;
            }
        }
        public bool ACK
        {
            get { return ack; }
            set
            {
                if (value != ACK) signalsChanged = true;
                ack = value;
            }
        }
        public bool ATN
        {
            get { return atn; }
            set
            {
                if (value != ATN) signalsChanged = true;
                atn = value;
            }
        }
        public bool RST
        {
            get { return rst; }
            set
            {
                if (value != RST) signalsChanged = true;
                rst = value;
            }
        }
        public byte DataBits  { get; set; } // data bits

        enum BusPhase
        {
            BusFree,
            Command,
            DataIn,
            DataOut,
            MessageIn,
            MessageOut,
            Status
        }

        bool busPhaseChanged;
        BusPhase Phase = BusPhase.BusFree;

        bool MessageCompleted;
        bool StatusCompleted;
        byte MessageValue;

        QuickList<byte> CommandBuffer = new QuickList<byte>(10); // 10 = biggest command
        public QuickQueue<byte> DataIn = new QuickQueue<byte>(2048); // one data sector

        // ******** Data Transfer / READ command support ********
        
        public long DataReadWaitTimer;
        public bool DataReadInProgress;
        public bool DataTransferWasDone;
        public bool DataTransferInProgress;
        public int CurrentReadingSector;
        public int SectorsLeftToRead;

        // ******** Resources ********

        PCEngine pce;
        public Disc disc;
        int audioStartLBA;
        int audioEndLBA;

        public ScsiCDBus(PCEngine pce, Disc disc)
        {
            this.pce = pce;
            this.disc = disc;
        }

        public void Think()
        {
            if (RST)
            {
                ResetDevice();
                return;
            }
            
            if (DataReadInProgress && pce.Cpu.TotalExecutedCycles > DataReadWaitTimer)
            {
                if (SectorsLeftToRead > 0)
                    pce.CoreComm.DriveLED = true;

                if (DataIn.Count == 0)
                {
                    // read in a sector and shove it in the queue
                    disc.ReadLBA_2048(CurrentReadingSector, DataIn.GetBuffer(), 0);
                    DataIn.SignalBufferFilled(2048);
                    CurrentReadingSector++;
                    SectorsLeftToRead--;

                    pce.IntDataTransferReady = true;

                    // If more sectors, should set the next think-clock to however long it takes to read 1 sector
                    // but I dont. I dont think transfers actually happen sector by sector
                    // like this, they probably become available as the bits come off the disc.
                    // but lets get some basic functionality before we go crazy.
                    //  Idunno, maybe they do come in a sector at a time.

					//note to vecna: maybe not at the sector level, but at a level > 1 sample and <= 1 sector, samples come out in blocks
					//due to the way they are jumbled up (seriously, like put into a blender) for error correction purposes. 
					//we may as well assume that the cd audio decoding magic works at the level of one sector, but it isnt one sample.

                    if (SectorsLeftToRead == 0)
                    {
                        DataReadInProgress = false;
                        DataTransferWasDone = true;
                    }
                    SetPhase(BusPhase.DataIn);
                }
            }

            do
            {
                signalsChanged = false;
                busPhaseChanged = false;

                if (SEL && !BSY)
                {
                    SetPhase(BusPhase.Command);
                } 
                else if (ATN && !REQ  && !ACK)
                {
                    SetPhase(BusPhase.MessageOut);
                }
                else switch (Phase)
                { 
                    case BusPhase.Command:    ThinkCommandPhase(); break;
                    case BusPhase.DataIn:     ThinkDataInPhase(); break;
                    case BusPhase.DataOut:    ThinkDataOutPhase(); break;
                    case BusPhase.MessageIn:  ThinkMessageInPhase(); break;
                    case BusPhase.MessageOut: ThinkMessageOutPhase(); break;
                    case BusPhase.Status:     ThinkStatusPhase(); break;
                    default: break;
                }
            } while (signalsChanged || busPhaseChanged);
        }

        void ResetDevice()
        {
            CD  = false;
            IO  = false;
            MSG = false;
            REQ = false;
            ACK = false;
            ATN = false;
            DataBits  = 0;
            Phase = BusPhase.BusFree;

            CommandBuffer.Clear();
            DataIn.Clear();
            DataReadInProgress = false;
            pce.CDAudio.Stop();
        }

        void ThinkCommandPhase()
        {
            if (REQ && ACK)
            {
                CommandBuffer.Add(DataBits);
                REQ = false;
            }

            if (!REQ && !ACK && CommandBuffer.Count > 0)
            {
                bool complete = CheckCommandBuffer();

                if (complete)
                {
                    CommandBuffer.Clear();
                }
                else
                {
                    REQ = true; // needs more data!
                }
            }
        }

        void ThinkDataInPhase()
        {
            if (REQ && ACK)
            {
                REQ = false;
            }
            else if (!REQ && !ACK)
            {
                if (DataIn.Count > 0)
                {
                    DataBits = DataIn.Dequeue();
                    REQ = true;
                } else { 
                    // data transfer is finished
                    
                    pce.IntDataTransferReady = false;
                    if (DataTransferWasDone)
                    {
                        DataTransferInProgress = false;
                        DataTransferWasDone = false;
                        pce.IntDataTransferComplete = true;
                    }
                    SetStatusMessage(STATUS_GOOD, 0);
                }
            }
        }

        void ThinkDataOutPhase()
        {
            Console.WriteLine("*********** DATA OUT PHASE, DOES THIS HAPPEN? ****************");
            SetPhase(BusPhase.BusFree);
        }

        void ThinkMessageInPhase()
        {
            if (REQ && ACK)
            {
                REQ = false;
                MessageCompleted = true;
            }

            if (!REQ && !ACK && MessageCompleted)
            {
                MessageCompleted = false;
                SetPhase(BusPhase.BusFree);
            }
        }

        void ThinkMessageOutPhase()
        {
            Console.WriteLine("******* IN MESSAGE OUT PHASE. DOES THIS EVER HAPPEN? ********");
            SetPhase(BusPhase.BusFree);
        }
        
        void ThinkStatusPhase()
        {
            if (REQ && ACK)
            {
                REQ = false;
                StatusCompleted = true;
            }
            if (!REQ && !ACK && StatusCompleted)
            {
                StatusCompleted = false;
                DataBits = MessageValue;
                SetPhase(BusPhase.MessageIn);
            }
        }

        // returns true if command completed, false if more data bytes needed
        bool CheckCommandBuffer()
        {
            switch (CommandBuffer[0])
            {
                case SCSI_TEST_UNIT_READY:
                    if (CommandBuffer.Count < 6) return false;
                    SetStatusMessage(STATUS_GOOD, 0);
                    return true;

                case SCSI_READ:
                    if (CommandBuffer.Count < 6) return false;
                    CommandRead();
                    return true;

                case SCSI_AUDIO_START_POS:
                    if (CommandBuffer.Count < 10) return false;
                    CommandAudioStartPos();
                    return true;

                case SCSI_AUDIO_END_POS:
                    if (CommandBuffer.Count < 10) return false;
                    CommandAudioEndPos();
                    return true;

                case SCSI_PAUSE:
                    if (CommandBuffer.Count < 10) return false;
                    CommandPause();
                    return true;

                case SCSI_READ_SUBCODE_Q:
                    if (CommandBuffer.Count < 10) return false;
                    CommandReadSubcodeQ();
                    return true;

                case SCSI_READ_TOC:
                    if (CommandBuffer.Count < 10) return false;
                    CommandReadTOC();
                    return true;

                default:
                    Console.WriteLine("UNRECOGNIZED SCSI COMMAND! {0:X2}", CommandBuffer[0]);
                    SetStatusMessage(STATUS_GOOD, 0);
                    break;
            }
            return false;
        }

        void CommandRead()
        {   
            int sector = (CommandBuffer[1] & 0x1f) << 16;
            sector |= CommandBuffer[2] << 8;
            sector |= CommandBuffer[3];

            DataReadInProgress = true;
            DataTransferInProgress = true;
            CurrentReadingSector = sector;
            SectorsLeftToRead = CommandBuffer[4];

            if (CommandBuffer[4] == 0)
                SectorsLeftToRead = 256;

            DataReadWaitTimer = pce.Cpu.TotalExecutedCycles + 5000; // figure out proper read delay later
            pce.CDAudio.Stop();
        }

        void CommandAudioStartPos()
        {
            switch (CommandBuffer[9] & 0xC0)
            {
                case 0x00: // Set start offset in LBA units
                    audioStartLBA = (CommandBuffer[3] << 16) | (CommandBuffer[4] << 8) | CommandBuffer[5];
                    break;

                case 0x40: // Set start offset in MSF units
                    byte m = CommandBuffer[2].BCDtoBin();
                    byte s = CommandBuffer[3].BCDtoBin();
                    byte f = CommandBuffer[4].BCDtoBin();
                    audioStartLBA = Disc.ConvertMSFtoLBA(m, s, f);
                    break;

                case 0x80: // Set start offset in track units
                    byte trackNo = CommandBuffer[2].BCDtoBin();
                    audioStartLBA = disc.TOC.Sessions[0].Tracks[trackNo - 1].Indexes[1].aba - 150;
                    break;
            }

            if (CommandBuffer[1] == 0)
            {
                pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                pce.CDAudio.Pause();
            } else {
                pce.CDAudio.PlayStartingAtLba(audioStartLBA);
            }

            SetStatusMessage(STATUS_GOOD, 0);
            pce.IntDataTransferComplete = true;
        }

        void CommandAudioEndPos()
        {
            switch (CommandBuffer[9] & 0xC0)
            {
                case 0x00: // Set end offset in LBA units
                    audioEndLBA = (CommandBuffer[3] << 16) | (CommandBuffer[4] << 8) | CommandBuffer[5];
                    break;

                case 0x40: // Set end offset in MSF units
                    byte m = CommandBuffer[2].BCDtoBin();
                    byte s = CommandBuffer[3].BCDtoBin();
                    byte f = CommandBuffer[4].BCDtoBin();
                    audioEndLBA = Disc.ConvertMSFtoLBA(m, s, f);
                    break;

                case 0x80: // Set end offset in track units
                    byte trackNo = CommandBuffer[2].BCDtoBin();
                    if (trackNo - 1 >= disc.TOC.Sessions[0].Tracks.Count)
                        audioEndLBA = disc.LBACount;
                    else 
                        audioEndLBA = disc.TOC.Sessions[0].Tracks[trackNo - 1].Indexes[1].aba - 150;
                    break;
            }

            switch (CommandBuffer[1])
            {
                case 0: // end immediately
                    pce.CDAudio.Stop(); 
                    break;
                case 1: // play in loop mode. I guess this constitues A-B looping
                    pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                    pce.CDAudio.EndLBA = audioEndLBA;
                    pce.CDAudio.PlayMode = CDAudio.PlaybackMode.LoopOnCompletion;
                    break;
                case 2: // Play audio, fire IRQ2 when end position reached, maybe
                    pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                    pce.CDAudio.EndLBA = audioEndLBA;
                    pce.CDAudio.PlayMode = CDAudio.PlaybackMode.CallbackOnCompletion;
                    break;
                case 3: // Play normal
                    pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                    pce.CDAudio.EndLBA = audioEndLBA;
                    pce.CDAudio.PlayMode = CDAudio.PlaybackMode.StopOnCompletion;
                    break;
            }
            SetStatusMessage(STATUS_GOOD, 0);
        }
        
        void CommandPause()
        {
            pce.CDAudio.Stop();
            SetStatusMessage(STATUS_GOOD, 0);
        }

        void CommandReadSubcodeQ()
        {
            bool playing = pce.CDAudio.Mode != CDAudio.CDAudioMode.Stopped;
            var sectorEntry = disc.ReadLBA_SectorEntry(playing ? pce.CDAudio.CurrentSector : CurrentReadingSector);

            DataIn.Clear();

            switch (pce.CDAudio.Mode)
            {
                case CDAudio.CDAudioMode.Playing: DataIn.Enqueue(0); break;
                case CDAudio.CDAudioMode.Paused:  DataIn.Enqueue(2); break;
                case CDAudio.CDAudioMode.Stopped: DataIn.Enqueue(3); break;
            }
            
			DataIn.Enqueue(sectorEntry.q_status);          // I do not know what status is
			DataIn.Enqueue(sectorEntry.q_tno.BCDValue);    // track
			DataIn.Enqueue(sectorEntry.q_index.BCDValue);  // index
			DataIn.Enqueue(sectorEntry.q_min.BCDValue);    // M(rel)
			DataIn.Enqueue(sectorEntry.q_sec.BCDValue);    // S(rel)
			DataIn.Enqueue(sectorEntry.q_frame.BCDValue);  // F(rel)
            DataIn.Enqueue(sectorEntry.q_amin.BCDValue);   // M(abs)
			DataIn.Enqueue(sectorEntry.q_asec.BCDValue);   // S(abs)
            DataIn.Enqueue(sectorEntry.q_aframe.BCDValue); // F(abs)

            SetPhase(BusPhase.DataIn);
        }

        void CommandReadTOC()
        {
            switch (CommandBuffer[1])
            {
                case 0: // return number of tracks
                    {
                        DataIn.Clear();
                        DataIn.Enqueue(0x01);
                        DataIn.Enqueue(((byte) disc.TOC.Sessions[0].Tracks.Count).BinToBCD());
                        SetPhase(BusPhase.DataIn);
                        break;
                    }
                case 1: // return total disc length in minutes/seconds/frames
                    {
                        int totalLbaLength = disc.LBACount;

                        byte m, s, f;
                        Disc.ConvertLBAtoMSF(totalLbaLength, out m, out s, out f);

                        DataIn.Clear();
                        DataIn.Enqueue(m.BinToBCD());
                        DataIn.Enqueue(s.BinToBCD());
                        DataIn.Enqueue(f.BinToBCD());
                        SetPhase(BusPhase.DataIn);
                        break;
                    }
                case 2: // Return starting position of specified track in MSF format
                    {
                        int track = CommandBuffer[2].BCDtoBin();
                        var tracks = disc.TOC.Sessions[0].Tracks;
                        if (CommandBuffer[2] > 0x99)
                            throw new Exception("invalid track number BCD request... is something I need to handle?");
                        if (track == 0) track = 1;
                        track--;

                        
                        int lbaPos;
                        
                        if (track > tracks.Count)
                            lbaPos = disc.TOC.Sessions[0].length_aba - 150;
                        else
                            lbaPos = tracks[track].Indexes[1].aba - 150;
                        
                        byte m, s, f;
                        Disc.ConvertLBAtoMSF(lbaPos, out m, out s, out f);
                        
                        DataIn.Clear();
                        DataIn.Enqueue(m.BinToBCD());
                        DataIn.Enqueue(s.BinToBCD());
                        DataIn.Enqueue(f.BinToBCD());

                        if (track > tracks.Count || disc.TOC.Sessions[0].Tracks[track].TrackType == ETrackType.Audio)
                            DataIn.Enqueue(0);
                        else
                            DataIn.Enqueue(4);
                        SetPhase(BusPhase.DataIn);
                        break;
                    }
                default:
                    Console.WriteLine("unimplemented READ TOC command argument!");
                    break;
            }
        }

        void SetStatusMessage(byte status, byte message)
        {
            MessageValue = message;
            StatusCompleted = false;
            MessageCompleted = false;
            DataBits = status == STATUS_GOOD ? (byte) 0x00 : (byte) 0x01;
            SetPhase(BusPhase.Status);
        }

        void SetPhase(BusPhase phase)
        {
            if (Phase == phase)
                return;

            Phase = phase;
            busPhaseChanged = true;

            switch (phase)
            {
                case BusPhase.BusFree:
                    BSY = false;
                    MSG = false;
                    CD  = false;
                    IO  = false;
                    REQ = false;
                    pce.IntDataTransferComplete = false;
                    break;
                case BusPhase.Command:
                    BSY = true;
                    MSG = false;
                    CD  = true;
                    IO  = false;
                    REQ = true;
                    break;
                case BusPhase.DataIn:
                    BSY = true;
                    MSG = false;
                    CD  = false;
                    IO  = true;
                    REQ = false;
                    break;
                case BusPhase.DataOut:
                    BSY = true;
                    MSG = false;
                    CD  = false;
                    IO  = false;
                    REQ = true;
                    break;
                case BusPhase.MessageIn:
                    BSY = true;
                    MSG = true;
                    CD  = true;
                    IO  = true;
                    REQ = true;
                    break;
                case BusPhase.MessageOut:
                    BSY = true;
                    MSG = true;
                    CD  = true;
                    IO  = false;
                    REQ = true;
                    break;
                case BusPhase.Status:
                    BSY = true;
                    MSG = false;
                    CD  = true;
                    IO  = true;
                    REQ = true;
                    break;
            }
        }
        
        // ***************************************************************************

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(BSY);
            writer.Write(SEL);
            writer.Write(CD);
            writer.Write(IO);
            writer.Write(MSG);
            writer.Write(REQ);
            writer.Write(ACK);
            writer.Write(ATN);
            writer.Write(RST);
            writer.Write(DataBits);
            writer.Write((byte)Phase);

            writer.Write(MessageCompleted);
            writer.Write(StatusCompleted);
            writer.Write(MessageValue);

            writer.Write(DataReadWaitTimer);
            writer.Write(DataReadInProgress);
            writer.Write(DataTransferWasDone);
            writer.Write(DataTransferInProgress);
            writer.Write(CurrentReadingSector);
            writer.Write((byte)SectorsLeftToRead);

            writer.Write(CommandBuffer.buffer);
            writer.Write((byte)CommandBuffer.Position);

            writer.Write(DataIn.buffer);
            writer.Write((short)DataIn.head);
            writer.Write((short)DataIn.tail);
            writer.Write((short)DataIn.size);

            writer.Write(audioStartLBA);
            writer.Write(audioEndLBA);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            BSY = reader.ReadBoolean();
            SEL = reader.ReadBoolean();
            CD = reader.ReadBoolean();
            IO = reader.ReadBoolean();
            MSG = reader.ReadBoolean();
            REQ = reader.ReadBoolean();
            ACK = reader.ReadBoolean();
            ATN = reader.ReadBoolean();
            RST = reader.ReadBoolean();
            DataBits = reader.ReadByte();
            Phase = (BusPhase) Enum.ToObject(typeof(BusPhase), reader.ReadByte());

            MessageCompleted = reader.ReadBoolean();
            StatusCompleted = reader.ReadBoolean();
            MessageValue = reader.ReadByte();
            
            DataReadWaitTimer = reader.ReadInt64();
            DataReadInProgress = reader.ReadBoolean();
            DataTransferWasDone = reader.ReadBoolean();
            DataTransferInProgress = reader.ReadBoolean();
            CurrentReadingSector = reader.ReadInt32();
            SectorsLeftToRead = reader.ReadByte();

            CommandBuffer.buffer = reader.ReadBytes(10);
            CommandBuffer.Position = reader.ReadByte();

            DataIn.buffer = reader.ReadBytes(2048);
            DataIn.head = reader.ReadInt16();
            DataIn.tail = reader.ReadInt16();
            DataIn.size = reader.ReadInt16();

            audioStartLBA = reader.ReadInt32();
            audioEndLBA = reader.ReadInt32();
        }

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[SCSI]");
            writer.WriteLine("BSY {0}",BSY);
            writer.WriteLine("SEL {0}", SEL);
            writer.WriteLine("CD {0}", CD);
            writer.WriteLine("IO {0}", IO);
            writer.WriteLine("MSG {0}", MSG);
            writer.WriteLine("REQ {0}", REQ);
            writer.WriteLine("ACK {0}", ACK);
            writer.WriteLine("ATN {0}", ATN);
            writer.WriteLine("RST {0}", RST);
            writer.WriteLine("DataBits {0:X2}", DataBits);
            writer.WriteLine("BusPhase "+Enum.GetName(typeof(BusPhase), Phase));
            writer.WriteLine();
            writer.WriteLine("MessageCompleted {0}", MessageCompleted);
            writer.WriteLine("StatusCompleted {0}", StatusCompleted);
            writer.WriteLine("MessageValue {0}", MessageValue);
            writer.WriteLine();
            writer.WriteLine("DataReadWaitTimer {0}", DataReadWaitTimer);
            writer.WriteLine("DataReadInProgress {0}", DataReadInProgress);
            writer.WriteLine("DataTransferWasDone {0}", DataTransferWasDone);
            writer.WriteLine("DataTransferInProgress {0}", DataTransferInProgress);
            writer.WriteLine("CurrentReadingSector {0}", CurrentReadingSector);
            writer.WriteLine("SectorsLeftToRead {0}", SectorsLeftToRead);
            writer.WriteLine();
            writer.WriteLine("AudioStartLBA {0}", audioStartLBA);
            writer.WriteLine("AudioEndLBA {0}", audioEndLBA);
            writer.WriteLine();
            writer.Write("CommandBuffer "); CommandBuffer.buffer.SaveAsHex(writer);
            writer.WriteLine("CommandBufferPosition {0}", CommandBuffer.Position);
            writer.WriteLine();
            writer.Write("DataInBuffer "); DataIn.buffer.SaveAsHex(writer);
            writer.WriteLine("DataInHead {0}", DataIn.head);
            writer.WriteLine("DataInTail {0}", DataIn.tail);
            writer.WriteLine("DataInSize {0}", DataIn.size);
            writer.WriteLine("[/SCSI]\n");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/SCSI]") break;
                if (args[0] == "BSY")
                    BSY = bool.Parse(args[1]);
                else if (args[0] == "SEL")
                    SEL = bool.Parse(args[1]);
                else if (args[0] == "CD")
                    CD = bool.Parse(args[1]);
                else if (args[0] == "IO")
                    IO = bool.Parse(args[1]);
                else if (args[0] == "MSG")
                    MSG = bool.Parse(args[1]);
                else if (args[0] == "REQ")
                    REQ = bool.Parse(args[1]);
                else if (args[0] == "ACK")
                    ACK = bool.Parse(args[1]);
                else if (args[0] == "ATN")
                    ATN = bool.Parse(args[1]);
                else if (args[0] == "RST")
                    RST = bool.Parse(args[1]);
                else if (args[0] == "DataBits")
                    DataBits = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "BusPhase")
                    Phase = (BusPhase)Enum.Parse(typeof(BusPhase), args[1]);

                else if (args[0] == "MessageCompleted")
                    MessageCompleted = bool.Parse(args[1]);
                else if (args[0] == "StatusCompleted")
                    StatusCompleted = bool.Parse(args[1]);
                else if (args[0] == "MessageValue")
                    MessageValue = byte.Parse(args[1]);

                else if (args[0] == "DataReadWaitTimer")
                    DataReadWaitTimer = long.Parse(args[1]);
                else if (args[0] == "DataReadInProgress")
                    DataReadInProgress = bool.Parse(args[1]);
                else if (args[0] == "DataTransferWasDone")
                    DataTransferWasDone = bool.Parse(args[1]);
                else if (args[0] == "DataTransferInProgress")
                    DataTransferInProgress = bool.Parse(args[1]);
                else if (args[0] == "CurrentReadingSector")
                    CurrentReadingSector = int.Parse(args[1]);
                else if (args[0] == "SectorsLeftToRead")
                    SectorsLeftToRead = int.Parse(args[1]);

                else if (args[0] == "AudioStartLBA")
                    audioStartLBA = int.Parse(args[1]);
                else if (args[0] == "AudioEndLBA")
                    audioEndLBA = int.Parse(args[1]);

                else if (args[0] == "CommandBuffer")
                    CommandBuffer.buffer.ReadFromHex(args[1]);
                else if (args[0] == "CommandBufferPosition")
                    CommandBuffer.Position = int.Parse(args[1]);

                else if (args[0] == "DataInBuffer")
                    DataIn.buffer.ReadFromHex(args[1]);
                else if (args[0] == "DataInHead")
                    DataIn.head = int.Parse(args[1]);
                else if (args[0] == "DataInTail")
                    DataIn.tail = int.Parse(args[1]);
                else if (args[0] == "DataInSize")
                    DataIn.size = int.Parse(args[1]);

                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }
    }
}