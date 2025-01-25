/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include "genesis.h"
#include "gst.h"
#include <string.h>
#include <stdio.h>

#define GST_68K_REGS 0x80
#define GST_68K_REG_SIZE (0xDA-GST_68K_REGS)
#define GST_68K_PC_OFFSET (0xC8-GST_68K_REGS)
#define GST_68K_SR_OFFSET (0xD0-GST_68K_REGS)
#define GST_68K_USP_OFFSET (0xD2-GST_68K_REGS)
#define GST_68K_SSP_OFFSET (0xD6-GST_68K_REGS)
#define GST_68K_RAM  0x2478
#define GST_Z80_REGS 0x404
#define GST_Z80_REG_SIZE (0x440-GST_Z80_REGS)
#define GST_Z80_RAM 0x474
#define GST_VDP_REGS 0xFA
#define GST_VDP_MEM 0x12478
#define GST_YM_OFFSET 0x1E4
#define GST_YM_SIZE (0x3E4-GST_YM_OFFSET)

uint32_t read_le_32(uint8_t * data)
{
	return data[3] << 24 | data[2] << 16 | data[1] << 8 | data[0];
}

uint16_t read_le_16(uint8_t * data)
{
	return data[1] << 8 | data[0];
}

uint16_t read_be_16(uint8_t * data)
{
	return data[0] << 8 | data[1];
}

void write_le_32(uint8_t * dst, uint32_t val)
{
	dst[0] = val;
	dst[1] = val >> 8;
	dst[2] = val >> 16;
	dst[3] = val >> 24;
}

void write_le_16(uint8_t * dst, uint16_t val)
{
	dst[0] = val;
	dst[1] = val >> 8;
}

void write_be_32(uint8_t * dst, uint32_t val)
{
	dst[0] = val >> 24;
	dst[1] = val >> 16;
	dst[2] = val >> 8;
	dst[3] = val;
}

void write_be_16(uint8_t * dst, uint16_t val)
{
	dst[0] = val >> 8;
	dst[1] = val;
}

uint32_t m68k_load_gst(m68k_context * context, FILE * gstfile)
{
	uint8_t buffer[GST_68K_REG_SIZE];
	fseek(gstfile, GST_68K_REGS, SEEK_SET);
	if (fread(buffer, 1, GST_68K_REG_SIZE, gstfile) != GST_68K_REG_SIZE) {
		fputs("Failed to read 68K registers from savestate\n", stderr);
		return 0;
	}
	uint8_t * curpos = buffer;
	for (int i = 0; i < 8; i++) {
		context->dregs[i] = read_le_32(curpos);
		curpos += sizeof(uint32_t);
	}
	for (int i = 0; i < 8; i++) {
		context->aregs[i] = read_le_32(curpos);
		curpos += sizeof(uint32_t);
	}
	uint32_t pc = read_le_32(buffer + GST_68K_PC_OFFSET);
	uint16_t sr = read_le_16(buffer + GST_68K_SR_OFFSET);
	context->status = sr >> 8;
	for (int flag = 4; flag >= 0; flag--) {
		context->flags[flag] = sr & 1;
		sr >>= 1;
	}
	if (context->status & (1 << 5)) {
		context->aregs[8] = read_le_32(buffer + GST_68K_USP_OFFSET);
	} else {
		context->aregs[8] = read_le_32(buffer + GST_68K_SSP_OFFSET);
	}
	
	return pc;
}

