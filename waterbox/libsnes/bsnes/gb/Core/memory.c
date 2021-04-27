#include <stdio.h>
#include <stdbool.h>
#include "gb.h"

typedef uint8_t GB_read_function_t(GB_gameboy_t *gb, uint16_t addr);
typedef void GB_write_function_t(GB_gameboy_t *gb, uint16_t addr, uint8_t value);

typedef enum {
    GB_BUS_MAIN, /* In DMG: Cart and RAM. In CGB: Cart only */
    GB_BUS_RAM, /* In CGB only. */
    GB_BUS_VRAM,
    GB_BUS_INTERNAL, /* Anything in highram. Might not be the most correct name. */
} GB_bus_t;

static GB_bus_t bus_for_addr(GB_gameboy_t *gb, uint16_t addr)
{
    if (addr < 0x8000) {
        return GB_BUS_MAIN;
    }
    if (addr < 0xA000) {
        return GB_BUS_VRAM;
    }
    if (addr < 0xC000) {
        return GB_BUS_MAIN;
    }
    if (addr < 0xFE00) {
        return GB_is_cgb(gb)? GB_BUS_RAM : GB_BUS_MAIN;
    }
    return GB_BUS_INTERNAL;
}

static uint8_t bitwise_glitch(uint8_t a, uint8_t b, uint8_t c)
{
    return ((a ^ c) & (b ^ c)) ^ c;
}

static uint8_t bitwise_glitch_read(uint8_t a, uint8_t b, uint8_t c)
{
    return b | (a & c);
}

static uint8_t bitwise_glitch_read_increase(uint8_t a, uint8_t b, uint8_t c, uint8_t d)
{
    return (b & (a | c | d)) | (a & c & d);
}

void GB_trigger_oam_bug(GB_gameboy_t *gb, uint16_t address)
{
    if (GB_is_cgb(gb)) return;
    
    if (address >= 0xFE00 && address < 0xFF00) {
        if (gb->accessed_oam_row != 0xff && gb->accessed_oam_row >= 8) {
            gb->oam[gb->accessed_oam_row] = bitwise_glitch(gb->oam[gb->accessed_oam_row],
                                                           gb->oam[gb->accessed_oam_row - 8],
                                                           gb->oam[gb->accessed_oam_row - 4]);
            gb->oam[gb->accessed_oam_row + 1] = bitwise_glitch(gb->oam[gb->accessed_oam_row + 1],
                                                               gb->oam[gb->accessed_oam_row - 7],
                                                               gb->oam[gb->accessed_oam_row - 3]);
            for (unsigned i = 2; i < 8; i++) {
                gb->oam[gb->accessed_oam_row + i] = gb->oam[gb->accessed_oam_row - 8 + i];
            }
        }
    }
}

void GB_trigger_oam_bug_read(GB_gameboy_t *gb, uint16_t address)
{
    if (GB_is_cgb(gb)) return;
    
    if (address >= 0xFE00 && address < 0xFF00) {
        if (gb->accessed_oam_row != 0xff && gb->accessed_oam_row >= 8) {
            gb->oam[gb->accessed_oam_row - 8] =
            gb->oam[gb->accessed_oam_row]     = bitwise_glitch_read(gb->oam[gb->accessed_oam_row],
                                                                    gb->oam[gb->accessed_oam_row - 8],
                                                                    gb->oam[gb->accessed_oam_row - 4]);
            gb->oam[gb->accessed_oam_row - 7] =
            gb->oam[gb->accessed_oam_row + 1] = bitwise_glitch_read(gb->oam[gb->accessed_oam_row + 1],
                                                                    gb->oam[gb->accessed_oam_row - 7],
                                                                    gb->oam[gb->accessed_oam_row - 3]);
            for (unsigned i = 2; i < 8; i++) {
                gb->oam[gb->accessed_oam_row + i] = gb->oam[gb->accessed_oam_row - 8 + i];
            }
        }
    }
}

void GB_trigger_oam_bug_read_increase(GB_gameboy_t *gb, uint16_t address)
{
    if (GB_is_cgb(gb)) return;
    
    if (address >= 0xFE00 && address < 0xFF00) {
        if (gb->accessed_oam_row != 0xff && gb->accessed_oam_row >= 0x20 && gb->accessed_oam_row < 0x98) {            
            gb->oam[gb->accessed_oam_row - 0x8] = bitwise_glitch_read_increase(gb->oam[gb->accessed_oam_row - 0x10],
                                                                               gb->oam[gb->accessed_oam_row - 0x08],
                                                                               gb->oam[gb->accessed_oam_row       ],
                                                                               gb->oam[gb->accessed_oam_row - 0x04]
                                                                               );
            gb->oam[gb->accessed_oam_row - 0x7] = bitwise_glitch_read_increase(gb->oam[gb->accessed_oam_row - 0x0f],
                                                                               gb->oam[gb->accessed_oam_row - 0x07],
                                                                               gb->oam[gb->accessed_oam_row + 0x01],
                                                                               gb->oam[gb->accessed_oam_row - 0x03]
                                                                               );
            for (unsigned i = 0; i < 8; i++) {
                gb->oam[gb->accessed_oam_row + i] = gb->oam[gb->accessed_oam_row - 0x10 + i] = gb->oam[gb->accessed_oam_row - 0x08 + i];
            }
        }
    }
}

static bool is_addr_in_dma_use(GB_gameboy_t *gb, uint16_t addr)
{
    if (!gb->dma_steps_left || (gb->dma_cycles < 0 && !gb->is_dma_restarting) || addr >= 0xFE00) return false;
    return bus_for_addr(gb, addr) == bus_for_addr(gb, gb->dma_current_src);
}

static bool effective_ir_input(GB_gameboy_t *gb)
{
    return gb->infrared_input || (gb->io_registers[GB_IO_RP] & 1) || gb->cart_ir;
}

