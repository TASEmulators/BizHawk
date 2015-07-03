using System;
using System.Collections.Generic;

using BizHawk.Common.BufferExtensions;

//main apis for emulator core routine use
//(this could probably use a lot of re-considering. we need to open up views to the disc, not interrogating it directly)

namespace BizHawk.Emulation.DiscSystem
{
	public class DiscSectorReaderPolicy
	{
		/// <summary>
		/// Different methods that can be used to get 2048 byte sectors
		/// </summary>
		public enum EUserData2048Mode
		{
			/// <summary>
			/// The contents of the sector should be inspected (mode and form) and 2048 bytes returned accordingly
			/// </summary>
			InspectSector,

			/// <summary>
			/// Read it as mode 1
			/// </summary>
			AssumeMode1,

			/// <summary>
			/// Read it as mode 2 (form 1)
			/// </summary> 
			AssumeMode2_Form1,
		}

		/// <summary>
		/// The method used to get 2048 byte sectors
		/// </summary>
		public EUserData2048Mode UserData2048Mode = EUserData2048Mode.InspectSector;

		/// <summary>
		/// Throw exceptions if 2048 byte data can't be read
		/// </summary>
		public bool ThrowExceptions2048 = true;

		/// <summary>
		/// Indicates whether subcode should be delivered deinterleaved. It isn't stored that way on actual discs. But it is in .sub files
		/// </summary>
		public bool DeinterleavedSubcode = true;

		/// <summary>
		/// Indicates whether the output buffer should be cleared before returning any data.
		/// This will unfortunately involve clearing sections you didn't ask for, and clearing sections about to be filled with data from the disc.
		/// It is a waste of performance, but it will ensure reliability.
		/// </summary>
		public bool DeterministicClearBuffer = true;
	}


	/// <summary>
	/// Main entry point for reading sectors from a disc.
	/// This is not a multi-thread capable interface.
	/// </summary>
	public class DiscSectorReader
	{
		public DiscSectorReaderPolicy Policy = new DiscSectorReaderPolicy();

		Disc disc;

		public DiscSectorReader(Disc disc)
		{
			this.disc = disc;
		}

		void PrepareJob(int lba)
		{
			job.LBA = lba;
			job.Params = disc.SynthParams;
			job.Disc = disc;
		}

		void PrepareBuffer(byte[] buffer, int offset, int size)
		{
			if (Policy.DeterministicClearBuffer) Array.Clear(buffer, offset, size);
		}

		//todo - methods to read only subcode

		/// <summary>
		/// Reads a full 2352 bytes of user data from a sector
		/// </summary>
		public int ReadLBA_2352(int lba, byte[] buffer, int offset)
		{
			var sector = disc.Sectors[lba + 150];

			PrepareBuffer(buffer, offset, 2352);
			PrepareJob(lba);
			job.DestBuffer2448 = buf2442;
			job.DestOffset = 0;
			job.Parts = ESectorSynthPart.User2352;
			job.Disc = disc;

			sector.SectorSynth.Synth(job);

			Buffer.BlockCopy(buf2442, 0, buffer, offset, 2352);

			return 2352;
		}

		/// <summary>
		/// Reads the absolutely complete 2442 byte sector including all the user data and subcode
		/// </summary>
		public int ReadLBA_2442(int lba, byte[] buffer, int offset)
		{
			var sector = disc.Sectors[lba + 150];

			PrepareBuffer(buffer, offset, 2352);
			PrepareJob(lba);
			job.DestBuffer2448 = buffer; //go straight to the caller's buffer
			job.DestOffset = offset; //go straight to the caller's buffer
			job.Parts = ESectorSynthPart.Complete2448;
			if (Policy.DeinterleavedSubcode)
				job.Parts |= ESectorSynthPart.SubcodeDeinterleave;

			sector.SectorSynth.Synth(job);

			//we went straight to the caller's buffer, so no need to copy
			return 2442;
		}

		int ReadLBA_2048_Mode1(int lba, byte[] buffer, int offset)
		{
			//we can read the 2048 bytes directly
			var sector = disc.Sectors[lba + 150];

			PrepareBuffer(buffer, offset, 2352);
			PrepareJob(lba);
			job.DestBuffer2448 = buf2442;
			job.DestOffset = 0;
			job.Parts = ESectorSynthPart.User2048;

			sector.SectorSynth.Synth(job);
			Buffer.BlockCopy(buf2442, 16, buffer, offset, 2048);

			return 2048;
		}

