/***************************************************************************************
 *  Genesis Plus
 *  CD drive processor & CD-DA fader
 *
 *  Copyright (C) 2012-2013  Eke-Eke (Genesis Plus GX)
 *
 *  Redistribution and use of this code or any derivative works are permitted
 *  provided that the following conditions are met:
 *
 *   - Redistributions may not be sold, nor may they be used in a commercial
 *     product or activity.
 *
 *   - Redistributions that are modified from the original source must include the
 *     complete source code, including the source code for all components used by a
 *     binary built from the modified sources. However, as a special exception, the
 *     source code distributed need not include anything that is normally distributed
 *     (in either source or binary form) with the major components (compiler, kernel,
 *     and so on) of the operating system on which the executable runs, unless that
 *     component itself accompanies the executable.
 *
 *   - Redistributions must reproduce the above copyright notice, this list of
 *     conditions and the following disclaimer in the documentation and/or other
 *     materials provided with the distribution.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *  AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *  IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 *  ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 *  LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *  CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 *  SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 *  INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 *  CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 *  POSSIBILITY OF SUCH DAMAGE.
 *
 ****************************************************************************************/

#include "../pico_int.h"
#include "genplus_macros.h"
#include "cdd.h"

cdd_t cdd;

void (*CDReadSector)(int lba, void *dest, int audio);

/* BCD conversion lookup tables */
static const uint8 lut_BCD_8[100] =
	{
		0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
		0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,
		0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
		0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
		0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
		0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
		0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
		0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
		0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
		0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99,
};

static const uint16 lut_BCD_16[100] =
	{
		0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009,
		0x0100, 0x0101, 0x0102, 0x0103, 0x0104, 0x0105, 0x0106, 0x0107, 0x0108, 0x0109,
		0x0200, 0x0201, 0x0202, 0x0203, 0x0204, 0x0205, 0x0206, 0x0207, 0x0208, 0x0209,
		0x0300, 0x0301, 0x0302, 0x0303, 0x0304, 0x0305, 0x0306, 0x0307, 0x0308, 0x0309,
		0x0400, 0x0401, 0x0402, 0x0403, 0x0404, 0x0405, 0x0406, 0x0407, 0x0408, 0x0409,
		0x0500, 0x0501, 0x0502, 0x0503, 0x0504, 0x0505, 0x0506, 0x0507, 0x0508, 0x0509,
		0x0600, 0x0601, 0x0602, 0x0603, 0x0604, 0x0605, 0x0606, 0x0607, 0x0608, 0x0609,
		0x0700, 0x0701, 0x0702, 0x0703, 0x0704, 0x0705, 0x0706, 0x0707, 0x0708, 0x0709,
		0x0800, 0x0801, 0x0802, 0x0803, 0x0804, 0x0805, 0x0806, 0x0807, 0x0808, 0x0809,
		0x0900, 0x0901, 0x0902, 0x0903, 0x0904, 0x0905, 0x0906, 0x0907, 0x0908, 0x0909,
};

/* pre-build TOC */
static const uint16 toc_snatcher[21] =
	{
		56014, 495, 10120, 20555, 1580, 5417, 12502, 16090, 6553, 9681,
		8148, 20228, 8622, 6142, 5858, 1287, 7424, 3535, 31697, 2485,
		31380};

static const uint16 toc_lunar[52] =
	{
		5422, 1057, 7932, 5401, 6380, 6592, 5862, 5937, 5478, 5870,
		6673, 6613, 6429, 4996, 4977, 5657, 3720, 5892, 3140, 3263,
		6351, 5187, 3249, 1464, 1596, 1750, 1751, 6599, 4578, 5205,
		1550, 1827, 2328, 1346, 1569, 1613, 7199, 4928, 1656, 2549,
		1875, 3901, 1850, 2399, 2028, 1724, 4889, 14551, 1184, 2132,
		685, 3167};

static const uint32 toc_shadow[15] =
	{
		10226, 70054, 11100, 12532, 12444, 11923, 10059, 10167, 10138, 13792,
		11637, 2547, 2521, 3856, 900};

static const uint32 toc_dungeon[13] =
	{
		2250, 22950, 16350, 24900, 13875, 19950, 13800, 15375, 17400, 17100,
		3325, 6825, 25275};

static const uint32 toc_ffight[26] =
	{
		11994, 9742, 10136, 9685, 9553, 14588, 9430, 8721, 9975, 9764,
		9704, 12796, 585, 754, 951, 624, 9047, 1068, 817, 9191, 1024,
		14562, 10320, 8627, 3795, 3047};

