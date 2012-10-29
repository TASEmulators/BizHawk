/*
this file contains the core psx system and cpu emulation declarations.
implementations may be in several places.
*/

#pragma once

//#define NOCASE() __assume(0);
#define NOCASE()
#define ENABLE_CONOUT true

//#define DPRINT(...)
#define NOPRINT(...)
#define DPRINT(...) { fprintf(__VA_ARGS__); }
#define DEBUG(...) { DPRINT(stdout,__VA_ARGS__); }
#define DEBUG_PRINT(...) { NOPRINT(stderr,__VA_ARGS__); }
#define DEBUG_LOAD(...) { NOPRINT(stdout,__VA_ARGS__); }
#define DEBUG_STORE(...) { NOPRINT(stdout,__VA_ARGS__); }
#define DEBUG_TRACE(...) { DPRINT(stdout,__VA_ARGS__); }
#define DEBUG_HWREG(...) { DPRINT(stdout,__VA_ARGS__); }
#define DEBUG_SIO(...) { DPRINT(stdout,__VA_ARGS__); }

#define PSX_CLOCK 33868800

#include "types.h"

enum eOp
{
	eOP_NULLIFIED, //nullified by exception
	eOP_ILL, 
	eOP_SLL, eOP_SRL, eOP_SRA, eOP_SLLV,
	eOP_SRLV, eOP_SRAV,
	eOP_JR, eOP_JALR,
	eOP_SYSCALL, eOP_BREAK,
	eOP_MFHI, eOP_MTHI, eOP_MFLO, eOP_MTLO,
	eOP_MULT, eOP_MULTU, eOP_DIV, eOP_DIVU,
	eOP_ADD, eOP_ADDU,
	eOP_SUB, eOP_SUBU,
	eOP_AND, eOP_OR,
	eOP_XOR, eOP_NOR,
	eOP_SLT, eOP_SLTU,
	eOP_NULL,
	eOP_BCOND, 
	eOP_J, eOP_JAL,
	eOP_BEQ, eOP_BNE, eOP_BLEZ, eOP_BGTZ,
	eOP_ADDI, eOP_ADDIU,
	eOP_SLTI, eOP_SLTIU,
	eOP_ANDI, eOP_ORI,
	eOP_XORI, eOP_LUI,
	eOP_COPROC,
	eOP_LB, eOP_LH, eOP_LWL, eOP_LW, eOP_LBU, eOP_LHU, eOP_LWR, eOP_SB, eOP_SH, eOP_SWL, eOP_SW, eOP_SWR,
	eOP_LWC0, eOP_LWC1, eOP_LWC2, eOP_LWC3,
	eOP_SWC0, eOP_SWC1, eOP_SWC2, eOP_SWC3
};


//reference PSXJIN sources for this
#pragma pack(push,1)
struct PSX_EXE_Header
{
	char magic[8]; //PS-X EXE
	u32 text_exe_offset; //location of text section in exe image
	u32 data_exe_offset; //location of data section in exe image
	u32 init_pc; //initial PC of program
	u32 init_gp; //initial GP of program
	u32 text_load_addr; //detination load address of text section
	u32 text_size; //size of text section
	u32 data_load_addr; //detination load address of data section
	u32 data_size; //size of data section	
	u32 b_addr, b_size; //unknown
	u32 stack_load_addr; //aka init_sp; initial SP of program
	u32 stack_size; //stack size (not used?)
	u32 saved_sp, saved_fp, saved_gp, saved_ra, saved_so; //not used
};
#pragma pack(pop)

#define RAM_SIZE (8*1024*1024)
#define RAM_MASK (RAM_SIZE-1)
#define VRAM_SIZE (1024*1024)
#define VRAM_MASK (VRAM_SIZE-1)
#define BIOS_SIZE (512*1024)
#define BIOS_MASK (BIOS_SIZE-1)
#define SCRATCH_SIZE (1024)
#define SCRATCH_MASK (SCRATCH_SIZE-1)
#define PIO_SIZE (65536)
#define PIO_MASK (PIO_SIZE-1)