		int ReadLBA_2048_Mode2_Form1(int lba, byte[] buffer, int offset)
		{
			//we can read the 2048 bytes directly but we have to get them from the mode 2 data
			var sector = disc.Sectors[lba + 150];

			PrepareBuffer(buffer, offset, 2352);
			PrepareJob(lba);
			job.DestBuffer2448 = buf2442;
			job.DestOffset = 0;
			job.Parts = ESectorSynthPart.User2336;

			sector.SectorSynth.Synth(job);
			Buffer.BlockCopy(buf2442, 24, buffer, offset, 2048);

			return 2048;
		}

		/// <summary>
		/// Reads 12 bytes of subQ data from a sector.
		/// This is necessarily deinterleaved.
		/// </summary>
		public int ReadLBA_SubQ(int lba, byte[] buffer, int offset)
		{
			var sector = disc.Sectors[lba + 150];

			PrepareBuffer(buffer, offset, 12);
			PrepareJob(lba);
			job.DestBuffer2448 = buf2442;
			job.DestOffset = 0;
			job.Parts = ESectorSynthPart.SubchannelQ | ESectorSynthPart.SubcodeDeinterleave;

			sector.SectorSynth.Synth(job);
			Buffer.BlockCopy(buf2442, 2352 + 12, buffer, offset, 12);

			return 12;
		}

		/// <summary>
		/// reads 2048 bytes of user data from a sector.
		/// This is only valid for Mode 1 and XA Mode 2 (Form 1) sectors.
		/// Attempting it on any other sectors is ill-defined.
		/// If any console is trying to do that, we'll have to add a policy for it, or handle it in the console.
		/// (We can add a method to this API that checks the type of a sector to make that easier)
		/// </summary>
		public int ReadLBA_2048(int lba, byte[] buffer, int offset)
		{
			if (Policy.UserData2048Mode == DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode1)
				return ReadLBA_2048_Mode1(lba, buffer, offset);
			else if (Policy.UserData2048Mode == DiscSectorReaderPolicy.EUserData2048Mode.AssumeMode2_Form1)
				return ReadLBA_2048_Mode2_Form1(lba, buffer, offset);
			else
			{
				//we need to determine the type of the sector.
				//in no case do we need the ECC so build special flags here
				var sector = disc.Sectors[lba + 150];

				PrepareBuffer(buffer, offset, 2352);
				PrepareJob(lba);
				job.DestBuffer2448 = buf2442;
				job.DestOffset = 0;
				job.Parts = ESectorSynthPart.Header16 | ESectorSynthPart.User2048 | ESectorSynthPart.EDC12;

				sector.SectorSynth.Synth(job);

				//now the inspection, based on the mode
				byte mode = buf2442[15];
				if (mode == 1)
				{
					Buffer.BlockCopy(buf2442, 16, buffer, offset, 2048);
					return 2048;
				}
				else if (mode == 2)
				{
					//greenbook pg II-22
					//we're going to do a sanity check here.. we're not sure what happens if we try to read 2048 bytes from a form-2 2324 byte sector
					//we could handle it by policy but for now the policy is exception
					byte submodeByte = buf2442[18];
					int form = ((submodeByte >> 5) & 1) + 1;
					if (form == 2)
					{
						if (Policy.ThrowExceptions2048)
							throw new InvalidOperationException("Unsupported scenario: reading 2048 bytes from a Mode2 Form 2 sector");
						else return 0;
					}

					//otherwise it's OK
					Buffer.BlockCopy(buf2442, 24, buffer, offset, 2048);
					return 2048;
				}
				else
				{
					if (Policy.ThrowExceptions2048)
						throw new InvalidOperationException("Unsupported scenario: reading 2048 bytes from an unhandled sector type");
					else return 0;
				}
			}
		}


		//lets not try to use this as a sector cache. it gets too complicated. its just a temporary variable.
		byte[] buf2442 = new byte[2448];
		SectorSynthJob job = new SectorSynthJob();
	}
}