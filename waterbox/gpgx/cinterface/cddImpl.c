#include "shared.h"
#include "megasd.h"
#include <callbacks.h>

static int sampleLba = 0;
static int sampleOffset = 0;
static int tocLba = 0;

void cdd_reset(void)
{
  /* reset drive access latency */
  cdd.latency = 0;

  /* reset track index */
  cdd.index = 0;

  /* reset logical block address */
  cdd.lba = 0;

  /* reset logical block address for audio*/
  sampleLba = 0;
  sampleOffset = 0;

  /* reset logical block address for Toc*/
  tocLba = 0;

  /* reset status */
  cdd.status = cdd.loaded ? CD_TOC : NO_DISC;
  
  /* reset CD-DA fader (full volume) */
  cdd.fader[0] = cdd.fader[1] = 0x400;

  /* clear CD-DA output */
  cdd.audio[0] = cdd.audio[1] = 0;
}

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

     /* Valid CD-ROM Mode 1 track found ? */
    if (cdd.toc.tracks[0].type == TYPE_MODE1)
    {
      /* simulate audio tracks if none found */
      if (cdd.toc.last == 1)
      {
        /* some games require exact TOC infos */
        if (strstr(header + 0x180,"T-95035") != NULL)
        {
          /* Snatcher */
          cdd.toc.last = cdd.toc.end = 0;
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + toc_snatcher[cdd.toc.last];
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while (cdd.toc.last < 21);
        }
        else if (strstr(header + 0x180,"T-127015") != NULL)
        {
          /* Lunar - The Silver Star */
          cdd.toc.last = cdd.toc.end = 0;
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + toc_lunar[cdd.toc.last];
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while (cdd.toc.last < 52);
        }
        else if (strstr(header + 0x180,"T-113045") != NULL)
        {
          /* Shadow of the Beast II */
          cdd.toc.last = cdd.toc.end = 0;
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + toc_shadow[cdd.toc.last];
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while (cdd.toc.last < 15);
        }
        else if (strstr(header + 0x180,"T-143025") != NULL)
        {
          /* Dungeon Explorer */
          cdd.toc.last = cdd.toc.end = 0;
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + toc_dungeon[cdd.toc.last];
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while (cdd.toc.last < 13);
        }
        else if (strstr(header + 0x180,"MK-4410") != NULL)
        {
          /* Final Fight CD (USA, Europe) */
          cdd.toc.last = cdd.toc.end = 0;
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + toc_ffight[cdd.toc.last];
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while (cdd.toc.last < 26);
        }
        else if (strstr(header + 0x180,"G-6013") != NULL)
        {
          /* Final Fight CD (Japan) */
          cdd.toc.last = cdd.toc.end = 0;
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + toc_ffightj[cdd.toc.last];
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while (cdd.toc.last < 29);
        }
        else if (strstr(header + 0x180,"T-06201-01") != NULL)
        {
          /* Sewer Shark (USA) (REV1) */
          /* no audio track */
        }
        else
        {
          /* default TOC (99 tracks & 2s per audio tracks) */
          do
          {
            cdd.toc.tracks[cdd.toc.last].start = cdd.toc.end + 2*75;
            cdd.toc.tracks[cdd.toc.last].end = cdd.toc.tracks[cdd.toc.last].start + 2*75;
            cdd.toc.end = cdd.toc.tracks[cdd.toc.last].end;
            cdd.toc.last++;
          }
          while ((cdd.toc.last < 99) && (cdd.toc.end < 56*60*75));
        }
      }
    }

	cdd.loaded = 1;
	return 1;
}