static uint8_t read_rom(GB_gameboy_t *gb, uint16_t addr)
{
    if (addr < 0x100 && !gb->boot_rom_finished) {
        return gb->boot_rom[addr];
    }

    if (addr >= 0x200 && addr < 0x900 && GB_is_cgb(gb) && !gb->boot_rom_finished) {
        return gb->boot_rom[addr];
    }

    if (!gb->rom_size) {
        return 0xFF;
    }
    unsigned effective_address = (addr & 0x3FFF) + gb->mbc_rom0_bank * 0x4000;
    return gb->rom[effective_address & (gb->rom_size - 1)];
}

static uint8_t read_mbc_rom(GB_gameboy_t *gb, uint16_t addr)
{
    unsigned effective_address = (addr & 0x3FFF) + gb->mbc_rom_bank * 0x4000;
    return gb->rom[effective_address & (gb->rom_size - 1)];
}

static uint8_t read_vram(GB_gameboy_t *gb, uint16_t addr)
{
    if (gb->vram_read_blocked) {
        return 0xFF;
    }
    if (gb->display_state == 22 && GB_is_cgb(gb) && !gb->cgb_double_speed) {
        if (addr & 0x1000) {
            addr = gb->last_tile_index_address;
        }
        else if (gb->last_tile_data_address & 0x1000) {
            /* TODO: This is case is more complicated then the rest and differ between revisions
               It's probably affected by how VRAM is layed out, might be easier after a decap is done*/
        }
        else {
            addr = gb->last_tile_data_address;
        }
    }
    return gb->vram[(addr & 0x1FFF) + (uint16_t) gb->cgb_vram_bank * 0x2000];
}

static uint8_t read_mbc_ram(GB_gameboy_t *gb, uint16_t addr)
{
    if (gb->cartridge_type->mbc_type == GB_HUC3) {
        switch (gb->huc3_mode) {
            case 0xC: // RTC read
                if (gb->huc3_access_flags == 0x2) {
                    return 1;
                }
                return gb->huc3_read;
            case 0xD: // RTC status
                return 1;
            case 0xE: // IR mode
                return effective_ir_input(gb); // TODO: What are the other bits?
            default:
                GB_log(gb, "Unsupported HuC-3 mode %x read: %04x\n", gb->huc3_mode, addr);
                return 1; // TODO: What happens in this case?
            case 0: // TODO: R/O RAM? (or is it disabled?)
            case 0xA: // RAM
                break;
        }
    }
    
    if ((!gb->mbc_ram_enable) &&
        gb->cartridge_type->mbc_subtype != GB_CAMERA &&
        gb->cartridge_type->mbc_type != GB_HUC1 &&
        gb->cartridge_type->mbc_type != GB_HUC3) {
        return 0xFF;
    }
    
    if (gb->cartridge_type->mbc_type == GB_HUC1 && gb->huc1.ir_mode) {
        return 0xc0 | effective_ir_input(gb);
    }
    
    if (gb->cartridge_type->has_rtc && gb->cartridge_type->mbc_type != GB_HUC3 &&
        gb->mbc3_rtc_mapped && gb->mbc_ram_bank <= 4) {
        /* RTC read */
        gb->rtc_latched.high |= ~0xC1; /* Not all bytes in RTC high are used. */
        return gb->rtc_latched.data[gb->mbc_ram_bank];
    }

    if (gb->camera_registers_mapped) {
        return GB_camera_read_register(gb, addr);
    }

    if (!gb->mbc_ram || !gb->mbc_ram_size) {
        return 0xFF;
    }

    if (gb->cartridge_type->mbc_subtype == GB_CAMERA && gb->mbc_ram_bank == 0 && addr >= 0xa100 && addr < 0xaf00) {
        return GB_camera_read_image(gb, addr - 0xa100);
    }

    uint8_t effective_bank = gb->mbc_ram_bank;
    if (gb->cartridge_type->mbc_type == GB_MBC3 && !gb->is_mbc30) {
        effective_bank &= 0x3;
    }
    uint8_t ret = gb->mbc_ram[((addr & 0x1FFF) + effective_bank * 0x2000) & (gb->mbc_ram_size - 1)];
    if (gb->cartridge_type->mbc_type == GB_MBC2) {
        ret |= 0xF0;
    }
    return ret;
}

static uint8_t read_ram(GB_gameboy_t *gb, uint16_t addr)
{
    return gb->ram[addr & 0x0FFF];
}

static uint8_t read_banked_ram(GB_gameboy_t *gb, uint16_t addr)
{
    return gb->ram[(addr & 0x0FFF) + gb->cgb_ram_bank * 0x1000];
}