uint8_t m68k_save_gst(m68k_context * context, uint32_t pc, FILE * gstfile)
{
	uint8_t buffer[GST_68K_REG_SIZE];
	uint8_t * curpos = buffer;
	for (int i = 0; i < 8; i++) {
		write_le_32(curpos, context->dregs[i]);
		curpos += sizeof(uint32_t);
	}
	for (int i = 0; i < 8; i++) {
		write_le_32(curpos, context->aregs[i]);
		curpos += sizeof(uint32_t);
	}
	write_le_32(buffer + GST_68K_PC_OFFSET, pc);
	uint16_t sr = context->status << 3;
	for (int flag = 4; flag >= 0; flag--) {
		sr <<= 1;
		sr |= context->flags[flag];
	}
	write_le_16(buffer + GST_68K_SR_OFFSET, sr);
	if (context->status & (1 << 5)) {
		write_le_32(buffer + GST_68K_USP_OFFSET, context->aregs[8]);
		write_le_32(buffer + GST_68K_SSP_OFFSET, context->aregs[7]);
	} else {
		write_le_32(buffer + GST_68K_USP_OFFSET, context->aregs[7]);
		write_le_32(buffer + GST_68K_SSP_OFFSET, context->aregs[8]);
	}
	fseek(gstfile, GST_68K_REGS, SEEK_SET);
	if (fwrite(buffer, 1, GST_68K_REG_SIZE, gstfile) != GST_68K_REG_SIZE) {
		fputs("Failed to write 68K registers to savestate\n", stderr);
		return 0;
	}

	return 1;
}

uint8_t z80_load_gst(z80_context * context, FILE * gstfile)
{
	uint8_t regdata[GST_Z80_REG_SIZE];
	fseek(gstfile, GST_Z80_REGS, SEEK_SET);
	if (fread(regdata, 1, sizeof(regdata), gstfile) != sizeof(regdata)) {
		fputs("Failed to read Z80 registers from savestate\n", stderr);
		return 0;
	}
	uint8_t * curpos = regdata;
	uint8_t f = *(curpos++);
#ifndef NEW_CORE
	context->flags[ZF_C] = f & 1;
	f >>= 1;
	context->flags[ZF_N] = f & 1;
	f >>= 1;
	context->flags[ZF_PV] = f & 1;
	f >>= 2;
	context->flags[ZF_H] = f & 1;
	f >>= 2;
	context->flags[ZF_Z] = f & 1;
	f >>= 1;
	context->flags[ZF_S] = f;

	context->regs[Z80_A] = *curpos;
	curpos += 3;
	for (int reg = Z80_C; reg <= Z80_IYH; reg++) {
		context->regs[reg++] = *(curpos++);
		context->regs[reg] = *curpos;
		curpos += 3;
	}
	context->pc = read_le_16(curpos);
	curpos += 4;
	context->sp = read_le_16(curpos);
	curpos += 4;
	f = *(curpos++);
	context->alt_flags[ZF_C] = f & 1;
	f >>= 1;
	context->alt_flags[ZF_N] = f & 1;
	f >>= 1;
	context->alt_flags[ZF_PV] = f & 1;
	f >>= 2;
	context->alt_flags[ZF_H] = f & 1;
	f >>= 2;
	context->alt_flags[ZF_Z] = f & 1;
	f >>= 1;
	context->alt_flags[ZF_S] = f;
	context->alt_regs[Z80_A] = *curpos;
	curpos += 3;
	for (int reg = Z80_C; reg <= Z80_H; reg++) {
		context->alt_regs[reg++] = *(curpos++);
		context->alt_regs[reg] = *curpos;
		curpos += 3;
	}
	context->regs[Z80_I] = *curpos;
	curpos += 2;
	context->iff1 = context->iff2 = *curpos;
	curpos += 2;
	context->reset = !*(curpos++);
	context->busreq = *curpos;
	curpos += 3;
	uint32_t bank = read_le_32(curpos);
	if (bank < 0x400000) {
		context->mem_pointers[1] = context->mem_pointers[2] + bank;
	} else {
		context->mem_pointers[1] = NULL;
	}
	context->bank_reg = bank >> 15;
#endif
	uint8_t buffer[Z80_RAM_BYTES];
	fseek(gstfile, GST_Z80_RAM, SEEK_SET);
	if(fread(buffer, 1, sizeof(buffer), gstfile) != (8*1024)) {
		fputs("Failed to read Z80 RAM from savestate\n", stderr);
		return 0;
	}
	for (int i = 0; i < Z80_RAM_BYTES; i++)
	{
		if (context->mem_pointers[0][i] != buffer[i]) {
			context->mem_pointers[0][i] = buffer[i];
#ifndef NEW_CORE
			z80_handle_code_write(i, context);
#endif
		}
	}
#ifndef NEW_CORE
	context->native_pc = NULL;
	context->extra_pc = NULL;
#endif
	return 1;
}

