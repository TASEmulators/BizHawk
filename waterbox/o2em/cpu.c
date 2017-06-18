
/*
 *   O2EM Free Odyssey2 / Videopac+ Emulator
 *
 *   Created by Daniel Boris <dboris@comcast.net>  (c) 1997,1998
 *
 **   Developed by Andre de la Rocha   <adlroc@users.sourceforge.net>
 *             Arlindo M. de Oliveira <dgtec@users.sourceforge.net>
 *
 *   http://o2em.sourceforge.net
 *
 *
 *
 *   8048 microcontroller emulation
 */

#include <stdio.h>
#include "types.h"
#include "vmachine.h"
#include "keyboard.h"
#include "voice.h"
#include "vdc.h"
#include "vpp.h"
#include "cpu.h"

static int64_t clk;

#define push(d)             \
	{                       \
		intRAM[sp++] = (d); \
		if (sp > 23)        \
			sp = 8;         \
	}
#define pull() (sp--, (sp < 8) ? (sp = 23) : 0, intRAM[sp])
#define make_psw()                             \
	{                                          \
		psw = (cy << 7) | ac | f0 | bs | 0x08; \
		psw = psw | ((sp - 8) >> 1);           \
	}
#define illegal(o) \
	{              \
	}
#define undef(i)                                                  \
	{                                                             \
		printf("** unimplemented instruction %x, %x**\n", i, pc); \
	}
#define ROM(adr) (rom[(adr)&0xfff])

void init_cpu(void)
{
	pc = 0;
	sp = 8;
	bs = 0;
	p1 = p2 = 0xFF;
	ac = cy = f0 = 0;
	A11 = A11ff = 0;
	timer_on = 0;
	count_on = 0;
	reg_pnt = 0;
	tirq_en = xirq_en = irq_ex = xirq_pend = tirq_pend = 0;
}

void ext_IRQ(void)
{
	int_clk = 5; /* length of pulse on /INT */
	if (xirq_en && !irq_ex)
	{
		irq_ex = 1;
		xirq_pend = 0;
		clk += 2;
		make_psw();
		push(pc & 0xFF);
		push(((pc & 0xF00) >> 8) | (psw & 0xF0));
		pc = 0x03;
		A11ff = A11;
		A11 = 0;
	}
	if (pendirq && (!xirq_en))
		xirq_pend = 1;
}

void tim_IRQ(void)
{
	if (tirq_en && !irq_ex)
	{
		irq_ex = 2;
		tirq_pend = 0;
		clk += 2;
		make_psw();
		push(pc & 0xFF);
		push(((pc & 0xF00) >> 8) | (psw & 0xF0));
		pc = 0x07;
		A11ff = A11;
		A11 = 0;
	}
	if (pendirq && (!tirq_en))
		tirq_pend = 1;
}

void make_psw_debug(void)
{
	make_psw();
}

