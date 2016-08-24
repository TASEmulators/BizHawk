#include "shared.h"
#include "eeprom_93c.h"
#include "eq.h"



extern struct
{
  uint8 enabled;
  uint8 status;
  uint8 *rom;
  uint8 *ram;
  uint16 regs[13];
  uint16 old[4];
  uint16 data[4];
  uint32 addr[4];
} action_replay;

typedef struct
{
  uint8 address_bits;     // number of bits needed to address memory: 7, 8 or 16 //
  uint16 size_mask;       // depends on the max size of the memory (in bytes) //
  uint16 pagewrite_mask;  // depends on the maximal number of bytes that can be written in a single write cycle //
  uint32 sda_in_adr;      // 68000 memory address mapped to SDA_IN //
  uint32 sda_out_adr;     // 68000 memory address mapped to SDA_OUT //
  uint32 scl_adr;         // 68000 memory address mapped to SCL //
  uint8 sda_in_bit;       // bit offset for SDA_IN //
  uint8 sda_out_bit;      // bit offset for SDA_OUT //
  uint8 scl_bit;          // bit offset for SCL //
} T_CONFIG_I2C;

typedef enum
{
  STAND_BY = 0,
  WAIT_STOP,
  GET_SLAVE_ADR,
  GET_WORD_ADR_7BITS,
  GET_WORD_ADR_HIGH,
  GET_WORD_ADR_LOW,
  WRITE_DATA,
  READ_DATA
} T_STATE_I2C;

typedef struct
{
  uint8 sda;            // current /SDA line state //
  uint8 scl;            // current /SCL line state //
  uint8 old_sda;        // previous /SDA line state //
  uint8 old_scl;        // previous /SCL line state //
  uint8 cycles;         // current operation cycle number (0-9) //
  uint8 rw;             // operation type (1:READ, 0:WRITE) //
  uint16 slave_mask;    // device address (shifted by the memory address width)//
  uint16 word_address;  // memory address //
  T_STATE_I2C state;    // current operation state //
  T_CONFIG_I2C config;  // EEPROM characteristics for this game //
} T_EEPROM_I2C;

extern T_EEPROM_I2C eeprom_i2c;

typedef enum
{
  STANDBY,
  GET_OPCODE_,//
  GET_ADDRESS,
  WRITE_BYTE,
  READ_BYTE
} T_STATE_SPI;

typedef struct
{
  uint8 cs;           // !CS line state //
  uint8 clk;          // SCLK line state //
  uint8 out;          // SO line state //
  uint8 status;       // status register //
  uint8 opcode;       // 8-bit opcode //
  uint8 buffer;       // 8-bit data buffer //
  uint16 addr;        // 16-bit address //
  uint32 cycles;      // current operation cycle //
  T_STATE_SPI state;  // current operation state //
} T_EEPROM_SPI;

extern T_EEPROM_SPI spi_eeprom;

extern struct
{
  uint8 enabled;
  uint8 *rom;
  uint16 regs[0x20];
  uint16 old[6];
  uint16 data[6];
  uint32 addr[6];
} ggenie;

extern struct
{
  uint8 State;
  uint8 Counter;
} activator[2];

extern uint8 pad_index;

extern struct
{
  uint8 State;
  uint8 Port;
} lightgun;

extern struct
{
  uint8 State;
  uint8 Counter;
  uint8 Wait;
  uint8 Port;
} mouse;

extern struct
{
  uint8 State;
} paddle[2];

extern struct
{
  uint8 State;
  uint8 Counter;
} sportspad[2];

extern struct
{
  uint8 State;
  uint8 Counter;
  uint8 Table[12];
} teamplayer[2];

extern struct
{
  uint8 axis;
  uint8 busy;
} tablet;

extern struct
{
  uint8 State;
  uint8 Counter;
  uint8 Latency;
} xe_a1p[2];

typedef struct
{
  // Configuration //
  int PreAmp[4][2];       // stereo channels pre-amplification ratio (%) //
  int NoiseFeedback;
  int SRWidth;

  // PSG registers: //
  int Registers[8];       // Tone, vol x4 //
  int LatchedRegister;
  int NoiseShiftRegister;
  int NoiseFreq;          // Noise channel signal generator frequency //

  // Output calculation variables //
  int ToneFreqVals[4];    // Frequency register values (counters) //
  int ToneFreqPos[4];     // Frequency channel flip-flops //
  int Channel[4][2];      // current amplitude of each (stereo) channel //
  int ChanOut[4][2];      // current output value of each (stereo) channel //

  // Internal M-clock counter //
  unsigned long clocks;

} SN76489_Context;