//this class should try to organize all psx system state and methods as compactly as possible.
//let's not mix up anything else in here.
class __declspec(dllexport) PSX
{
public:
  u8 vram[VRAM_SIZE];
  u8 bios[BIOS_SIZE];
	u8 scratch[SCRATCH_SIZE];
	u8 pio[PIO_SIZE];
  u8 ram[RAM_SIZE];

	enum eScheduleItemType
	{
		eScheduleItemType_NIL, //used as a sentinel for list processing
		eScheduleItemType_null,
		eScheduleItemType_test, //to be removed later
		eScheduleItemType_sio0,
		eScheduleItemType_sio1,
		eScheduleItemType_gpu,
		eScheduleItemType_NUM
	};
	struct SCHED
	{
		struct IScheduleItem
		{
			u32 time;
			eScheduleItemType next, prev;
		};

		union
		{
			IScheduleItem items[eScheduleItemType_NUM];
			struct
			{
				IScheduleItem NIL, null, test;
				IScheduleItem sio[2];
				IScheduleItem gpu;
			};
		};
		eScheduleItemType head;
		u32 nextTime;
		void escape() { nextTime = 0; }
		
		//dequeues the head item from the list.
		eScheduleItemType dequeue();
		//removes the specified item from the list
		void remove(eScheduleItemType todoType);
		//inserts this item to the list in sorted order by timestamp
		void insert(eScheduleItemType todoType);

	} sched;


	struct IRQ
	{
		static const u16 WIRE_MASK = 0x3FD;
		union
		{
			struct 
			{
				u16 vsync:1, gpu:1, cd:1, dma:1, rcnt0:1, rcnt1:1, rcnt2:1, sio0:1, sio1:1, spu:1, extcd:1;
				u16 unknown2:6;
			};
			u16 value;
		} flags;
		union
		{
			struct 
			{
				u16 vsync:1, gpu:1, cd:1, dma:1, rcnt0:1, rcnt1:1, rcnt2:1, sio0:1, sio1:1, spu:1, extcd:1;
				u16 unknown2:6;
			};
			u16 value;
		} mask;
	} irq;

	struct SystemRegs
	{
		//regs starting at 0x1f801000 accessed at bios init and maybe other times.
		u32 biosInit[9]; //mednafen knows a little bit about these but doesnt use them for anything
	} sysregs;

	struct SioController
	{
		union
		{
			struct
			{
				u16 prescaler_type:2;
			};
			u16 value;
		} mode;

		union StatusReg
		{
			struct
			{
				u16 TX_RDY:1;
				u16 RX_RDY:1;
				u16 TX_EMPTY:1;
				u16 nothing:1;
				u16 OVERRUN:1;
				u16 nothing2:2;
				u16 DSR:1;
				u16 nothing3:1;
				u16 IRQ:1;
			};
			u16 value;
		};

		union ControlReg
		{
			struct 
			{
				u16 TX_ENA:1;
				u16 DTR:1; //this differs from mame's code (this seems to be the actual DTR signal. )
				u16 nothing:2;
				u16 IACK:1;
				u16 nothing2:1;
				u16 RESET:1;
				u16 nothing3:3;
				u16 TX_IENA:1;
				u16 RX_IENA:1;
				u16 DSR_IENA:1;
				u16 PORT_SEL:1; //this differs from mame's code (this seems to be the port select). but that doesnt make sense because the 5x registers should be used for the other port. is the other port the actual serial i/o module?
			};
			u16 value;
		};

		StatusReg status;
		ControlReg control;
		u16 baud_reg;
		//this is just used for diagnostic purposes (maybe it should be labeled as such?)
		float CalculateBaud();

		void Reset();

	} sio[2];

