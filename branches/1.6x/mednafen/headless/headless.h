#ifndef _HEADLESS_H
#define _HEADLESS_H

//make sure this file gets included by everything
//configurations related to headlessness shall be in here

#ifdef LIBMEDNAHAWK
#define HEADLESS
#define WANT_RESAMPLER
#define WANT_PSF
#define WANT_DEINTERLACER

//in libmednahawk, we compile all cores together
//in libretro, we'd want to compile them individually and choose the appropriate WANT_CD
#define WANT_CDIF

#endif

#ifdef HEADLESS

#else

//headless features are enabled
#define WANT_MOVIE
#define WANT_AVDUMP
#define WANT_NETPLAY
#define WANT_AVDUMP
#define WANT_REWIND
#define WANT_DEARCHIVE
#define WANT_IPS
#define WANT_RESAMPLER

//features you may want to consider -D in a headless library's makefile
#define WANT_PSF
#define WANT_SOFTGUI
#define WANT_CDIF_MT

#endif

#endif