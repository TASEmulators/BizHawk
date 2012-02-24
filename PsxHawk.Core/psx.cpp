/*
this file contains the core psx system and cpu emulation.
where readily possible, components have been split off into separate modules.
*/

//http://www.d.umn.edu/~gshute/spimsal/talref.html

#include "psx.h"
#include "dis.h"
#include <stdio.h>
#include <assert.h>
#include <string.h>
#include <intrin.h>
#include <Windows.h>

bool dotrace = false;

void PSX::reset()
{
	cpu.p_fetch.in_fetch_addr = 0xbfc00000; //this is the reset vector
}

void PSX::poweron(eConsoleType type)
{
	//the psx struct only contains blittable data, so we can just memset it here to make sure we've got complete coverage
	//(we'll put any non-blittable stuff behind one pointer later so we can reconstruct it as necessary)
	memset(this,0,sizeof(this));

	//reset cp0 regs (should this be in reset?)
	memset(&cpu.cp0,0,sizeof(cpu.cp0));
	cpu.cp0.SR.BEV = 1; //taken from mednafen
	cpu.cp0.SR.TS = 1; //taken from mednafen
	cpu.cp0.PRid = 0x300; // PRId: FIXME(test on real thing) //taken from mednafen

	//clear cpu state: pipeline setup
	cpu.unit_muldiv.timer = 0;
	//p_fetch will be taken care of by the bootstrapper
	cpu.p_alu.out_pc.enabled = false;
	cpu.p_alu.in_pc = 0;
	cpu.p_alu.decode.instr.value = 0;
	cpu.p_alu.decode.op = eOP_NULL;
	cpu.p_mem.in_from_alu.op = CPU::eMemOp_None;

	//clear sio
	sio[0].Reset();
	sio[1].Reset();

	//reset the scheduling system
	sched.null.time = 0xFFFFFFFF; //this item will never happen so it will float at the end as a sentinel (it will continually get rebased)
	//actually, its already at the end due to the memset
	sched.head = eScheduleItemType_null;
	sched.gpu.time = PSX_CLOCK / 60;
	sched.insert(eScheduleItemType_gpu);

	//setup the system configuration
	//programs built targeting DTL units will ask for memory near the end of 8MB at program boot-up time (maybe other times?). maybe it puts the heap there? not sure about it.
	//anyway, we need to handle that much memory if we want those to work.
	//be aware that right now, psxhawk will mirror 2MB across the 8MB region for retail consoles, until we get better memory mapping
	if(type == eConsoleType_DTL) config.ram_size = 8*1024*1024;
	else config.ram_size = 2*1024*1024;
	config.ram_mask = config.ram_size - 1;
}

PSX::eScheduleItemType PSX::SCHED::dequeue()
{
	eScheduleItemType ret = head;
	head = items[head].next;
	//todo - rmeove this for a trivial optimization (but not from debug builds)
	items[ret].next = eScheduleItemType_NIL;
	items[ret].prev = eScheduleItemType_NIL;
	return ret;
}

void PSX::SCHED::remove(eScheduleItemType todoType)
{
	IScheduleItem &todo = items[todoType];
	eScheduleItemType prevType = todo.prev;
	eScheduleItemType nextType = todo.next;
	if(prevType != eScheduleItemType_NIL) items[prevType].next = nextType;
	if(nextType != eScheduleItemType_NIL) items[nextType].prev = prevType;
	if(head == todoType)
		head = nextType;
}

void PSX::SCHED::insert(eScheduleItemType todoType)
{
	IScheduleItem &todo = items[todoType];

	//ensure that it isnt already in the list
	assert(todo.prev == eScheduleItemType_NIL && todo.next == eScheduleItemType_NIL);

	//insert this schedule item into the list in sorted order
	u32 todo_time = todo.time;
	eScheduleItemType prevType = eScheduleItemType_NIL;
	eScheduleItemType currType = head;
	while(items[currType].time < todo_time) //TODO - tiebreaker priorities?
	{
		prevType = currType;
		currType = items[currType].next;
	}
	//now, currType and prevType are pointing correctly.
	if(prevType == eScheduleItemType_NIL)
	{
		//handle the case where this is the new head
		items[todoType].next = head;
		items[head].prev = todoType;
		head = todoType;
	}
	else
	{
		//handle the case where it isnt the new head
		items[todoType].next = items[prevType].next;
		items[prevType].next = todoType;
		items[todoType].prev = prevType;
		items[currType].prev = todoType;
	}
}

void PSX::exec_shed(eScheduleItemType type)
{
	switch(type)
	{
	case eScheduleItemType_null:
		break;
	case eScheduleItemType_test:
		printf("TEST!!!\n");
		sched.test.time += 100000;
		sched.insert(eScheduleItemType_test);
		break;
	case eScheduleItemType_gpu:
		sched.gpu.time += PSX_CLOCK / 60;
		sched.insert(eScheduleItemType_gpu);
		vblank_trigger();
		break;
	}
}

