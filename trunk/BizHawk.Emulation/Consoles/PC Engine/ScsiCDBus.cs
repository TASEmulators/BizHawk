using System;
using BizHawk.DiscSystem;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
    public sealed class ScsiCDBus
    {
        private const int STATUS_GOOD               = 0;
        private const int STATUS_CHECK_CONDITION    = 1;
        private const int STATUS_CONDITION_MET      = 2;
        private const int STATUS_BUSY               = 4;
        private const int STATUS_INTERMEDIATE       = 8;

        private const int SCSI_TEST_UNIT_READY      = 0x00;
        private const int SCSI_REQUEST_SENSE        = 0x03;
        private const int SCSI_READ                 = 0x08;
        private const int SCSI_AUDIO_START_POS      = 0xD8;
        private const int SCSI_AUDIO_END_POS        = 0xD9;
        private const int SCSI_PAUSE                = 0xDA;
        private const int SCSI_READ_SUBCODE_Q       = 0xDD;
        private const int SCSI_READ_TOC             = 0xDE;

        private bool bsy, sel, cd, io, msg, req, ack, atn, rst;
        private bool signalsChanged;

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

        private enum BusPhase
        {
            BusFree,
            Command,
            DataIn,
            DataOut,
            MessageIn,
            MessageOut,
            Status
        }

        private bool busPhaseChanged;
        private BusPhase Phase = BusPhase.BusFree;

        private bool MessageCompleted;
        private bool StatusCompleted;
        private byte MessageValue;

        private QuickList<byte> CommandBuffer = new QuickList<byte>(10); // 10 = biggest command
        public QuickQueue<byte> DataIn = new QuickQueue<byte>(2048); // one data sector

        // ******** Data Transfer / READ command support ********
        
        public long DataReadWaitTimer;
        public bool DataReadInProgress;
        public bool DataTransferWasDone;
        public bool DataTransferInProgress;
        public int CurrentReadingSector;
        public int SectorsLeftToRead;

        // ******** Resources ********

        private PCEngine pce;
        public Disc disc;

        // ******** Events ********

        public Action<bool> DataTransferReady;
        public Action<bool> DataTransferComplete;

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
                if (DataIn.Count == 0)
                {
                    //Console.WriteLine("Sector available to read!!!");
                    // read in a sector and shove it in the queue
                    disc.ReadLBA_2048(CurrentReadingSector, DataIn.GetBuffer(), 0);
                    DataIn.SignalBufferFilled(2048);
                    CurrentReadingSector++;
                    SectorsLeftToRead--;

                    DataTransferReady(true);

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

        private void ResetDevice()
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

        private void ThinkCommandPhase()
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

        private void ThinkDataInPhase()
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
                    
                    DataTransferReady(false);
                    if (DataTransferWasDone)
                    {
                        Console.WriteLine("DATA TRANSFER FINISHED!");
                        DataTransferInProgress = false;
                        DataTransferWasDone = false;
                        DataTransferComplete(true);
                    }
                    SetStatusMessage(STATUS_GOOD, 0);
                }
            }
        }

        private void ThinkDataOutPhase()
        {
            Console.WriteLine("*********** DATA OUT PHASE, DOES THIS HAPPEN? ****************");
            SetPhase(BusPhase.BusFree);
        }

        private void ThinkMessageInPhase()
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

        private void ThinkMessageOutPhase()
        {
            Console.WriteLine("******* IN MESSAGE OUT PHASE. DOES THIS EVER HAPPEN? ********");
            SetPhase(BusPhase.BusFree);
        }
        
        private void ThinkStatusPhase()
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
        private bool CheckCommandBuffer()
        {
            switch (CommandBuffer[0])
            {
                case SCSI_TEST_UNIT_READY:
                    if (CommandBuffer.Count < 6) return false;
                    Log.Note("CD", "Execute TEST_UNIT_READY");
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
                    break;
            }
            return false;
        }

        private void CommandRead()
        {   
            int sector = (CommandBuffer[1] & 0x1f) << 16;
            sector |= CommandBuffer[2] << 8;
            sector |= CommandBuffer[3];

if (CommandBuffer[4] == 0)
throw new Exception("requesting 0 sectors read.............................");

            DataReadInProgress = true;
            DataTransferInProgress = true;
            CurrentReadingSector = sector;
            SectorsLeftToRead = CommandBuffer[4];

            Console.WriteLine("STARTED READ: {0} SECTORS FROM {1}",SectorsLeftToRead, CurrentReadingSector);
            DataReadWaitTimer = pce.Cpu.TotalExecutedCycles + 5000; // figure out proper read delay later
            pce.CDAudio.Stop();
        }

        private int audioStartLBA;
        private int audioEndLBA;

        private void CommandAudioStartPos()
        {
            switch (CommandBuffer[9] & 0xC0)
            {
                case 0x00: // Set start offset in LBA units
                    audioStartLBA = (CommandBuffer[3] << 16) | (CommandBuffer[4] << 8) | CommandBuffer[5];
                    Console.WriteLine("Set Start LBA: "+audioStartLBA);
                    break;

                case 0x40: // Set start offset in MSF units
                    byte m = CommandBuffer[2].BCDtoBin();
                    byte s = CommandBuffer[3].BCDtoBin();
                    byte f = CommandBuffer[4].BCDtoBin();
                    audioStartLBA = Disc.ConvertMSFtoLBA(m, s, f);
                    Console.WriteLine("Set Start MSF: {0} {1} {2} lba={3}",m,s,f,audioStartLBA);
                    break;

                case 0x80: // Set start offset in track units
                    byte trackNo = CommandBuffer[2].BCDtoBin();
                    audioStartLBA = disc.TOC.Sessions[0].Tracks[trackNo - 1].Indexes[1].aba - 150;
                    Console.WriteLine("Set Start track: {0} lba={1}", trackNo, audioStartLBA);
                    break;
            }

            if (CommandBuffer[1] == 0)
            {
                pce.CDAudio.Pause();
                // silent?
            } else {
                pce.CDAudio.PlayStartingAtLba(audioStartLBA);
            }
            
            // TODO there are some flags in command[1]
            // wat we do if audio is already playing
            // wat we do if audio paused

            SetStatusMessage(STATUS_GOOD, 0);
            // irq callback?
        }

        private void CommandAudioEndPos()
        {
            switch (CommandBuffer[9] & 0xC0)
            {
                case 0x00: // Set end offset in LBA units
                    audioEndLBA = (CommandBuffer[3] << 16) | (CommandBuffer[4] << 8) | CommandBuffer[5];
                    Console.WriteLine("Set End LBA: " + audioEndLBA);
                    break;

                case 0x40: // Set end offset in MSF units
                    byte m = CommandBuffer[2].BCDtoBin();
                    byte s = CommandBuffer[3].BCDtoBin();
                    byte f = CommandBuffer[4].BCDtoBin();
                    audioEndLBA = Disc.ConvertMSFtoLBA(m, s, f);
                    Console.WriteLine("Set End MSF: {0} {1} {2} lba={3}", m, s, f, audioEndLBA);
                    break;

                case 0x80: // Set end offset in track units
                    byte trackNo = CommandBuffer[2].BCDtoBin();
                    audioEndLBA = disc.TOC.Sessions[0].Tracks[trackNo - 1].Indexes[1].aba - 150;
                    Console.WriteLine("Set End track: {0} lba={1}", trackNo, audioEndLBA);
                    break;
            }

            switch (CommandBuffer[1])
            {
                case 0: // end immediately
                    pce.CDAudio.Stop(); 
                    break;
                case 1: // play in loop mode. I guess this constitues A-B looping
                    Console.WriteLine("DOING A-B LOOP. NOT SURE IF RIGHT.");
                    pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                    pce.CDAudio.EndLBA = audioEndLBA;
                    pce.CDAudio.PlayMode = CDAudio.PlaybackMode.LoopOnCompletion;
                    break;
                case 2: // Play audio, fire IRQ2 when end position reached
                    Console.WriteLine("STOP MODE 2 ENGAGED, BUT NOTE. IRQ WILL NOT FIRE YET.");
                    pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                    pce.CDAudio.EndLBA = audioEndLBA;
                    pce.CDAudio.PlayMode = CDAudio.PlaybackMode.CallbackOnCompletion;
                    break;
                case 3: // Play normal
                    Console.WriteLine("*** SET END POS, IN PLAY NORMAL MODE? STARTING AT _START_ POS. IS THAT RIGHT?");
                    pce.CDAudio.PlayStartingAtLba(audioStartLBA);
                    pce.CDAudio.EndLBA = audioEndLBA;
                    pce.CDAudio.PlayMode = CDAudio.PlaybackMode.StopOnCompletion;
                    break;
            }
            SetStatusMessage(STATUS_GOOD, 0);
        }
        
        private void CommandPause()
        {
            // apparently pause means stop? I guess? Idunno.
            pce.CDAudio.Stop();
            SetStatusMessage(STATUS_GOOD, 0);
            // TODO send error if already stopped.. or paused... or something.
        }

        private void CommandReadSubcodeQ()
        {
			Console.WriteLine("poll subcode");
            var sectorEntry = disc.ReadLBA_SectorEntry(pce.CDAudio.CurrentSector);

            DataIn.Clear();

            switch (pce.CDAudio.Mode)
            {
                case CDAudio.CDAudioMode.Playing: DataIn.Enqueue(0); break;
                case CDAudio.CDAudioMode.Paused:  DataIn.Enqueue(2); break;
                case CDAudio.CDAudioMode.Stopped: DataIn.Enqueue(3); break;
            }
            
			DataIn.Enqueue(sectorEntry.q_status);          // unused?
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

        private void CommandReadTOC()
        {
            switch (CommandBuffer[1])
            {
                case 0: // return number of tracks
                    {
                        Log.Error("CD","Execute READ_TOC : num of tracks");
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

                        Log.Error("CD","EXECUTE READ_TOC : length of disc, LBA {0}, m:{1},s:{2},f:{3}",
                                          totalLbaLength, m, s, f);
                        break;
                    }
                case 2: // Return starting position of specified track in MSF format
                    {
                        int track = CommandBuffer[2].BCDtoBin();
                        if (CommandBuffer[2] > 0x99)
                            throw new Exception("invalid track number BCD request... is something I need to handle?");
                        if (track == 0) track = 1;
                        track--;
                        if (track > disc.TOC.Sessions[0].Tracks.Count)
                            throw new Exception("Request more tracks than exist.... need to do error handling");
                        // I error handled your mom last night

                        int lbaPos = disc.TOC.Sessions[0].Tracks[track].Indexes[1].aba - 150;
                        byte m, s, f;
                        Disc.ConvertLBAtoMSF(lbaPos, out m, out s, out f);
                        
                        DataIn.Clear();
                        DataIn.Enqueue(m.BinToBCD());
                        DataIn.Enqueue(s.BinToBCD());
                        DataIn.Enqueue(f.BinToBCD());
                        if (disc.TOC.Sessions[0].Tracks[track].TrackType == ETrackType.Audio)
                            DataIn.Enqueue(0);
                        else
                            DataIn.Enqueue(4);
                        SetPhase(BusPhase.DataIn);

                        Log.Error("CD", "EXECUTE READ_TOC : start pos of TRACK {4}, LBA {0}, m:{1},s:{2},f:{3}",
                                          lbaPos, m, s, f, track);

                        break;
                    }
                default:
                    Console.WriteLine("unimplemented READ TOC command argument!");
                    break;
            }
        }

        private void SetStatusMessage(byte status, byte message)
        {
            MessageValue = message;
            StatusCompleted = false;
            MessageCompleted = false;
            DataBits = status == STATUS_GOOD ? (byte) 0x00 : (byte) 0x01;
            SetPhase(BusPhase.Status);
        }

        private void SetPhase(BusPhase phase)
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
                    DataTransferComplete(false);
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
    }
}