  struct CPU
  {
		enum eException //taken from mednafen
		{
			eException_INT = 0,
			eException_MOD = 1,
			eException_TLBL = 2,
			eException_TLBS = 3,
			eException_ADEL = 4, // Address error on load
			eException_ADES = 5, // Address error on store
			eException_IBE = 6, // Instruction bus error
			eException_DBE = 7, // Data bus error
			eException_SYSCALL = 8, // System call
			eException_BP = 9, // Breakpoint
			eException_RI = 10, // Reserved instruction
			eException_COPU = 11,  // Coprocessor unusable
			eException_OV = 12,	// Arithmetic overflow
			eException_None = 16
		};

    union
    {
      struct
      {
        u32 r0, //should this be called zero or r0? well, psxjin and mednafen call it r0 so we'll stick with it
					at, v0, v1, a0, a1, a2, a3,
          t0, t1, t2, t3, t4, t5, t6, t7,
          s0, s1, s2, s3, s4, s5, s6, s7,
          t8, t9, k0, k1, gp, 
					sp, //29 - stack pointer
					s8, //30 - ??
					ra, //31 - return address (link register)
          lo, hi;
          //pc;
      };
      
      //Lo, Hi in r[33] and r[34];
      //PC in r[35]
      u32 r[34]; 
    } regs;

		union SR_REG
		{
			//these bits must be zero when read from the register (not wired to anything) [todo - check whether they cache values]
			static const int ZERO_MASK = 0x8D8000C0;

			//info taken from http://psx.rules.org/system.txt
			struct
			{
				u32
					IEc:1, //Interrupts Enabled current (0=enabled, 1=disabled) 
					KUc:1, //KUcurrent: privilege level (0=user, 1=kernel)
					IEp:1, //?
					KUp:1, //KUpushed: KUc pushes here on an exception, rfe pops KUo here
					IEo:1, //Interrupts Enabled (0=enabled, 1=disabled) (rfe pops KUp here)
					KUo:1, //KUother?: KUp gets pushed here on an exception
					zeros:2, 
					IM:8, //interrupt mask fields (are these used in any way? im not sure. you would expect at least one of them to be) [bit2 maybe?]
					IsC:1, //Isolate [data] Cache: unhook data cache from memory. PSX has no data cache, so this causes memory writes to get discarded
					SwC:1, //Swap Cache: Not sure what this does on PSX but its suggested to use with IsC to invalidate the I-Cache. May need to investigate this.
					PZ:1, //When set cache parity bits are written as 0
					CM:1, //something relating to data cache
					PE:1, //Cache parity error. Does not cause exception.
					TS:1, //TLB shutdown. Gets set if a programm address simultaniously matches 2 TLB entries.
					BEV:1, //boot exception vectors (0=ram, 1=rom [kseg1])
					zeros_2:2, 
					RE:1, //reverse endianness. hope nobody uses this!
					zeros_3:2, 
					CU0:1, //coprocessor 0 control (0=kernel mode, 1=user mode); controls access to certain instructions
					CU1:1, //coprocessor 1 enabled (does nothing in PSX. should it return zeros?)
					CU2:1, //coprocessor 2 enabled
					zeros_4:1;
			};
			u32 value;
		};
		union CAUSE_REG
		{
			struct
			{
				u32 zeros_1:2;
				u32 ExcCode:4;
				u32 zeros_2:2;
				u32 Sw:2; //software interrupts ?
				u32 IP:6; //interrupts pending (external interrupts latched? IP[5..0] = Interrupt[5..0] according to r2000 arch doc)
				u32 zeros_3:12;
				u32 CE:2; //coprocessor error-which coprocessor threw a Coprocessor Unusable exception?
				u32 zeros_4:1;
				u32 BD:1; //set to 1 if the last exception was taken while executing in a branch delay slot
			};
			u32 value;
		};

		union
		{
			struct
			{
				u32
					Index,     Random,    EntryLo0,  EntryLo1,
					Context,   PageMask,  Wired,     Reserved0,
					BadVAddr,  Count,     EntryHi,   Compare;
				
				SR_REG SR; //12 - status register
				CAUSE_REG Cause; //13 - cause register
	
				u32	EPC; //14 - the PC of the victim
				u32 PRid; //15 - ??