static uint8_t read_high_memory(GB_gameboy_t *gb, uint16_t addr)
{

    if (gb->hdma_on) {
        return gb->last_opcode_read;
    }
    
    if (addr < 0xFE00) {
        return read_banked_ram(gb, addr);
    }

    if (addr < 0xFF00) {
        if (gb->oam_write_blocked && !GB_is_cgb(gb)) {
            GB_trigger_oam_bug_read(gb, addr);
            return 0xff;
        }
        
        if ((gb->dma_steps_left && (gb->dma_cycles > 0 || gb->is_dma_restarting))) {
            /* Todo: Does reading from OAM during DMA causes the OAM bug? */
            return 0xff;
        }
        
        if (gb->oam_read_blocked) {
            if (!GB_is_cgb(gb)) {
                if (addr < 0xFEA0) {
                    if (gb->accessed_oam_row == 0) {
                        gb->oam[(addr & 0xf8)] =
                        gb->oam[0] = bitwise_glitch_read(gb->oam[0],
                                                         gb->oam[(addr & 0xf8)],
                                                         gb->oam[(addr & 0xfe)]);
                        gb->oam[(addr & 0xf8) + 1] =
                        gb->oam[1] = bitwise_glitch_read(gb->oam[1],
                                                         gb->oam[(addr & 0xf8) + 1],
                                                         gb->oam[(addr & 0xfe) | 1]);
                        for (unsigned i = 2; i < 8; i++) {
                            gb->oam[i] = gb->oam[(addr & 0xf8) + i];
                        }
                    }
                    else if (gb->accessed_oam_row == 0xa0) {
                        gb->oam[0x9e] = bitwise_glitch_read(gb->oam[0x9c],
                                                            gb->oam[0x9e],
                                                            gb->oam[(addr & 0xf8) | 6]);
                        gb->oam[0x9f] = bitwise_glitch_read(gb->oam[0x9d],
                                                            gb->oam[0x9f],
                                                            gb->oam[(addr & 0xf8) | 7]);
                        
                        for (unsigned i = 0; i < 8; i++) {
                            gb->oam[(addr & 0xf8) + i] = gb->oam[0x98 + i];
                        }
                    }
                }
            }
            return 0xff;
        }
        
        if (addr < 0xFEA0) {
            return gb->oam[addr & 0xFF];
        }
        
        if (gb->oam_read_blocked) {
            return 0xFF;
        }
        
        switch (gb->model) {
            case GB_MODEL_CGB_E:
            case GB_MODEL_AGB:
                return (addr & 0xF0) | ((addr >> 4) & 0xF);

            /*
            case GB_MODEL_CGB_D:
                if (addr > 0xfec0) {
                    addr |= 0xf0;
                }
                return gb->extra_oam[addr - 0xfea0];
            */
                
            case GB_MODEL_CGB_C:
            /*
             case GB_MODEL_CGB_B:
             case GB_MODEL_CGB_A:
             case GB_MODEL_CGB_0:
             */
                addr &= ~0x18;
                return gb->extra_oam[addr - 0xfea0];
                
            case GB_MODEL_DMG_B:
            case GB_MODEL_SGB_NTSC:
            case GB_MODEL_SGB_PAL:
            case GB_MODEL_SGB_NTSC_NO_SFC:
            case GB_MODEL_SGB_PAL_NO_SFC:
            case GB_MODEL_SGB2:
            case GB_MODEL_SGB2_NO_SFC:
                break;
        }
    }

    if (addr < 0xFF00) {
  
        return 0;

    }

    if (addr < 0xFF80) {
        switch (addr & 0xFF) {
            case GB_IO_IF:
                return gb->io_registers[GB_IO_IF] | 0xE0;
            case GB_IO_TAC:
                return gb->io_registers[GB_IO_TAC] | 0xF8;
            case GB_IO_STAT:
                return gb->io_registers[GB_IO_STAT] | 0x80;
            case GB_IO_OPRI:
                if (!GB_is_cgb(gb)) {
                    return 0xFF;
                }
                return gb->io_registers[GB_IO_OPRI] | 0xFE;

            case GB_IO_PCM_12:
                if (!GB_is_cgb(gb)) return 0xFF;
                return ((gb->apu.is_active[GB_SQUARE_2] ? (gb->apu.samples[GB_SQUARE_2] << 4) : 0) |
                        (gb->apu.is_active[GB_SQUARE_1] ? (gb->apu.samples[GB_SQUARE_1]) : 0)) & (gb->model <= GB_MODEL_CGB_C? gb->apu.pcm_mask[0] : 0xFF);
            case GB_IO_PCM_34:
                if (!GB_is_cgb(gb)) return 0xFF;
                return ((gb->apu.is_active[GB_NOISE] ? (gb->apu.samples[GB_NOISE] << 4) : 0) |
                        (gb->apu.is_active[GB_WAVE] ? (gb->apu.samples[GB_WAVE]) : 0))  & (gb->model <= GB_MODEL_CGB_C? gb->apu.pcm_mask[1] : 0xFF);
            case GB_IO_JOYP:
                GB_timing_sync(gb);
            case GB_IO_TMA:
            case GB_IO_LCDC:
            case GB_IO_SCY:
            case GB_IO_SCX:
            case GB_IO_LY:
            case GB_IO_LYC:
            case GB_IO_BGP:
            case GB_IO_OBP0:
            case GB_IO_OBP1:
            case GB_IO_WY:
            case GB_IO_WX:
            case GB_IO_SC:
            case GB_IO_SB:
            case GB_IO_DMA:
                return gb->io_registers[addr & 0xFF];
            case GB_IO_TIMA:
                if (gb->tima_reload_state == GB_TIMA_RELOADING) {
                    return 0;
                }
                return gb->io_registers[GB_IO_TIMA];
            case GB_IO_DIV:
                return gb->div_counter >> 8;
            case GB_IO_HDMA5:
                if (!gb->cgb_mode) return 0xFF;
                return ((gb->hdma_on || gb->hdma_on_hblank)? 0 : 0x80) | ((gb->hdma_steps_left - 1) & 0x7F);
            case GB_IO_SVBK:
                if (!gb->cgb_mode) {
                    return 0xFF;
                }
                return gb->cgb_ram_bank | ~0x7;
            case GB_IO_VBK:
                if (!GB_is_cgb(gb)) {
                    return 0xFF;
                }
                return gb->cgb_vram_bank | ~0x1;

            /* Todo: It seems that a CGB in DMG mode can access BGPI and OBPI, but not BGPD and OBPD? */
            case GB_IO_BGPI:
            case GB_IO_OBPI:
                if (!GB_is_cgb(gb)) {
                    return 0xFF;
                }
                return gb->io_registers[addr & 0xFF] | 0x40;

            case GB_IO_BGPD:
            case GB_IO_OBPD:
            {
                if (!gb->cgb_mode && gb->boot_rom_finished) {
                    return 0xFF;
                }
                if (gb->cgb_palettes_blocked) {
                    return 0xFF;
                }
                uint8_t index_reg = (addr & 0xFF) - 1;
                return ((addr & 0xFF) == GB_IO_BGPD?
                       gb->background_palettes_data :
                       gb->sprite_palettes_data)[gb->io_registers[index_reg] & 0x3F];
            }

            case GB_IO_KEY1:
                if (!gb->cgb_mode) {
                    return 0xFF;
                }
                return (gb->io_registers[GB_IO_KEY1] & 0x7F) | (gb->cgb_double_speed? 0xFE : 0x7E);

            case GB_IO_RP: {
                if (!gb->cgb_mode) return 0xFF;
                /* You will read your own IR LED if it's on. */
                uint8_t ret = (gb->io_registers[GB_IO_RP] & 0xC1) | 0x3C;
                if ((gb->io_registers[GB_IO_RP] & 0xC0) == 0xC0 && effective_ir_input(gb)) {
                    ret |= 2;
                }
                return ret;
            }
            case GB_IO_UNKNOWN2:
            case GB_IO_UNKNOWN3:
                return GB_is_cgb(gb)? gb->io_registers[addr & 0xFF] : 0xFF;
            case GB_IO_UNKNOWN4:
                return gb->cgb_mode? gb->io_registers[addr & 0xFF] : 0xFF;
            case GB_IO_UNKNOWN5:
                return GB_is_cgb(gb)? gb->io_registers[addr & 0xFF] | 0x8F : 0xFF;
            default:
                if ((addr & 0xFF) >= GB_IO_NR10 && (addr & 0xFF) <= GB_IO_WAV_END) {
                    return GB_apu_read(gb, addr & 0xFF);
                }
                return 0xFF;
        }
        /* Hardware registers */
        return 0;
    }

    if (addr == 0xFFFF) {
        /* Interrupt Mask */
        return gb->interrupt_enable;
    }

    /* HRAM */
    return gb->hram[addr - 0xFF80];
}