int64_t cpu_exec(int64_t ncycles)
{
	int64_t startclk = clk;
	int64_t targetclk = clk + ncycles;

	Byte op;
	ADDRESS adr;
	Byte dat;
	int temp;

	while (clk < targetclk)
	{
		lastpc = pc;
		op = ROM(pc++);
		switch (op)
		{
		case 0x00: /* NOP */
			clk++;
			break;
		case 0x01: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0x02: /* OUTL BUS,A */
			clk += 2;
			undef(0x02);
			break;
		case 0x03: /* ADD A,#data */
			clk += 2;
			cy = ac = 0;
			dat = ROM(pc++);
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x04: /* JMP */
			pc = ROM(pc) | A11;
			clk += 2;
			break;
		case 0x05: /* EN I */
			xirq_en = 1;
			clk++;
			break;
		case 0x06: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x07: /* DEC A */
			acc--;
			clk++;
			break;
		case 0x08: /* INS A,BUS*/
			clk += 2;
			acc = in_bus();
			break;
		case 0x09: /* IN A,Pp */
			acc = p1;
			clk += 2;
			break;
		case 0x0A: /* IN A,Pp */
			acc = read_P2();
			clk += 2;
			break;
		case 0x0B: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x0C: /* MOVD A,P4 */
			clk += 2;
			acc = read_PB(0);
			break;
		case 0x0D: /* MOVD A,P5 */
			clk += 2;
			acc = read_PB(1);
			break;
		case 0x0E: /* MOVD A,P6 */
			clk += 2;
			acc = read_PB(2);
			break;
		case 0x0F: /* MOVD A,P7 */
			clk += 2;
			acc = read_PB(3);
			break;
		case 0x10: /* INC @Ri */
			intRAM[intRAM[reg_pnt] & 0x3F]++;
			clk++;
			break;
		case 0x11: /* INC @Ri */
			intRAM[intRAM[reg_pnt + 1] & 0x3F]++;
			clk++;
			break;
		case 0x12: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x01)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x13: /* ADDC A,#data */
			clk += 2;
			dat = ROM(pc++);
			ac = 0;
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;

		case 0x14: /* CALL */
			make_psw();
			adr = ROM(pc) | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0x15: /* DIS I */
			xirq_en = 0;
			clk++;
			break;
		case 0x16: /* JTF */
			clk += 2;
			dat = ROM(pc);
			if (t_flag)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			t_flag = 0;
			break;
		case 0x17: /* INC A */
			acc++;
			clk++;
			break;
		case 0x18: /* INC Rr */
			intRAM[reg_pnt]++;
			clk++;
			break;
		case 0x19: /* INC Rr */
			intRAM[reg_pnt + 1]++;
			clk++;
			break;
		case 0x1A: /* INC Rr */
			intRAM[reg_pnt + 2]++;
			clk++;
			break;
		case 0x1B: /* INC Rr */
			intRAM[reg_pnt + 3]++;
			clk++;
			break;
		case 0x1C: /* INC Rr */
			intRAM[reg_pnt + 4]++;
			clk++;
			break;
		case 0x1D: /* INC Rr */
			intRAM[reg_pnt + 5]++;
			clk++;
			break;
		case 0x1E: /* INC Rr */
			intRAM[reg_pnt + 6]++;
			clk++;
			break;
		case 0x1F: /* INC Rr */
			intRAM[reg_pnt + 7]++;
			clk++;
			break;
		case 0x20: /* XCH A,@Ri */
			clk++;
			dat = acc;
			acc = intRAM[intRAM[reg_pnt] & 0x3F];
			intRAM[intRAM[reg_pnt] & 0x3F] = dat;
			break;
		case 0x21: /* XCH A,@Ri */
			clk++;
			dat = acc;
			acc = intRAM[intRAM[reg_pnt + 1] & 0x3F];
			intRAM[intRAM[reg_pnt + 1] & 0x3F] = dat;
			break;
		case 0x22: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x23: /* MOV a,#data */
			clk += 2;
			acc = ROM(pc++);
			break;

		case 0x24: /* JMP */
			pc = ROM(pc) | 0x100 | A11;
			clk += 2;
			break;
		case 0x25: /* EN TCNTI */
			tirq_en = 1;
			clk++;
			break;
		case 0x26: /* JNT0 */
			clk += 2;
			dat = ROM(pc);
			if (!get_voice_status())
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x27: /* CLR A */
			clk++;
			acc = 0;
			break;
		case 0x28: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt];
			intRAM[reg_pnt] = dat;
			clk++;
			break;
		case 0x29: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 1];
			intRAM[reg_pnt + 1] = dat;
			clk++;
			break;
		case 0x2A: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 2];
			intRAM[reg_pnt + 2] = dat;
			clk++;
			break;
		case 0x2B: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 3];
			intRAM[reg_pnt + 3] = dat;
			clk++;
			break;
		case 0x2C: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 4];
			intRAM[reg_pnt + 4] = dat;
			clk++;
			break;
		case 0x2D: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 5];
			intRAM[reg_pnt + 5] = dat;
			clk++;
			break;
		case 0x2E: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 6];
			intRAM[reg_pnt + 6] = dat;
			clk++;
			break;
		case 0x2F: /* XCH A,Rr */
			dat = acc;
			acc = intRAM[reg_pnt + 7];
			intRAM[reg_pnt + 7] = dat;
			clk++;
			break;
		case 0x30: /* XCHD A,@Ri */
			clk++;
			adr = intRAM[reg_pnt] & 0x3F;
			dat = acc & 0x0F;
			acc = acc & 0xF0;
			acc = acc | (intRAM[adr] & 0x0F);
			intRAM[adr] &= 0xF0;
			intRAM[adr] |= dat;
			break;
		case 0x31: /* XCHD A,@Ri */
			clk++;
			adr = intRAM[reg_pnt + 1] & 0x3F;
			dat = acc & 0x0F;
			acc = acc & 0xF0;
			acc = acc | (intRAM[adr] & 0x0F);
			intRAM[adr] &= 0xF0;
			intRAM[adr] |= dat;
			break;
		case 0x32: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x02)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x33: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x34: /* CALL */
			make_psw();
			adr = ROM(pc) | 0x100 | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0x35: /* DIS TCNTI */
			tirq_en = 0;
			tirq_pend = 0;
			clk++;
			break;
		case 0x36: /* JT0 */
			clk += 2;
			dat = ROM(pc);
			if (get_voice_status())
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x37: /* CPL A */
			acc = acc ^ 0xFF;
			clk++;
			break;
		case 0x38: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x39: /* OUTL P1,A */
			clk += 2;
			write_p1(acc);
			break;
		case 0x3A: /* OUTL P2,A */
			clk += 2;
			p2 = acc;
			break;
		case 0x3B: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x3C: /* MOVD P4,A */
			clk += 2;
			write_PB(0, acc);
			break;
		case 0x3D: /* MOVD P5,A */
			clk += 2;
			write_PB(1, acc);
			break;
		case 0x3E: /* MOVD P6,A */
			clk += 2;
			write_PB(2, acc);
			break;
		case 0x3F: /* MOVD P7,A */
			clk += 2;
			write_PB(3, acc);
			break;
		case 0x40: /* ORL A,@Ri */
			clk++;
			acc = acc | intRAM[intRAM[reg_pnt] & 0x3F];
			break;
		case 0x41: /* ORL A,@Ri */
			clk++;
			acc = acc | intRAM[intRAM[reg_pnt + 1] & 0x3F];
			break;
		case 0x42: /* MOV A,T */
			clk++;
			acc = itimer;
			break;
		case 0x43: /* ORL A,#data */
			clk += 2;
			acc = acc | ROM(pc++);
			break;
		case 0x44: /* JMP */
			pc = ROM(pc) | 0x200 | A11;
			clk += 2;
			break;
		case 0x45: /* STRT CNT */
			/* printf("START: %d=%d\n",master_clk/22,itimer); */
			count_on = 1;
			clk++;
			break;
		case 0x46: /* JNT1 */
			clk += 2;
			dat = ROM(pc);
			if (!read_t1())
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x47: /* SWAP A */
			clk++;
			dat = (acc & 0xF0) >> 4;
			acc = acc << 4;
			acc = acc | dat;
			break;
		case 0x48: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt];
			break;
		case 0x49: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 1];
			break;
		case 0x4A: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 2];
			break;
		case 0x4B: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 3];
			break;
		case 0x4C: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 4];
			break;
		case 0x4D: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 5];
			break;
		case 0x4E: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 6];
			break;
		case 0x4F: /* ORL A,Rr */
			clk++;
			acc = acc | intRAM[reg_pnt + 7];
			break;

		case 0x50: /* ANL A,@Ri */
			acc = acc & intRAM[intRAM[reg_pnt] & 0x3F];
			clk++;
			break;
		case 0x51: /* ANL A,@Ri */
			acc = acc & intRAM[intRAM[reg_pnt + 1] & 0x3F];
			clk++;
			break;
		case 0x52: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x04)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x53: /* ANL A,#data */
			clk += 2;
			acc = acc & ROM(pc++);
			break;
		case 0x54: /* CALL */
			make_psw();
			adr = ROM(pc) | 0x200 | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0x55: /* STRT T */
			timer_on = 1;
			clk++;
			break;
		case 0x56: /* JT1 */
			clk += 2;
			dat = ROM(pc);
			if (read_t1())
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x57: /* DA A */
			clk++;
			if (((acc & 0x0F) > 0x09) || ac)
			{
				if (acc > 0xf9)
					cy = 1;
				acc += 6;
			}
			dat = (acc & 0xF0) >> 4;
			if ((dat > 9) || cy)
			{
				dat += 6;
				cy = 1;
			}
			acc = (acc & 0x0F) | (dat << 4);
			break;
		case 0x58: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt];
			break;
		case 0x59: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 1];
			break;
		case 0x5A: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 2];
			break;
		case 0x5B: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 3];
			break;
		case 0x5C: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 4];
			break;
		case 0x5D: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 5];
			break;
		case 0x5E: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 6];
			break;
		case 0x5F: /* ANL A,Rr */
			clk++;
			acc = acc & intRAM[reg_pnt + 7];
			break;

		case 0x60: /* ADD A,@Ri */
			clk++;
			cy = ac = 0;
			dat = intRAM[intRAM[reg_pnt] & 0x3F];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x61: /* ADD A,@Ri */
			clk++;
			cy = ac = 0;
			dat = intRAM[intRAM[reg_pnt + 1] & 0x3F];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x62: /* MOV T,A */
			clk++;
			itimer = acc;
			break;
		case 0x63: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x64: /* JMP */
			pc = ROM(pc) | 0x300 | A11;
			clk += 2;
			break;
		case 0x65: /* STOP TCNT */
			clk++;
			/* printf("STOP %d\n",master_clk/22); */
			count_on = timer_on = 0;
			break;
		case 0x66: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x67: /* RRC A */
			dat = cy;
			cy = acc & 0x01;
			acc = acc >> 1;
			if (dat)
				acc = acc | 0x80;
			else
				acc = acc & 0x7F;
			clk++;
			break;
		case 0x68: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x69: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 1];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x6A: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 2];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x6B: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 3];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x6C: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 4];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x6D: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 5];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x6E: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 6];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x6F: /* ADD A,Rr */
			clk++;
			cy = ac = 0;
			dat = intRAM[reg_pnt + 7];
			if (((acc & 0x0f) + (dat & 0x0f)) > 0x0f)
				ac = 0x40;
			temp = acc + dat;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x70: /* ADDC A,@Ri */
			clk++;
			ac = 0;
			dat = intRAM[intRAM[reg_pnt] & 0x3F];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x71: /* ADDC A,@Ri */
			clk++;
			ac = 0;
			dat = intRAM[intRAM[reg_pnt + 1] & 0x3F];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;

		case 0x72: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x08)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x73: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x74: /* CALL */
			make_psw();
			adr = ROM(pc) | 0x300 | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0x75: /* EN CLK */
			clk++;
			undef(op);
			break;
		case 0x76: /* JF1 address */
			clk += 2;
			dat = ROM(pc);
			if (f1)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x77: /* RR A */
			clk++;
			dat = acc & 0x01;
			acc = acc >> 1;
			if (dat)
				acc = acc | 0x80;
			else
				acc = acc & 0x7f;
			break;

		case 0x78: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x79: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 1];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x7A: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 2];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x7B: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 3];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x7C: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 4];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x7D: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 5];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x7E: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 6];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;
		case 0x7F: /* ADDC A,Rr */
			clk++;
			ac = 0;
			dat = intRAM[reg_pnt + 7];
			if (((acc & 0x0f) + (dat & 0x0f) + cy) > 0x0f)
				ac = 0x40;
			temp = acc + dat + cy;
			cy = 0;
			if (temp > 0xFF)
				cy = 1;
			acc = (temp & 0xFF);
			break;

		case 0x80: /* MOVX  A,@Ri */
			acc = ext_read(intRAM[reg_pnt]);
			clk += 2;
			break;
		case 0x81: /* MOVX A,@Ri */
			acc = ext_read(intRAM[reg_pnt + 1]);
			clk += 2;
			break;
		case 0x82: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0x83: /* RET */
			clk += 2;
			pc = ((pull() & 0x0F) << 8);
			pc = pc | pull();
			break;
		case 0x84: /* JMP */
			pc = ROM(pc) | 0x400 | A11;
			clk += 2;
			break;
		case 0x85: /* CLR F0 */
			clk++;
			f0 = 0;
			break;
		case 0x86: /* JNI address */
			clk += 2;
			dat = ROM(pc);
			if (int_clk > 0)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x87: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0x88: /* BUS,#data */
			clk += 2;
			undef(op);
			break;
		case 0x89: /* ORL Pp,#data */
			write_p1(p1 | ROM(pc++));
			clk += 2;
			break;
		case 0x8A: /* ORL Pp,#data */
			p2 = p2 | ROM(pc++);
			clk += 2;
			break;
		case 0x8B: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0x8C: /* ORLD P4,A */
			write_PB(0, read_PB(0) | acc);
			clk += 2;
			break;
		case 0x8D: /* ORLD P5,A */
			write_PB(1, read_PB(1) | acc);
			clk += 2;
			break;
		case 0x8E: /* ORLD P6,A */
			write_PB(2, read_PB(2) | acc);
			clk += 2;
			break;
		case 0x8F: /* ORLD P7,A */
			write_PB(3, read_PB(3) | acc);
			clk += 2;
			break;
		case 0x90: /* MOVX @Ri,A */
			ext_write(acc, intRAM[reg_pnt]);
			clk += 2;
			break;
		case 0x91: /* MOVX @Ri,A */
			ext_write(acc, intRAM[reg_pnt + 1]);
			clk += 2;
			break;
		case 0x92: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x10)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x93: /* RETR*/
			/* printf("RETR %d\n",master_clk/22); */
			clk += 2;
			dat = pull();
			pc = (dat & 0x0F) << 8;
			cy = (dat & 0x80) >> 7;
			ac = dat & 0x40;
			f0 = dat & 0x20;
			bs = dat & 0x10;
			if (bs)
				reg_pnt = 24;
			else
				reg_pnt = 0;
			pc = pc | pull();
			irq_ex = 0;
			A11 = A11ff;
			break;
		case 0x94: /* CALL */
			make_psw();
			adr = ROM(pc) | 0x400 | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0x95: /* CPL F0 */
			f0 = f0 ^ 0x20;
			clk++;
			break;
		case 0x96: /* JNZ address */
			clk += 2;
			dat = ROM(pc);
			if (acc != 0)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0x97: /* CLR C */
			cy = 0;
			clk++;
			break;
		case 0x98: /* ANL BUS,#data */
			clk += 2;
			undef(op);
			break;
		case 0x99: /* ANL Pp,#data */
			write_p1(p1 & ROM(pc++));
			clk += 2;
			break;
		case 0x9A: /* ANL Pp,#data */
			p2 = p2 & ROM(pc++);
			clk += 2;
			break;
		case 0x9B: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0x9C: /* ANLD P4,A */
			write_PB(0, read_PB(0) & acc);
			clk += 2;
			break;
		case 0x9D: /* ANLD P5,A */
			write_PB(1, read_PB(1) & acc);
			clk += 2;
			break;
		case 0x9E: /* ANLD P6,A */
			write_PB(2, read_PB(2) & acc);
			clk += 2;
			break;
		case 0x9F: /* ANLD P7,A */
			write_PB(3, read_PB(3) & acc);
			clk += 2;
			break;
		case 0xA0: /* MOV @Ri,A */
			intRAM[intRAM[reg_pnt] & 0x3F] = acc;
			clk++;
			break;
		case 0xA1: /* MOV @Ri,A */
			intRAM[intRAM[reg_pnt + 1] & 0x3F] = acc;
			clk++;
			break;
		case 0xA2: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0xA3: /* MOVP A,@A */
			acc = ROM((pc & 0xF00) | acc);
			clk += 2;
			break;
		case 0xA4: /* JMP */
			pc = ROM(pc) | 0x500 | A11;
			clk += 2;
			break;
		case 0xA5: /* CLR F1 */
			clk++;
			f1 = 0;
			break;
		case 0xA6: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xA7: /* CPL C */
			cy = cy ^ 0x01;
			clk++;
			break;
		case 0xA8: /* MOV Rr,A */
			intRAM[reg_pnt] = acc;
			clk++;
			break;
		case 0xA9: /* MOV Rr,A */
			intRAM[reg_pnt + 1] = acc;
			clk++;
			break;
		case 0xAA: /* MOV Rr,A */
			intRAM[reg_pnt + 2] = acc;
			clk++;
			break;
		case 0xAB: /* MOV Rr,A */
			intRAM[reg_pnt + 3] = acc;
			clk++;
			break;
		case 0xAC: /* MOV Rr,A */
			intRAM[reg_pnt + 4] = acc;
			clk++;
			break;
		case 0xAD: /* MOV Rr,A */
			intRAM[reg_pnt + 5] = acc;
			clk++;
			break;
		case 0xAE: /* MOV Rr,A */
			intRAM[reg_pnt + 6] = acc;
			clk++;
			break;
		case 0xAF: /* MOV Rr,A */
			intRAM[reg_pnt + 7] = acc;
			clk++;
			break;
		case 0xB0: /* MOV @Ri,#data */
			intRAM[intRAM[reg_pnt] & 0x3F] = ROM(pc++);
			clk += 2;
			break;
		case 0xB1: /* MOV @Ri,#data */
			intRAM[intRAM[reg_pnt + 1] & 0x3F] = ROM(pc++);
			clk += 2;
			break;
		case 0xB2: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x20)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xB3: /* JMPP @A */
			adr = (pc & 0xF00) | acc;
			pc = (pc & 0xF00) | ROM(adr);
			clk += 2;
			break;
		case 0xB4: /* CALL */
			make_psw();
			adr = ROM(pc) | 0x500 | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0xB5: /* CPL F1 */
			f1 = f1 ^ 0x01;
			clk++;
			break;
		case 0xB6: /* JF0 address */
			clk += 2;
			dat = ROM(pc);
			if (f0)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xB7: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0xB8: /* MOV Rr,#data */
			intRAM[reg_pnt] = ROM(pc++);
			clk += 2;
			break;
		case 0xB9: /* MOV Rr,#data */
			intRAM[reg_pnt + 1] = ROM(pc++);
			clk += 2;
			break;
		case 0xBA: /* MOV Rr,#data */
			intRAM[reg_pnt + 2] = ROM(pc++);
			clk += 2;
			break;
		case 0xBB: /* MOV Rr,#data */
			intRAM[reg_pnt + 3] = ROM(pc++);
			clk += 2;
			break;
		case 0xBC: /* MOV Rr,#data */
			intRAM[reg_pnt + 4] = ROM(pc++);
			clk += 2;
			break;
		case 0xBD: /* MOV Rr,#data */
			intRAM[reg_pnt + 5] = ROM(pc++);
			clk += 2;
			break;
		case 0xBE: /* MOV Rr,#data */
			intRAM[reg_pnt + 6] = ROM(pc++);
			clk += 2;
			break;
		case 0xBF: /* MOV Rr,#data */
			intRAM[reg_pnt + 7] = ROM(pc++);
			clk += 2;
			break;
		case 0xC0: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xC1: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xC2: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xC3: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xC4: /* JMP */
			pc = ROM(pc) | 0x600 | A11;
			clk += 2;
			break;
		case 0xC5: /* SEL RB0 */
			bs = reg_pnt = 0;
			clk++;
			break;
		case 0xC6: /* JZ address */
			clk += 2;
			dat = ROM(pc);
			if (acc == 0)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xC7: /* MOV A,PSW */
			clk++;
			make_psw();
			acc = psw;
			break;
		case 0xC8: /* DEC Rr */
			intRAM[reg_pnt]--;
			clk++;
			break;
		case 0xC9: /* DEC Rr */
			intRAM[reg_pnt + 1]--;
			clk++;
			break;
		case 0xCA: /* DEC Rr */
			intRAM[reg_pnt + 2]--;
			clk++;
			break;
		case 0xCB: /* DEC Rr */
			intRAM[reg_pnt + 3]--;
			clk++;
			break;
		case 0xCC: /* DEC Rr */
			intRAM[reg_pnt + 4]--;
			clk++;
			break;
		case 0xCD: /* DEC Rr */
			intRAM[reg_pnt + 5]--;
			clk++;
			break;
		case 0xCE: /* DEC Rr */
			intRAM[reg_pnt + 6]--;
			clk++;
			break;
		case 0xCF: /* DEC Rr */
			intRAM[reg_pnt + 7]--;
			clk++;
			break;
		case 0xD0: /* XRL A,@Ri */
			acc = acc ^ intRAM[intRAM[reg_pnt] & 0x3F];
			clk++;
			break;
		case 0xD1: /* XRL A,@Ri */
			acc = acc ^ intRAM[intRAM[reg_pnt + 1] & 0x3F];
			clk++;
			break;
		case 0xD2: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x40)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xD3: /* XRL A,#data */
			clk += 2;
			acc = acc ^ ROM(pc++);
			break;
		case 0xD4: /* CALL */
			make_psw();
			adr = ROM(pc) | 0x600 | A11;
			pc++;
			clk += 2;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0xD5: /* SEL RB1 */
			bs = 0x10;
			reg_pnt = 24;
			clk++;
			break;
		case 0xD6: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xD7: /* MOV PSW,A */
			psw = acc;
			clk++;
			cy = (psw & 0x80) >> 7;
			ac = psw & 0x40;
			f0 = psw & 0x20;
			bs = psw & 0x10;
			if (bs)
				reg_pnt = 24;
			else
				reg_pnt = 0;
			sp = (psw & 0x07) << 1;
			sp += 8;
			break;
		case 0xD8: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt];
			clk++;
			break;
		case 0xD9: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 1];
			clk++;
			break;
		case 0xDA: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 2];
			clk++;
			break;
		case 0xDB: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 3];
			clk++;
			break;
		case 0xDC: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 4];
			clk++;
			break;
		case 0xDD: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 5];
			clk++;
			break;
		case 0xDE: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 6];
			clk++;
			break;
		case 0xDF: /* XRL A,Rr */
			acc = acc ^ intRAM[reg_pnt + 7];
			clk++;
			break;
		case 0xE0: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0xE1: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0xE2: /* ILL */
			clk++;
			illegal(op);
			break;
		case 0xE3: /* MOVP3 A,@A */

			adr = 0x300 | acc;
			acc = ROM(adr);
			clk += 2;
			break;
		case 0xE4: /* JMP */
			pc = ROM(pc) | 0x700 | A11;
			clk += 2;
			break;
		case 0xE5: /* SEL MB0 */
			A11 = 0;
			A11ff = 0;
			clk++;
			break;
		case 0xE6: /* JNC address */
			clk += 2;
			dat = ROM(pc);
			if (!cy)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xE7: /* RL A */
			clk++;
			dat = acc & 0x80;
			acc = acc << 1;
			if (dat)
				acc = acc | 0x01;
			else
				acc = acc & 0xFE;
			break;
		case 0xE8: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xE9: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 1]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 1] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xEA: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 2]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 2] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xEB: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 3]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 3] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xEC: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 4]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 4] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xED: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 5]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 5] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xEE: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 6]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 6] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xEF: /* DJNZ Rr,address */
			clk += 2;
			intRAM[reg_pnt + 7]--;
			dat = ROM(pc);
			if (intRAM[reg_pnt + 7] != 0)
			{
				pc = pc & 0xF00;
				pc = pc | dat;
			}
			else
				pc++;
			break;
		case 0xF0: /* MOV A,@Ri */
			clk++;
			acc = intRAM[intRAM[reg_pnt] & 0x3F];
			break;
		case 0xF1: /* MOV A,@Ri */
			clk++;
			acc = intRAM[intRAM[reg_pnt + 1] & 0x3F];
			break;
		case 0xF2: /* JBb address */
			clk += 2;
			dat = ROM(pc);
			if (acc & 0x80)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xF3: /* ILL */
			illegal(op);
			clk++;
			break;
		case 0xF4: /* CALL */
			clk += 2;
			make_psw();
			adr = ROM(pc) | 0x700 | A11;
			pc++;
			push(pc & 0xFF);
			push(((pc & 0xF00) >> 8) | (psw & 0xF0));
			pc = adr;
			break;
		case 0xF5: /* SEL MB1 */
			if (irq_ex)
			{
				A11ff = 0x800;
			}
			else
			{
				A11 = 0x800;
				A11ff = 0x800;
			}
			clk++;
			break;
		case 0xF6: /* JC address */
			clk += 2;
			dat = ROM(pc);
			if (cy)
				pc = (pc & 0xF00) | dat;
			else
				pc++;
			break;
		case 0xF7: /* RLC A */
			dat = cy;
			cy = (acc & 0x80) >> 7;
			acc = acc << 1;
			if (dat)
				acc = acc | 0x01;
			else
				acc = acc & 0xFE;
			clk++;
			break;
		case 0xF8: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt];
			break;
		case 0xF9: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 1];
			break;
		case 0xFA: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 2];
			break;
		case 0xFB: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 3];
			break;
		case 0xFC: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 4];
			break;
		case 0xFD: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 5];
			break;
		case 0xFE: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 6];
			break;
		case 0xFF: /* MOV A,Rr */
			clk++;
			acc = intRAM[reg_pnt + 7];
			break;
		}

		master_clk += clk;
		h_clk += clk;
		clk_counter += clk;

		/* flag for JNI */
		if (int_clk > clk)
			int_clk -= clk;
		else
			int_clk = 0;

		/* pending IRQs */
		if (xirq_pend)
			ext_IRQ();
		if (tirq_pend)
			tim_IRQ();

		if (h_clk > LINECNT - 1)
		{
			h_clk -= LINECNT;
			if (enahirq && (VDCwrite[0xA0] & 0x01))
				ext_IRQ();
			if (count_on && mstate == 0)
			{
				itimer++;
				if (itimer == 0)
				{
					t_flag = 1;
					tim_IRQ();
					draw_region();
				}
			}
		}

		if (timer_on)
		{
			master_count += clk;
			if (master_count > 31)
			{
				master_count -= 31;
				itimer++;
				if (itimer == 0)
				{
					t_flag = 1;
					tim_IRQ();
				}
			}
		}

		if ((mstate == 0) && (master_clk > VBLCLK))
			handle_vbl();

		if ((mstate == 1) && (master_clk > evblclk))
		{
			handle_evbl();
			if (app_data.crc == 0xA7344D1F)
				handle_evbll(); /* Atlantis */
			break;
		}
	}
	return clk - startclk;
}