void PSX::exec_cycle()
{
	while(counter >= sched.items[sched.head].time)
	{
		eScheduleItemType todo = sched.dequeue();
		exec_shed(todo);
	}
	sched.nextTime = sched.items[sched.head].time;

	if(irq.flags.value & irq.mask.value)
	{
		dotrace = true;
		cpu_exception(CPU::eException_INT, cpu.p_alu.in_pc);
		//nullify instructions which will be rerun.
		//in this emulator, interrupts cancel the instruction in the alu (notice we set it as the victim in the exception logic above)
		//instructions in MEM and WB will proceed normally
		//this was chosen because we didnt want to have to drag the PC down the pipeline and give it to the MEM stage
		//HOWEVER -- this is maybe unrealistic (maybe we should nullify MEM and ALU and make MEM the victim) 
		//and maybe we'll have to drag the PC down later anyway for other proper exception handling
		//(see pg 94 of see mips run)
		cpu.p_alu.decode.op = eOP_NULLIFIED;
	}

	while(counter < sched.nextTime)
		cpu_exec_cycle();
}

void PSX::vblank_trigger()
{
	irq.flags.vsync = 1;
	irq_update();
}

void PSX::cpu_wr_quick(u8* const buf, const int size, const u32 addr, const u32 val)
{
	if(size==1) buf[addr] = val;
	else if(size==2) {
		*(u16*)&buf[addr] = val;
	}
	else {
		*(u32*)&buf[addr] = val;
	}
}
u32 PSX::cpu_rd_quick(const u8* const buf, const int size, const u32 addr)
{
	if(size==1) return buf[addr];
	else if(size==2) {
		return *(u16*)&buf[addr];
	}
	else {
		return *(u32*)&buf[addr];
	}
}

u32 PSX::cpu_rd_ram(const int size, const u32 addr)
{
	if(size==1) 
		return ram[addr];
	else if(size==2) {
		return *(u16*)&ram[addr];
	}
	else {
		return *(u32*)&ram[addr];
	}
}

void PSX::cpu_wr_ram(const int size, const u32 addr, const u32 val)
{
	if(size==1) 
		ram[addr] = val;
	else if(size==2) {
		*(u16*)&ram[addr] = val;
	}
	else {
		*(u32*)&ram[addr] = val;
	}
}

void PSX::cpu_wr_scratch(const int size, const u32 addr, const u32 val) { cpu_wr_quick(scratch,size,addr,val); }
u32 PSX::cpu_rd_scratch(const int size, const u32 addr) { return cpu_rd_quick(scratch,size,addr); }
void PSX::cpu_wr_bios(const int size, const u32 addr, const u32 val) { cpu_wr_quick(bios,size,addr,val); }
u32 PSX::cpu_rd_bios(const int size, const u32 addr) { return cpu_rd_quick(bios,size,addr); }
void PSX::cpu_wr_pio(const int size, const u32 addr, const u32 val) { cpu_wr_quick(pio,size,addr,val); }
u32 PSX::cpu_rd_pio(const int size, const u32 addr) { return cpu_rd_quick(pio,size,addr); }

template<int size> void PSX::cpu_wrmem(u32 addr, const u32 val) { cpu_wrmem<size,false>(addr,val); }
template<int size, bool POKE> void PSX::cpu_wrmem(u32 addr, const u32 val)
{
	if(!POKE && cpu.cp0.SR.IsC) 
		return; //IsC (isolate [data] cache is set). theres no data cache here, so discard write

	if(addr<0x00800000) { cpu_wr_ram(size,addr&config.ram_mask,val); return; }
	if(addr<0x1F000000) goto BUS_ERROR;
	if(addr<0x1F010000) { cpu_wr_pio(size,addr&PIO_MASK,val); return; }
	if(addr<0x1F800000) goto BUS_ERROR;
	if(addr<0x1F800400) { cpu_wr_scratch(size,addr&SCRATCH_MASK,val); return; }
	if(addr<0x1F801000) goto BUS_ERROR;
	if(addr<0x1FC00000) { cpu_wr_hwreg(size,addr,val); return; }
	if(addr<0x1FC80000) goto BUS_ERROR; //can't write to bios?
	if(addr<0x80000000) goto BUS_ERROR;

	if(addr<0x80800000) { cpu_wr_ram(size,addr&config.ram_mask,val); return; }
	if(addr<0x9F000000) goto BUS_ERROR;
	if(addr<0x9F010000) { cpu_wr_pio(size,addr&PIO_MASK,val); return; }
	if(addr<0x9FC00000) goto BUS_ERROR;
	if(addr<0x9FC80000) goto BUS_ERROR; //can't write to bios?
	if(addr<0xA0000000) goto BUS_ERROR;

	if(addr<0xA0800000) { cpu_wr_ram(size,addr&config.ram_mask,val); return; }
	if(addr<0xBF000000) goto BUS_ERROR;
	if(addr<0xBF010000) { cpu_wr_pio(size,addr&PIO_MASK,val); return; }
	if(addr<0xBFC00000) goto BUS_ERROR;
	if(addr<0xBFC80000) { if(POKE) { cpu_wr_bios(size,addr&BIOS_MASK,val); return; } else goto BUS_ERROR; } //can't write to bios?
	
	if(addr == 0xFFFE0130)
	{
		//check psx sources for information on this. it seems to make everything read only?
		//DEBUG("mystery 0xFFFE0130 reg set to 0x%08X\n",val);
		return;
	}

	goto BUS_ERROR;

BUS_ERROR:
	DEBUG("bus error exception (write) at 0x%08X\n",addr);
}

void PSX::patch(const u32 addr, const u32 val)
{
	//should use the poke versions one day
	cpu_wrmem<4,true>(addr,val);
}

void PSX::spu_wr(const int size, const u32 addr, const u32 val)
{
	//1d80/1d82 main volume left / right
	//1d84/1d86 reverberation depth left / right
	DEBUG_HWREG("spu write size %d addr %08X = %08X\n",size,addr,val);
}