void cdd_unload(void)
{
  cdd.loaded = 0;
  cdd_readcallback = NULL;

  /* reset TOC */
  memset(&cdd.toc, 0x00, sizeof(cdd.toc));
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
    cdd.status = CD_PAUSE;
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
		  sampleLba = cdd.lba + 1;
		  sampleOffset = 0;
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
	sampleLba += cdd.scanOffset;

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
		sampleLba = cdd.lba;
		sampleOffset = 0;
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


void cdd_read_data(uint8 *dst, uint8 *subheader)
{
  /* only allow reading (first) CD-ROM track sectors */
  if (cdd.toc.tracks[cdd.index].type && (cdd.lba >= 0))
  {
    /* check sector size */
    if (cdd.sectorSize == 2048)
    {
      /* read Mode 1 user data (2048 bytes) */
	  cdd_readcallback(cdd.lba, dst, 0);
    //   cdStreamSeek(trackStream[0], cdd.lba * 2048, SEEK_SET);
    //   cdStreamRead(dst, 2048, 1, trackStream[0]);
    }
    else
    {
      /* check if sub-header is required (Mode 2 sector only) */
      if (!subheader)
      {
        /* skip block sync pattern (12 bytes) + block header (4 bytes) then read Mode 1 user data (2048 bytes) */
		cdd_readcallback(cdd.lba, dst, 0);
        // cdStreamSeek(trackStream[0], (cdd.lba * 2352) + 12 + 4, SEEK_SET);
        // cdStreamRead(dst, 2048, 1, trackStream[0]);
      }
      else
      {
        /* skip block sync pattern (12 bytes) + block header (4 bytes) + Mode 2 sub-header (first 4 bytes) then read Mode 2 sub-header (last 4 bytes) */
		// Is this all handled by bizhawk?
		cdd_readcallback(cdd.lba, dst, 0);
        // cdStreamSeek(trackStream[0], (cdd.lba * 2352) + 12 + 4 + 4, SEEK_SET);
        // cdStreamRead(subheader, 4, 1, trackStream[0]);

        /* read Mode 2 user data (max 2328 bytes) */
        // cdStreamRead(dst, 2328, 1, trackStream[0]);
      }
    }
  }
}

void cdd_seek_audio(int index, int lba)
{
	cdd.index = index;
	sampleLba = lba;
//   /* seek to track position */
//   if (trackStream[index])
//   {
//     /* PCM AUDIO track */
//     cdStreamSeek(trackStream[index], (lba * 2352) - trackOffset[index], SEEK_SET);
//   }
}

void cdd_read_audio(unsigned int samples)
{
  /* previous audio outputs */
  int prev_l = cdd.audio[0];
  int prev_r = cdd.audio[1];

  /* audio track playing ? */
  if (!scd.regs[0x36>>1].byte.h)
  {
    int i, mul, l, r;

    /* current CD-DA fader volume */
    int curVol = cdd.fader[0];

    /* CD-DA fader volume setup (0-1024) */
    int endVol = cdd.fader[1];

    /* read samples from current block */
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
		cdd_readcallback(sampleLba, scratch, 1);
		memcpy(cdc.ram, scratch + sampleOffset * 4, 2352 - sampleOffset * 4);
		sampleLba++;
		nsampreq -= 588 - sampleOffset;
		dest += 2352 - sampleOffset * 4;
		sampleOffset = 0;
		// fill full sectors
		while (nsampreq >= 588)
		{
		  cdd_readcallback(sampleLba, scratch, 1);
		  memcpy(dest, scratch, 2352);
		  sampleLba++;
		  nsampreq -= 588;
		  dest += 2352;
		}
		// do last partial sector
		if (nsampreq > 0)
		{
		  cdd_readcallback(sampleLba, scratch, 1);
		  memcpy(dest, scratch, nsampreq * 4);
		  sampleOffset = nsampreq;
		  dest += nsampreq * 4;
		  nsampreq = 0;
		}
	    //printf("samples: %i\n", samples);
		//memset(cdc.ram, 0, samples * 4);
        //fread(cdc.ram, 1, samples * 4, cdd.toc.tracks[cdd.index].fd);
	  }

      /* process 16-bit (little-endian) stereo samples */
      for (i=0; i<samples; i++)
      {
        /* CD-DA fader multiplier (cf. LC7883 datasheet) */
        /* (MIN) 0,1,2,3,4,8,12,16,20...,1020,1024 (MAX) */
        mul = (curVol & 0x7fc) ? (curVol & 0x7fc) : (curVol & 0x03);

        /* left & right channels */
#ifdef LSB_FIRST
        l = ((ptr[0] * mul) / 1024);
        r = ((ptr[1] * mul) / 1024);
        ptr+=2;
#else
        l = (((int16)((ptr[0] + ptr[1]*256)) * mul) / 1024);
        r = (((int16)((ptr[2] + ptr[3]*256)) * mul) / 1024);
        ptr+=4;
#endif

        /* CD-DA output mixing volume (0-100%) */
        l = (l * config.cdda_volume) / 100;
        r = (r * config.cdda_volume) / 100;

        /* update blip buffer */
        blip_add_delta_fast(snd.blips[2], i, l-prev_l, r-prev_r);
        prev_l = l;
        prev_r = r;

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
    cdd.fader[0] = curVol;

    /* save last audio output for next frame */
    cdd.audio[0] = prev_l;
    cdd.audio[1] = prev_r;
  }
  else
  {
    /* no audio output */
    if (prev_l | prev_r)
    {
      /* update blip buffer */
      blip_add_delta_fast(snd.blips[2], 0, -prev_l, -prev_r);

      /* save audio output for next frame */
      cdd.audio[0] = 0;
      cdd.audio[1] = 0;
    }
  }

  /* end of blip buffer timeframe */
  blip_end_frame(snd.blips[2], samples);
}

void cdd_seek_toc(int lba)
{
	tocLba = lba;
//   if (tocStream == NULL) return;
//   cdStreamSeek(tocStream, cdd.lba * 96, SEEK_SET);
}

void cdd_read_toc(uint8 *dst, size_t size)
{
	if (size > 2560) { fprintf(stderr, "Excessive size requested (%lu) on cdd_read_toc()\n", size); exit(1); }
	uint8_t buffer[2560];
	cdd_readcallback(tocLba, buffer, 0);
	memcpy(dst, buffer, size);
//   if (tocStream == NULL) return;
//   cdStreamRead(dst, 1, size, tocStream);
}