				u32
					Config,    LLAddr,    WatchLO,   WatchHI,
					XContext,  Reserved1, Reserved2, Reserved3,
					Reserved4, Reserved5, ECC,       CacheErr,
					TagLo,     TagHi,     ErrorEPC,  Reserved6;
			};
			u32 r[32];
		} cp0;

		enum eFormat
		{
			eFormat_IType, eFormat_RType, eFormat_JType_J, eFormat_JType_JAL, eFormat_CType, eFormat_Other
		};

		struct Instruction_ITYPE
		{
			u32 immediate:16;
			s32 signed_offset() const { return (s16)immediate; }
			s32 signed_target() const { return (signed_offset()<<2) + 4; /*hack!!! dunno how this PC accounting is supposed to work*/ }
			u32 rt:5;
			u32 rs:5;
			u32 base() const { return rs; }
			u32 opcode:6;
		};

		struct Instruction_CTYPE
		{
			u32 function:6;
			u32 zeros:5;
			u32 rd:5;
			u32 rt:5;
			u32 format:5;
			u32 cpnum:2;
			u32 opcode_hi:4;
		};

		union Instruction_RTYPE
		{
			struct 
			{
				u32 function:6;
				u32 sa:5;
				u32 rd:5;
				u32 rt:5;
				u32 rs:5;
				u32 opcode:6;
			};
			u32 value;
			u32 break_code() const { return (value>>6)&0xFFFFF; }
		};

		struct Instruction_JTYPE
		{
			u32 target:26;
			u32 opcode:6;
		};

		union Instruction
		{
			Instruction_ITYPE ITYPE;
			Instruction_RTYPE RTYPE;
			Instruction_JTYPE JTYPE;
			Instruction_CTYPE CTYPE;
			u32 value;
		};

		struct DecodedInstruction
		{
			Instruction instr;
			eOp op;
		};

		struct {
			bool IsRunning() const { return timer>0; }
			u32 timer;
			u32 lo, hi;
		} unit_muldiv;

		struct 
		{
			u32 in_fetch_addr;
			DecodedInstruction decode; //call this output?
		} p_fetch;

		struct 
		{
			u32 instr;
			u32 regs[3];
		} p_rd;

		enum eMemOp
		{
			eMemOp_Unset, eMemOp_None, 
			eMemOp_StoreWord, eMemOp_StoreHalfword, eMemOp_StoreByte,
			eMemOp_LoadWord, eMemOp_LoadHalfwordSigned, eMemOp_LoadHalfwordUnsigned, eMemOp_LoadByteSigned, eMemOp_LoadByteUnsigned,
			eMemOp_MTC, eMemOp_MFC,
		};

		struct ALU_OUTPUT
		{
			union
			{
				u32 addr;
			};
			union {
				u32 value;
				u32 rt;
			};
			eMemOp op;
		};

		struct ALU_PC_OUTPUT
		{
			u32 pc;
			bool enabled;
		};

		struct P_ALU
		{
			DecodedInstruction decode;
			//TODO its a shame to copy a big alu output every time.. try to avoid that somehow? thats a serious micro-optimization though..
			ALU_OUTPUT out_mem;
			ALU_PC_OUTPUT out_pc;
			u32 in_pc;
			CPU::eException exception;
		} p_alu;

		enum eStall
		{
			eStall_None=0,
			eStall_MulDiv=1,
		};

		u32 stall_depends;
		u32 stall_user;

		struct
		{
			DecodedInstruction decode;
			ALU_OUTPUT in_from_alu;
		} p_mem;


		enum eDelayState
		{
			eDelayState_None,
			eDelayState_Branch, //set PC to a branch target arg
			eDelayState_BranchRelative, //set PC relative (arg as s32) to current PC (that will be the PC of the instruction in the branch delay slot)
			eDelayState_StoreWord, //store a word arg2 to memory at arg
			eDelayState_StoreHalfword, //store a halfword arg2 to memory at arg
			eDelayState_StoreByte,  //store a byte arg2 to memory at arg
			eDelayState_SetGPR, //set GPR arg to value arg2
			eDelayState_MTCz, //set MTCz, reg # arg, to value arg2 (z and reg are packed into arg)
		};