static const uint32 toc_ffightj[29] =
	{
		11994, 9752, 10119, 9690, 9567, 14575, 9431, 8731, 9965, 9763,
		9716, 12791, 579, 751, 958, 630, 9050, 1052, 825, 9193, 1026,
		14553, 9834, 10542, 1699, 1792, 1781, 3783, 3052};

void cdd_reset(void)
{
	/* reset cycle counter */
	cdd.cycles = 0;

	/* reset drive access latency */
	cdd.latency = 0;

	/* reset track index */
	cdd.index = 0;

	/* reset logical block address */
	cdd.lba = 0;

	// reset audio subblock position
	cdd.sampleOffset = 0;

	// reset audio read position
	cdd.sampleLba = 0;

	/* reset status */
	cdd.status = NO_DISC;

	/* reset CD-DA fader (full volume) */
	cdd.volume = 0x400;

	/* clear CD-DA output */
	cdd.audio[0] = cdd.audio[1] = 0;
}

/* FIXME: use cdd_read_audio() instead */
static void cdd_change_track(int index, int lba)
{
	/*int i, base, lba_offset, lb_len;

	for (i = index; i > 0; i--)
		if (cdd.toc.tracks[i].fd != NULL)
			break;

	Pico_mcd->cdda_stream = cdd.toc.tracks[i].fd;
	base = cdd.toc.tracks[index].offset;
	lba_offset = lba - cdd.toc.tracks[index].start;
	lb_len = cdd.toc.tracks[index].end - cdd.toc.tracks[index].start;

	elprintf(EL_CD, "play #%d lba %d base %d", index, lba, base);

	cdda_start_play(base, lba_offset, lb_len);*/
}

int cdd_load(const char *filename, int type)
{
	FILE *f = fopen("toc", "rb");
	fread(&cdd.toc, 1, sizeof(cdd.toc), f);
	fclose(f);

	char sector[2048];

	CDReadSector(0, sector, 0);

	/* look for valid CD image ID string */
	if (memcmp("SEGADISCSYSTEM", sector, 14))
	{
		/* if not found, read next 16 bytes */

		/* look again for valid CD image ID string */
		if (memcmp("SEGADISCSYSTEM", sector + 16, 14))
		{
			elprintf(EL_STATUS | EL_ANOMALY, "cd: bad cd image?");
			/* assume bin without security code */
		}
	}

	/* Lead-out */
	// cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;

	/* CD loaded */
	cdd.loaded = 1;

	/* disc not scanned yet */
	cdd.status = NO_DISC;

	return 0;
}

void cdd_read_data(uint8 *dst)
{
	/* only read DATA track sectors */
	if ((cdd.lba >= 0) && (cdd.lba < cdd.toc.tracks[0].end))
	{
		CDReadSector(cdd.lba, dst, 0);
	}
}

