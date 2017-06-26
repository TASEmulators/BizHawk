/*
 *   O2EM Free Odyssey2 / Videopac+ Emulator
 *
 *   Created by Daniel Boris <dboris@comcast.net>  (c) 1997,1998
 *
 *   Developed by Andre de la Rocha   <adlroc@users.sourceforge.net>
 *             Arlindo M. de Oliveira <dgtec@users.sourceforge.net>
 *
 *   http://o2em.sourceforge.net
 *
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include "crc32.h"
#include "audio.h"
#include "vmachine.h"
#include "config.h"
#include "vdc.h"
#include "cpu.h"
#include "keyboard.h"
#include "voice.h"
#include "score.h"

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

char name_f, rom_f, c_j;
char pathx, *k, identify;
static int load_bios(const char *data, int size);
static int load_cart(const char *data, int size);
int parse_option(char *attr, char *val);

ECL_EXPORT int Init(const char *rom, int romlen, const char *bios, int bioslen)
{
	int i, cnt, cnt2;

	app_data.bank = 0;
	app_data.voice = 1;
	app_data.exrom = 0;
	app_data.three_k = 0;
	app_data.crc = 0;
	app_data.euro = 0;
	app_data.openb = 0;
	app_data.vpp = 0;
	app_data.bios = 0;
	app_data.scoretype = 0;
	app_data.scoreaddress = 0;
	app_data.default_highscore = 0;
	app_data.megaxrom = 0;

	init_audio();

	if (!load_bios(bios, bioslen))
		return 0;

	if (!load_cart(rom, romlen))
		return 0;

	//if (app_data.voice)
	//load_voice_samples(NULL);
	init_display();
	init_cpu();
	init_system();
	//set_score(app_data.scoretype, app_data.scoreaddress, app_data.default_highscore);

	//run();
	//if (app_data.scoretype != 0)
	//save_highscore(get_score(app_data.scoretype, app_data.scoreaddress),
	//scorefile);

	return 1;
}

ECL_EXPORT void FrameAdvance(FrameInfo* f)
{
	cpu_exec(6026);
	f->Samples = 735;
	f->Width = 320;
	f->Height = 240;
	blit(f->VideoBuffer);
}
ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	m[0].Data = intRAM;
	m[0].Name = "RAM";
	m[0].Size = 64;
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_WORDSIZE1;
}

ECL_EXPORT void SetInputCallback(void (*callback)(void))
{
	// TODO
}

int parse_option(char *attr, char *val)
{
	int control_scheme;
	if (!strcmp(attr, "nolimit"))
	{
	}
	else if (!strcmp(attr, "novoice"))
	{
		app_data.voice = !(val[0] != '0');
	}
	else if ((!strcmp(attr, "s1")) || (!strcmp(attr, "s2")))
	{
	}
	else if (!strcmp(attr, "fullscreen"))
	{
	}
	else if (!strcmp(attr, "euro"))
	{
		app_data.euro = (val[0] != '0');
	}
	else if (!strcmp(attr, "exrom"))
	{
		app_data.exrom = (val[0] != '0');
	}
	else if (!strcmp(attr, "3k"))
	{
		app_data.three_k = (val[0] != '0');
	}
	else if (!strcmp(attr, "g7400"))
	{
	}
	else if (!strcmp(attr, "scoreadr"))
	{
		control_scheme = -1;
		sscanf(val, "%d", &control_scheme);
		if ((control_scheme >= 0) && (control_scheme <= 255))
			app_data.scoreaddress = control_scheme;
		else
		{
			fprintf(stderr, "Invalid value for option %s\n", attr);
			return 0;
		}
	}
	else if (!strcmp(attr, "scoretype"))
	{
		control_scheme = -1;
		sscanf(val, "%d", &control_scheme);
		if ((control_scheme >= 0) && (control_scheme <= 9999))
			app_data.scoretype = control_scheme;
		else
		{
			fprintf(stderr, "Invalid value for option %s\n", attr);
			return 0;
		}
	}
	else if (!strcmp(attr, "score"))
	{
		control_scheme = -1;
		sscanf(val, "%d", &control_scheme);
		if ((control_scheme >= 0) && (control_scheme <= 999999))
			app_data.default_highscore = control_scheme;
		else
		{
			fprintf(stderr, "Invalid value for option %s\n", attr);
			return 0;
		}
	}
	return 1;
}

static int load_bios(const char *data, int size)
{
	if (size != 1024)
		return 0;

	for (int i = 0; i < 8; i++)
	{
		memcpy(rom_table[i], data, 1024);
	}
	uint32_t crc = crc32_buf(rom_table[0], 1024);
	if (crc == 0x8016A315)
	{
		printf("Odyssey2 bios ROM loaded\n");
		app_data.vpp = 0;
		app_data.bios = ROM_O2;
	}
	else if (crc == 0xE20A9F41)
	{
		printf("Videopac+ G7400 bios ROM loaded\n");
		app_data.vpp = 1;
		app_data.bios = ROM_G7400;
	}
	else if (crc == 0xA318E8D6)
	{
		printf("C52 bios ROM loaded\n");
		app_data.vpp = 0;
		app_data.bios = ROM_C52;
	}
	else if (crc == 0x11647CA5)
	{
		printf("Jopac bios ROM loaded\n");
		app_data.vpp = 1;
		app_data.bios = ROM_JOPAC;
	}
	else
	{
		printf("Bios ROM loaded (unknown version)\n");
		app_data.vpp = 0;
		app_data.bios = ROM_UNKNOWN;
		return 0;
	}
	return 1;
}

static int load_cart(const char *data, int size)
{
	app_data.crc = crc32_buf(data, size);
	if (app_data.crc == 0xAFB23F89)
		app_data.exrom = 1; /* Musician */
	if (app_data.crc == 0x3BFEF56B)
		app_data.exrom = 1; /* Four in 1 Row! */
	if (app_data.crc == 0x9B5E9356)
		app_data.exrom = 1; /* Four in 1 Row! (french) */

	if (app_data.crc == 0x975AB8DA || app_data.crc == 0xE246A812)
	{
		fprintf(stderr, "Error: file is an incomplete ROM dump\n");
		return 0;
	}

	if (size & 1023)
	{
		fprintf(stderr, "Error: file is an invalid ROM dump\n");
		return 0;
	}

	const int l = size;
	int nb, i;

	/* special MegaCART design by Soeren Gust */
	if ((l == 32768) || (l == 65536) || (l == 131072) || (l == 262144) || (l == 524288) || (l == 1048576))
	{
		app_data.megaxrom = 1;
		app_data.bank = 1;
		megarom = malloc(1048576);
		memcpy(megarom, data, size);

		/* mirror shorter files into full megabyte */
		if (l < 65536)
			memcpy(megarom + 32768, megarom, 32768);
		if (l < 131072)
			memcpy(megarom + 65536, megarom, 65536);
		if (l < 262144)
			memcpy(megarom + 131072, megarom, 131072);
		if (l < 524288)
			memcpy(megarom + 262144, megarom, 262144);
		if (l < 1048576)
			memcpy(megarom + 524288, megarom, 524288);
		/* start in bank 0xff */
		memcpy(&rom_table[0][1024], megarom + 4096 * 255 + 1024, 3072);
		printf("MegaCart %ldK", l / 1024);
		nb = 1;
	}
	else if (((l % 3072) == 0))
	{
		app_data.three_k = 1;
		nb = l / 3072;

		for (int offset = 0, i = nb - 1; i >= 0; i--, offset += 3072)
		{
			memcpy(&rom_table[i][1024], data + offset, 3072);
		}
		printf("%dK", nb * 3);
	}
	else
	{
		nb = l / 2048;
		if ((nb == 2) && (app_data.exrom))
		{
			memcpy(&extROM[0], data, 1024);
			memcpy(&rom_table[0][1024], data + 1024, 3072);
			printf("3K EXROM");
		}
		else
		{
			for (int offset = 0, i = nb - 1; i >= 0; i--, offset += 2048)
			{
				memcpy(&rom_table[i][1024], data + offset, 2048);
				memcpy(&rom_table[i][3072], &rom_table[i][2048], 1024); /* simulate missing A10 */
			}
			printf("%dK", nb * 2);
		}
	}

	rom = rom_table[0];
	if (nb == 1)
		app_data.bank = 1;
	else if (nb == 2)
		app_data.bank = app_data.exrom ? 1 : 2;
	else if (nb == 4)
		app_data.bank = 3;
	else
		app_data.bank = 4;

	if ((rom_table[nb - 1][1024 + 12] == 'O') && (rom_table[nb - 1][1024 + 13] == 'P') && (rom_table[nb - 1][1024 + 14] == 'N') && (rom_table[nb - 1][1024 + 15] == 'B'))
		app_data.openb = 1;

	printf("  CRC: %08lX\n", app_data.crc);
	return 1;
}

int main(void)
{
	return 0;
}
