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
#include "shared.h"
#include "../cinterface/callbacks.h"

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

static blip_t* blip[2];

typedef struct
{
	toc_t toc;
} frontendcd_t;

int cdd_load(const char *key, char *header)
{
	frontendcd_t fecd;
	char data[2048];
	int startoffs;


	int bytes = sizeof(frontendcd_t);
	if (load_archive(key, (unsigned char *)&fecd, bytes, NULL) != bytes)
		return 0;

	// look for valid header
	cdd_readcallback(0, data, 0);
	if (memcmp("SEGADISCSYSTEM", data, 14) == 0)
		startoffs = 0;
	else if (memcmp("SEGADISCSYSTEM", data + 16, 14) == 0)
		startoffs = 16;
	else
		return 0;
	// copy security block
	memcpy(header, data + startoffs, 0x210);

	// copy disk information
	memcpy(&cdd.toc, &fecd.toc, sizeof(toc_t));

	cdd.loaded = 1;
	return 1;
}

void cdd_init(blip_t* left, blip_t* right)
{
  /* CD-DA is running by default at 44100 Hz */
  /* Audio stream is resampled to desired rate using Blip Buffer */
  blip[0] = left;
  blip[1] = right;
  blip_set_rates(left, 44100, snd.sample_rate);
  blip_set_rates(right, 44100, snd.sample_rate);
}

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
  cdd.status = cdd.loaded ? CD_STOP : NO_DISC;

  /* reset CD-DA fader (full volume) */
  cdd.volume = 0x400;

  /* clear CD-DA output */
  cdd.audio[0] = cdd.audio[1] = 0;
}

void cdd_unload(void)
{
  cdd.loaded = 0;
  cdd_readcallback = NULL;

  /* reset TOC */
  memset(&cdd.toc, 0x00, sizeof(cdd.toc));
}

void cdd_hotswap(const toc_t *toc)
{
	if (toc)
	{
		cdd.loaded = 1;
		memcpy(&cdd.toc, toc, sizeof(cdd.toc));
	}
	else
	{
		cdd.loaded = 0;
		memset(&cdd.toc, 0x00, sizeof(cdd.toc));
	}

	cdd_reset();
}

void cdd_read_data(uint8 *dst)
{
  /* only read DATA track sectors */
  if ((cdd.lba >= 0) && (cdd.lba < cdd.toc.tracks[0].end))
  {
	cdd_readcallback(cdd.lba, dst, 0);
  }
}