u32 PSX::spu_rd(const int size, const u32 addr)
{
	u32 ret = 0;
	DEBUG_HWREG("spu read size %d addr %08X = %08X\n",size,addr,ret);
	return 0;
}

void PSX::irq_wr(const int size, const u32 addr, const u32 val)
{
	if(addr == 0)
	{
		//acknowledge the specified irq bits
		irq.flags.value &= ~(val&IRQ::WIRE_MASK);
	}
	else if(addr == 4)
	{
		//set the irq mask directly
		irq.mask.value = val & IRQ::WIRE_MASK;
	}
	else assert(false);
	irq_update();
}

u32 PSX::irq_rd(const int size, const u32 addr)
{
	if(addr == 0)
		return irq.flags.value;
	else if(addr == 4)
		return irq.mask.value;
	else assert(false);
}

void PSX::irq_update()
{
	sched.escape();
}

void PSX::cpu_wr_hwreg(const int size, const u32 addr, const u32 val)
{
	if(addr>=0x1F801C00 && addr <= 0x1F801DFF)
	{
		spu_wr(size,addr,val);
		return;
	}
	else if(addr>=0x1F801040 && addr < 0x1F80105F)
	{
		sio_wr(size,addr,val);
		return;
	}
	else switch(addr)
	{
	case 0x1F801000: sysregs.biosInit[0] = val; break;
	case 0x1F801004: sysregs.biosInit[1] = val; break;
	case 0x1F801008: sysregs.biosInit[2] = val; break;
	case 0x1F80100C: sysregs.biosInit[3] = val; break;
	case 0x1F801010: sysregs.biosInit[4] = val; break;
	case 0x1F801014: sysregs.biosInit[5] = val; break;
	case 0x1F801018: sysregs.biosInit[6] = val; break;
	case 0x1F80101C: sysregs.biosInit[7] = val; break;
	case 0x1F801020: sysregs.biosInit[8] = val; break;
	//case 0x1f801080-0x1f8010ff dma
	//case 0x1F801100/10/20/30 root counters
	case 0x1F801070: irq_wr(size, 0, val); break;
	case 0x1F801071: assert(false);
	case 0x1F801072: assert(false);
	case 0x1F801073: assert(false);
	case 0x1F801074: irq_wr(size, 4, val); break;
	case 0x1F801075: assert(false);
	case 0x1F801076: assert(false);
	case 0x1F801077: assert(false);
	//1F801060 // unknown
	//1F801D80 // unknown
	//1f802041 // unknown
	default:
		DEBUG_HWREG("UNKNOWN ");
	}
	DEBUG_HWREG("hwreg write size %d addr %08X = %08X\n",size,addr,val);
}
u32 PSX::cpu_rd_hwreg(const int size, const u32 addr)
{
	u32 ret = 0;
	if(addr>=0x1F801C00 && addr <= 0x1F801DFF)
	{
		return spu_rd(size,addr);
	}
	else if(addr>=0x1F801040 && addr < 0x1F80105F)
	{
		return sio_rd(size,addr);
	}
	else switch(addr)
	{
	case 0x1F801000: ret = sysregs.biosInit[0]; break;
	case 0x1F801004: ret = sysregs.biosInit[1]; break;
	case 0x1F801008: ret = sysregs.biosInit[2]; break;
	case 0x1F80100C: ret = sysregs.biosInit[3]; break;
	case 0x1F801010: ret = sysregs.biosInit[4]; break;
	case 0x1F801014: ret = sysregs.biosInit[5]; break;
	case 0x1F801018: ret = sysregs.biosInit[6]; break;
	case 0x1F80101C: ret = sysregs.biosInit[7]; break;
	case 0x1F801020: ret = sysregs.biosInit[8]; break;
	case 0x1F801070: ret = irq_rd(size, 0); break;
	case 0x1F801071: assert(false);
	case 0x1F801072: assert(false);
	case 0x1F801073: assert(false);
	case 0x1F801074: ret = irq_rd(size, 4); break;
	case 0x1F801075: assert(false);
	case 0x1F801076: assert(false);
	case 0x1F801077: assert(false);
	default:
		DEBUG_HWREG("UNKNOWN ");
		break;
	}
	DEBUG_HWREG("hwreg read size %d addr %08X = %08X\n",size,addr,ret);
	return ret;
}

template<int size> u32 PSX::cpu_rdmem(const u32 addr)
{
	if(addr<0x00800000) return cpu_rd_ram(size,addr&config.ram_mask);
	if(addr<0x1F000000) goto BUS_ERROR;
	if(addr<0x1F010000) return cpu_rd_pio(size,addr&PIO_MASK);
	if(addr<0x1F800000) goto BUS_ERROR;
	if(addr<0x1F800400) return cpu_rd_scratch(size,addr&SCRATCH_MASK);
	if(addr<0x1F801000) goto BUS_ERROR;
	if(addr<0x1FC00000) return cpu_rd_hwreg(size,addr);
	if(addr<0x1FC80000) return cpu_rd_bios(size,addr&BIOS_MASK);
	if(addr<0x80000000) goto BUS_ERROR;

	if(addr<0x80800000) return cpu_rd_ram(size,addr&config.ram_mask);
	if(addr<0x9F000000) goto BUS_ERROR;
	if(addr<0x9F010000) return cpu_rd_pio(size,addr&PIO_MASK);
	if(addr<0x9FC00000) goto BUS_ERROR;
	if(addr<0x9FC80000) return cpu_rd_bios(size,addr&BIOS_MASK);
	if(addr<0xA0000000) goto BUS_ERROR;

	if(addr<0xA0800000) return cpu_rd_ram(size,addr&config.ram_mask);
	if(addr<0xBF000000) goto BUS_ERROR;
	if(addr<0xBF010000) return cpu_rd_pio(size,addr&PIO_MASK);
	if(addr<0xBFC00000) goto BUS_ERROR;
	if(addr<0xBFC80000) return cpu_rd_bios(size,addr&BIOS_MASK);
	
	goto BUS_ERROR;

BUS_ERROR:
	DEBUG("bus error exception (read) at 0x%08X\n",addr);
	return 0;
}

