#ifndef __V30MZ_H_
#define __V30MZ_H_

#include "system.h"

namespace MDFN_IEN_WSWAN
{

typedef union
{                   /* eight general registers */
    uint16 w[8];    /* viewed as 16 bits registers */
    uint8  b[16];   /* or as 8 bit registers */
} v30mz_basicregs_t;

typedef struct
{
	v30mz_basicregs_t regs;
 	uint16	sregs[4];

	uint16	pc;

	int32	SignVal;
	uint32  AuxVal, OverVal, ZeroVal, CarryVal, ParityVal; /* 0 or non-0 valued flags */
	uint8 TF, IF, DF;
} v30mz_regs_t;

namespace V30MZEnum
{

typedef enum { DS1, PS, SS, DS0 } SREGS;
typedef enum { AW, CW, DW, BW, SP, BP, IX, IY } WREGS;

#ifdef LSB_FIRST
typedef enum { AL,AH,CL,CH,DL,DH,BL,BH,SPL,SPH,BPL,BPH,IXL,IXH,IYL,IYH } BREGS;
#else
typedef enum { AH,AL,CH,CL,DH,DL,BH,BL,SPH,SPL,BPH,BPL,IXH,IXL,IYH,IYL } BREGS;
#endif
}

class V30MZ
{
public:
	V30MZ();

	void execute(int cycles);
	void set_reg(int, unsigned);
	unsigned get_reg(int regnum) const;
	void reset();

	void interrupt(uint32 vector, bool IgnoreIF = FALSE);

private:
	uint16 old_CS, old_IP;

public:
	uint32 timestamp;
private:
	int32 ICount;

	v30mz_regs_t I;
	bool InHLT;

	uint32 prefix_base;	/* base address of the latest prefix segment */
	char seg_prefix;		/* prefix segment indicator */

	uint8 parity_table[256];

	uint32 EA;
	uint16 EO;
	uint16 E16;

	struct {
	struct {
		V30MZEnum::WREGS w[256];
		V30MZEnum::BREGS b[256];
	} reg;
	struct {
		V30MZEnum::WREGS w[256];
		V30MZEnum::BREGS b[256];
	} RM;
	} Mod_RM;

private:
	void nec_interrupt(unsigned int_num);
	bool CheckInHLT();
	void DoOP(uint8 opcode);

	void i_real_pushf();
	void i_real_popf();

	void i_real_insb();
	void i_real_insw();
	void i_real_outsb();
	void i_real_outsw();
	void i_real_movsb(); 
	void i_real_movsw(); 
	void i_real_cmpsb(); 
	void i_real_cmpsw(); 
	void i_real_stosb(); 
	void i_real_stosw(); 
	void i_real_lodsb(); 
	void i_real_lodsw(); 
	void i_real_scasb(); 
	void i_real_scasw(); 

private:
	unsigned EA_000(); 
	unsigned EA_001(); 
	unsigned EA_002(); 
	unsigned EA_003(); 
	unsigned EA_004(); 
	unsigned EA_005(); 
	unsigned EA_006(); 
	unsigned EA_007(); 

	unsigned EA_100(); 
	unsigned EA_101(); 
	unsigned EA_102(); 
	unsigned EA_103(); 
	unsigned EA_104(); 
	unsigned EA_105(); 
	unsigned EA_106(); 
	unsigned EA_107(); 

	unsigned EA_200(); 
	unsigned EA_201(); 
	unsigned EA_202(); 
	unsigned EA_203(); 
	unsigned EA_204(); 
	unsigned EA_205(); 
	unsigned EA_206(); 
	unsigned EA_207();

private:
	void SetupEA();
	
	typedef unsigned(V30MZ::*EAFPtr)();
	EAFPtr GetEA[192];

	// memory callback system plumbing
public:
	void (*ReadHook)(uint32 addr);
	void (*WriteHook)(uint32 addr);
	void (*ExecHook)(uint32 addr);
private:
	uint8 cpu_readop(uint32 addr);
	uint8 cpu_readop_arg(uint32 addr);
	uint8 cpu_readmem20(uint32 addr);
	void cpu_writemem20(uint32 addr, uint8 val);


public:
	System *sys;
	template<bool isReader>void SyncState(NewState *ns);
};


enum {
	NEC_PC=1, NEC_AW, NEC_CW, NEC_DW, NEC_BW, NEC_SP, NEC_BP, NEC_IX, NEC_IY,
	NEC_FLAGS, NEC_DS1, NEC_PS, NEC_SS, NEC_DS0
};

}

#endif