void cdd_read_audio(unsigned int samples)
{
  // previous audio outputs //
  int16 l = cdd.audio[0];
  int16 r = cdd.audio[1];

  // get number of internal clocks (samples) needed //
  samples = blip_clocks_needed(blip[0], samples);

  // audio track playing ? //
  if (!scd.regs[0x36>>1].byte.h)
  {
    int i, mul, delta;

    // current CD-DA fader volume //
    int curVol = cdd.volume;

    // CD-DA fader volume setup (0-1024) //
    int endVol = scd.regs[0x34>>1].w >> 4;

    // read samples from current block //
    {
#ifdef LSB_FIRST
      int16 *ptr = (int16 *) (cdc.ram);
#else
      uint8 *ptr = cdc.ram;
#endif
	  {
		char scratch[2352];
		// copy the end of current sector
	    int nsampreq = samples;
		unsigned char *dest = cdc.ram;
		cdd_readcallback(cdd.sampleLba, scratch, 1);
		memcpy(cdc.ram, scratch + cdd.sampleOffset * 4, 2352 - cdd.sampleOffset * 4);
		cdd.sampleLba++;
		nsampreq -= 588 - cdd.sampleOffset;
		dest += 2352 - cdd.sampleOffset * 4;
		cdd.sampleOffset = 0;
		// fill full sectors
		while (nsampreq >= 588)
		{
		  cdd_readcallback(cdd.sampleLba, scratch, 1);
		  memcpy(dest, scratch, 2352);
		  cdd.sampleLba++;
		  nsampreq -= 588;
		  dest += 2352;
		}
		// do last partial sector
		if (nsampreq > 0)
		{
		  cdd_readcallback(cdd.sampleLba, scratch, 1);
		  memcpy(dest, scratch, nsampreq * 4);
		  cdd.sampleOffset = nsampreq;
		  dest += nsampreq * 4;
		  nsampreq = 0;
		}
	    //printf("samples: %i\n", samples);
		//memset(cdc.ram, 0, samples * 4);
        //fread(cdc.ram, 1, samples * 4, cdd.toc.tracks[cdd.index].fd);
	  }

      // process 16-bit (little-endian) stereo samples //
      for (i=0; i<samples; i++)
      {
        // CD-DA fader multiplier (cf. LC7883 datasheet) //
        // (MIN) 0,1,2,3,4,8,12,16,20...,1020,1024 (MAX) //
        mul = (curVol & 0x7fc) ? (curVol & 0x7fc) : (curVol & 0x03);

        // left channel //
  #ifdef LSB_FIRST
        delta = ((ptr[0] * mul) / 1024) - l;
        ptr++;
  #else
        delta = (((int16)((ptr[0] + ptr[1]*256)) * mul) / 1024) - l;
        ptr += 2;
  #endif
        l += delta;
        blip_add_delta_fast(blip[0], i, delta);

        // right channel //
  #ifdef LSB_FIRST
        delta = ((ptr[0] * mul) / 1024) - r;
        ptr++;
  #else
        delta = (((int16)((ptr[0] + ptr[1]*256)) * mul) / 1024) - r;
        ptr += 2;
  #endif
        r += delta;
        blip_add_delta_fast(blip[1], i, delta);

        // update CD-DA fader volume (one step/sample) //
        if (curVol < endVol)
        {
          // fade-in //
          curVol++;
        }
        else if (curVol > endVol)
        {
          // fade-out //
          curVol--;
        }
        else if (!curVol)
        {
          // audio will remain muted until next setup //
          break;
        }
      }
    }

    // save current CD-DA fader volume //
    cdd.volume = curVol;

    // save last audio output for next frame //
    cdd.audio[0] = l;
    cdd.audio[1] = r;
  }
  else
  {
    // no audio output //
    if (l) blip_add_delta_fast(blip[0], 0, -l);
    if (r) blip_add_delta_fast(blip[1], 0, -r);

    // save audio output for next frame //
    cdd.audio[0] = 0;
    cdd.audio[1] = 0;
  }

  // end of Blip Buffer timeframe //
  blip_end_frame(blip[0], samples);
  blip_end_frame(blip[1], samples);
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
      cdd.lba += cdc_decoder_update(*(uint32 *)(header));
    }
    else if (cdd.index < cdd.toc.last)
    {
      /* check against audio track start index */
      if (cdd.lba >= cdd.toc.tracks[cdd.index].start)
      {
        /* audio track playing */
		// if it wasn't before, set the audio start position
		if (scd.regs[0x36>>1].byte.h)
		{
		  cdd.sampleLba = cdd.lba + 1;
		  cdd.sampleOffset = 0;
		}
        scd.regs[0x36>>1].byte.h = 0x00;
      }

      /* audio blocks are still sent to CDC as well as CD DAC/Fader */
      cdc_decoder_update(0);

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
      scd.regs[0x36>>1].byte.h = 0x01;
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
      	scd.regs[0x36>>1].byte.h = 0x00;
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
      scd.regs[0x36>>1].byte.h = 0x01;

      /* end of disc */
      cdd.index = cdd.toc.last;
      cdd.lba = cdd.toc.end;
      cdd.status = CD_END;
      return;
    }
  }
}