void PSX::cpu_copz_mtc(const u32 z, const u32 rd, const u32 value)
{
	//DEBUG("Writing cop%d, %d = 0x%08X\n",z,rd,value);
	if(z==0) cpu_cop0_mtc(rd,value);
}

void PSX::cpu_cop0_mtc(const u32 rd, const u32 value)
{
	cpu.cp0.r[rd] = value;
	cpu.cp0.SR.value &= ~CPU::SR_REG::ZERO_MASK;
	//TODO - other register masks
}

u32 PSX::cpu_copz_mfc(const u32 z, const u32 rd)
{
	//DEBUG("Reading cop%d, %d\n",z,rd);
	if(z==0) return cpu_cop0_mfc(rd);
	return 0;
}

u32 PSX::cpu_cop0_mfc(const u32 rd)
{
	return cpu.cp0.r[rd];
}

u32 PSX::cpu_fetch(const u32 addr)
{
	return cpu_rdmem<4>(addr);
}

void PSX::cpu_run_alu_bioshack()
{
	const u32 ram_pc = cpu.p_alu.in_pc & 0x0FFFFFFF;

	const u32 function = cpu.regs.t1;
	switch(function)
	{
	case 0x00: break; //open
	case 0x16: break; //strncat
	case 0x18: break; //strncmp
	case 0x35: //lsearch?? but yet the sdk uses this for printf. maybe it depends on the bios version
		//also the putchar() stdlib function uses this by dropping a character in some kind of buffer that is reserved for it
		if(ENABLE_CONOUT)
		{
			//TODO - make this safer, if necessary, by not going straight into main ram
			//a1 = address of string
			//a2 = length of string
			u8* const raw_addr = ram + (cpu.regs.a1 & config.ram_mask);
			const u32 len = cpu.regs.a2;
			for(u32 i=0;i<len;i++)
				fputc(raw_addr[i],stdout);
		}
		break;
	case 0x3C: //putchar
		if(ENABLE_CONOUT) cpu_run_alu_bioshack_putchar(cpu.regs.a0);
		break;
	case 0x3D: //gets... ? but yet, the kernel uses this to print
		if(ENABLE_CONOUT) cpu_run_alu_bioshack_putchar(cpu.regs.a0);
	case 0x3F: //printf. i think putchar will get used internally, no need to handle this
		break;
	default:
		printf("unknown bios call: 0x%02X\n", function);
		break;
	}
	return;
}

void PSX::cpu_run_alu_bioshack_putchar(const u32 regval)
{
	char c = regval;
	fputc(c,stdout);
	return;
}

void PSX::cpu_break(const u32 code)
{
	switch(code)
	{
	case eFakeBreakOp_BootEXE:
		{
			//perform basic exe booting steps
			//the program has already been loaded. all we need to do is setup the regs specified in the header
			cpu.p_fetch.in_fetch_addr = exeBootHeader.init_pc;
			cpu.regs.gp = exeBootHeader.init_gp;
			cpu.regs.sp = exeBootHeader.stack_load_addr; //aka init_sp
			break;
		}
	case eFakeBreakOp_BiosHack:
		//first, run the effect of the patched instruction, which was: lui t0, 0x0000
		cpu.regs.t0 = 0;
		cpu_run_alu_bioshack();
		break;
	default:
		break;
	}
}

#define SIGNBIT(x) ((x) & 0x80000000)

void PSX::cpu_exception(CPU::eException ex, u32 pc_victim)
{
	cpu.cp0.EPC = pc_victim;
	
	cpu.cp0.Cause.ExcCode = ex;
	cpu.cp0.Cause.Sw = 0; //?
	cpu.cp0.Cause.IP = 0; //?
	cpu.cp0.Cause.CE = 0; //TBD
	cpu.cp0.Cause.BD = 0; //TODO - BD flag

	//choose the rom or ram handler according to this flag
	u32 handler = 0x80000080;
	if(cpu.cp0.SR.BEV)
		handler = 0xBFC00180;

	cpu.cp0.SR.KUo = cpu.cp0.SR.KUp;
	cpu.cp0.SR.IEo = cpu.cp0.SR.IEp;
	cpu.cp0.SR.KUp = cpu.cp0.SR.KUc;
	cpu.cp0.SR.IEp = cpu.cp0.SR.IEc;
	cpu.cp0.SR.KUc = 0;
	cpu.cp0.SR.IEc = 0;

	cpu.p_fetch.in_fetch_addr = handler;
}

void PSX::cpu_run_muldiv()
{
	if(cpu.unit_muldiv.timer > 0)
	{
		//TODO - think about whether this is the exactly right amount of time (maybe off by one)
		cpu.unit_muldiv.timer--;
		if(cpu.unit_muldiv.timer == 0)
		{
			cpu.regs.lo = cpu.unit_muldiv.lo;
			cpu.regs.hi = cpu.unit_muldiv.hi;
			cpu.stall_depends &= ~CPU::eStall_MulDiv;
		}
	}
}

