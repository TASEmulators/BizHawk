#ifndef _OSD_H
#define _OSD_H

#ifdef _MSC_VER
#include <stdio.h>
typedef unsigned char bool;
#define strncasecmp _strnicmp
#endif

#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#define MAX_INPUTS 8
#define MAX_KEYS 8
#define MAXPATHLEN 1024

#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif

#ifndef M_PI
#define M_PI 3.1415926535897932385
#endif

typedef struct 
{
  int8 device;
  uint8 port;
  uint8 padtype;
} t_input_config;

struct 
{
  char version[16];
  uint8 hq_fm;
  uint8 filter;
  uint8 psgBoostNoise;
  uint8 dac_bits;
  uint8 ym2413;
  uint8 mono;
  int16 psg_preamp;
  int16 fm_preamp;
  uint16 lp_range;
  int16 low_freq;
  int16 high_freq;
  int16 lg;
  int16 mg;
  int16 hg;
  uint8 system;
  uint8 region_detect;
  uint8 master_clock;
  uint8 vdp_mode;
  uint8 force_dtack;
  uint8 addr_error;
  uint8 bios;
  uint8 lock_on;
  uint8 overscan;
  uint8 ntsc;
  uint8 gg_extra;
  uint8 render;
  t_input_config input[MAX_INPUTS];
} config;

extern char GG_ROM[256];
extern char AR_ROM[256];
extern char SK_ROM[256];
extern char SK_UPMEM[256];
extern char GG_BIOS[256];
extern char CD_BIOS_EU[256];
extern char CD_BIOS_US[256];
extern char CD_BIOS_JP[256];
extern char MS_BIOS_US[256];
extern char MS_BIOS_EU[256];
extern char MS_BIOS_JP[256];

void osd_input_update(void);
int load_archive(const char *filename, unsigned char *buffer, int maxsize, char *extension);
void real_input_callback(void);

#endif /* _OSD_H */