void cdd_process(void)
{
  /* Process CDD command */
  switch (scd.regs[0x42>>1].byte.h & 0x0f)
  {
    case 0x00:  /* Drive Status */
    {
      /* RS1-RS8 normally unchanged */
      scd.regs[0x38>>1].byte.h = cdd.status;

      /* unless RS1 indicated invalid track infos */
      if (scd.regs[0x38>>1].byte.l == 0x0f)
      {
        /* and SEEK has ended */
        if (cdd.status != CD_SEEK)
        {
          /* then return valid track infos, e.g current track number in RS2-RS3 (fixes Lunar - The Silver Star) */
          scd.regs[0x38>>1].byte.l = 0x02;
          scd.regs[0x3a>>1].w = (cdd.index < cdd.toc.last) ? lut_BCD_16[cdd.index + 1] : 0x0A0A;
        }
      }
      break;
    }

    case 0x01:  /* Stop Drive */
    {
      /* update status */
      cdd.status = cdd.loaded ? CD_STOP : NO_DISC;

      /* no audio track playing */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* RS1-RS8 ignored, expects 0x0 ("no disc" ?) in RS0 once */
      scd.regs[0x38>>1].w = 0x0000;
      scd.regs[0x3a>>1].w = 0x0000;
      scd.regs[0x3c>>1].w = 0x0000;
      scd.regs[0x3e>>1].w = 0x0000;
      scd.regs[0x40>>1].w = 0x000f;
      return;
    }

    case 0x02:  /* Read TOC */
    {
      /* Infos automatically retrieved by CDD processor from Q-Channel */
      /* commands 0x00-0x02 (current block) and 0x03-0x05 (Lead-In) */
      switch (scd.regs[0x44>>1].byte.l)
      {
        case 0x00:  /* Current Absolute Time (MM:SS:FF) */
        {
          int lba = cdd.lba + 150;
          scd.regs[0x38>>1].w = cdd.status << 8;
          scd.regs[0x3a>>1].w = lut_BCD_16[(lba/75)/60];
          scd.regs[0x3c>>1].w = lut_BCD_16[(lba/75)%60];
          scd.regs[0x3e>>1].w = lut_BCD_16[(lba%75)];
          scd.regs[0x40>>1].byte.h = cdd.index ? 0x00 : 0x04; /* Current block flags in RS8 (bit0 = mute status, bit1: pre-emphasis status, bit2: track type) */
          break;
        }

        case 0x01:  /* Current Track Relative Time (MM:SS:FF) */
        {
          int lba = cdd.lba - cdd.toc.tracks[cdd.index].start;
          scd.regs[0x38>>1].w = (cdd.status << 8) | 0x01;
          scd.regs[0x3a>>1].w = lut_BCD_16[(lba/75)/60];
          scd.regs[0x3c>>1].w = lut_BCD_16[(lba/75)%60];
          scd.regs[0x3e>>1].w = lut_BCD_16[(lba%75)];
          scd.regs[0x40>>1].byte.h = cdd.index ? 0x00 : 0x04; /* Current block flags in RS8 (bit0 = mute status, bit1: pre-emphasis status, bit2: track type) */
          break;
        }

        case 0x02:  /* Current Track Number */
        {
          scd.regs[0x38>>1].w = (cdd.status << 8) | 0x02;
          scd.regs[0x3a>>1].w = (cdd.index < cdd.toc.last) ? lut_BCD_16[cdd.index + 1] : 0x0A0A;
          scd.regs[0x3c>>1].w = 0x0000;
          scd.regs[0x3e>>1].w = 0x0000; /* Disk Control Code (?) in RS6 */
          scd.regs[0x40>>1].byte.h = 0x00;
          break;
        }

        case 0x03:  /* Total length (MM:SS:FF) */
        {
          int lba = cdd.toc.end + 150;
          scd.regs[0x38>>1].w = (cdd.status << 8) | 0x03;
          scd.regs[0x3a>>1].w = lut_BCD_16[(lba/75)/60];
          scd.regs[0x3c>>1].w = lut_BCD_16[(lba/75)%60];
          scd.regs[0x3e>>1].w = lut_BCD_16[(lba%75)];
          scd.regs[0x40>>1].byte.h = 0x00;
          break;
        }

        case 0x04:  /* First & Last Track Numbers */
        {
          scd.regs[0x38>>1].w = (cdd.status << 8) | 0x04;
          scd.regs[0x3a>>1].w = 0x0001;
          scd.regs[0x3c>>1].w = lut_BCD_16[cdd.toc.last];
          scd.regs[0x3e>>1].w = 0x0000; /* Drive Version (?) in RS6-RS7 */
          scd.regs[0x40>>1].byte.h = 0x00;  /* Lead-In flags in RS8 (bit0 = mute status, bit1: pre-emphasis status, bit2: track type) */
          break;
        }

        case 0x05:  /* Track Start Time (MM:SS:FF) */
        {
          int track = scd.regs[0x46>>1].byte.h * 10 + scd.regs[0x46>>1].byte.l;
          int lba = cdd.toc.tracks[track-1].start + 150;
          scd.regs[0x38>>1].w = (cdd.status << 8) | 0x05;
          scd.regs[0x3a>>1].w = lut_BCD_16[(lba/75)/60];
          scd.regs[0x3c>>1].w = lut_BCD_16[(lba/75)%60];
          scd.regs[0x3e>>1].w = lut_BCD_16[(lba%75)];
          scd.regs[0x40>>1].byte.h = track % 10;  /* Track Number (low digit) */
          if (track == 1)
          {
            /* RS6 bit 3 is set for the first (DATA) track */
            scd.regs[0x3e>>1].byte.h |= 0x08;
          }
          break;
        }

        default:
        {
#ifdef LOG_ERROR
          error("Unknown CDD Command %02X (%X)\n", scd.regs[0x44>>1].byte.l, s68k.pc);
#endif
          return;
        }
      }
      break;
    }

    case 0x03:  /* Play  */
    {
      /* reset track index */
      int index = 0;

      /* new LBA position */
      int lba = ((scd.regs[0x44>>1].byte.h * 10 + scd.regs[0x44>>1].byte.l) * 60 +
                 (scd.regs[0x46>>1].byte.h * 10 + scd.regs[0x46>>1].byte.l)) * 75 +
                 (scd.regs[0x48>>1].byte.h * 10 + scd.regs[0x48>>1].byte.l) - 150;

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
      while ((cdd.toc.tracks[index].end <= lba) && (index < cdd.toc.last)) index++;

      /* update current track index */
      cdd.index = index;

      /* no audio track playing (yet) */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* update status */
      cdd.status = CD_PLAY;

      /* return track index in RS2-RS3 */
      scd.regs[0x38>>1].w = (CD_PLAY << 8) | 0x02;
      scd.regs[0x3a>>1].w = (cdd.index < cdd.toc.last) ? lut_BCD_16[index + 1] : 0x0A0A;
      scd.regs[0x3c>>1].w = 0x0000;
      scd.regs[0x3e>>1].w = 0x0000;
      scd.regs[0x40>>1].byte.h = 0x00;
      break;
    }

    case 0x04:  /* Seek */
    {
      /* reset track index */
      int index = 0;

      /* new LBA position */
      int lba = ((scd.regs[0x44>>1].byte.h * 10 + scd.regs[0x44>>1].byte.l) * 60 +
                 (scd.regs[0x46>>1].byte.h * 10 + scd.regs[0x46>>1].byte.l)) * 75 +
                 (scd.regs[0x48>>1].byte.h * 10 + scd.regs[0x48>>1].byte.l) - 150;

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
      while ((cdd.toc.tracks[index].end <= lba) && (index < cdd.toc.last)) index++;

      /* update current track index */
      cdd.index = index;

      /* no audio track playing */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* update status */
      cdd.status = CD_SEEK;

      /* unknown RS1-RS8 values (returning 0xF in RS1 invalidates track infos in RS2-RS8 and fixes Final Fight CD intro when seek time is emulated) */
      scd.regs[0x38>>1].w = (CD_SEEK << 8) | 0x0f;
      scd.regs[0x3a>>1].w = 0x0000;
      scd.regs[0x3c>>1].w = 0x0000;
      scd.regs[0x3e>>1].w = 0x0000;
      scd.regs[0x40>>1].w = ~(CD_SEEK + 0xf) & 0x0f;
      return;
    }

    case 0x06:  /* Pause */
    {
      /* no audio track playing */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* update status (RS1-RS8 unchanged) */
      cdd.status = scd.regs[0x38>>1].byte.h = CD_READY;
      break;
    }

    case 0x07:  /* Resume */
    {
      /* update status (RS1-RS8 unchanged) */
      cdd.status = scd.regs[0x38>>1].byte.h = CD_PLAY;
      break;
    }

    case 0x08:  /* Forward Scan */
    {
      /* reset scanning direction / speed */
      cdd.scanOffset = CD_SCAN_SPEED;

      /* update status (RS1-RS8 unchanged) */
      cdd.status = scd.regs[0x38>>1].byte.h = CD_SCAN;
      break;
    }

    case 0x09:  /* Rewind Scan */
    {
      /* reset scanning direction / speed */
      cdd.scanOffset = -CD_SCAN_SPEED;

      /* update status (RS1-RS8 unchanged) */
      cdd.status = scd.regs[0x38>>1].byte.h = CD_SCAN;
      break;
    }


    case 0x0a:  /* N-Track Jump Control ? (usually sent before CD_SEEK or CD_PLAY commands) */
    {
      /* TC3 corresponds to seek direction (00=forward, FF=reverse) */
      /* TC4-TC7 are related to seek length (4x4 bits i.e parameter values are between -65535 and +65535) */
      /* Maybe related to number of auto-sequenced track jumps/moves for CD DSP (cf. CXD2500BQ datasheet) */
      /* also see US Patent nr. 5222054 for a detailled description of seeking operation using Track Jump */

      /* no audio track playing */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* update status (RS1-RS8 unchanged) */
      cdd.status = scd.regs[0x38>>1].byte.h = CD_READY;
      break;
    }

    case 0x0c:  /* Close Tray */
    {
      /* no audio track playing */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* update status */
      cdd.status = cdd.loaded ? CD_STOP : NO_DISC;

      /* RS1-RS8 ignored, expects 0x0 ("no disc" ?) in RS0 once */
      scd.regs[0x38>>1].w = 0x0000;
      scd.regs[0x3a>>1].w = 0x0000;
      scd.regs[0x3c>>1].w = 0x0000;
      scd.regs[0x3e>>1].w = 0x0000;
      scd.regs[0x40>>1].w = 0x000f;

#ifdef CD_TRAY_CALLBACK
      CD_TRAY_CALLBACK
#endif
      return;
    }

    case 0x0d:  /* Open Tray */
    {
      /* no audio track playing */
      scd.regs[0x36>>1].byte.h = 0x01;

      /* update status (RS1-RS8 ignored) */
      cdd.status = CD_OPEN;
      scd.regs[0x38>>1].w = CD_OPEN << 8;
      scd.regs[0x3a>>1].w = 0x0000;
      scd.regs[0x3c>>1].w = 0x0000;
      scd.regs[0x3e>>1].w = 0x0000;
      scd.regs[0x40>>1].w = ~CD_OPEN & 0x0f;

#ifdef CD_TRAY_CALLBACK
      CD_TRAY_CALLBACK
#endif
      return;
    }

    default:  /* Unknown command */
#ifdef LOG_CDD
      error("Unknown CDD Command !!!\n");
#endif
      scd.regs[0x38>>1].byte.h = cdd.status;
      break;
  }

  /* only compute checksum when necessary */
  scd.regs[0x40>>1].byte.l = ~(scd.regs[0x38>>1].byte.h + scd.regs[0x38>>1].byte.l +
                               scd.regs[0x3a>>1].byte.h + scd.regs[0x3a>>1].byte.l +
                               scd.regs[0x3c>>1].byte.h + scd.regs[0x3c>>1].byte.l +
                               scd.regs[0x3e>>1].byte.h + scd.regs[0x3e>>1].byte.l +
                               scd.regs[0x40>>1].byte.h) & 0x0f;
}
