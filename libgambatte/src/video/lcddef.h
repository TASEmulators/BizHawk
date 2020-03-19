#ifndef LCDDEF_H
#define LCDDEF_H

namespace gambatte {

	enum {
		lcdc_bgen = 0x01,
		lcdc_objen = 0x02,
		lcdc_obj2x = 0x04,
		lcdc_bgtmsel = 0x08,
		lcdc_tdsel = 0x10,
		lcdc_we = 0x20,
		lcdc_wtmsel = 0x40,
		lcdc_en = 0x80
	};

	enum {
		lcdstat_lycflag = 0x04,
		lcdstat_m0irqen = 0x08,
		lcdstat_m1irqen = 0x10,
		lcdstat_m2irqen = 0x20,
		lcdstat_lycirqen = 0x40
	};

	enum {
		lcd_hres = 160,
		lcd_vres = 144,
		lcd_lines_per_frame = 154,
		lcd_max_num_sprites_per_line = 10,
		lcd_num_oam_entries = 40,
		lcd_cycles_per_line = 456,
		lcd_force_signed_enum1 = -1
	};
	enum {
		lcd_cycles_per_frame = 1l * lcd_lines_per_frame * lcd_cycles_per_line,
		lcd_force_signed_enum2 = -1
	};

}

#endif