static GB_read_function_t * const read_map[] =
{
    read_rom,         read_rom,         read_rom, read_rom,         /* 0XXX, 1XXX, 2XXX, 3XXX */
    read_mbc_rom,     read_mbc_rom,     read_mbc_rom, read_mbc_rom, /* 4XXX, 5XXX, 6XXX, 7XXX */
    read_vram,        read_vram,                                    /* 8XXX, 9XXX */
    read_mbc_ram,     read_mbc_ram,                                 /* AXXX, BXXX */
    read_ram,         read_banked_ram,                              /* CXXX, DXXX */
    read_ram,         read_high_memory,                             /* EXXX FXXX */
};

void GB_set_read_memory_callback(GB_gameboy_t *gb, GB_read_memory_callback_t callback)
{
    gb->read_memory_callback = callback;
}

uint8_t GB_read_memory(GB_gameboy_t *gb, uint16_t addr)
{
    if (gb->n_watchpoints) {
        GB_debugger_test_read_watchpoint(gb, addr);
    }
    if (is_addr_in_dma_use(gb, addr)) {
        addr = gb->dma_current_src;
    }
    uint8_t data = read_map[addr >> 12](gb, addr);
    GB_apply_cheat(gb, addr, &data);
    if (gb->read_memory_callback) {
        data = gb->read_memory_callback(gb, addr, data);
    }
    return data;
}

static void write_mbc(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    switch (gb->cartridge_type->mbc_type) {
        case GB_NO_MBC: return;
        case GB_MBC1:
            switch (addr & 0xF000) {
                case 0x0000: case 0x1000: gb->mbc_ram_enable = (value & 0xF) == 0xA; break;
                case 0x2000: case 0x3000: gb->mbc1.bank_low  = value; break;
                case 0x4000: case 0x5000: gb->mbc1.bank_high = value; break;
                case 0x6000: case 0x7000: gb->mbc1.mode      = value; break;
            }
            break;
        case GB_MBC2:
            switch (addr & 0x4100) {
                case 0x0000: gb->mbc_ram_enable = (value & 0xF) == 0xA; break;
                case 0x0100: gb->mbc2.rom_bank  = value; break;
            }
            break;
        case GB_MBC3:
            switch (addr & 0xF000) {
                case 0x0000: case 0x1000: gb->mbc_ram_enable = (value & 0xF) == 0xA; break;
                case 0x2000: case 0x3000: gb->mbc3.rom_bank  = value; break;
                case 0x4000: case 0x5000:
                    gb->mbc3.ram_bank  = value;
                    gb->mbc3_rtc_mapped = value & 8;
                    break;
                case 0x6000: case 0x7000:
                    if (!gb->rtc_latch && (value & 1)) { /* Todo: verify condition is correct */
                        memcpy(&gb->rtc_latched, &gb->rtc_real, sizeof(gb->rtc_real));
                    }
                    gb->rtc_latch = value & 1;
                    break;
            }
            break;
        case GB_MBC5:
            switch (addr & 0xF000) {
                case 0x0000: case 0x1000: gb->mbc_ram_enable      = (value & 0xF) == 0xA; break;
                case 0x2000:              gb->mbc5.rom_bank_low   = value; break;
                case 0x3000:              gb->mbc5.rom_bank_high  = value; break;
                case 0x4000: case 0x5000:
                    if (gb->cartridge_type->has_rumble) {
                        if (!!(value & 8) != gb->rumble_state) {
                            gb->rumble_state = !gb->rumble_state;
                        }
                        value &= 7;
                    }
                    gb->mbc5.ram_bank = value;
                    gb->camera_registers_mapped = (value & 0x10) && gb->cartridge_type->mbc_subtype == GB_CAMERA;
                    break;
            }
            break;
        case GB_HUC1:
            switch (addr & 0xF000) {
                case 0x0000: case 0x1000: gb->huc1.ir_mode = (value & 0xF) == 0xE; break;
                case 0x2000: case 0x3000: gb->huc1.bank_low  = value; break;
                case 0x4000: case 0x5000: gb->huc1.bank_high = value; break;
                case 0x6000: case 0x7000: gb->huc1.mode      = value; break;
            }
            break;
        case GB_HUC3:
            switch (addr & 0xF000) {
                case 0x0000: case 0x1000:
                    gb->huc3_mode = value & 0xF;
                    gb->mbc_ram_enable = gb->huc3_mode == 0xA;
                    break;
                case 0x2000: case 0x3000: gb->huc3.rom_bank  = value; break;
                case 0x4000: case 0x5000: gb->huc3.ram_bank  = value; break;
            }
            break;
    }
    GB_update_mbc_mappings(gb);
}