		struct DelayState
		{
			eDelayState state;
			u32 arg,arg2;
		} delay;
  } cpu;

	enum eConsoleType
	{
		eConsoleType_Normal,
		eConsoleType_DTL
	};

	struct
	{
		u32 ram_size;
		u32 ram_mask;
	} config;

	void poweron(eConsoleType type);
	void reset();

	//execs one psx cycle
	void exec_cycle();
	void exec_shed(eScheduleItemType type);

	static CPU::eFormat util_decode_format(u32 opcode);
	void TraceALU();
	void cpu_break(const u32 code);
  void cpu_run_alu_bioshack(); //called once per cycle to implement bios hacks (stdout mostly)
	void cpu_run_alu_bioshack_putchar(const u32 regval);

	//debug-pokes a value into the psx address space (maybe rename to poke)
	//TODO - make a check argument for sanity checking (to keep from patching the wrong thing)
	void patch(const u32 addr, const u32 val);

	//executes one cpu cycle
  void cpu_exec_cycle();

	//run each of the pipeline stages
	void cpu_run_fetch();
	void cpu_run_muldiv();
	void cpu_run_alu();
	void cpu_run_mem();
	void cpu_run_wb();

	//trigger an exception
	void cpu_exception(CPU::eException ex, u32 pc_victim);

	//coprocessor interfaces to be called from the alu pipeline
	void cpu_copz_mtc(const u32 z, const u32 rd, const u32 value);
	void cpu_cop0_mtc(const u32 rd, const u32 value);
	u32 cpu_copz_mfc(const u32 z, const u32 rd);
	u32 cpu_cop0_mfc(const u32 rd);
	
	//main memory io interfaces
	u32 cpu_fetch(const u32 addr);
	template<int size> u32 cpu_rdmem(const u32 addr);
	template<int size, bool POKE> void cpu_wrmem(const u32 addr, const u32 val);
	template<int size> void cpu_wrmem(const u32 addr, const u32 val);

	//memory mapping handlers
	void cpu_wr_ram(const int size, const u32 addr, const u32 val);
	u32 cpu_rd_ram(const int size, const u32 addr);
	void cpu_wr_scratch(const int size, const u32 addr, const u32 val);
	u32 cpu_rd_scratch(const int size, const u32 addr);
	void cpu_wr_bios(const int size, const u32 addr, const u32 val);
	u32 cpu_rd_bios(const int size, const u32 addr);
	void cpu_wr_pio(const int size, const u32 addr, const u32 val);
	u32 cpu_rd_pio(const int size, const u32 addr);
	void cpu_wr_quick(u8* const buf, const int size, const u32 addr, const u32 val);
	u32 cpu_rd_quick(const u8* const buf, const int size, const u32 addr);
	void cpu_wr_hwreg(const int size, const u32 addr, const u32 val);
	u32 cpu_rd_hwreg(const int size, const u32 addr);
	
	void spu_wr(const int size, const u32 addr, const u32 val);
	u32 spu_rd(const int size, const u32 addr);
	
	void sio_wr(const int size, const u32 addr, const u32 val);
	u32 sio_rd(const int size, const u32 addr);
	//sets the dtr rising edge signal for the specified port (e.g. strobe signal; port should latch its values then presumably)
	void sio_dtr(const u32 port);

	u32 irq_rd(const int size, const u32 addr);
	void irq_wr(const int size, const u32 addr, const u32 val);
	void irq_update();

	//miscellaneous not real stuff
	u32 counter;
	u64 abscounter;
	//this will be used to boot the game if an appropriate signal is received
	PSX_EXE_Header exeBootHeader;

	enum eFakeBreakOp
	{
		eFakeBreakOp_None=0,
		eFakeBreakOp_BootEXE=1,
		eFakeBreakOp_BiosHack=2,
	};

	void RunForever();
	void vblank_trigger();
};

