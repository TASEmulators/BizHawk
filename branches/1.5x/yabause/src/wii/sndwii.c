/*  Copyright 2008 Theo Berkau

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include <stdlib.h>
#include <string.h>
#include <ogcsys.h>
#include "../scsp.h"
#include "sndwii.h"

#define NUMSOUNDBLOCKS  4
#define SOUNDLEN (44100 / 50)
#define SOUNDTRUELEN (48000 / 50)
#define SOUNDBUFSIZE (SOUNDTRUELEN * NUMSOUNDBLOCKS * 2 * 2)

static u32 soundlen=SOUNDLEN;
static u32 soundtruelen=SOUNDTRUELEN;
static u32 soundbufsize=SOUNDBUFSIZE;

static u32 soundoffset=0;
static s16 *truesoundoffset=0;
volatile u32 soundpos;
static int issoundmuted;

typedef s16 sndbuf[SOUNDTRUELEN * 2];
sndbuf stereodata16[NUMSOUNDBLOCKS] ATTRIBUTE_ALIGN(32);

//////////////////////////////////////////////////////////////////////////////

static void StartDMA(void)
{
   AUDIO_StopDMA();
   soundpos++;
   if (soundpos >= NUMSOUNDBLOCKS)
      soundpos = 0;

   AUDIO_InitDMA((u32)stereodata16[soundpos], soundtruelen * 4);
   DCFlushRange((void *)stereodata16[soundpos], soundtruelen * 4);
   AUDIO_StartDMA();
}

//////////////////////////////////////////////////////////////////////////////

int SNDWiiInit()
{
   AUDIO_Init(NULL);
   AUDIO_SetDSPSampleRate(AI_SAMPLERATE_48KHZ);

   soundpos = 0;
   soundlen = 44100 / 60; // 60 for NTSC or 50 for PAL. Initially assume it's going to be NTSC.
   soundtruelen = 48000 / 60;
   truesoundoffset = stereodata16[0];

   memset(stereodata16, 0, SOUNDBUFSIZE);

   issoundmuted = 0;

   AUDIO_RegisterDMACallback(StartDMA);
   AUDIO_InitDMA((u32)stereodata16[soundpos], soundlen * 4);
   DCFlushRange((void *)stereodata16[soundpos], soundlen * 4);
   AUDIO_StartDMA();

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void SNDWiiDeInit()
{
}

//////////////////////////////////////////////////////////////////////////////

int SNDWiiReset()
{
   return 0;
}

//////////////////////////////////////////////////////////////////////////////

int SNDWiiChangeVideoFormat(int vertfreq)
{
   soundlen = 44100 / vertfreq;
   soundtruelen = 48000 / vertfreq;
   soundbufsize = soundlen * NUMSOUNDBLOCKS * 2 * 2;
   memset(stereodata16, 0, sizeof(stereodata16));
   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void ScspConvert32utoWiiAudio(s32 *srcL, s32 *srcR, s16 *dst, u32 len)
{   
   u32 i;
   u32 truelen = len * 48000 / 44100;
   u32 counter = 0;
   u32 inc = (1 << 20) - ((u32)((44100 / 60) << 20) / ((48000 / 60)+1));

   for (i = 0; i < truelen; i++)
   {
      // Left Channel
      if (*srcL > 0x7FFF) *dst = 0x7FFF;
      else if (*srcL < -0x8000) *dst = -0x8000;
      else *dst = *srcL;
      dst++;
      // Right Channel
      if (*srcR > 0x7FFF) *dst = 0x7FFF;
      else if (*srcR < -0x8000) *dst = -0x8000;
      else *dst = *srcR;
      dst++;
      if (counter < (1 << 20))
      {
         srcL++;
         srcR++;
      }
      else
         counter -= (1 << 20);
      counter += inc;
   }
}

//////////////////////////////////////////////////////////////////////////////

void SNDWiiUpdateAudio(u32 *leftchanbuffer, u32 *rightchanbuffer, u32 num_samples)
{
   u32 copy1size=0, copy2size=0;
   u32 temp;

   if (((soundlen * NUMSOUNDBLOCKS) - soundoffset) < num_samples)
   {
      copy1size = (soundlen * NUMSOUNDBLOCKS) - soundoffset;
      copy2size = num_samples - copy1size;
   }
   else
   {
      copy1size = num_samples;
      copy2size = 0;
   }

   temp = (soundoffset % soundlen) * 800 / 735;
   ScspConvert32utoWiiAudio((s32 *)leftchanbuffer, (s32 *)rightchanbuffer, (s16 *)&stereodata16[soundoffset / soundlen][temp], copy1size);

   if (copy2size)
      ScspConvert32utoWiiAudio((s32 *)leftchanbuffer + copy1size, (s32 *)rightchanbuffer + copy1size, (s16 *)stereodata16[0], copy2size);

   soundoffset += copy1size + copy2size;
   soundoffset %= (soundlen * NUMSOUNDBLOCKS);
}

//////////////////////////////////////////////////////////////////////////////

u32 SNDWiiGetAudioSpace()
{
   u32 freespace=0;

   if (soundoffset > (soundpos * soundlen))
      freespace = (soundlen * NUMSOUNDBLOCKS) - soundoffset + (soundpos * soundlen);
   else
      freespace = (soundpos * soundlen) - soundoffset;

   return freespace;
}

//////////////////////////////////////////////////////////////////////////////

void SNDWiiMuteAudio()
{
   issoundmuted = 1;
}

//////////////////////////////////////////////////////////////////////////////

void SNDWiiUnMuteAudio()
{
   issoundmuted = 0;
}

//////////////////////////////////////////////////////////////////////////////

void SNDWiiSetVolume(int volume)
{
}

//////////////////////////////////////////////////////////////////////////////

SoundInterface_struct SNDWII = {
SNDCORE_WII,
"Wii Sound Interface",
SNDWiiInit,
SNDWiiDeInit,
SNDWiiReset,
SNDWiiChangeVideoFormat,
SNDWiiUpdateAudio,
SNDWiiGetAudioSpace,
SNDWiiMuteAudio,
SNDWiiUnMuteAudio,
SNDWiiSetVolume
};