static void write_vram(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    if (gb->vram_write_blocked) {
        //GB_log(gb, "Wrote %02x to %04x (VRAM) during mode 3\n", value, addr);
        return;
    }
    /* TODO: not verified */
    if (gb->display_state == 22 && GB_is_cgb(gb) && !gb->cgb_double_speed) {
        if (addr & 0x1000) {
            addr = gb->last_tile_index_address;
        }
        else if (gb->last_tile_data_address & 0x1000) {
            /* TODO: This is case is more complicated then the rest and differ between revisions
             It's probably affected by how VRAM is layed out, might be easier after a decap is done */
        }
        else {
            addr = gb->last_tile_data_address;
        }
    }
    gb->vram[(addr & 0x1FFF) + (uint16_t) gb->cgb_vram_bank * 0x2000] = value;
}

static bool huc3_write(GB_gameboy_t *gb, uint8_t value)
{
    switch (gb->huc3_mode) {
        case 0xB: // RTC Write
            switch (value >> 4) {
                case 1:
                    if (gb->huc3_access_index < 3) {
                        gb->huc3_read = (gb->huc3_minutes >> (gb->huc3_access_index  * 4)) & 0xF;
                    }
                    else if (gb->huc3_access_index < 7) {
                        gb->huc3_read = (gb->huc3_days >> ((gb->huc3_access_index - 3) * 4)) & 0xF;
                    }
                    else {
                        // GB_log(gb, "Attempting to read from unsupported HuC-3 register: %03x\n", gb->huc3_access_index);
                    }
                    gb->huc3_access_index++;
                    break;
                case 2:
                case 3:
                    if (gb->huc3_access_index < 3) {
                        gb->huc3_minutes &= ~(0xF << (gb->huc3_access_index * 4));
                        gb->huc3_minutes |= ((value & 0xF) << (gb->huc3_access_index * 4));
                    }
                    else if (gb->huc3_access_index < 7)  {
                        gb->huc3_days &= ~(0xF << ((gb->huc3_access_index - 3) * 4));
                        gb->huc3_days |= ((value & 0xF) << ((gb->huc3_access_index - 3) * 4));
                    }
                    else if (gb->huc3_access_index >= 0x58 && gb->huc3_access_index <= 0x5a) {
                        gb->huc3_alarm_minutes &= ~(0xF << ((gb->huc3_access_index - 0x58) * 4));
                        gb->huc3_alarm_minutes |= ((value & 0xF) << ((gb->huc3_access_index - 0x58) * 4));
                    }
                    else if (gb->huc3_access_index >= 0x5b && gb->huc3_access_index <= 0x5e) {
                        gb->huc3_alarm_days &= ~(0xF << ((gb->huc3_access_index - 0x5b) * 4));
                        gb->huc3_alarm_days |= ((value & 0xF) << ((gb->huc3_access_index - 0x5b) * 4));
                    }
                    else if (gb->huc3_access_index == 0x5f) {
                        gb->huc3_alarm_enabled = value & 1;
                    }
                    else {
                        // GB_log(gb, "Attempting to write %x to unsupported HuC-3 register: %03x\n", value & 0xF, gb->huc3_access_index);
                    }
                    if ((value >> 4) == 3) {
                        gb->huc3_access_index++;
                    }
                    break;
                case 4:
                    gb->huc3_access_index &= 0xF0;
                    gb->huc3_access_index |= value & 0xF;
                    break;
                case 5:
                    gb->huc3_access_index &= 0x0F;
                    gb->huc3_access_index |= (value & 0xF) << 4;
                    break;
                case 6:
                    gb->huc3_access_flags = (value & 0xF);
                    break;
                    
                default:
                    break;
            }
            
            return true;
        case 0xD: // RTC status
            // Not sure what writes here mean, they're always 0xFE
            return true;
        case 0xE: { // IR mode
            bool old_input = effective_ir_input(gb);
            gb->cart_ir = value & 1;
            bool new_input = effective_ir_input(gb);
            if (new_input != old_input) {
                if (gb->infrared_callback) {
                    gb->infrared_callback(gb, new_input, gb->cycles_since_ir_change);
                }
                gb->cycles_since_ir_change = 0;
            }
            return true;
        }
        case 0xC:
            return true;
        default:
            return false;
        case 0: // Disabled
        case 0xA: // RAM
            return false;
    }
}

static void write_mbc_ram(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    if (gb->cartridge_type->mbc_type == GB_HUC3) {
        if (huc3_write(gb, value)) return;
    }
    
    if (gb->camera_registers_mapped) {
        GB_camera_write_register(gb, addr, value);
        return;
    }
    
    if ((!gb->mbc_ram_enable)
       && gb->cartridge_type->mbc_type != GB_HUC1) return;
    
    if (gb->cartridge_type->mbc_type == GB_HUC1 && gb->huc1.ir_mode) {
        bool old_input = effective_ir_input(gb);
        gb->cart_ir = value & 1;
        bool new_input = effective_ir_input(gb);
        if (new_input != old_input) {
            if (gb->infrared_callback) {
                gb->infrared_callback(gb, new_input, gb->cycles_since_ir_change);
            }
            gb->cycles_since_ir_change = 0;
        }
        return;
    }

    if (gb->cartridge_type->has_rtc && gb->mbc3_rtc_mapped && gb->mbc_ram_bank <= 4) {
        gb->rtc_latched.data[gb->mbc_ram_bank] = gb->rtc_real.data[gb->mbc_ram_bank] = value;
        return;
    }

    if (!gb->mbc_ram || !gb->mbc_ram_size) {
        return;
    }
    
    uint8_t effective_bank = gb->mbc_ram_bank;
    if (gb->cartridge_type->mbc_type == GB_MBC3 && !gb->is_mbc30) {
        effective_bank &= 0x3;
    }

    gb->mbc_ram[((addr & 0x1FFF) + effective_bank * 0x2000) & (gb->mbc_ram_size - 1)] = value;
}

static void write_ram(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    gb->ram[addr & 0x0FFF] = value;
}

