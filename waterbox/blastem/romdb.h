#ifndef ROMDB_H_
#define ROMDB_H_

#define REGION_J 1
#define REGION_U 2
#define REGION_E 4

#define RAM_FLAG_ODD  0x18
#define RAM_FLAG_EVEN 0x10
#define RAM_FLAG_BOTH 0x00
#define RAM_FLAG_MASK RAM_FLAG_ODD
#define SAVE_I2C      0x01
#define SAVE_NOR      0x02
#define SAVE_HBPT     0x03
#define SAVE_NONE     0xFF

#include "tern.h"
#include "serialize.h"
#include "system_header.h"

typedef struct {
	uint32_t     start;
	uint32_t     end;
	uint16_t     sda_write_mask;
	uint16_t     scl_mask;
	uint8_t      sda_read_bit;
} eeprom_map;

typedef struct {
	uint8_t     *buffer;
	uint8_t     *page_buffer;
	uint32_t    size;
	uint32_t    page_size;
	uint32_t    current_page;
	uint32_t    last_write_cycle;
	uint32_t    cmd_address1;
	uint32_t    cmd_address2;
	uint16_t    product_id;
	uint8_t     mode;
	uint8_t     cmd_state;
	uint8_t     alt_cmd;
	uint8_t     bus_flags;
} nor_state;

enum {
	MAPPER_NONE,
	MAPPER_SEGA,
	MAPPER_SEGA_SRAM,
	MAPPER_REALTEC,
	MAPPER_XBAND,
	MAPPER_MULTI_GAME,
	MAPPER_JCART
};


typedef struct rom_info rom_info;

#include "memmap.h"

struct rom_info {
	char          *name;
	memmap_chunk  *map;
	uint8_t       *save_buffer;
	void          *rom;
	eeprom_map    *eeprom_map;
	char          *port1_override;
	char          *port2_override;
	char          *ext_override;
	char          *mouse_mode;
	nor_state     *nor;
	uint32_t      num_eeprom;
	uint32_t      map_chunks;
	uint32_t      rom_size;
	uint32_t      save_size;
	uint32_t      save_mask;
	uint16_t      mapper_start_index;
	uint8_t       save_type;
	uint8_t       save_bus; //only used for NOR currently
	uint8_t       mapper_type;
	uint8_t       regions;
	uint8_t       is_save_lock_on; //Does the save buffer actually belong to a lock-on cart?
};

#define GAME_ID_OFF 0x183
#define GAME_ID_LEN 8

tern_node *load_rom_db();
rom_info configure_rom(tern_node *rom_db, void *vrom, uint32_t rom_size, void *lock_on, uint32_t lock_on_size, memmap_chunk const *base_map, uint32_t base_chunks);
rom_info configure_rom_heuristics(uint8_t *rom, uint32_t rom_size, memmap_chunk const *base_map, uint32_t base_chunks);
uint8_t translate_region_char(uint8_t c);
char const *save_type_name(uint8_t save_type);
//Note: free_rom_info only frees things pointed to by a rom_info struct, not the struct itself
//this is because rom_info structs are typically stack allocated
void free_rom_info(rom_info *info);
void cart_serialize(system_header *sys, serialize_buffer *buf);
void cart_deserialize(deserialize_buffer *buf, void *vcontext);

#endif //ROMDB_H_