void PSX::cpu_run_mem()
{
	CPU::ALU_OUTPUT &input = cpu.p_mem.in_from_alu;

	//mem stage
	switch(input.op)
	{
	case CPU::eMemOp_StoreWord:
		DEBUG_STORE("{%12lld} MEM: StoreWord [%08X] = %08X\n",abscounter,input.addr,input.value);
		cpu_wrmem<4>(input.addr,input.value);
		break;
	case CPU::eMemOp_StoreHalfword:
		DEBUG_STORE("{%12lld} MEM: StoreWord [%08X] = %04X\n",abscounter,input.addr,input.value);
		cpu_wrmem<2>(input.addr,input.value);
		break;
	case CPU::eMemOp_StoreByte:
		DEBUG_STORE("{%12lld} MEM: StoreByte [%08X] = %02X\n",abscounter,input.addr,input.value);
		cpu_wrmem<1>(input.addr,input.value);
		break;
	case CPU::eMemOp_LoadWord:
		{
			const u32 temp = cpu_rdmem<4>(input.addr);
			DEBUG_LOAD("{%12lld} MEM: LoadWord [%08X] == %08X\n",abscounter,input.addr,temp);
			cpu.regs.r[input.rt] = temp;
			break;
		}
	case CPU::eMemOp_LoadHalfwordSigned:
		{
			const u32 temp = (s16)cpu_rdmem<2>(input.addr);
			DEBUG_LOAD("{%12lld} MEM: LoadHalfwordSigned [%08X] == %04X\n",abscounter,input.addr,temp);
			cpu.regs.r[input.rt] = temp;
			break;
		}
	case CPU::eMemOp_LoadHalfwordUnsigned:
		{
			const u32 temp = cpu_rdmem<2>(input.addr);
			DEBUG_LOAD("{%12lld} MEM: LoadHalfwordUnsigned [%08X] == %04X\n",abscounter,input.addr,temp);
			cpu.regs.r[input.rt] = temp;
			break;
		}
	case CPU::eMemOp_LoadByteSigned:
		{
			const u32 temp = (s8)cpu_rdmem<1>(input.addr);
			DEBUG_LOAD("{%12lld} MEM: LoadByte [%08X] == %02X\n",abscounter,input.addr,temp);
			cpu.regs.r[input.rt] = temp;
			break;
		}
	case CPU::eMemOp_LoadByteUnsigned:
		{
			const u32 temp = cpu_rdmem<1>(input.addr);
			DEBUG_LOAD("{%12lld} MEM: LoadByteUnsigned [%08X] == %02X\n",abscounter,input.addr,temp);
			cpu.regs.r[input.rt] = temp;
			break;
		}

	case CPU::eMemOp_MTC:
		{
			const CPU::Instruction_CTYPE instr = cpu.p_mem.decode.instr.CTYPE;
			cpu_copz_mtc(instr.cpnum,instr.rd,input.value);
			break;
		}
	case CPU::eMemOp_MFC:
		{
			const CPU::Instruction_CTYPE instr = cpu.p_mem.decode.instr.CTYPE;
			cpu.regs.r[instr.rt] = cpu_copz_mfc(instr.cpnum,instr.rd);
			break;
		}

	case CPU::eMemOp_None:
		break;
	case CPU::eMemOp_Unset:
		printf("*** UNHANDLED MEM OP ***\n");
		break;
	default:
		NOCASE();
	}

	cpu.regs.r0 = 0;
}

void PSX::TraceALU()
{
	DEBUG_TRACE("{%12lld} %08X: [%08X] (%d,%d) %s\n",abscounter,cpu.p_alu.in_pc,cpu.p_alu.decode.instr.value,
		cpu.p_alu.decode.instr.value>>26,
		cpu.p_alu.decode.instr.value&0x3F,
		MDFN_IEN_PSX::DisassembleMIPS(cpu.p_alu.in_pc,cpu.p_alu.decode.instr.value).c_str()
		);
}

void PSX::cpu_run_wb()
{
	//TBD
}

static const eOp DecodeTable[] =
{
	eOP_SLL, eOP_ILL, eOP_SRL, eOP_SRA, eOP_SLLV, eOP_ILL, eOP_SRLV, eOP_SRAV,
	eOP_JR, eOP_JALR, eOP_ILL, eOP_ILL, eOP_SYSCALL, eOP_BREAK, eOP_ILL, eOP_ILL,
	eOP_MFHI, eOP_MTHI, eOP_MFLO, eOP_MTLO, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_MULT, eOP_MULTU, eOP_DIV, eOP_DIVU, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_ADD, eOP_ADDU, eOP_SUB, eOP_SUBU, eOP_AND, eOP_OR, eOP_XOR, eOP_NOR,
	eOP_ILL, eOP_ILL, eOP_SLT, eOP_SLTU, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,

	eOP_NULL, eOP_BCOND, eOP_J, eOP_JAL, eOP_BEQ, eOP_BNE, eOP_BLEZ, eOP_BGTZ,
	eOP_ADDI, eOP_ADDIU, eOP_SLTI, eOP_SLTIU, eOP_ANDI, eOP_ORI, eOP_XORI, eOP_LUI,
	eOP_COPROC, eOP_COPROC, eOP_COPROC, eOP_COPROC, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_LB, eOP_LH, eOP_LWL, eOP_LW, eOP_LBU, eOP_LHU, eOP_LWR, eOP_ILL,
	eOP_SB, eOP_SH, eOP_SWL, eOP_SW, eOP_ILL, eOP_ILL, eOP_SWR, eOP_ILL,
	eOP_LWC0, eOP_LWC1, eOP_LWC2, eOP_LWC3, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
	eOP_SWC0, eOP_SWC1, eOP_SWC2, eOP_SWC3, eOP_ILL, eOP_ILL, eOP_ILL, eOP_ILL,
};

