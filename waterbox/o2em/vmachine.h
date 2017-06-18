#ifndef VMACHINE_H
#define VMACHINE_H

#include "types.h"

#define LINECNT 21
#define MAXLINES 500
#define MAXSNAP 50

#define VBLCLK 5493
#define EVBLCLK_NTSC 5964
#define EVBLCLK_PAL 7259

#define FPS_NTSC 60
#define FPS_PAL 50

extern int last_line;

extern int evblclk;

extern int master_clk;		/* Master clock */
extern int int_clk;		/* counter for length of /INT pulses for JNI */
extern int h_clk;   /* horizontal clock */
extern Byte coltab[256];
extern int mstate;

extern Byte rom_table[8][4096];
extern Byte intRAM[];
extern Byte extRAM[];
extern Byte extROM[];
extern Byte VDCwrite[256];
extern Byte ColorVector[MAXLINES];
extern Byte AudioVector[MAXLINES];
extern Byte *rom;
extern Byte *megarom;

extern int frame;
extern int key2[128];
extern int key2vcnt;
extern unsigned long clk_counter;

extern int enahirq;
extern int pendirq;
extern int useforen;
extern long regionoff;
extern int sproff;
extern int tweakedaudio;

Byte read_P2(void);
int snapline(int pos, Byte reg, int t);
void ext_write(Byte dat, ADDRESS adr);
Byte ext_read(ADDRESS adr);
void handle_vbl(void);
void handle_evbl(void);
void handle_evbll(void);
Byte in_bus(void);
void write_p1(Byte d);
Byte read_t1(void);
void init_system(void);
void init_roms(void);
void run(void);

extern struct resource {
	int bank;
	int speed;
	int voice;
	int exrom;
	int three_k;
	int euro;
	int openb;
	int megaxrom;
	int vpp;
	int bios;
	uint32_t crc;
	int scoretype;
	int scoreaddress;
	int default_highscore;
} app_data;


#endif  /* VMACHINE_H */