static void write_banked_ram(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    gb->ram[(addr & 0x0FFF) + gb->cgb_ram_bank * 0x1000] = value;
}

static void write_high_memory(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    if (addr < 0xFE00) {
        GB_log(gb, "Wrote %02x to %04x (RAM Mirror)\n", value, addr);
        write_banked_ram(gb, addr, value);
        return;
    }

    if (addr < 0xFF00) {
        if (gb->oam_write_blocked) {
            GB_trigger_oam_bug(gb, addr);
            return;
        }
        
        if ((gb->dma_steps_left && (gb->dma_cycles > 0 || gb->is_dma_restarting))) {
            /* Todo: Does writing to OAM during DMA causes the OAM bug? */
            return;
        }
        
        if (GB_is_cgb(gb)) {
            if (addr < 0xFEA0) {
                gb->oam[addr & 0xFF] = value;
            }
            switch (gb->model) {
                /*
                case GB_MODEL_CGB_D:
                    if (addr > 0xfec0) {
                        addr |= 0xf0;
                    }
                    gb->extra_oam[addr - 0xfea0] = value;
                    break;
                 */
                case GB_MODEL_CGB_C:
                /*
                 case GB_MODEL_CGB_B:
                 case GB_MODEL_CGB_A:
                 case GB_MODEL_CGB_0:
                 */
                    addr &= ~0x18;
                    gb->extra_oam[addr - 0xfea0] = value;
                    break;
                case GB_MODEL_DMG_B:
                case GB_MODEL_SGB_NTSC:
                case GB_MODEL_SGB_PAL:
                case GB_MODEL_SGB_NTSC_NO_SFC:
                case GB_MODEL_SGB_PAL_NO_SFC:
                case GB_MODEL_SGB2:
                case GB_MODEL_SGB2_NO_SFC:
                case GB_MODEL_CGB_E:
                case GB_MODEL_AGB:
                    break;
            }
            return;
        }
        
        if (addr < 0xFEA0) {
            if (gb->accessed_oam_row == 0xa0) {
                for (unsigned i = 0; i < 8; i++) {
                    if ((i & 6)  != (addr & 6)) {
                        gb->oam[(addr & 0xf8) + i] = gb->oam[0x98 + i];
                    }
                    else {
                        gb->oam[(addr & 0xf8) + i] = bitwise_glitch(gb->oam[(addr & 0xf8) + i], gb->oam[0x9c], gb->oam[0x98 + i]);
                    }
                }
            }
            
            gb->oam[addr & 0xFF] = value;
            
            if (gb->accessed_oam_row == 0) {
                gb->oam[0] = bitwise_glitch(gb->oam[0],
                                            gb->oam[(addr & 0xf8)],
                                            gb->oam[(addr & 0xfe)]);
                gb->oam[1] = bitwise_glitch(gb->oam[1],
                                            gb->oam[(addr & 0xf8) + 1],
                                            gb->oam[(addr & 0xfe) | 1]);
                for (unsigned i = 2; i < 8; i++) {
                    gb->oam[i] = gb->oam[(addr & 0xf8) + i];
                }
            }
        }
        else if (gb->accessed_oam_row == 0) {
            gb->oam[addr & 0x7] = value;
        }
        return;
    }

    /* Todo: Clean this code up: use a function table and move relevant code to display.c and timing.c
       (APU read and writes are already at apu.c) */
    if (addr < 0xFF80) {
        /* Hardware registers */
        switch (addr & 0xFF) {
            case GB_IO_WY:
                if (value == gb->current_line) {
                    gb->wy_triggered = true;
                }
            case GB_IO_WX:
            case GB_IO_IF:
            case GB_IO_SCX:
            case GB_IO_SCY:
            case GB_IO_BGP:
            case GB_IO_OBP0:
            case GB_IO_OBP1:
            case GB_IO_SB:
            case GB_IO_UNKNOWN2:
            case GB_IO_UNKNOWN3:
            case GB_IO_UNKNOWN4:
            case GB_IO_UNKNOWN5:
                gb->io_registers[addr & 0xFF] = value;
                return;
            case GB_IO_OPRI:
                if ((!gb->boot_rom_finished || (gb->io_registers[GB_IO_KEY0] & 8)) && GB_is_cgb(gb)) {
                    gb->io_registers[addr & 0xFF] = value;
                    gb->object_priority = (value & 1) ? GB_OBJECT_PRIORITY_X : GB_OBJECT_PRIORITY_INDEX;
                }
                else if (gb->cgb_mode) {
                    gb->io_registers[addr & 0xFF] = value;

                }
                return;
            case GB_IO_LYC:
                
                /* TODO: Probably completely wrong in double speed mode */
                
                /* TODO: This hack is disgusting */
                if (gb->display_state == 29 && GB_is_cgb(gb)) {
                    gb->ly_for_comparison = 153;
                    GB_STAT_update(gb);
                    gb->ly_for_comparison = 0;
                }
                
                gb->io_registers[addr & 0xFF] = value;
                
                /* These are the states when LY changes, let the display routine call GB_STAT_update for use
                   so it correctly handles T-cycle accurate LYC writes */
                if (!GB_is_cgb(gb)  || (
                    gb->display_state != 35 &&
                    gb->display_state != 26 &&
                    gb->display_state != 15 &&
                    gb->display_state != 16)) {
                    
                    /* More hacks to make LYC write conflicts work */
                    if (gb->display_state == 14 && GB_is_cgb(gb)) {
                        gb->ly_for_comparison = 153;
                        GB_STAT_update(gb);
                        gb->ly_for_comparison = -1;
                    }
                    else {
                        GB_STAT_update(gb);
                    }
                }
                return;
                
            case GB_IO_TIMA:
                if (gb->tima_reload_state != GB_TIMA_RELOADED) {
                    gb->io_registers[GB_IO_TIMA] = value;
                }
                return;

            case GB_IO_TMA:
                gb->io_registers[GB_IO_TMA] = value;
                if (gb->tima_reload_state != GB_TIMA_RUNNING) {
                    gb->io_registers[GB_IO_TIMA] = value;
                }
                return;

            case GB_IO_TAC:
                GB_emulate_timer_glitch(gb, gb->io_registers[GB_IO_TAC], value);
                gb->io_registers[GB_IO_TAC] = value;
                return;


            case GB_IO_LCDC:
                if ((value & 0x80) && !(gb->io_registers[GB_IO_LCDC] & 0x80)) {
                    gb->display_cycles = 0;
                    gb->display_state = 0;
                    if (GB_is_sgb(gb)) {
                        gb->frame_skip_state = GB_FRAMESKIP_SECOND_FRAME_RENDERED;
                    }
                    else if (gb->frame_skip_state == GB_FRAMESKIP_SECOND_FRAME_RENDERED) {
                        gb->frame_skip_state = GB_FRAMESKIP_LCD_TURNED_ON;
                    }
                }
                else if (!(value & 0x80) && (gb->io_registers[GB_IO_LCDC] & 0x80)) {
                    /* Sync after turning off LCD */
                    GB_timing_sync(gb);
                    GB_lcd_off(gb);
                }
                /* Handle disabling objects while already fetching an object */
                if ((gb->io_registers[GB_IO_LCDC] & 2) && !(value & 2)) {
                    if (gb->during_object_fetch) {
                        gb->cycles_for_line += gb->display_cycles;
                        gb->display_cycles = 0;
                        gb->object_fetch_aborted = true;
                    }
                }
                gb->io_registers[GB_IO_LCDC] = value;
                if (!(value & 0x20)) {
                    gb->wx_triggered = false;
                    gb->wx166_glitch = false;
                }
                return;

            case GB_IO_STAT:
                /* Delete previous R/W bits */
                gb->io_registers[GB_IO_STAT] &= 7;
                /* Set them by value */
                gb->io_registers[GB_IO_STAT] |= value & ~7;
                /* Set unused bit to 1 */
                gb->io_registers[GB_IO_STAT] |= 0x80;
                
                GB_STAT_update(gb);
                return;

            case GB_IO_DIV:
                /* Reset the div state machine */
                gb->div_state = 0;
                gb->div_cycles = 0;
                return;

            case GB_IO_JOYP:
                if (gb->joyp_write_callback) {
                    gb->joyp_write_callback(gb, value);
                    GB_update_joyp(gb);
                }
                else if ((gb->io_registers[GB_IO_JOYP] & 0x30) != (value & 0x30)) {
                    GB_sgb_write(gb, value);
                    gb->io_registers[GB_IO_JOYP] = (value & 0xF0) | (gb->io_registers[GB_IO_JOYP] & 0x0F);
                    GB_update_joyp(gb);
                }
                return;

            case GB_IO_BANK:
                gb->boot_rom_finished = true;
                return;

            case GB_IO_KEY0:
                if (GB_is_cgb(gb) && !gb->boot_rom_finished) {
                    gb->cgb_mode = !(value & 0xC); /* The real "contents" of this register aren't quite known yet. */
                    gb->io_registers[GB_IO_KEY0] = value;
                }
                return;

            case GB_IO_DMA:
                if (gb->dma_steps_left) {
                    /* This is not correct emulation, since we're not really delaying the second DMA.
                       One write that should have happened in the first DMA will not happen. However,
                       since that byte will be overwritten by the second DMA before it can actually be
                       read, it doesn't actually matter. */
                    gb->is_dma_restarting = true;
                }
                gb->dma_cycles = -7;
                gb->dma_current_dest = 0;
                gb->dma_current_src = value << 8;
                gb->dma_steps_left = 0xa0;
                gb->io_registers[GB_IO_DMA] = value;
                return;
            case GB_IO_SVBK:
                if (!gb->cgb_mode) {
                    return;
                }
                gb->cgb_ram_bank = value & 0x7;
                if (!gb->cgb_ram_bank) {
                    gb->cgb_ram_bank++;
                }
                return;
            case GB_IO_VBK:
                if (!gb->cgb_mode) {
                    return;
                }
                gb->cgb_vram_bank = value & 0x1;
                return;

            case GB_IO_BGPI:
            case GB_IO_OBPI:
                if (!GB_is_cgb(gb)) {
                    return;
                }
                gb->io_registers[addr & 0xFF] = value;
                return;
            case GB_IO_BGPD:
            case GB_IO_OBPD:
                if (!gb->cgb_mode && gb->boot_rom_finished) {
                    /* Todo: Due to the behavior of a broken Game & Watch Gallery 2 ROM on a real CGB. A proper test ROM
                       is required. */
                    return;
                }
                
                uint8_t index_reg = (addr & 0xFF) - 1;
                if (gb->cgb_palettes_blocked) {
                    if (gb->io_registers[index_reg] & 0x80) {
                        gb->io_registers[index_reg]++;
                        gb->io_registers[index_reg] |= 0x80;
                    }
                    return;
                }
                ((addr & 0xFF) == GB_IO_BGPD?
                 gb->background_palettes_data :
                 gb->sprite_palettes_data)[gb->io_registers[index_reg] & 0x3F] = value;
                GB_palette_changed(gb, (addr & 0xFF) == GB_IO_BGPD, gb->io_registers[index_reg] & 0x3F);
                if (gb->io_registers[index_reg] & 0x80) {
                    gb->io_registers[index_reg]++;
                    gb->io_registers[index_reg] |= 0x80;
                }
                return;
            case GB_IO_KEY1:
                if (!gb->cgb_mode) {
                    return;
                }
                gb->io_registers[GB_IO_KEY1] = value;
                return;
            case GB_IO_HDMA1:
                if (gb->cgb_mode) {
                    gb->hdma_current_src &= 0xF0;
                    gb->hdma_current_src |= value << 8;
                }
                return;
            case GB_IO_HDMA2:
                if (gb->cgb_mode) {
                    gb->hdma_current_src &= 0xFF00;
                    gb->hdma_current_src |= value & 0xF0;
                }
                return;
            case GB_IO_HDMA3:
                if (gb->cgb_mode) {
                    gb->hdma_current_dest &= 0xF0;
                    gb->hdma_current_dest |= value << 8;
                }
                return;
            case GB_IO_HDMA4:
                if (gb->cgb_mode) {
                    gb->hdma_current_dest &= 0x1F00;
                    gb->hdma_current_dest |= value & 0xF0;
                }
                return;
            case GB_IO_HDMA5:
                if (!gb->cgb_mode) return;
                if ((value & 0x80) == 0 && gb->hdma_on_hblank) {
                    gb->hdma_on_hblank = false;
                    return;
                }
                gb->hdma_on = (value & 0x80) == 0;
                gb->hdma_on_hblank = (value & 0x80) != 0;
                if (gb->hdma_on_hblank && (gb->io_registers[GB_IO_STAT] & 3) == 0) {
                    gb->hdma_on = true;
                }
                gb->io_registers[GB_IO_HDMA5] = value;
                gb->hdma_steps_left = (gb->io_registers[GB_IO_HDMA5] & 0x7F) + 1;
                /* Todo: Verify this. Gambatte's DMA tests require this. */
                if (gb->hdma_current_dest + (gb->hdma_steps_left << 4) > 0xFFFF) {
                    gb->hdma_steps_left = (0x10000 - gb->hdma_current_dest) >> 4;
                }
                gb->hdma_cycles = -12;
                return;

            /*  Todo: what happens when starting a transfer during a transfer?
                What happens when starting a transfer during external clock? 
            */
            case GB_IO_SC:
                if (!gb->cgb_mode) {
                    value |= 2;
                }
                gb->io_registers[GB_IO_SC] = value | (~0x83);
                if ((value & 0x80) && (value & 0x1) ) {
                    gb->serial_length = gb->cgb_mode && (value & 2)? 16 : 512;
                    gb->serial_count = 0;
                    /* Todo: This is probably incorrect for CGB's faster clock mode. */
                    gb->serial_cycles &= 0xFF;
                    if (gb->serial_transfer_bit_start_callback) {
                        gb->serial_transfer_bit_start_callback(gb, gb->io_registers[GB_IO_SB] & 0x80);
                    }
                }
                else {
                    gb->serial_length = 0;
                }
                return;

            case GB_IO_RP: {
                if (!GB_is_cgb(gb)) {
                    return;
                }
                bool old_input = effective_ir_input(gb);
                gb->io_registers[GB_IO_RP] = value;
                bool new_input = effective_ir_input(gb);
                if (new_input != old_input) {
                    if (gb->infrared_callback) {
                        gb->infrared_callback(gb, new_input, gb->cycles_since_ir_change);
                    }
                    gb->cycles_since_ir_change = 0;
                }
                return;
            }

            default:
                if ((addr & 0xFF) >= GB_IO_NR10 && (addr & 0xFF) <= GB_IO_WAV_END) {
                    GB_apu_write(gb, addr & 0xFF, value);
                    return;
                }
                GB_log(gb, "Wrote %02x to %04x (HW Register)\n", value, addr);
                return;
        }
    }

    if (addr == 0xFFFF) {
        /* Interrupt mask */
        gb->interrupt_enable = value;
        return;
    }
    
    /* HRAM */
    gb->hram[addr - 0xFF80] = value;
}



