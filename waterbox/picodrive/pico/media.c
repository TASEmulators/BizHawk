/*
 * PicoDrive
 * (C) notaz, 2006-2010,2013
 *
 * This work is licensed under the terms of MAME license.
 * See COPYING file in the top-level directory.
 */

#include <string.h>
#include "pico_int.h"

unsigned char media_id_header[0x100];

static void strlwr_(char *string)
{
	char *p;
	for (p = string; *p; p++)
		if ('A' <= *p && *p <= 'Z')
			*p += 'a' - 'A';
}

static void get_ext(const char *file, char *ext)
{
	const char *p;

	p = file + strlen(file) - 4;
	if (p < file)
		p = file;
	strncpy(ext, p, 4);
	ext[4] = 0;
	strlwr_(ext);
}

enum media_type_e PicoLoadMedia(
	const char *filename,
	const char *carthw_cfg_fname,
	const char *(*get_bios_filename)(int *region, const char *cd_fname),
	void (*do_region_override)(const char *media_filename), enum media_type_e media_type)
{
	const char *rom_fname = filename;
	enum cd_img_type cd_img_type = CIT_NOT_CD;
	unsigned char *rom_data = NULL;
	unsigned int rom_size = 0;
	pm_file *rom = NULL;
	int cd_region = 0;
	int ret;

	if (media_type == PM_BAD_DETECT)
		goto out;

	PicoAHW = 0;
	PicoQuirks = 0;

	if (media_type == PM_CD)
	{
		// check for MegaCD image
		cd_img_type = PicoCdCheck(filename, &cd_region);
		if ((int)cd_img_type >= 0 && cd_img_type != CIT_NOT_CD)
		{
			// valid CD image, ask frontend for BIOS..
			rom_fname = NULL;
			if (get_bios_filename != NULL)
				rom_fname = get_bios_filename(&cd_region, filename);
			if (rom_fname == NULL)
			{
				media_type = PM_BAD_CD_NO_BIOS;
				goto out;
			}

			PicoAHW |= PAHW_MCD;
		}
		else
		{
			media_type = PM_BAD_CD;
			goto out;
		}
	}
	else if (media_type == PM_MARK3)
	{
		lprintf("detected SMS ROM\n");
		PicoAHW = PAHW_SMS;
	}

	rom = pm_open(rom_fname);
	if (rom == NULL)
	{
		lprintf("Failed to open ROM");
		media_type = PM_ERROR;
		goto out;
	}

	ret = PicoCartLoad(rom, &rom_data, &rom_size, (PicoAHW & PAHW_SMS) ? 1 : 0);
	pm_close(rom);
	if (ret != 0)
	{
		if (ret == 2)
			lprintf("Out of memory");
		else if (ret == 3)
			lprintf("Read failed");
		else
			lprintf("PicoCartLoad() failed.");
		media_type = PM_ERROR;
		goto out;
	}

	// detect wrong files
	if (strncmp((char *)rom_data, "Pico", 4) == 0)
	{
		lprintf("savestate selected?\n");
		media_type = PM_BAD_DETECT;
		goto out;
	}

	if (!(PicoAHW & PAHW_SMS))
	{
		unsigned short *d = (unsigned short *)(rom_data + 4);
		if ((((d[0] << 16) | d[1]) & 0xffffff) >= (int)rom_size)
		{
			lprintf("bad reset vector\n");
			media_type = PM_BAD_DETECT;
			goto out;
		}
	}

	// load config for this ROM (do this before insert to get correct region)
	if (!(PicoAHW & PAHW_MCD))
	{
		memcpy(media_id_header, rom_data + 0x100, sizeof(media_id_header));
		if (do_region_override != NULL)
			do_region_override(filename);
	}

	if (PicoCartInsert(rom_data, rom_size, carthw_cfg_fname))
	{
		media_type = PM_ERROR;
		goto out;
	}
	rom_data = NULL; // now belongs to PicoCart
	Pico.m.ncart_in = 0;

	// insert CD if it was detected
	if (cd_img_type != CIT_NOT_CD)
	{
		ret = cdd_load(filename, cd_img_type);
		if (ret != 0)
		{
			media_type = PM_BAD_CD;
			goto out;
		}
		Pico.m.ncart_in = 1;
	}

	if (PicoQuirks & PQUIRK_FORCE_6BTN)
		PicoSetInputDevice(0, PICO_INPUT_PAD_6BTN);

out:
	if (rom_data)
		free(rom_data);
	return media_type;
}
