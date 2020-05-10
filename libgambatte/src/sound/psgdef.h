#ifndef PSGDEF_H
#define PSGDEF_H

namespace gambatte {

	enum {
		psg_nr10_rsh = 0x07,
		psg_nr10_neg = 0x08,
		psg_nr10_time = 0x70
	};

	enum {
		psg_nr2_step = 0x07,
		psg_nr2_inc = 0x08,
		psg_nr2_initvol = 0xF0
	};

	enum {
		psg_nr43_r = 0x07,
		psg_nr43_7biten = 0x08,
		psg_nr43_s = 0xF0
	};

	enum {
		psg_nr4_lcen = 0x40,
		psg_nr4_init = 0x80
	};

}

#endif