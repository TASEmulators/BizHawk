using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.PCEngine
{
	// TODO we can adjust this to have Think take the number of cycles and not require
	// a reference to Cpu.TotalExecutedCycles
	// which incidentally would allow us to put it back to an int from a long if we wanted to
	public sealed class ScsiCDBus
	{
		private const int STATUS_GOOD = 0;
		private const int STATUS_CHECK_CONDITION = 1;
		private const int STATUS_CONDITION_MET = 2;
		private const int STATUS_BUSY = 4;
		private const int STATUS_INTERMEDIATE = 8;

		private const int SCSI_TEST_UNIT_READY = 0x00;
		private const int SCSI_REQUEST_SENSE = 0x03;
		private const int SCSI_READ = 0x08;
		private const int SCSI_AUDIO_START_POS = 0xD8;
		private const int SCSI_AUDIO_END_POS = 0xD9;
		private const int SCSI_PAUSE = 0xDA;
		private const int SCSI_READ_SUBCODE_Q = 0xDD;
		private const int SCSI_READ_TOC = 0xDE;

		private bool bsy, sel, cd, io, msg, req, ack, atn, rst;
		private bool signalsChanged;

		public bool BSY
		{
			get => bsy;
			set
			{
				if (value != BSY) signalsChanged = true;
				bsy = value;
			}
		}

		public bool SEL
		{
			get => sel;
			set
			{
				if (value != SEL) signalsChanged = true;
				sel = value;
			}
		}

		public bool CD // CONTROL = true, DATA = false
		{
			get => cd;
			set
			{
				if (value != CD) signalsChanged = true;
				cd = value;
			}
		}

		public bool IO // INPUT = true, OUTPUT = false
		{
			get => io;
			set
			{
				if (value != IO) signalsChanged = true;
				io = value;
			}
		}

		public bool MSG
		{
			get => msg;
			set
			{
				if (value != MSG) signalsChanged = true;
				msg = value;
			}
		}

		public bool REQ
		{
			get => req;
			set
			{
				if (value != REQ) signalsChanged = true;
				req = value;
			}
		}

		public bool ACK
		{
			get => ack;
			set
			{
				if (value != ACK) signalsChanged = true;
				ack = value;
			}
		}

		public bool ATN
		{
			get => atn;
			set
			{
				if (value != ATN) signalsChanged = true;
				atn = value;
			}
		}

		public bool RST
		{
			get => rst;
			set
			{
				if (value != RST) signalsChanged = true;
				rst = value;
			}
		}

		public byte DataBits;

		private const byte BusPhase_BusFree = 0;
		private const byte BusPhase_Command = 1;
		private const byte BusPhase_DataIn = 2;
		private const byte BusPhase_DataOut = 3;
		private const byte BusPhase_MessageIn = 4;
		private const byte BusPhase_MessageOut = 5;
		private const byte BusPhase_Status = 6;

		private bool busPhaseChanged;
		private byte Phase = BusPhase_BusFree;

		private bool MessageCompleted;
		private bool StatusCompleted;
		private byte MessageValue;

		private readonly QuickList<byte> CommandBuffer = new QuickList<byte>(10); // 10 = biggest command
		public QuickQueue<byte> DataIn = new QuickQueue<byte>(2048); // one data sector

		// ******** Data Transfer / READ command support ********

		public long DataReadWaitTimer;
		public bool DataReadInProgress;
		public bool DataTransferWasDone;
		public bool DataTransferInProgress;
		public int CurrentReadingSector;
		public int SectorsLeftToRead;

		// ******** Resources ********

		private readonly PCEngine pce;
		public Disc disc;
		private readonly DiscSectorReader DiscSectorReader;
		private SubchannelQ subchannelQ;
		private int audioStartLBA;
		private int audioEndLBA;

		public ScsiCDBus(PCEngine pce, Disc disc)
		{
			this.pce = pce;
			this.disc = disc;
			DiscSectorReader = new DiscSectorReader(disc);
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
					pce.DriveLightOn = true;

				if (DataIn.Count == 0)
				{
					// read in a sector and shove it in the queue
					var dsr = new DiscSectorReader(disc); // TODO - cache reader
					dsr.ReadLBA_2048(CurrentReadingSector, DataIn.GetBuffer(), 0);
					DataIn.SignalBufferFilled(2048);
					CurrentReadingSector++;
					SectorsLeftToRead--;

					pce.IntDataTransferReady = true;

					// If more sectors, should set the next think-clock to however long it takes to read 1 sector
					// but I don't. I don't think transfers actually happen sector by sector
					// like this, they probably become available as the bits come off the disc.
					// but lets get some basic functionality before we go crazy.
					//  Idunno, maybe they do come in a sector at a time.

					// note to vecna: maybe not at the sector level, but at a level > 1 sample and <= 1 sector, samples come out in blocks
					// due to the way they are jumbled up (seriously, like put into a blender) for error correction purposes. 
					// we may as well assume that the cd audio decoding magic works at the level of one sector, but it isnt one sample.

					if (SectorsLeftToRead == 0)
					{
						DataReadInProgress = false;
						DataTransferWasDone = true;
					}

					SetPhase(BusPhase_DataIn);
				}
			}

			do
			{
				signalsChanged = false;
				busPhaseChanged = false;

				if (SEL && !BSY)
				{
					SetPhase(BusPhase_Command);
				}
				else if (ATN && !REQ && !ACK)
				{
					SetPhase(BusPhase_MessageOut);
				}
				else switch (Phase)
					{
						case BusPhase_Command: ThinkCommandPhase(); break;
						case BusPhase_DataIn: ThinkDataInPhase(); break;
						case BusPhase_DataOut: ThinkDataOutPhase(); break;
						case BusPhase_MessageIn: ThinkMessageInPhase(); break;
						case BusPhase_MessageOut: ThinkMessageOutPhase(); break;
						case BusPhase_Status: ThinkStatusPhase(); break;
						default: break;
					}
			} while (signalsChanged || busPhaseChanged);
		}

		private void ResetDevice()
		{
			CD = false;
			IO = false;
			MSG = false;
			REQ = false;
			ACK = false;
			ATN = false;
			DataBits = 0;
			Phase = BusPhase_BusFree;

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
				}
				else
				{
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

		private void ThinkDataOutPhase()
		{
			Console.WriteLine("*********** DATA OUT PHASE, DOES THIS HAPPEN? ****************");
			SetPhase(BusPhase_BusFree);
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
				SetPhase(BusPhase_BusFree);
			}
		}

		private void ThinkMessageOutPhase()
		{
			Console.WriteLine("******* IN MESSAGE OUT PHASE. DOES THIS EVER HAPPEN? ********");
			SetPhase(BusPhase_BusFree);
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
				SetPhase(BusPhase_MessageIn);
			}
		}

		// returns true if command completed, false if more data bytes needed
		private bool CheckCommandBuffer()
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

		private void CommandRead()
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

			// figure out proper read delay later
			// 10000 fixes Mugen Senshi Valis, which runs code in a timed loop, expecting a certain number of VBlanks
			// to happen before reading is complete
			// 175000 fixes 4 in 1 CD, loading Gate of Thunder
			// which expects a certain number of timer interrupts to happen before loading is complete
			DataReadWaitTimer = pce.Cpu.TotalExecutedCycles + 175000; 
			pce.CDAudio.Stop();
		}

		private void CommandAudioStartPos()
		{
			switch (CommandBuffer[9] & 0xC0)
			{
				case 0x00: // Set start offset in LBA units
					audioStartLBA = (CommandBuffer[3] << 16) | (CommandBuffer[4] << 8) | CommandBuffer[5];
					break;

				case 0x40: // Set start offset in absolute MSF units
					byte m = CommandBuffer[2].BCDtoBin();
					byte s = CommandBuffer[3].BCDtoBin();
					byte f = CommandBuffer[4].BCDtoBin();
					audioStartLBA = DiscUtils.Convert_AMSF_To_LBA(m, s, f);
					break;

				case 0x80: // Set start offset in track units
					byte trackNo = CommandBuffer[2].BCDtoBin();
					audioStartLBA = disc.Session1.Tracks[trackNo].LBA;
					break;
			}

			if (CommandBuffer[1] == 0)
			{
				pce.CDAudio.PlayStartingAtLba(audioStartLBA);
				pce.CDAudio.Pause();
			}
			else
			{
				pce.CDAudio.PlayStartingAtLba(audioStartLBA);
			}

			SetStatusMessage(STATUS_GOOD, 0);
			pce.IntDataTransferComplete = true;
		}

		private void CommandAudioEndPos()
		{
			switch (CommandBuffer[9] & 0xC0)
			{
				case 0x00: // Set end offset in LBA units
					audioEndLBA = (CommandBuffer[3] << 16) | (CommandBuffer[4] << 8) | CommandBuffer[5];
					break;

				case 0x40: // Set end offset in absolute MSF units
					byte m = CommandBuffer[2].BCDtoBin();
					byte s = CommandBuffer[3].BCDtoBin();
					byte f = CommandBuffer[4].BCDtoBin();
					audioEndLBA = DiscUtils.Convert_AMSF_To_LBA(m, s, f);
					break;

				case 0x80: // Set end offset in track units
					byte trackNo = CommandBuffer[2].BCDtoBin();
					if (trackNo - 1 >= disc.Session1.Tracks.Count)
						audioEndLBA = disc.Session1.LeadoutLBA;
					else
						audioEndLBA = disc.Session1.Tracks[trackNo].LBA;
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
					pce.CDAudio.PlayMode = CDAudio.PlaybackMode_LoopOnCompletion;
					break;
				case 2: // Play audio, fire IRQ2 when end position reached, maybe
					pce.CDAudio.PlayStartingAtLba(audioStartLBA);
					pce.CDAudio.EndLBA = audioEndLBA;
					pce.CDAudio.PlayMode = CDAudio.PlaybackMode_CallbackOnCompletion;
					break;
				case 3: // Play normal
					pce.CDAudio.PlayStartingAtLba(audioStartLBA);
					pce.CDAudio.EndLBA = audioEndLBA;
					pce.CDAudio.PlayMode = CDAudio.PlaybackMode_StopOnCompletion;
					break;
			}
			SetStatusMessage(STATUS_GOOD, 0);
		}

		private void CommandPause()
		{
			pce.CDAudio.Stop();
			SetStatusMessage(STATUS_GOOD, 0);
		}

		private void CommandReadSubcodeQ()
		{
			bool playing = pce.CDAudio.Mode != CDAudio.CDAudioMode_Stopped;
			int sectorNum = playing ? pce.CDAudio.CurrentSector : CurrentReadingSector;

			DataIn.Clear();

			switch (pce.CDAudio.Mode)
			{
				case CDAudio.CDAudioMode_Playing: DataIn.Enqueue(0); break;
				case CDAudio.CDAudioMode_Paused: DataIn.Enqueue(2); break;
				case CDAudio.CDAudioMode_Stopped: DataIn.Enqueue(3); break;
			}

			DiscSectorReader.ReadLBA_SubQ(sectorNum, out subchannelQ);
			DataIn.Enqueue(subchannelQ.q_status); // status (control and q-mode; control is useful to know if it's a data or audio track)
			DataIn.Enqueue(subchannelQ.q_tno.BCDValue);    // track //zero 03-jul-2015 - did I adapt this right>
			DataIn.Enqueue(subchannelQ.q_index.BCDValue);  // index //zero 03-jul-2015 - did I adapt this right>
			DataIn.Enqueue(subchannelQ.min.BCDValue);    // M(rel)
			DataIn.Enqueue(subchannelQ.sec.BCDValue);    // S(rel)
			DataIn.Enqueue(subchannelQ.frame.BCDValue);  // F(rel)
			DataIn.Enqueue(subchannelQ.ap_min.BCDValue);   // M(abs)
			DataIn.Enqueue(subchannelQ.ap_sec.BCDValue);   // S(abs)
			DataIn.Enqueue(subchannelQ.ap_frame.BCDValue); // F(abs)

			SetPhase(BusPhase_DataIn);
		}

		private void CommandReadTOC()
		{
			switch (CommandBuffer[1])
			{
				case 0: // return number of tracks
					{
						DataIn.Clear();
						DataIn.Enqueue(0x01);
						DataIn.Enqueue(((byte)disc.Session1.Tracks.Count).BinToBCD());
						SetPhase(BusPhase_DataIn);
						break;
					}
				case 1: // return total disc length in minutes/seconds/frames
					{
						// zero 07-jul-2015 - I may have broken this
						int totalLbaLength = disc.Session1.LeadoutLBA;

						DiscUtils.Convert_LBA_To_AMSF(totalLbaLength + 150, out var m, out var s, out var f);

						DataIn.Clear();
						DataIn.Enqueue(m.BinToBCD());
						DataIn.Enqueue(s.BinToBCD());
						DataIn.Enqueue(f.BinToBCD());
						SetPhase(BusPhase_DataIn);
						break;
					}
				case 2: // Return starting position of specified track in MSF format. TODO - did zero adapt this right? track indexing might be off
					{
						int track = CommandBuffer[2].BCDtoBin();
						var tracks = disc.Session1.Tracks;
						if (CommandBuffer[2] > 0x99)
							throw new Exception("invalid track number BCD request... is something I need to handle?");
						if (track == 0) track = 1;

						int lbaPos;

						if (track > disc.Session1.InformationTrackCount)
							lbaPos = disc.Session1.LeadoutLBA; //zero 03-jul-2015 - did I adapt this right?
						else
							lbaPos = tracks[track].LBA;

						DiscUtils.Convert_LBA_To_AMSF(lbaPos, out var m, out var s, out var f);

						DataIn.Clear();
						DataIn.Enqueue(m.BinToBCD());
						DataIn.Enqueue(s.BinToBCD());
						DataIn.Enqueue(f.BinToBCD());

						if (track > tracks.Count || disc.Session1.Tracks[track].IsAudio)
							DataIn.Enqueue(0);
						else
							DataIn.Enqueue(4);
						SetPhase(BusPhase_DataIn);
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
			DataBits = status == STATUS_GOOD ? (byte)0x00 : (byte)0x01;
			SetPhase(BusPhase_Status);
		}

		private void SetPhase(byte phase)
		{
			if (Phase == phase)
			{
				return;
			}

			Phase = phase;
			busPhaseChanged = true;

			switch (phase)
			{
				case BusPhase_BusFree:
					BSY = false;
					MSG = false;
					CD = false;
					IO = false;
					REQ = false;
					pce.IntDataTransferComplete = false;
					break;
				case BusPhase_Command:
					BSY = true;
					MSG = false;
					CD = true;
					IO = false;
					REQ = true;
					break;
				case BusPhase_DataIn:
					BSY = true;
					MSG = false;
					CD = false;
					IO = true;
					REQ = false;
					break;
				case BusPhase_DataOut:
					BSY = true;
					MSG = false;
					CD = false;
					IO = false;
					REQ = true;
					break;
				case BusPhase_MessageIn:
					BSY = true;
					MSG = true;
					CD = true;
					IO = true;
					REQ = true;
					break;
				case BusPhase_MessageOut:
					BSY = true;
					MSG = true;
					CD = true;
					IO = false;
					REQ = true;
					break;
				case BusPhase_Status:
					BSY = true;
					MSG = false;
					CD = true;
					IO = true;
					REQ = true;
					break;
			}
		}

		// ***************************************************************************

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("SCSI");
			ser.Sync("BSY", ref bsy);
			ser.Sync("SEL", ref sel);
			ser.Sync("CD", ref cd);
			ser.Sync("IO", ref io);
			ser.Sync("MSG", ref msg);
			ser.Sync("REQ", ref req);
			ser.Sync("ACK", ref ack);
			ser.Sync("ATN", ref atn);
			ser.Sync("RST", ref rst);
			ser.Sync(nameof(DataBits), ref DataBits);
			ser.Sync(nameof(Phase), ref Phase);

			ser.Sync(nameof(MessageCompleted), ref MessageCompleted);
			ser.Sync(nameof(StatusCompleted), ref StatusCompleted);
			ser.Sync(nameof(MessageValue), ref MessageValue);

			ser.Sync(nameof(DataReadWaitTimer), ref DataReadWaitTimer);
			ser.Sync(nameof(DataReadInProgress), ref DataReadInProgress);
			ser.Sync(nameof(DataTransferWasDone), ref DataTransferWasDone);
			ser.Sync(nameof(DataTransferInProgress), ref DataTransferInProgress);
			ser.Sync(nameof(CurrentReadingSector), ref CurrentReadingSector);
			ser.Sync(nameof(SectorsLeftToRead), ref SectorsLeftToRead);

			ser.Sync("CommandBuffer", ref CommandBuffer.buffer, false);
			ser.Sync("CommandBufferPosition", ref CommandBuffer.Position);

			ser.Sync("DataInBuffer", ref DataIn.buffer, false);
			ser.Sync("DataInHead", ref DataIn.head);
			ser.Sync("DataInTail", ref DataIn.tail);
			ser.Sync("DataInSize", ref DataIn.size);

			ser.Sync("AudioStartLBA", ref audioStartLBA);
			ser.Sync("AudioEndLBA", ref audioEndLBA);
			ser.EndSection();
		}
	}
}