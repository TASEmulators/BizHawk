#include "gb.h"

/* TODO: Emulation is VERY basic and assumes the ROM correctly uses the printer's interface.
         Incorrect usage is not correctly emulated, as it's not well documented, nor do I
         have my own GB Printer to figure it out myself.
 
         It also does not currently emulate communication timeout, which means that a bug
         might prevent the printer operation until the GameBoy is restarted.
 
         Also, field mask values are assumed. */

static void handle_command(GB_gameboy_t *gb)
{
    
    switch (gb->printer.command_id) {
        case GB_PRINTER_INIT_COMMAND:
            gb->printer.status = 0;
            gb->printer.image_offset = 0;
            break;
            
        case GB_PRINTER_START_COMMAND:
            if (gb->printer.command_length == 4) {
                gb->printer.status = 6; /* Printing */
                uint32_t image[gb->printer.image_offset];
                uint8_t palette = gb->printer.command_data[2];
                uint32_t colors[4] = {gb->rgb_encode_callback(gb, 0xff, 0xff, 0xff),
                                      gb->rgb_encode_callback(gb, 0xaa, 0xaa, 0xaa),
                                      gb->rgb_encode_callback(gb, 0x55, 0x55, 0x55),
                                      gb->rgb_encode_callback(gb, 0x00, 0x00, 0x00)};
                for (unsigned i = 0; i < gb->printer.image_offset; i++) {
                    image[i] = colors[(palette >> (gb->printer.image[i] * 2)) & 3];
                }
                
                if (gb->printer.callback) {
                    gb->printer.callback(gb, image, gb->printer.image_offset / 160,
                                         gb->printer.command_data[1] >> 4, gb->printer.command_data[1] & 7,
                                         gb->printer.command_data[3] & 0x7F);
                }
                
                gb->printer.image_offset = 0;
            }
            break;
            
        case GB_PRINTER_DATA_COMMAND:
            if (gb->printer.command_length == GB_PRINTER_DATA_SIZE) {
                gb->printer.image_offset %= sizeof(gb->printer.image);
                gb->printer.status = 8; /* Received 0x280 bytes */
                
                uint8_t *byte = gb->printer.command_data;
                
                for (unsigned row = 2; row--; ) {
                    for (unsigned tile_x = 0; tile_x < 160 / 8; tile_x++) {
                        for (unsigned y = 0; y < 8; y++, byte += 2) {
                            for (unsigned x_pixel = 0; x_pixel < 8; x_pixel++) {
                                    gb->printer.image[gb->printer.image_offset + tile_x * 8 + x_pixel + y * 160] =
                                        ((*byte) >> 7) | (((*(byte + 1)) >> 7) << 1);
                                    (*byte) <<= 1;
                                    (*(byte + 1)) <<= 1;
                            }
                        }
                    }
                    
                    gb->printer.image_offset += 8 * 160;
                }
            }
            
        case GB_PRINTER_NOP_COMMAND:
        default:
            break;
    }
}

static void serial_start(GB_gameboy_t *gb, uint8_t byte_received)
{
    gb->printer.byte_to_send = 0;
    switch (gb->printer.command_state) {
        case GB_PRINTER_COMMAND_MAGIC1:
            if (byte_received != 0x88) {
                return;
            }
            gb->printer.status &= ~1;
            gb->printer.command_length = 0;
            gb->printer.checksum = 0;
            break;
            
        case GB_PRINTER_COMMAND_MAGIC2:
            if (byte_received != 0x33) {
                if (byte_received != 0x88) {
                    gb->printer.command_state = GB_PRINTER_COMMAND_MAGIC1;
                }
                return;
            }
            break;
            
        case GB_PRINTER_COMMAND_ID:
            gb->printer.command_id = byte_received & 0xF;
            break;
            
        case GB_PRINTER_COMMAND_COMPRESSION:
            gb->printer.compression = byte_received & 1;
            break;
            
        case GB_PRINTER_COMMAND_LENGTH_LOW:
            gb->printer.length_left = byte_received;
            break;
            
        case GB_PRINTER_COMMAND_LENGTH_HIGH:
            gb->printer.length_left |= (byte_received & 3) << 8;
            break;
            
        case GB_PRINTER_COMMAND_DATA:
            if (gb->printer.command_length != GB_PRINTER_MAX_COMMAND_LENGTH) {
                if (gb->printer.compression) {
                    if (!gb->printer.compression_run_lenth) {
                        gb->printer.compression_run_is_compressed = byte_received & 0x80;
                        gb->printer.compression_run_lenth = (byte_received & 0x7F) + 1 + gb->printer.compression_run_is_compressed;
                    }
                    else if (gb->printer.compression_run_is_compressed) {
                        while (gb->printer.compression_run_lenth) {
                            gb->printer.command_data[gb->printer.command_length++] = byte_received;
                            gb->printer.compression_run_lenth--;
                            if (gb->printer.command_length == GB_PRINTER_MAX_COMMAND_LENGTH) {
                                gb->printer.compression_run_lenth = 0;
                            }
                        }
                    }
                    else {
                        gb->printer.command_data[gb->printer.command_length++] = byte_received;
                        gb->printer.compression_run_lenth--;
                    }
                }
                else {
                    gb->printer.command_data[gb->printer.command_length++] = byte_received;
                }
            }
            gb->printer.length_left--;
            break;
            
        case GB_PRINTER_COMMAND_CHECKSUM_LOW:
            gb->printer.checksum ^= byte_received;
            break;
            
        case GB_PRINTER_COMMAND_CHECKSUM_HIGH:
            gb->printer.checksum ^= byte_received << 8;
            if (gb->printer.checksum) {
                gb->printer.status |= 1; /* Checksum error*/
                gb->printer.command_state = GB_PRINTER_COMMAND_MAGIC1;
                return;
            }
            break;
        case GB_PRINTER_COMMAND_ACTIVE:
            gb->printer.byte_to_send = 0x81;
            break;
        case GB_PRINTER_COMMAND_STATUS:
            
            if ((gb->printer.command_id & 0xF) == GB_PRINTER_INIT_COMMAND) {
                /* Games expect INIT commands to return 0? */
                gb->printer.byte_to_send = 0;
            }
            else {
                gb->printer.byte_to_send = gb->printer.status;
            }
            
            /* Printing is done instantly, but let the game recieve a 6 (Printing) status at least once, for compatibility */
            if (gb->printer.status == 6) {
               gb->printer.status = 4; /* Done */
            }
            
            gb->printer.command_state = GB_PRINTER_COMMAND_MAGIC1;
            handle_command(gb);
            return;
    }

    if (gb->printer.command_state >= GB_PRINTER_COMMAND_ID && gb->printer.command_state < GB_PRINTER_COMMAND_CHECKSUM_LOW) {
        gb->printer.checksum += byte_received;
    }

    if (gb->printer.command_state != GB_PRINTER_COMMAND_DATA) {
        gb->printer.command_state++;
    }

    if (gb->printer.command_state == GB_PRINTER_COMMAND_DATA) {
        if (gb->printer.length_left == 0) {
            gb->printer.command_state++;
        }
    }

}

static uint8_t serial_end(GB_gameboy_t *gb)
{
    return gb->printer.byte_to_send;
}

void GB_connect_printer(GB_gameboy_t *gb, GB_print_image_callback_t callback)
{
    memset(&gb->printer, 0, sizeof(gb->printer));
    GB_set_serial_transfer_start_callback(gb, serial_start);
    GB_set_serial_transfer_end_callback(gb, serial_end);
    gb->printer.callback = callback;
}