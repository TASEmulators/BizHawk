#ifndef _HEADLESS_H
#define _HEADLESS_H

//make sure this file gets included by everything
//configurations related to headlessness shall be in here

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

//features you may want to consider -D in a headless library's makefile
#define WANT_PSF
#define WANT_SOFTGUI
#define WANT_CDIF_MT

#endif

#endif