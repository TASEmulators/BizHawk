#ifndef CPU_H
#define CPU_H

#include "types.h"

extern Byte acc;		/* Accumulator */
extern ADDRESS pc;		/* Program counter */

extern Byte itimer;		/* Internal timer */
extern Byte reg_pnt;	/* pointer to register bank */
extern Byte timer_on;  /* 0=timer off/1=timer on */
extern Byte count_on;  /* 0=count off/1=count on */

extern Byte t_flag;		/* Timer flag */

extern Byte psw;		/* Processor status word */
extern Byte sp;		/* Stack pointer (part of psw) */

extern Byte p1;		/* I/O port 1 */
extern Byte p2;		/* I/O port 2 */

extern Byte xirq_pend;
extern Byte tirq_pend;

void init_cpu(void);
int64_t cpu_exec(int64_t ncycles);
void ext_IRQ(void);
void tim_IRQ(void);
void make_psw_debug(void);

Byte acc;		/* Accumulator */
ADDRESS pc;		/* Program counter */

Byte itimer;	/* Internal timer */
Byte reg_pnt;	/* pointer to register bank */
Byte timer_on;  /* 0=timer off/1=timer on */
Byte count_on;  /* 0=count off/1=count on */
Byte psw;		/* Processor status word */
Byte sp;		/* Stack pointer (part of psw) */

Byte p1;		/* I/O port 1 */
Byte p2; 		/* I/O port 2 */
Byte xirq_pend; /* external IRQ pending */
Byte tirq_pend; /* timer IRQ pending */
Byte t_flag;	/* Timer flag */

ADDRESS lastpc;
ADDRESS A11;		/* PC bit 11 */
ADDRESS A11ff;
Byte bs; 		/* Register Bank (part of psw) */
Byte f0;			/* Flag Bit (part of psw) */
Byte f1;			/* Flag Bit 1 */
Byte ac;			/* Aux Carry (part of psw) */
Byte cy;			/* Carry flag (part of psw) */
Byte xirq_en;	/* external IRQ's enabled */
Byte tirq_en;	/* Timer IRQ enabled */
Byte irq_ex;		/* IRQ executing */

int master_count;


#endif  /* CPU_H */

