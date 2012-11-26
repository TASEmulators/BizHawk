/* SexyAL - Simple audio abstraction library.

Copyright (c) 2005-2007 Mednafen Team

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#include "sexyal.h"

#include <string.h>
#include "convert.h"

static inline uint16_t FLIP16(uint16_t b)
{
 return((b<<8)|((b>>8)&0xFF));
}

static inline uint32_t FLIP32(uint32_t b)
{
 return( (b<<24) | ((b>>8)&0xFF00) | ((b<<8)&0xFF0000) | ((b>>24)&0xFF) );
}
#include <stdlib.h>
template<typename dsf_t, uint32_t dsf>
static inline dsf_t SAMP_CONVERT(int16_t in_sample)
{
 if(dsf == SEXYAL_FMT_PCMU8)
  return((in_sample + 32768 /*+ (rand() & 127)*/ ) >> 8);

 if(dsf == SEXYAL_FMT_PCMS8)
  return(in_sample >> 8);

 if(dsf == SEXYAL_FMT_PCMU16)
  return(in_sample + 32768);

 if(dsf == SEXYAL_FMT_PCMS16)
  return(in_sample);

 if(dsf == SEXYAL_FMT_PCMU24)
  return((in_sample + 32768) << 8);

 if(dsf == SEXYAL_FMT_PCMS24)
  return(in_sample << 8);

 if(dsf == SEXYAL_FMT_PCMU32)
  return((uint32_t)(in_sample + 32768) << 16);

 if(dsf == SEXYAL_FMT_PCMS32)
  return(in_sample << 16);

 if(dsf == SEXYAL_FMT_PCMFLOAT)
  return((float)in_sample / 32768);
}


template<typename dsf_t, uint32_t dsf>
static void ConvertLoop(const int16_t *src, dsf_t *dest, const int src_chan, const int dest_chan, int32_t frames)
{
 if(src_chan == 1 && dest_chan >= 2)
 {
  for(int i = 0; i < frames; i++)
  {
   dsf_t temp = SAMP_CONVERT<dsf_t, dsf>(*src);
   dest[0] = temp;
   dest[1] = temp;
   src += 1;
   dest += 2;

   for(int padc = 2; padc < dest_chan; padc++)
    *dest++ = SAMP_CONVERT<dsf_t, dsf>(0);
  }
 }
 else if(src_chan == 2 && dest_chan == 1)
 {
  for(int i = 0; i < frames; i++)
  {
   int32_t mt = (src[0] + src[1]) >> 1;

   dest[0] = SAMP_CONVERT<dsf_t, dsf>(mt);
   src += 2;
   dest += 1;
  }
 }
 else //if(src_chan == 2 && dest_chan >= 2)
 {
  for(int i = 0; i < frames; i++)
  {
   dest[0] = SAMP_CONVERT<dsf_t, dsf>(src[0]);
   dest[1] = SAMP_CONVERT<dsf_t, dsf>(src[1]);
   src += 2;
   dest += 2;
  }

  for(int padc = 2; padc < dest_chan; padc++)
   *dest++ = SAMP_CONVERT<dsf_t, dsf>(0);
 }
}


/* Only supports one input sample format right now:  SEXYAL_FMT_PCMS16 */
void SexiALI_Convert(const SexyAL_format *srcformat, const SexyAL_format *destformat, const void *vsrc, void *vdest, uint32_t frames)
{
 const int16_t *src = (int16_t *)vsrc;

 if(destformat->sampformat == srcformat->sampformat)
  if(destformat->channels == srcformat->channels)
   if(destformat->revbyteorder == srcformat->revbyteorder)
   {
    memcpy(vdest, vsrc, frames * (destformat->sampformat >> 4) * destformat->channels);
    return;
   }

 switch(destformat->sampformat)
 {
  case SEXYAL_FMT_PCMU8:
	ConvertLoop<uint8_t, SEXYAL_FMT_PCMU8>(src, (uint8_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMS8:
	ConvertLoop<int8_t, SEXYAL_FMT_PCMS8>(src, (int8_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMU16:
	ConvertLoop<uint16_t, SEXYAL_FMT_PCMU16>(src, (uint16_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMS16:
	ConvertLoop<int16_t, SEXYAL_FMT_PCMS16>(src, (int16_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMU24:
	ConvertLoop<uint32_t, SEXYAL_FMT_PCMU24>(src, (uint32_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMS24:
	ConvertLoop<int32_t, SEXYAL_FMT_PCMS24>(src, (int32_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMU32:
	ConvertLoop<uint32_t, SEXYAL_FMT_PCMU32>(src, (uint32_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMS32:
	ConvertLoop<int32_t, SEXYAL_FMT_PCMS32>(src, (int32_t*)vdest, srcformat->channels, destformat->channels, frames);
	break;

  case SEXYAL_FMT_PCMFLOAT:
	ConvertLoop<float, SEXYAL_FMT_PCMFLOAT>(src, (float*)vdest, srcformat->channels, destformat->channels, frames);
	break;
 }

 if(destformat->revbyteorder != srcformat->revbyteorder)
 {
  if((destformat->sampformat >> 4) == 2)
  {
   uint16_t *dest = (uint16_t *)vdest;
   for(uint32_t x = 0; x < frames * destformat->channels; x++)
   {
    *dest = FLIP16(*dest);
    dest++;
   }
  }
  else if((destformat->sampformat >> 4) == 4)
  {
   uint32_t *dest = (uint32_t *)vdest;
   for(uint32_t x = 0; x < frames * destformat->channels; x++)
   {
    *dest = FLIP32(*dest);
    dest++;
   }
  }

 }
}