extern SN76489_Context SN76489;

extern int fm_buffer[1080 * 2];
extern int fm_last[2];
extern int *fm_ptr;

// Cycle-accurate FM samples //
extern uint32 fm_cycles_ratio;
extern uint32 fm_cycles_start;
extern uint32 fm_cycles_count;

// YM chip function pointers //
extern void (*YM_Reset)(void);
extern void (*YM_Update)(int *buffer, int length);
extern void (*YM_Write)(unsigned int a, unsigned int v);

typedef struct 
{
  UINT32  ar;       // attack rate: AR<<2           //
  UINT32  dr;       // decay rate:  DR<<2           //
  UINT32  rr;       // release rate:RR<<2           //
  UINT8  KSR;       // key scale rate               //
  UINT8  ksl;       // keyscale level               //
  UINT8  ksr;       // key scale rate: kcode>>KSR   //
  UINT8  mul;       // multiple: mul_tab[ML]        //

  // Phase Generator //
  UINT32 phase;     // frequency counter            //
  UINT32 freq;      // frequency counter step       //
  UINT8 fb_shift;   // feedback shift value         //
  INT32 op1_out[2]; // slot1 output for feedback    //

  // Envelope Generator //
  UINT8  eg_type;   // percussive/nonpercussive mode  //
  UINT8  state;     // phase type                     //
  UINT32  TL;       // total level: TL << 2           //
  INT32  TLL;       // adjusted now TL                //
  INT32  volume;    // envelope counter               //
  UINT32  sl;       // sustain level: sl_tab[SL]      //

  UINT8  eg_sh_dp;  // (dump state)                   //
  UINT8  eg_sel_dp; // (dump state)                   //
  UINT8  eg_sh_ar;  // (attack state)                 //
  UINT8  eg_sel_ar; // (attack state)                 //
  UINT8  eg_sh_dr;  // (decay state)                  //
  UINT8  eg_sel_dr; // (decay state)                  //
  UINT8  eg_sh_rr;  // (release state for non-perc.)  //
  UINT8  eg_sel_rr; // (release state for non-perc.)  //
  UINT8  eg_sh_rs;  // (release state for perc.mode)  //
  UINT8  eg_sel_rs; // (release state for perc.mode)  //

  UINT32  key;      // 0 = KEY OFF, >0 = KEY ON //

  // LFO //
  UINT32  AMmask;   // LFO Amplitude Modulation enable mask //
  UINT8  vib;       // LFO Phase Modulation enable flag (active high)//

  // waveform select //
  unsigned int wavetable;
} YM2413_OPLL_SLOT;

typedef struct 
{
  YM2413_OPLL_SLOT SLOT[2];

  // phase generator state //
  UINT32  block_fnum;   // block+fnum //
  UINT32  fc;           // Freq. freqement base //
  UINT32  ksl_base;     // KeyScaleLevel Base step  //
  UINT8   kcode;        // key code (for key scaling) //
  UINT8   sus;          // sus on/off (release speed in percussive mode)  //
} YM2413_OPLL_CH;

// chip state //
typedef struct {
    YM2413_OPLL_CH P_CH[9];   // OPLL chips have 9 channels //
  UINT8  instvol_r[9];        // instrument/volume (or volume/volume in percussive mode)  //

  UINT32  eg_cnt;             // global envelope generator counter  //
  UINT32  eg_timer;           // global envelope generator counter works at frequency = chipclock/72 //
  UINT32  eg_timer_add;       // step of eg_timer //
  UINT32  eg_timer_overflow;  // envelope generator timer overlfows every 1 sample (on real chip) //

  UINT8  rhythm;              // Rhythm mode  //

  // LFO //
  UINT32  lfo_am_cnt;
  UINT32  lfo_am_inc;
  UINT32  lfo_pm_cnt;
  UINT32  lfo_pm_inc;

  UINT32  noise_rng;      // 23 bit noise shift register  //
  UINT32  noise_p;        // current noise 'phase'  //
  UINT32  noise_f;        // current noise period //


// instrument settings //
//
//0-user instrument
//1-15 - fixed instruments
//16 -bass drum settings
//17,18 - other percussion instruments
//
  UINT8 inst_tab[19][8];

  UINT32  fn_tab[1024];     // fnumber->increment counter  //

  UINT8 address;          // address register //
  UINT8 status;          // status flag       //

  double clock;         // master clock  (Hz) //
  int rate;            // sampling rate (Hz)  //
} YM2413;