static GB_write_function_t * const write_map[] =
{
    write_mbc,         write_mbc,        write_mbc, write_mbc, /* 0XXX, 1XXX, 2XXX, 3XXX */
    write_mbc,         write_mbc,        write_mbc, write_mbc, /* 4XXX, 5XXX, 6XXX, 7XXX */
    write_vram,        write_vram,                             /* 8XXX, 9XXX */
    write_mbc_ram,     write_mbc_ram,                          /* AXXX, BXXX */
    write_ram,         write_banked_ram,                       /* CXXX, DXXX */
    write_ram,         write_high_memory,                     /* EXXX FXXX */
};

void GB_write_memory(GB_gameboy_t *gb, uint16_t addr, uint8_t value)
{
    if (gb->n_watchpoints) {
        GB_debugger_test_write_watchpoint(gb, addr, value);
    }
    if (is_addr_in_dma_use(gb, addr)) {
        /* Todo: What should happen? Will this affect DMA? Will data be written? What and where? */
        return;
    }
    write_map[addr >> 12](gb, addr, value);
}

void GB_dma_run(GB_gameboy_t *gb)
{
    while (gb->dma_cycles >= 4 && gb->dma_steps_left) {
        /* Todo: measure this value */
        gb->dma_cycles -= 4;
        gb->dma_steps_left--;
        
        if (gb->dma_current_src < 0xe000) {
            gb->oam[gb->dma_current_dest++] = GB_read_memory(gb, gb->dma_current_src);
        }
        else {
            /* Todo: Not correct on the CGB */
            gb->oam[gb->dma_current_dest++] = GB_read_memory(gb, gb->dma_current_src & ~0x2000);
        }
        
        /* dma_current_src must be the correct value during GB_read_memory */
        gb->dma_current_src++;
        if (!gb->dma_steps_left) {
            gb->is_dma_restarting = false;
        }
    }
}

void GB_hdma_run(GB_gameboy_t *gb)
{
    if (!gb->hdma_on) return;

    while (gb->hdma_cycles >= 0x4) {
        gb->hdma_cycles -= 0x4;

        GB_write_memory(gb, 0x8000 | (gb->hdma_current_dest++ & 0x1FFF), GB_read_memory(gb, (gb->hdma_current_src++)));
        
        if ((gb->hdma_current_dest & 0xf) == 0) {
            if (--gb->hdma_steps_left == 0) {
                gb->hdma_on = false;
                gb->hdma_on_hblank = false;
                gb->hdma_starting = false;
                gb->io_registers[GB_IO_HDMA5] &= 0x7F;
                break;
            }
            if (gb->hdma_on_hblank) {
                gb->hdma_on = false;
                break;
            }
        }
    }
}