void cdd_read_audio(short *buffer, unsigned int samples)
{
	short *outptr = buffer;
	/* audio track playing ? */
	if (!Pico_mcd->s68k_regs[0x36 + 0])
	{
		int i, mul;

		/* current CD-DA fader volume */
		int curVol = cdd.volume;

		/* CD-DA fader volume setup (0-1024) */
		int endVol = Pico_mcd->s68k_regs[0x34] << 4 | Pico_mcd->s68k_regs[0x35] >> 4;

		/* read samples from current block */
		{
			uint8_t audio_scratch[4096];
#ifdef LSB_FIRST
			int16 *ptr = (int16 *)audio_scratch;
#else
			uint8 *ptr = audio_scratch;
#endif
			{
				char scratch[2352];
				int nsampreq = samples;
				unsigned char *dest = audio_scratch;
				while (nsampreq > 0)
				{
					int tocopy = 588 - cdd.sampleOffset;
					if (tocopy > nsampreq)
						tocopy = nsampreq;
					CDReadSector(cdd.sampleLba, scratch, 1);
					memcpy(dest, scratch + cdd.sampleOffset * 4, tocopy * 4);
					nsampreq -= tocopy;
					dest += tocopy * 4;
					cdd.sampleOffset += tocopy;
					if (cdd.sampleOffset == 588)
					{
						cdd.sampleOffset = 0;
						cdd.sampleLba++;
					}
				}

				//printf("samples: %i\n", samples);
				//memset(cdc.ram, 0, samples * 4);
				//fread(cdc.ram, 1, samples * 4, cdd.toc.tracks[cdd.index].fd);
			}

			/* process 16-bit (little-endian) stereo samples */
			for (i = 0; i < samples; i++)
			{
				/* CD-DA fader multiplier (cf. LC7883 datasheet) */
				/* (MIN) 0,1,2,3,4,8,12,16,20...,1020,1024 (MAX) */
				mul = (curVol & 0x7fc) ? (curVol & 0x7fc) : (curVol & 0x03);

/* left channel */
#ifdef LSB_FIRST
				*outptr++ = ((ptr[0] * mul) / 1024);
				ptr++;
#else
				*outptr++ = (((int16)((ptr[0] + ptr[1] * 256)) * mul) / 1024);
				ptr += 2;
#endif

/* right channel */
#ifdef LSB_FIRST
				*outptr++ = ((ptr[0] * mul) / 1024);
				ptr++;
#else
				*outptr++ = (((int16)((ptr[0] + ptr[1] * 256)) * mul) / 1024);
				ptr += 2;
#endif

				/* update CD-DA fader volume (one step/sample) */
				if (curVol < endVol)
				{
					/* fade-in */
					curVol++;
				}
				else if (curVol > endVol)
				{
					/* fade-out */
					curVol--;
				}
				else if (!curVol)
				{
					/* audio will remain muted until next setup */
					break;
				}
			}
		}

		/* save current CD-DA fader volume */
		cdd.volume = curVol;
	}
	else
	{
		/* no audio output */
		memset(buffer, 0, samples * 4);
	}
}

void cdd_update(void)
{
#ifdef LOG_CDD
	error("LBA = %d (track n�%d)(latency=%d)\n", cdd.lba, cdd.index, cdd.latency);
#endif

	/* seeking disc */
	if (cdd.status == CD_SEEK)
	{
		/* drive latency */
		if (cdd.latency > 0)
		{
			cdd.latency--;
			return;
		}

		/* drive is ready */
		cdd.status = CD_READY;
	}

	/* reading disc */
	else if (cdd.status == CD_PLAY)
	{
		/* drive latency */
		if (cdd.latency > 0)
		{
			cdd.latency--;
			return;
		}

		/* track type */
		if (!cdd.index)
		{
			/* DATA sector header (CD-ROM Mode 1) */
			uint8 header[4];
			uint32 msf = cdd.lba + 150;
			header[0] = lut_BCD_8[(msf / 75) / 60];
			header[1] = lut_BCD_8[(msf / 75) % 60];
			header[2] = lut_BCD_8[(msf % 75)];
			header[3] = 0x01;

			/* data track sector read is controlled by CDC */
			cdd.lba += cdc_decoder_update(header);
		}
		else if (cdd.index < cdd.toc.last)
		{
			uint8 header[4] = {
				0,
			};

			/* check against audio track start index */
			if (cdd.lba >= cdd.toc.tracks[cdd.index].start)
			{
				/* audio track playing */
				// if it wasn't before, set the audio start position
				if (Pico_mcd->s68k_regs[0x36 + 0])
				{
					cdd.sampleLba = cdd.lba + 1;
					cdd.sampleOffset = 0;
				}
				Pico_mcd->s68k_regs[0x36 + 0] = 0x00;
			}

			/* audio blocks are still sent to CDC as well as CD DAC/Fader */
			cdc_decoder_update(header);

			/* next audio block is automatically read */
			cdd.lba++;
		}
		else
		{
			/* end of disc */
			cdd.status = CD_END;
			return;
		}

		/* check end of current track */
		if (cdd.lba >= cdd.toc.tracks[cdd.index].end)
		{
			/* play next track */
			cdd.index++;

			/* PAUSE between tracks */
			Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

/* seek to next audio track start */
#if 0
      if (cdd.toc.tracks[cdd.index].fd)
      {
        fseek(cdd.toc.tracks[cdd.index].fd, (cdd.toc.tracks[cdd.index].start * 2352) - cdd.toc.tracks[cdd.index].offset, SEEK_SET);
      }
#else
			{
				cdd_change_track(cdd.index, cdd.lba);
			}
#endif
		}
	}

	/* scanning disc */
	else if (cdd.status == CD_SCAN)
	{
		/* fast-forward or fast-rewind */
		cdd.lba += cdd.scanOffset;
		cdd.sampleLba += cdd.scanOffset;

		/* check current track limits */
		if (cdd.lba >= cdd.toc.tracks[cdd.index].end)
		{
			/* next track */
			cdd.index++;

			/* skip directly to track start position */
			cdd.lba = cdd.toc.tracks[cdd.index].start;

			/* AUDIO track playing ? */
			if (cdd.status == CD_PLAY)
			{
				Pico_mcd->s68k_regs[0x36 + 0] = 0x00;
				// set audio start point
				cdd.sampleLba = cdd.lba;
				cdd.sampleOffset = 0;
			}
		}
		else if (cdd.lba < cdd.toc.tracks[cdd.index].start)
		{
			/* previous track */
			cdd.index--;

			/* skip directly to track end position */
			cdd.lba = cdd.toc.tracks[cdd.index].end;
		}

		/* check disc limits */
		if (cdd.index < 0)
		{
			cdd.index = 0;
			cdd.lba = 0;
		}
		else if (cdd.index >= cdd.toc.last)
		{
			/* no AUDIO track playing */
			Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

			/* end of disc */
			cdd.index = cdd.toc.last;
			cdd.lba = cdd.toc.end;
			cdd.status = CD_END;
			return;
		}

		/* seek to current block */
		if (!cdd.index)
		{
			/* no AUDIO track playing */
			Pico_mcd->s68k_regs[0x36 + 0] = 0x01;
		}
#if 0
    else if (cdd.toc.tracks[cdd.index].fd)
    {
      /* PCM AUDIO track */
      fseek(cdd.toc.tracks[cdd.index].fd, (cdd.lba * 2352) - cdd.toc.tracks[cdd.index].offset, SEEK_SET);
    }
#else
		else
		{
			cdd_change_track(cdd.index, cdd.lba);
		}
#endif
	}
}

