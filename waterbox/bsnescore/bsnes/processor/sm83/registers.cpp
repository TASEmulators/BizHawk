#define AF r.af.word
#define BC r.bc.word
#define DE r.de.word
#define HL r.hl.word
#define SP r.sp.word
#define PC r.pc.word

#define A r.af.byte.hi
#define F r.af.byte.lo
#define B r.bc.byte.hi
#define C r.bc.byte.lo
#define D r.de.byte.hi
#define E r.de.byte.lo
#define H r.hl.byte.hi
#define L r.hl.byte.lo

#define CF bit1(r.af.byte.lo,4)
#define HF bit1(r.af.byte.lo,5)
#define NF bit1(r.af.byte.lo,6)
#define ZF bit1(r.af.byte.lo,7)