uint8_t vdp_load_gst(vdp_context * context, FILE * state_file)
{
	uint8_t tmp_buf[VRAM_SIZE];
	fseek(state_file, GST_VDP_REGS, SEEK_SET);
	if (fread(tmp_buf, 1, VDP_REGS, state_file) != VDP_REGS) {
		fputs("Failed to read VDP registers from savestate\n", stderr);
		return 0;
	}
	for (uint16_t i = 0; i < VDP_REGS; i++)
	{
		vdp_control_port_write(context, 0x8000 | (i << 8) | tmp_buf[i]);
	}
	if (fread(tmp_buf, 1, CRAM_SIZE*2, state_file) != CRAM_SIZE*2) {
		fputs("Failed to read CRAM from savestate\n", stderr);
		return 0;
	}
	for (int i = 0; i < CRAM_SIZE; i++) {
		uint16_t value;
		write_cram_internal(context, i, (tmp_buf[i*2+1] << 8) | tmp_buf[i*2]);
	}
	if (fread(tmp_buf, 2, MIN_VSRAM_SIZE, state_file) != MIN_VSRAM_SIZE) {
		fputs("Failed to read VSRAM from savestate\n", stderr);
		return 0;
	}
	for (int i = 0; i < MIN_VSRAM_SIZE; i++) {
		context->vsram[i] = (tmp_buf[i*2+1] << 8) | tmp_buf[i*2];
	}
	fseek(state_file, GST_VDP_MEM, SEEK_SET);
	if (fread(tmp_buf, 1, VRAM_SIZE, state_file) != VRAM_SIZE) {
		fputs("Failed to read VRAM from savestate\n", stderr);
		return 0;
	}
	for (int i = 0; i < VRAM_SIZE; i++) {
		context->vdpmem[i] = tmp_buf[i];
		vdp_check_update_sat_byte(context, i, tmp_buf[i]);
	}
	return 1;
}

uint8_t vdp_save_gst(vdp_context * context, FILE * outfile)
{
	uint8_t tmp_buf[CRAM_SIZE*2];
	fseek(outfile, GST_VDP_REGS, SEEK_SET);
	if(fwrite(context->regs, 1, VDP_REGS, outfile) != VDP_REGS) {
		fputs("Error writing VDP regs to savestate\n", stderr);
		return 0;
	}
	for (int i = 0; i < CRAM_SIZE; i++)
	{
		tmp_buf[i*2] = context->cram[i];
		tmp_buf[i*2+1] = context->cram[i] >> 8;
	}
	if (fwrite(tmp_buf, 1, sizeof(tmp_buf), outfile) != sizeof(tmp_buf)) {
		fputs("Error writing CRAM to savestate\n", stderr);
		return 0;
	}
	for (int i = 0; i < MIN_VSRAM_SIZE; i++)
	{
		tmp_buf[i*2] = context->vsram[i];
		tmp_buf[i*2+1] = context->vsram[i] >> 8;
	}
	if (fwrite(tmp_buf, 2, MIN_VSRAM_SIZE, outfile) != MIN_VSRAM_SIZE) {
		fputs("Error writing VSRAM to savestate\n", stderr);
		return 0;
	}
	fseek(outfile, GST_VDP_MEM, SEEK_SET);
	if (fwrite(context->vdpmem, 1, VRAM_SIZE, outfile) != VRAM_SIZE) {
		fputs("Error writing VRAM to savestate\n", stderr);
		return 0;
	}
	return 1;
}