extern YM2413 ym2413;

// struct describing a single operator (SLOT) //
typedef struct
{
  INT32   *DT;        // detune          :dt_tab[DT]      //
  UINT8   KSR;        // key scale rate  :3-KSR           //
  UINT32  ar;         // attack rate                      //
  UINT32  d1r;        // decay rate                       //
  UINT32  d2r;        // sustain rate                     //
  UINT32  rr;         // release rate                     //
  UINT8   ksr;        // key scale rate  :kcode>>(3-KSR)  //
  UINT32  mul;        // multiple        :ML_TABLE[ML]    //

  // Phase Generator //
  UINT32  phase;      // phase counter //
  INT32   Incr;       // phase step //

  // Envelope Generator //
  UINT8   state;      // phase type //
  UINT32  tl;         // total level: TL << 3 //
  INT32   volume;     // envelope counter //
  UINT32  sl;         // sustain level:sl_table[SL] //
  UINT32  vol_out;    // current output from EG circuit (without AM from LFO) //

  UINT8  eg_sh_ar;    //  (attack state)  //
  UINT8  eg_sel_ar;   //  (attack state)  //
  UINT8  eg_sh_d1r;   //  (decay state)   //
  UINT8  eg_sel_d1r;  //  (decay state)   //
  UINT8  eg_sh_d2r;   //  (sustain state) //
  UINT8  eg_sel_d2r;  //  (sustain state) //
  UINT8  eg_sh_rr;    //  (release state) //
  UINT8  eg_sel_rr;   //  (release state) //

  UINT8  ssg;         // SSG-EG waveform  //
  UINT8  ssgn;        // SSG-EG negated output  //

  UINT8  key;         // 0=last key was KEY OFF, 1=KEY ON //

  // LFO //
  UINT32  AMmask;     // AM enable flag //

} FM_SLOT;

typedef struct
{
  FM_SLOT  SLOT[4];     // four SLOTs (operators) //

  UINT8   ALGO;         // algorithm //
  UINT8   FB;           // feedback shift //
  INT32   op1_out[2];   // op1 output for feedback //

  INT32   *connect1;    // SLOT1 output pointer //
  INT32   *connect3;    // SLOT3 output pointer //
  INT32   *connect2;    // SLOT2 output pointer //
  INT32   *connect4;    // SLOT4 output pointer //

  INT32   *mem_connect; // where to put the delayed sample (MEM) //
  INT32   mem_value;    // delayed sample (MEM) value //

  INT32   pms;          // channel PMS //
  UINT8   ams;          // channel AMS //

  UINT32  fc;           // fnum,blk //
  UINT8   kcode;        // key code //
  UINT32  block_fnum;   // blk/fnum value (for LFO PM calculations) //
} FM_CH;


typedef struct
{
  UINT16  address;        // address register     //
  UINT8   status;         // status flag          //
  UINT32  mode;           // mode  CSM / 3SLOT    //
  UINT8   fn_h;           // freq latch           //
  INT32   TA;             // timer a value        //
  INT32   TAL;            // timer a base         //
  INT32   TAC;            // timer a counter      //
  INT32   TB;             // timer b value        //
  INT32   TBL;            // timer b base         //
  INT32   TBC;            // timer b counter      //
  INT32   dt_tab[8][32];  // DeTune table         //

} FM_ST;


// OPN unit                                                //


// OPN 3slot struct //
typedef struct
{
  UINT32  fc[3];          // fnum3,blk3: calculated //
  UINT8   fn_h;           // freq3 latch //
  UINT8   kcode[3];       // key code //
  UINT32  block_fnum[3];  // current fnum value for this slot (can be different betweeen slots of one channel in 3slot mode) //
  UINT8   key_csm;        // CSM mode Key-ON flag //

} FM_3SLOT;

// OPN/A/B common state //
typedef struct
{
  FM_ST  ST;                  // general state //
  FM_3SLOT SL3;               // 3 slot mode state //
  unsigned int pan[6*2];      // fm channels output masks (0xffffffff = enable) //

  // EG //
  UINT32  eg_cnt;             // global envelope generator counter //
  UINT32  eg_timer;           // global envelope generator counter works at frequency = chipclock/144/3 //

  // LFO //
  UINT8   lfo_cnt;            // current LFO phase (out of 128) //
  UINT32  lfo_timer;          // current LFO phase runs at LFO frequency //
  UINT32  lfo_timer_overflow; // LFO timer overflows every N samples (depends on LFO frequency) //
  UINT32  LFO_AM;             // current LFO AM step //
  UINT32  LFO_PM;             // current LFO PM step //

} FM_OPN;

