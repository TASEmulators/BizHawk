#include "rumble.h"
#include "gb.h"

void GB_set_rumble_mode(GB_gameboy_t *gb, GB_rumble_mode_t mode)
{
    gb->rumble_mode = mode;
    if (gb->rumble_callback) {
        gb->rumble_callback(gb, 0);
    }
}

void GB_handle_rumble(GB_gameboy_t *gb)
{
    if (gb->rumble_callback) {
        if (gb->rumble_mode == GB_RUMBLE_DISABLED) {
            return;
        }
        if (gb->cartridge_type->has_rumble) {
            if (gb->rumble_on_cycles + gb->rumble_off_cycles) {
                gb->rumble_callback(gb, gb->rumble_on_cycles / (double)(gb->rumble_on_cycles + gb->rumble_off_cycles));
                gb->rumble_on_cycles = gb->rumble_off_cycles = 0;
            }
        }
        else if (gb->rumble_mode == GB_RUMBLE_ALL_GAMES) {
            unsigned volume = (gb->io_registers[GB_IO_NR50] & 7) + 1 + ((gb->io_registers[GB_IO_NR50] >> 4) & 7) + 1;
            unsigned ch4_volume = volume * (!!(gb->io_registers[GB_IO_NR51] & 8) + !!(gb->io_registers[GB_IO_NR51] & 0x80));
            unsigned ch1_volume = volume * (!!(gb->io_registers[GB_IO_NR51] & 1) + !!(gb->io_registers[GB_IO_NR51] & 0x10));
            
            double ch4_rumble = (MIN(gb->apu.noise_channel.sample_length * (gb->apu.noise_channel.narrow? 8 : 1) , 4096) * ((signed) gb->apu.noise_channel.current_volume * gb->apu.noise_channel.current_volume * ch4_volume / 32.0 - 50) - 2048) / 2048.0;
            
            ch4_rumble = MIN(ch4_rumble, 1.0);
            ch4_rumble = MAX(ch4_rumble, 0.0);
            
            double ch1_rumble = 0;
            if (gb->apu.sweep_enabled && ((gb->io_registers[GB_IO_NR10] >> 4) & 7)) {
                double sweep_speed = (gb->io_registers[GB_IO_NR10] & 7) / (double)((gb->io_registers[GB_IO_NR10] >> 4) & 7);
                ch1_rumble = gb->apu.square_channels[GB_SQUARE_1].current_volume * ch1_volume / 32.0 * sweep_speed / 8.0 - 0.5;
                ch1_rumble = MIN(ch1_rumble, 1.0);
                ch1_rumble = MAX(ch1_rumble, 0.0);
            }
            
            if (!gb->apu.is_active[GB_NOISE]) {
                ch4_rumble = 0;
            }
            
            if (!gb->apu.is_active[GB_SQUARE_1]) {
                ch1_rumble = 0;
            }
            
            gb->rumble_callback(gb, MIN(MAX(ch1_rumble / 2 + ch4_rumble, 0.0), 1.0));
        }
    }
}