void PSX::cpu_run_fetch()
{
	const u32 _pc = cpu.p_fetch.in_fetch_addr;
	const u32 instr = cpu_fetch(cpu.p_fetch.in_fetch_addr);
	const u32 opc = instr>>26;
	cpu.p_fetch.decode.instr.value = instr;

	const u32 _opc = instr>>26;
	const u32 _func = instr&0x3F;
	if(dotrace) DEBUG_TRACE("{%12lld} %08X: [%08X] (%d,%d) %s\n",abscounter, _pc, instr,_opc,_func,MDFN_IEN_PSX::DisassembleMIPS(_pc,instr).c_str());

	//this decode table approach was taken from mednafen.
	u32 opf = instr & 0x3F;
	if(instr & (0x3F << 26))
		opf = 0x40 | (instr >> 26);

	cpu.p_fetch.decode.op = DecodeTable[opf];

	//dont try to pad out these switches, it wont help
	switch(cpu.p_fetch.decode.op)
	{
	case eOP_MFLO: 
	case eOP_MFHI:
			cpu.stall_user |= CPU::eStall_MulDiv;
			break;
	}
}

#define BRANCH_SET_PC() { cpu.p_alu.out_pc.enabled = true, cpu.p_alu.out_pc.pc = cpu.p_alu.in_pc + instr.signed_target(); }
void PSX::cpu_run_alu()
{
	//most alu instructions will not set the out PC
	cpu.p_alu.out_pc.enabled = false;

	//alu needs to have registers ready before it begins.
	//some of these will write their results at the end of this stage
#ifndef NDEBUG 
	cpu.p_alu.out_mem.op = CPU::eMemOp_Unset;
#endif
	cpu.p_alu.exception = CPU::eException_None;
	switch(cpu.p_alu.decode.op)
	{
	case eOP_ILL:
	default:
		//NOCASE();
		printf("*** UNHANDLED ALU OP***\n");
		TraceALU();
		break;	

	case eOP_NULLIFIED:
	case eOP_NULL:
		{
			//?? whats this?
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}

	case eOP_J:  //j (jump)
		{
			const CPU::Instruction_JTYPE instr = cpu.p_alu.decode.instr.JTYPE;
			cpu.p_alu.out_pc.enabled = true;
			cpu.p_alu.out_pc.pc = (cpu.p_alu.in_pc&0xF0000000) | (instr.target << 2);
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		} 
	case eOP_JAL: //jal (jump and link)
		{
			const CPU::Instruction_JTYPE instr = cpu.p_alu.decode.instr.JTYPE;
			cpu.p_alu.out_pc.enabled = true;
			cpu.p_alu.out_pc.pc = (cpu.p_alu.in_pc&0xF0000000) | (instr.target << 2);
			cpu.regs.ra = cpu.p_alu.in_pc + 8;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		} 

	case eOP_COPROC: //coproc operations
		{
			const CPU::Instruction_CTYPE instr = cpu.p_alu.decode.instr.CTYPE;
			switch(instr.format)
			{
			case 0: //mfcz (move from coprocessor z)
				//TODO - should these have a delay? the psx is supposed to hav a delay for cp1, does that mean there is a delay for cp0? 
				cpu.p_alu.out_mem.op = CPU::eMemOp_MFC;
				break;
			case 4: //mtcz (move to coprocessor z)
				//TODO - should these have a delay? the psx is supposed to hav a delay for cp1, does that mean there is a delay for cp0? 
				cpu.p_alu.out_mem.value = cpu.regs.r[instr.rt];
				cpu.p_alu.out_mem.op = CPU::eMemOp_MTC;
				break;
			case 16: //a whole bunch of junk, maybe associated with cp0?
				switch(instr.function)
				{
				case 16: //rfe (return from exception)
					cpu.cp0.SR.IEc = cpu.cp0.SR.IEp;
					cpu.cp0.SR.KUc = cpu.cp0.SR.KUp;
					cpu.cp0.SR.IEp = cpu.cp0.SR.IEo;
					cpu.cp0.SR.KUp = cpu.cp0.SR.KUo;
					//KUo and IEo unchanged
					cpu.p_alu.out_mem.op = CPU::eMemOp_None;
					break;
				default:
					cpu.p_alu.out_mem.op = CPU::eMemOp_None;
					break;
				}
				break;
			default:
				NOCASE();
				printf("*** UNHANDLED ALU CTYPE ***\n");
				TraceALU();
			}
			break;
		}

	case eOP_SLL: //sll (shift left logical) //nop
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rt] << instr.sa;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SRL: //srl (shift right logical)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rt] >> instr.sa;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SRA: //sra (shift right arithmetic)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = (s32)cpu.regs.r[instr.rt] >> instr.sa;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SLLV: //sllv (shift left logical variable)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rt] << (cpu.regs.r[instr.rs] & 0x1F);
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SRLV: //srlv (shift right logical variable)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rt] >> (cpu.regs.r[instr.rs] & 0x1F);
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SRAV: //srav (shift right arithmetic variable)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = (s32)cpu.regs.r[instr.rt] >> (cpu.regs.r[instr.rs] & 0x1F);
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_JR: //jr (jump register)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.p_alu.out_pc.enabled = true;
			cpu.p_alu.out_pc.pc = cpu.regs.r[instr.rs];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_JALR: //jalr (jump and link register)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.p_alu.out_pc.enabled = true;
			cpu.p_alu.out_pc.pc = cpu.regs.r[instr.rs];
			cpu.regs.ra = cpu.p_alu.in_pc + 8;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SYSCALL: //syscall
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.p_alu.exception = CPU::eException_SYSCALL;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_BREAK: //break
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu_break(instr.break_code());
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_MFHI: //mfhi (move from hi)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.hi;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_MTHI: //mthi (move to hi)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.hi = cpu.regs.r[instr.rs];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_MFLO: //mflo (move from lo)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.lo;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_MTLO: //mtlo (move to lo)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.lo = cpu.regs.r[instr.rs];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_MULT: //mult (multiply)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			const u32 a = cpu.regs.r[instr.rs];
			const u32 b = cpu.regs.r[instr.rt];
			s64 product = (s64)a * (s32)b;
			cpu.unit_muldiv.lo = ((u32*)&product)[0];
			cpu.unit_muldiv.hi = ((u32*)&product)[1];
			cpu.unit_muldiv.timer = 12;
			cpu.stall_depends |= CPU::eStall_MulDiv;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_MULTU: //multu (multiply unsigned)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			const u32 a = cpu.regs.r[instr.rs];
			const u32 b = cpu.regs.r[instr.rt];
			u64 product = (u64)a * b;
			cpu.unit_muldiv.lo = ((u32*)&product)[0];
			cpu.unit_muldiv.hi = ((u32*)&product)[1];
			cpu.unit_muldiv.timer = 12;
			cpu.stall_depends |= CPU::eStall_MulDiv;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_DIV: //div
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			//TODO - special handling for divide by zero and such (take from mednafen)
			const u32 dividend = cpu.regs.r[instr.rs];
			const u32 divisor = cpu.regs.r[instr.rt];
			cpu.unit_muldiv.lo = (s32)dividend / (s32)divisor;
			cpu.unit_muldiv.hi = (s32)dividend % (s32)divisor;
			cpu.unit_muldiv.timer = 35;
			cpu.stall_depends |= CPU::eStall_MulDiv;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_DIVU: //divu
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			//TODO - special handling for divide by zero and such (take from mednafen)
			const u32 dividend = cpu.regs.r[instr.rs];
			const u32 divisor = cpu.regs.r[instr.rt];
			cpu.unit_muldiv.lo = dividend / divisor;
			cpu.unit_muldiv.hi = dividend % divisor;
			cpu.unit_muldiv.timer = 35;
			cpu.stall_depends |= CPU::eStall_MulDiv;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_ADD: //add [overflow (TBD)]
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rs] + cpu.regs.r[instr.rt];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_ADDU: //addu (add unsigned) [no overflow]
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rs] + cpu.regs.r[instr.rt];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SUBU: //subu (subtract unsigned) [no overflow]
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rs] - cpu.regs.r[instr.rt];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_AND: //and
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rs] & cpu.regs.r[instr.rt];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_OR: //or
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = cpu.regs.r[instr.rs] | cpu.regs.r[instr.rt];
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_NOR: //nor
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = ~(cpu.regs.r[instr.rs] | cpu.regs.r[instr.rt]);
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SLT: //slt (set on less than) [signed]
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = ((s32)cpu.regs.r[instr.rs] < (s32)cpu.regs.r[instr.rt]) ? 1 : 0;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SLTU://sltu (set on less than unsigned)
		{
			const CPU::Instruction_RTYPE instr = cpu.p_alu.decode.instr.RTYPE;
			cpu.regs.r[instr.rd] = (cpu.regs.r[instr.rs] < cpu.regs.r[instr.rt]) ? 1 : 0;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_BCOND: 
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			switch(instr.rt)
			{
			case 0: //bltz (branch on less than zero)
				if(SIGNBIT(cpu.regs.r[instr.rs])!=0)
					BRANCH_SET_PC();
				cpu.p_alu.out_mem.op = CPU::eMemOp_None;
				break;
			case 1: //bgez (branch on greater than or equal to zero)
				if(SIGNBIT(cpu.regs.r[instr.rs])==0)
					BRANCH_SET_PC();
				cpu.p_alu.out_mem.op = CPU::eMemOp_None;
				break;
			}
			break;
		}
	case eOP_BEQ: //beq (branch on equal)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			if(cpu.regs.r[instr.rs] == cpu.regs.r[instr.rt])
				BRANCH_SET_PC();
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_BNE: //bne (branch on not equal)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			if(cpu.regs.r[instr.rs] != cpu.regs.r[instr.rt])
				BRANCH_SET_PC();
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_BLEZ: //blez (branch on less than or equal to zero)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			if(SIGNBIT(cpu.regs.r[instr.rs])!=0 || cpu.regs.r[instr.rs] == 0)
				BRANCH_SET_PC();
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_BGTZ: //bgtz (branch on greater than zero)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			if(SIGNBIT(cpu.regs.r[instr.rs])==0 && cpu.regs.r[instr.rs] != 0)
				BRANCH_SET_PC();
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_ADDI: //addi (add immediate) [sign extend immediate and throw overflow exception (TBD)]
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.regs.r[instr.rt] = cpu.regs.r[instr.rs] + (s16)instr.immediate;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_ADDIU: //addiu (add immediate unsigned) [sign extend immediate and no overflow exception]
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.regs.r[instr.rt] = cpu.regs.r[instr.rs] + (s16)instr.immediate;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SLTI: //slti (set on less than immediate)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			if((s32)cpu.regs.r[instr.rs] < (s16)instr.immediate) cpu.regs.r[instr.rt] = 1;
			else cpu.regs.r[instr.rt] = 0;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_SLTIU: //sltiu (set on less than immediate unsigned)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			if(cpu.regs.r[instr.rs] < (u32)(s16)instr.immediate) cpu.regs.r[instr.rt] = 1;
			else cpu.regs.r[instr.rt] = 0;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_ANDI: //andi (and immediate)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.regs.r[instr.rt] = cpu.regs.r[instr.rs] & instr.immediate;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_ORI: //ori (or immediate) 
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.regs.r[instr.rt] = cpu.regs.r[instr.rs] | instr.immediate;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_LUI: //lui (load upper immediate)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.regs.r[instr.rt] = instr.immediate<<16;
			cpu.p_alu.out_mem.op = CPU::eMemOp_None;
			break;
		}
	case eOP_LB: //lb (load byte)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.rt = instr.rt;
			cpu.p_alu.out_mem.op = CPU::eMemOp_LoadByteSigned;
			break;
		}
	case eOP_LH: //lh (load halfword)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.rt = instr.rt;
			cpu.p_alu.out_mem.op = CPU::eMemOp_LoadHalfwordSigned;
			break;
		}
	case eOP_LW: //lw (load word)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.rt = instr.rt;
			cpu.p_alu.out_mem.op = CPU::eMemOp_LoadWord;
			break;
		}
	case eOP_LBU: //lbu (load byte unsigned)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.rt = instr.rt;
			cpu.p_alu.out_mem.op = CPU::eMemOp_LoadByteUnsigned;
			break;
		}
	case eOP_LHU: //lhu (load halfword unsigned)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.rt = instr.rt;
			cpu.p_alu.out_mem.op = CPU::eMemOp_LoadHalfwordUnsigned;
			break;
		}
	case eOP_SB: //sb (store byte)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.value = cpu.regs.r[instr.rt] & 0xFF;
			cpu.p_alu.out_mem.op = CPU::eMemOp_StoreByte;
			break;
		}
	case eOP_SH: //sh (store halfword)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.value = cpu.regs.r[instr.rt] & 0xFFFF;
			cpu.p_alu.out_mem.op = CPU::eMemOp_StoreHalfword;
			break;
		}
	case eOP_SW: //sw (store word)
		{
			const CPU::Instruction_ITYPE instr = cpu.p_alu.decode.instr.ITYPE;
			cpu.p_alu.out_mem.addr = cpu.regs.r[instr.base()] + (s16)instr.immediate;
			cpu.p_alu.out_mem.value = cpu.regs.r[instr.rt];
			cpu.p_alu.out_mem.op = CPU::eMemOp_StoreWord;
			break;
		}
	}

	cpu.regs.r0 = 0;

	//handle exceptions from alu
	//if(cpu.p_alu.exception != CPU::eException_None)
	//{
	//	cpu_exception(cpu.p_alu.exception, cpu.p_alu.in_pc);
	//}
}