// YM2612 chip                                                //
typedef struct
{
  FM_CH   CH[6];  // channel state //
  UINT8   dacen;  // DAC mode  //
  INT32   dacout; // DAC output //
  FM_OPN  OPN;    // OPN state //

} YM2612;

extern YM2612 ym2612;

// current chip state //
extern INT32  m2,c1,c2;   // Phase Modulation input for operators 2,3,4 //
extern INT32  mem;        // one sample delay memory //
extern INT32  out_fm[8];  // outputs of working channels //
extern UINT32 bitmask;    // working channels output bitmasking (DAC quantization) // 

extern uint8 tmss[4];     // TMSS security register //

extern uint8 rom_region;

extern uint8 pause_b;
extern EQSTATE eq;
extern int16 llp,rrp;



#define Z(a) memset((a), 0, sizeof(*(a)))
#define Y(a) memset((a), 0, sizeof((a)))

void zap(void)
{
	Z(&config);
	Z(&eeprom_93c);
	
	Z(&ext);
	Y(boot_rom);
	Y(work_ram);
	Y(zram);
	Z(&zbank);
	Z(&zstate);
	Z(&pico_current);
	Z(&input);
	memset(old_system, -1, sizeof(old_system));
	Y(io_reg);
	Z(&region_code);
	Z(&rominfo);
	Z(&romtype);
	Z(&m68k);
	Z(&s68k);
	
	Y(zbank_memory_map);
	
	Z(&sram); // NB: sram.sram is not allocated

	Z(&svp);
	
	Z(&bitmap);
	Z(&snd);

	Z(&mcycles_vdp);

	Z(&system_hw);
	Z(&system_bios);
	Z(&system_clock);

	Y(reg);
	Y(sat);
	Y(vram);
	Y(cram);
	Y(vsram);
	Z(&hint_pending);
	Z(&vint_pending);
	Z(&status);
	Z(&dma_length);
	
	Z(&ntab);
	Z(&ntbb);
	Z(&ntwb);
	Z(&satb);
	Z(&hscb);
	Y(bg_name_dirty);
	Y(bg_name_list);
	Z(&bg_list_index);
	Z(&hscroll_mask);
	Z(&playfield_shift);
	Z(&playfield_col_mask);
	Z(&playfield_row_mask);
	Z(&odd_frame);
	Z(&im2_flag);
	Z(&interlaced);
	Z(&vdp_pal);
	Z(&v_counter);
	Z(&vc_max);
	Z(&vscroll);
	Z(&lines_per_frame);
	Z(&max_sprite_pixels);
	Z(&fifo_write_cnt);
	Z(&fifo_slots);
	Z(&hvc_latch);
	Z(&hctab);
	
	Z(&vdp_68k_data_w);
	Z(&vdp_z80_data_w);
	Z(&vdp_68k_data_r);
	Z(&vdp_z80_data_r);
	
	Z(&spr_col);
	
	Z(&Z80);
	
	Y(z80_readmap);
	Y(z80_writemap);

	Z(&z80_writemem);
	Z(&z80_readmem);
	Z(&z80_writeport);
	Z(&z80_readport);

	//=======

	Z(&action_replay);
	
	Z(&eeprom_i2c);
	Z(&spi_eeprom);
	
	Z(&ggenie);
	
	Y(activator);
	
	Y(gamepad);
	
	Z(&pad_index);
	
	
	Z(&lightgun);
	
	Z(&mouse);
	
	Y(paddle);
	
	Y(sportspad);
	
	Y(teamplayer);
	
	Z(&tablet);
	
	Y(xe_a1p);
	
	Z(&SN76489);
	
	Y(fm_buffer);
	Y(fm_last);
	Z(&fm_ptr);
	
	Z(&fm_cycles_ratio);
	Z(&fm_cycles_start);
	Z(&fm_cycles_count);
	
	Z(&YM_Reset);
	Z(&YM_Update);
	Z(&YM_Write);
	
	Z(&ym2413);
	
	Z(&ym2612);
	
	Z(&m2);
	Z(&c1);
	Z(&c2);
	Z(&mem);
	Y(out_fm);
	Z(&bitmask);
	
	Y(tmss);
	
	Z(&rom_region);
	
	Z(&pause_b);
	Z(&eq);
	Z(&llp);
	Z(&rrp);
}