uint8_t z80_save_gst(z80_context * context, FILE * gstfile)
{
	uint8_t regdata[GST_Z80_REG_SIZE];
	uint8_t * curpos = regdata;
	memset(regdata, 0, sizeof(regdata));
#ifndef NEW_CORE
	uint8_t f = context->flags[ZF_S];
	f <<= 1;
	f |= context->flags[ZF_Z] ;
	f <<= 2;
	f |= context->flags[ZF_H];
	f <<= 2;
	f |= context->flags[ZF_PV];
	f <<= 1;
	f |= context->flags[ZF_N];
	f <<= 1;
	f |= context->flags[ZF_C];
	*(curpos++) = f;
	*curpos = context->regs[Z80_A];

	curpos += 3;
	for (int reg = Z80_C; reg <= Z80_IYH; reg++) {
		*(curpos++) = context->regs[reg++];
		*curpos = context->regs[reg];
		curpos += 3;
	}
	write_le_16(curpos, context->pc);
	curpos += 4;
	write_le_16(curpos, context->sp);
	curpos += 4;
	f = context->alt_flags[ZF_S];
	f <<= 1;
	f |= context->alt_flags[ZF_Z] ;
	f <<= 2;
	f |= context->alt_flags[ZF_H];
	f <<= 2;
	f |= context->alt_flags[ZF_PV];
	f <<= 1;
	f |= context->alt_flags[ZF_N];
	f <<= 1;
	f |= context->alt_flags[ZF_C];
	*(curpos++) = f;
	*curpos = context->alt_regs[Z80_A];
	curpos += 3;
	for (int reg = Z80_C; reg <= Z80_H; reg++) {
		*(curpos++) = context->alt_regs[reg++];
		*curpos = context->alt_regs[reg];
		curpos += 3;
	}
	*curpos = context->regs[Z80_I];
	curpos += 2;
	*curpos = context->iff1;
	curpos += 2;
	*(curpos++) = !context->reset;
	*curpos = context->busreq;
	curpos += 3;
	uint32_t bank = context->bank_reg << 15;
	write_le_32(curpos, bank);
#endif
	fseek(gstfile, GST_Z80_REGS, SEEK_SET);
	if (fwrite(regdata, 1, sizeof(regdata), gstfile) != sizeof(regdata)) {
		return 0;
	}
	fseek(gstfile, GST_Z80_RAM, SEEK_SET);
	if(fwrite(context->mem_pointers[0], 1, 8*1024, gstfile) != (8*1024)) {
		fputs("Failed to write Z80 RAM to savestate\n", stderr);
		return 0;
	}
	return 1;
}

uint8_t ym_load_gst(ym2612_context * context, FILE * gstfile)
{
	uint8_t regdata[GST_YM_SIZE];
	fseek(gstfile, GST_YM_OFFSET, SEEK_SET);
	if (fread(regdata, 1, sizeof(regdata), gstfile) != sizeof(regdata)) {
		return 0;
	}
	for (int i = 0; i < sizeof(regdata); i++) {
		if (i & 0x100) {
			ym_address_write_part2(context, i & 0xFF);
		} else {
			ym_address_write_part1(context, i);
		}
		ym_data_write(context, regdata[i]);
	}
	return 1;
}

uint8_t ym_save_gst(ym2612_context * context, FILE * gstfile)
{
	uint8_t regdata[GST_YM_SIZE];
	for (int i = 0; i < sizeof(regdata); i++) {
		if (i & 0x100) {
			int reg = (i & 0xFF);
			if (reg >= YM_PART2_START && reg < YM_REG_END) {
				regdata[i] = context->part2_regs[reg-YM_PART2_START];
			} else {
				regdata[i] = 0xFF;
			}
		} else {
			if (i >= YM_PART1_START && i < YM_REG_END) {
				regdata[i] = context->part1_regs[i-YM_PART1_START];
			} else {
				regdata[i] = 0xFF;
			}
		}
	}
	fseek(gstfile, GST_YM_OFFSET, SEEK_SET);
	if (fwrite(regdata, 1, sizeof(regdata), gstfile) != sizeof(regdata)) {
		return 0;
	}
	return 1;
}