void PSX::cpu_exec_cycle()
{
	counter++;
	abscounter++;

	//rd can sample registers after alu completes
	//rd can sample registers after mem completes
	//fetch can sample PC after first half of alu completes (branches need to be done in one early)

	//we sort of pretend the RD stage isnt there, and the fetch stage is shifted over, so we have
	//| IF | ALU | MEM | WB |
	//the chief consequences are:
	//RD can latch registers that the ALU is asserting
	// (so, we will pretend RD doesnt exist and have ALU latch registers that the previous ALU cycle asserted)
	// (we'll do this by having an extra buffer of registers)
	//we don't worry much about the branch addresses getting passed to IF: we're logically shifted the IF to the right,
	//so the ALU will naturally be asserting them 

	cpu_run_muldiv();

	if(cpu.stall_depends & cpu.stall_user)
	{
		//stalled
	}
	else
	{
		cpu_run_fetch();
		cpu_run_alu();
		cpu_run_mem();
		cpu_run_wb();

		//latch mem<-alu
		cpu.p_mem.decode = cpu.p_alu.decode;
		cpu.p_mem.in_from_alu = cpu.p_alu.out_mem;

		//latch alu<-fetch
		cpu.p_alu.decode = cpu.p_fetch.decode;
		cpu.p_alu.in_pc = cpu.p_fetch.in_fetch_addr;

		//latch fetch<-alu
		cpu.p_fetch.in_fetch_addr = cpu.p_alu.out_pc.enabled ? cpu.p_alu.out_pc.pc : (cpu.p_fetch.in_fetch_addr + 4);

		//check for exceptions. this should be more sophisticated
		if(cpu.p_alu.exception != CPU::eException_None)
		{
			cpu_exception(cpu.p_alu.exception,cpu.p_alu.in_pc);
		}
	}
}

void PSX::RunForever()
{
		static const int work = 33*1024*1024*20;
		DWORD a = timeGetTime();
		for(;;)
		{
			exec_cycle();
			if(counter == work) break;
		}
		DWORD b = timeGetTime();
		printf("%d ms\n",b-a);
}