#define set_reg16(r, v)                     \
	{                                       \
		uint16 _v = v;                      \
		Pico_mcd->s68k_regs[(r)] = _v >> 8; \
		Pico_mcd->s68k_regs[(r) + 1] = _v;  \
	}

void cdd_process(void)
{
	/* Process CDD command */
	switch (Pico_mcd->s68k_regs[0x42 + 0] & 0x0f)
	{
	case 0x00: /* Drive Status */
	{
		/* RS1-RS8 normally unchanged */
		Pico_mcd->s68k_regs[0x38 + 0] = cdd.status;

		/* unless RS1 indicated invalid track infos */
		if (Pico_mcd->s68k_regs[0x38 + 1] == 0x0f)
		{
			/* and SEEK has ended */
			if (cdd.status != CD_SEEK)
			{
				/* then return valid track infos, e.g current track number in RS2-RS3 (fixes Lunar - The Silver Star) */
				Pico_mcd->s68k_regs[0x38 + 1] = 0x02;
				set_reg16(0x3a, (cdd.index < cdd.toc.last) ? lut_BCD_16[cdd.index + 1] : 0x0A0A);
			}
		}
		break;
	}

	case 0x01: /* Stop Drive */
	{
		/* update status */
		cdd.status = cdd.loaded ? CD_STOP : NO_DISC;

		/* no audio track playing */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* RS1-RS8 ignored, expects 0x0 ("no disc" ?) in RS0 once */
		set_reg16(0x38, 0x0000);
		set_reg16(0x3a, 0x0000);
		set_reg16(0x3c, 0x0000);
		set_reg16(0x3e, 0x0000);
		set_reg16(0x40, 0x000f);
		return;
	}

	case 0x02: /* Read TOC */
	{
		if (cdd.status == NO_DISC)
			cdd.status = cdd.loaded ? CD_STOP : NO_DISC;

		/* Infos automatically retrieved by CDD processor from Q-Channel */
		/* commands 0x00-0x02 (current block) and 0x03-0x05 (Lead-In) */
		switch (Pico_mcd->s68k_regs[0x44 + 1])
		{
		case 0x00: /* Current Absolute Time (MM:SS:FF) */
		{
			int lba = cdd.lba + 150;
			set_reg16(0x38, cdd.status << 8);
			set_reg16(0x3a, lut_BCD_16[(lba / 75) / 60]);
			set_reg16(0x3c, lut_BCD_16[(lba / 75) % 60]);
			set_reg16(0x3e, lut_BCD_16[(lba % 75)]);
			Pico_mcd->s68k_regs[0x40 + 0] = cdd.index ? 0x00 : 0x04; /* Current block flags in RS8 (bit0 = mute status, bit1: pre-emphasis status, bit2: track type) */
			break;
		}

		case 0x01: /* Current Track Relative Time (MM:SS:FF) */
		{
			int lba = cdd.lba - cdd.toc.tracks[cdd.index].start;
			set_reg16(0x38, (cdd.status << 8) | 0x01);
			set_reg16(0x3a, lut_BCD_16[(lba / 75) / 60]);
			set_reg16(0x3c, lut_BCD_16[(lba / 75) % 60]);
			set_reg16(0x3e, lut_BCD_16[(lba % 75)]);
			Pico_mcd->s68k_regs[0x40 + 0] = cdd.index ? 0x00 : 0x04; /* Current block flags in RS8 (bit0 = mute status, bit1: pre-emphasis status, bit2: track type) */
			break;
		}

		case 0x02: /* Current Track Number */
		{
			set_reg16(0x38, (cdd.status << 8) | 0x02);
			set_reg16(0x3a, (cdd.index < cdd.toc.last) ? lut_BCD_16[cdd.index + 1] : 0x0A0A);
			set_reg16(0x3c, 0x0000);
			set_reg16(0x3e, 0x0000); /* Disk Control Code (?) in RS6 */
			Pico_mcd->s68k_regs[0x40 + 0] = 0x00;
			break;
		}

		case 0x03: /* Total length (MM:SS:FF) */
		{
			int lba = cdd.toc.end + 150;
			set_reg16(0x38, (cdd.status << 8) | 0x03);
			set_reg16(0x3a, lut_BCD_16[(lba / 75) / 60]);
			set_reg16(0x3c, lut_BCD_16[(lba / 75) % 60]);
			set_reg16(0x3e, lut_BCD_16[(lba % 75)]);
			Pico_mcd->s68k_regs[0x40 + 0] = 0x00;
			break;
		}

		case 0x04: /* First & Last Track Numbers */
		{
			set_reg16(0x38, (cdd.status << 8) | 0x04);
			set_reg16(0x3a, 0x0001);
			set_reg16(0x3c, lut_BCD_16[cdd.toc.last]);
			set_reg16(0x3e, 0x0000);			  /* Drive Version (?) in RS6-RS7 */
			Pico_mcd->s68k_regs[0x40 + 0] = 0x00; /* Lead-In flags in RS8 (bit0 = mute status, bit1: pre-emphasis status, bit2: track type) */
			break;
		}

		case 0x05: /* Track Start Time (MM:SS:FF) */
		{
			int track = Pico_mcd->s68k_regs[0x46 + 0] * 10 + Pico_mcd->s68k_regs[0x46 + 1];
			int lba = cdd.toc.tracks[track - 1].start + 150;
			set_reg16(0x38, (cdd.status << 8) | 0x05);
			set_reg16(0x3a, lut_BCD_16[(lba / 75) / 60]);
			set_reg16(0x3c, lut_BCD_16[(lba / 75) % 60]);
			set_reg16(0x3e, lut_BCD_16[(lba % 75)]);
			Pico_mcd->s68k_regs[0x40 + 0] = track % 10; /* Track Number (low digit) */
			if (track == 1)
			{
				/* RS6 bit 3 is set for the first (DATA) track */
				Pico_mcd->s68k_regs[0x3e + 0] |= 0x08;
			}
			break;
		}

		default:
		{
#ifdef LOG_ERROR
			error("Unknown CDD Command %02X (%X)\n", Pico_mcd->s68k_regs[0x44 + 1], s68k.pc);
#endif
			return;
		}
		}
		break;
	}

	case 0x03: /* Play  */
	{
		/* reset track index */
		int index = 0;

		/* new LBA position */
		int lba = ((Pico_mcd->s68k_regs[0x44 + 0] * 10 + Pico_mcd->s68k_regs[0x44 + 1]) * 60 +
				   (Pico_mcd->s68k_regs[0x46 + 0] * 10 + Pico_mcd->s68k_regs[0x46 + 1])) *
					  75 +
				  (Pico_mcd->s68k_regs[0x48 + 0] * 10 + Pico_mcd->s68k_regs[0x48 + 1]) - 150;

		/* CD drive latency */
		if (!cdd.latency)
		{
			/* Fixes a few games hanging during intro because they expect data to be read with some delay */
			/* Radical Rex needs at least one interrupt delay */
			/* Wolf Team games (Anet Futatabi, Cobra Command, Road Avenger & Time Gal) need at least 6 interrupts delay  */
			/* Space Adventure Cobra (2nd morgue scene) needs at least 13 interrupts delay (incl. seek time, so 6 is OK) */
			/* Jeopardy & ESPN Sunday Night NFL are picky about this as well: 10 interrupts delay (+ seek time) seems OK */
			cdd.latency = 10;
		}

		/* CD drive seek time */
		/* max. seek time = 1.5 s = 1.5 x 75 = 112.5 CDD interrupts (rounded to 120) for 270000 sectors max on disc. */
		/* Note: This is only a rough approximation since, on real hardware, seek time is much likely not linear and */
		/* latency much larger than above value, but this model works fine for Sonic CD (track 26 playback needs to  */
		/* be enough delayed to start in sync with intro sequence, as compared with real hardware recording).        */
		if (lba > cdd.lba)
		{
			cdd.latency += (((lba - cdd.lba) * 120) / 270000);
		}
		else
		{
			cdd.latency += (((cdd.lba - lba) * 120) / 270000);
		}

		/* update current LBA */
		cdd.lba = lba;

		/* get track index */
		while ((cdd.toc.tracks[index].end <= lba) && (index < cdd.toc.last))
			index++;

		/* update current track index */
		cdd.index = index;

		/* stay within track limits when seeking files */
		if (lba < cdd.toc.tracks[index].start)
		{
			lba = cdd.toc.tracks[index].start;
		}

		/* seek to current block */
		if (!index)
		{
			/* DATA track */
		}
#if 0
      else if (cdd.toc.tracks[index].fd)
      {
        /* PCM AUDIO track */
        fseek(cdd.toc.tracks[index].fd, (lba * 2352) - cdd.toc.tracks[index].offset, SEEK_SET);
      }
#else
		else
		{
			cdd_change_track(index, lba);
		}
#endif

		/* no audio track playing (yet) */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* update status */
		cdd.status = CD_PLAY;

		/* return track index in RS2-RS3 */
		set_reg16(0x38, (CD_PLAY << 8) | 0x02);
		set_reg16(0x3a, (cdd.index < cdd.toc.last) ? lut_BCD_16[index + 1] : 0x0A0A);
		set_reg16(0x3c, 0x0000);
		set_reg16(0x3e, 0x0000);
		Pico_mcd->s68k_regs[0x40 + 0] = 0x00;
		break;
	}

	case 0x04: /* Seek */
	{
		/* reset track index */
		int index = 0;

		/* new LBA position */
		int lba = ((Pico_mcd->s68k_regs[0x44 + 0] * 10 + Pico_mcd->s68k_regs[0x44 + 1]) * 60 +
				   (Pico_mcd->s68k_regs[0x46 + 0] * 10 + Pico_mcd->s68k_regs[0x46 + 1])) *
					  75 +
				  (Pico_mcd->s68k_regs[0x48 + 0] * 10 + Pico_mcd->s68k_regs[0x48 + 1]) - 150;

		/* CD drive seek time  */
		/* We are using similar linear model as above, although still not exactly accurate, */
		/* it works fine for Switch/Panic! intro (Switch needs at least 30 interrupts while */
		/* seeking from 00:05:63 to 24:03:19, Panic! when seeking from 00:05:60 to 24:06:07) */
		if (lba > cdd.lba)
		{
			cdd.latency = ((lba - cdd.lba) * 120) / 270000;
		}
		else
		{
			cdd.latency = ((cdd.lba - lba) * 120) / 270000;
		}

		/* update current LBA */
		cdd.lba = lba;

		/* get current track index */
		while ((cdd.toc.tracks[index].end <= lba) && (index < cdd.toc.last))
			index++;

		/* update current track index */
		cdd.index = index;

		/* stay within track limits */
		if (lba < cdd.toc.tracks[index].start)
		{
			lba = cdd.toc.tracks[index].start;
		}

		/* seek to current block */
		if (!index)
		{
			/* DATA track */
		}
#if 0
      else if (cdd.toc.tracks[index].fd)
      {
        /* PCM AUDIO track */
        fseek(cdd.toc.tracks[index].fd, (lba * 2352) - cdd.toc.tracks[index].offset, SEEK_SET);
      }
#endif

		/* no audio track playing */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* update status */
		cdd.status = CD_SEEK;

		/* unknown RS1-RS8 values (returning 0xF in RS1 invalidates track infos in RS2-RS8 and fixes Final Fight CD intro when seek time is emulated) */
		set_reg16(0x38, (CD_SEEK << 8) | 0x0f);
		set_reg16(0x3a, 0x0000);
		set_reg16(0x3c, 0x0000);
		set_reg16(0x3e, 0x0000);
		set_reg16(0x40, ~(CD_SEEK + 0xf) & 0x0f);
		return;
	}

	case 0x06: /* Pause */
	{
		/* no audio track playing */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* update status (RS1-RS8 unchanged) */
		cdd.status = Pico_mcd->s68k_regs[0x38 + 0] = CD_READY;
		break;
	}

	case 0x07: /* Resume */
	{
		/* update status (RS1-RS8 unchanged) */
		cdd.status = Pico_mcd->s68k_regs[0x38 + 0] = CD_PLAY;
		break;
	}

	case 0x08: /* Forward Scan */
	{
		/* reset scanning direction / speed */
		cdd.scanOffset = CD_SCAN_SPEED;

		/* update status (RS1-RS8 unchanged) */
		cdd.status = Pico_mcd->s68k_regs[0x38 + 0] = CD_SCAN;
		break;
	}

	case 0x09: /* Rewind Scan */
	{
		/* reset scanning direction / speed */
		cdd.scanOffset = -CD_SCAN_SPEED;

		/* update status (RS1-RS8 unchanged) */
		cdd.status = Pico_mcd->s68k_regs[0x38 + 0] = CD_SCAN;
		break;
	}

	case 0x0a: /* N-Track Jump Control ? (usually sent before CD_SEEK or CD_PLAY commands) */
	{
		/* TC3 corresponds to seek direction (00=forward, FF=reverse) */
		/* TC4-TC7 are related to seek length (4x4 bits i.e parameter values are between -65535 and +65535) */
		/* Maybe related to number of auto-sequenced track jumps/moves for CD DSP (cf. CXD2500BQ datasheet) */
		/* also see US Patent nr. 5222054 for a detailled description of seeking operation using Track Jump */

		/* no audio track playing */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* update status (RS1-RS8 unchanged) */
		cdd.status = Pico_mcd->s68k_regs[0x38 + 0] = CD_READY;
		break;
	}

	case 0x0c: /* Close Tray */
	{
		/* no audio track playing */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* update status */
		cdd.status = cdd.loaded ? CD_STOP : NO_DISC;

		/* RS1-RS8 ignored, expects 0x0 ("no disc" ?) in RS0 once */
		set_reg16(0x38, 0x0000);
		set_reg16(0x3a, 0x0000);
		set_reg16(0x3c, 0x0000);
		set_reg16(0x3e, 0x0000);
		set_reg16(0x40, 0x000f);

		if (PicoMCDcloseTray)
			PicoMCDcloseTray();

		return;
	}

	case 0x0d: /* Open Tray */
	{
		/* no audio track playing */
		Pico_mcd->s68k_regs[0x36 + 0] = 0x01;

		/* update status (RS1-RS8 ignored) */
		cdd.status = CD_OPEN;
		set_reg16(0x38, CD_OPEN << 8);
		set_reg16(0x3a, 0x0000);
		set_reg16(0x3c, 0x0000);
		set_reg16(0x3e, 0x0000);
		set_reg16(0x40, ~CD_OPEN & 0x0f);

		if (PicoMCDopenTray)
			PicoMCDopenTray();
		return;
	}

	default: /* Unknown command */
#ifdef LOG_CDD
		error("Unknown CDD Command !!!\n");
#endif
		Pico_mcd->s68k_regs[0x38 + 0] = cdd.status;
		break;
	}

	/* only compute checksum when necessary */
	Pico_mcd->s68k_regs[0x40 + 1] =
		~(Pico_mcd->s68k_regs[0x38 + 0] + Pico_mcd->s68k_regs[0x38 + 1] +
		  Pico_mcd->s68k_regs[0x3a + 0] + Pico_mcd->s68k_regs[0x3a + 1] +
		  Pico_mcd->s68k_regs[0x3c + 0] + Pico_mcd->s68k_regs[0x3c + 1] +
		  Pico_mcd->s68k_regs[0x3e + 0] + Pico_mcd->s68k_regs[0x3e + 1] +
		  Pico_mcd->s68k_regs[0x40 + 0]) &
		0x0f;
}