uint32_t load_gst(genesis_context * gen, char * fname)
{
	char buffer[4096];
	FILE * gstfile = fopen(fname, "rb");
	if (!gstfile) {
		fprintf(stderr, "Could not open file %s for reading\n", fname);
		goto error;
	}
	char ident[5];
	if (fread(ident, 1, sizeof(ident), gstfile) != sizeof(ident)) {
		fprintf(stderr, "Could not read ident code from %s\n", fname);
		goto error_close;
	}
	if (memcmp(ident, "GST\x40\xE0", 3) != 0) {
		fprintf(stderr, "%s doesn't appear to be a GST savestate. The ident code is %c%c%c\\x%X\\x%X instead of GST\\x40\\xE0.\n", fname, ident[0], ident[1], ident[2], ident[3], ident[4]);
		goto error_close;
	}
	uint32_t pc = m68k_load_gst(gen->m68k, gstfile);
	if (!pc) {
		goto error_close;
	}
	
	if (!vdp_load_gst(gen->vdp, gstfile)) {
		goto error_close;
	}
	if (!ym_load_gst(gen->ym, gstfile)) {
		goto error_close;
	}
	if (!z80_load_gst(gen->z80, gstfile)) {
		goto error_close;
	}
	gen->io.ports[0].control = 0x40;
	gen->io.ports[1].control = 0x40;
	
	fseek(gstfile, GST_68K_RAM, SEEK_SET);
	for (int i = 0; i < (32*1024);) {
		if (fread(buffer, 1, sizeof(buffer), gstfile) != sizeof(buffer)) {
			fputs("Failed to read 68K RAM from savestate\n", stderr);
			return 0;
		}
		for(char *curpos = buffer; curpos < (buffer + sizeof(buffer)); curpos += sizeof(uint16_t)) {
			uint16_t word = read_be_16(curpos);
			if (word != gen->work_ram[i]) {
				gen->work_ram[i] = word;
				m68k_handle_code_write(0xFF0000 | (i << 1), gen->m68k);
			}
			i++;
		}
	}
	fclose(gstfile);
	return pc;

error_close:
	fclose(gstfile);
error:
	return 0;
}

uint8_t save_gst(genesis_context * gen, char *fname, uint32_t m68k_pc)
{
	char buffer[4096];
	FILE * gstfile = fopen(fname, "wb");
	if (!gstfile) {
		fprintf(stderr, "Could not open %s for writing\n", fname);
		goto error;
	}
	if (fwrite("GST\x40\xE0", 1, 5, gstfile) != 5) {
		fputs("Error writing signature to savestate\n", stderr);
		goto error_close;
	}
	if (!m68k_save_gst(gen->m68k, m68k_pc, gstfile)) {
		goto error_close;
	}
	if (!z80_save_gst(gen->z80, gstfile)) {
		goto error_close;
	}
	if (!vdp_save_gst(gen->vdp, gstfile)) {
		goto error_close;
	}
	if (!ym_save_gst(gen->ym, gstfile)) {
		goto error_close;
	}
	fseek(gstfile, GST_68K_RAM, SEEK_SET);
	for (int i = 0; i < (32*1024);) {
		for(char *curpos = buffer; curpos < (buffer + sizeof(buffer)); curpos += sizeof(uint16_t)) {
			write_be_16(curpos, gen->work_ram[i++]);
		}
		if (fwrite(buffer, 1, sizeof(buffer), gstfile) != sizeof(buffer)) {
			fputs("Failed to write 68K RAM to savestate\n", stderr);
			return 0;
		}
	}
	return 1;

error_close:
	fclose(gstfile);
error:
	return 0;
}
