/*
 * PicoDrive
 * (c) Copyright Dave, 2004
 * (C) notaz, 2006-2010
 *
 * This work is licensed under the terms of MAME license.
 * See COPYING file in the top-level directory.
 */

#include "pico_int.h"
#include <stdint.h>
#include <emulibc.h>

static const uint32_t crc32tab[256] = {
	0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,
	0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
	0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
	0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
	0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
	0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
	0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
	0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
	0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
	0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
	0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
	0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
	0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
	0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
	0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
	0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
	0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,
	0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
	0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,
	0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
	0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
	0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
	0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
	0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
	0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
	0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
	0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
	0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
	0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
	0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
	0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
	0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
	0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
	0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
	0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
	0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
	0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
	0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
	0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
	0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
	0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
	0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
	0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
	0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
	0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
	0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
	0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
	0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
	0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,
	0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
	0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
	0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
	0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
	0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
	0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
	0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
	0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
	0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
	0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
	0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
	0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
	0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
	0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
	0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d,
};

uint32_t crc32(const uint8_t *p, size_t size)
{
	uint32_t crc = ~0;
	while (size--)
		crc = (crc >> 8) ^ crc32tab[(crc ^ (*p++)) & 0xFF];
	return ~crc;
}


static int rom_alloc_size;
static const char *rom_exts[] = { "bin", "gen", "smd", "iso", "sms", "gg", "sg" };

void (*PicoCartMemSetup)(void);

void (*PicoCDLoadProgressCB)(const char *fname, int percent) = NULL; // handled in Pico/cd/cd_file.c

int PicoGameLoaded;

static void PicoCartDetect(const char *carthw_cfg);

static const char *get_ext(const char *path)
{
  const char *ext;
  if (strlen(path) < 4)
    return ""; // no ext

  // allow 2 or 3 char extensions for now
  ext = path + strlen(path) - 2;
  if (ext[-1] != '.') ext--;
  if (ext[-1] != '.')
    return "";
  return ext;
}

// byteswap, data needs to be int aligned, src can match dst
void Byteswap(void *dst, const void *src, int len)
{
  const unsigned int *ps = src;
  unsigned int *pd = dst;
  int i, m;

  if (len < 2)
    return;

  m = 0x00ff00ff;
  for (i = 0; i < len / 4; i++) {
    unsigned int t = ps[i];
    pd[i] = ((t & m) << 8) | ((t & ~m) >> 8);
  }
}

// Interleve a 16k block and byteswap
static int InterleveBlock(unsigned char *dest,unsigned char *src)
{
  int i=0;
  for (i=0;i<0x2000;i++) dest[(i<<1)  ]=src[       i]; // Odd
  for (i=0;i<0x2000;i++) dest[(i<<1)+1]=src[0x2000+i]; // Even
  return 0;
}

// Decode a SMD file
static int DecodeSmd(unsigned char *data,int len)
{
  unsigned char *temp=NULL;
  int i=0;

  temp=(unsigned char *)malloc(0x4000);
  if (temp==NULL) return 1;
  memset(temp,0,0x4000);

  // Interleve each 16k block and shift down by 0x200:
  for (i=0; i+0x4200<=len; i+=0x4000)
  {
    InterleveBlock(temp,data+0x200+i); // Interleve 16k to temporary buffer
    memcpy(data+i,temp,0x4000); // Copy back in
  }

  free(temp);
  return 0;
}

static unsigned char *PicoCartAlloc(int filesize, int is_sms)
{
  unsigned char *rom;

  if (is_sms) {
    // make size power of 2 for easier banking handling
    int s = 0, tmp = filesize;
    while ((tmp >>= 1) != 0)
      s++;
    if (filesize > (1 << s))
      s++;
    rom_alloc_size = 1 << s;
    // be sure we can cover all address space
    if (rom_alloc_size < 0x10000)
      rom_alloc_size = 0x10000;
  }
  else {
    // make alloc size at least sizeof(mcd_state),
    // in case we want to switch to CD mode
    if (filesize < sizeof(mcd_state))
      filesize = sizeof(mcd_state);

    // align to 512K for memhandlers
    rom_alloc_size = (filesize + 0x7ffff) & ~0x7ffff;
  }
  if (rom_alloc_size < 0x400000) {
	// sh2 memory mapping assumes that there's at least this much readable memory
	// The comment in that code is `0x3fffff; // FIXME`, but I guess it was never fixed
    rom_alloc_size = 0x400000;
  }

  if (rom_alloc_size - filesize < 4)
    rom_alloc_size += 4; // padding for out-of-bound exec protection

  // Allocate space for the rom plus padding
  rom = alloc_sealed(rom_alloc_size);
  return rom;
}

int PicoCartLoad(pm_file *f,unsigned char **prom,unsigned int *psize,int is_sms)
{
  unsigned char *rom;
  int size, bytes_read;

  if (f == NULL)
    return 1;

  size = f->size;
  if (size <= 0) return 1;
  size = (size+3)&~3; // Round up to a multiple of 4

  // Allocate space for the rom plus padding
  rom = PicoCartAlloc(size, is_sms);
  if (rom == NULL) {
    elprintf(EL_STATUS, "out of memory (wanted %i)", size);
    return 2;
  }

  bytes_read = pm_read(rom,size,f); // Load up the rom
  if (bytes_read <= 0) {
    elprintf(EL_STATUS, "read failed");
    return 3;
  }

  if (!is_sms)
  {
    // maybe we are loading MegaCD BIOS?
    if (!(PicoAHW & PAHW_MCD) && size == 0x20000 && (!strncmp((char *)rom+0x124, "BOOT", 4) ||
         !strncmp((char *)rom+0x128, "BOOT", 4))) {
      PicoAHW |= PAHW_MCD;
    }

    // Check for SMD:
    if (size >= 0x4200 && (size&0x3fff) == 0x200 &&
        ((rom[0x2280] == 'S' && rom[0x280] == 'E') || (rom[0x280] == 'S' && rom[0x2281] == 'E'))) {
      elprintf(EL_STATUS, "SMD format detected.");
      DecodeSmd(rom,size); size-=0x200; // Decode and byteswap SMD
    }
    else Byteswap(rom, rom, size); // Just byteswap
  }
  else
  {
    if (size >= 0x4200 && (size&0x3fff) == 0x200) {
      elprintf(EL_STATUS, "SMD format detected.");
      // at least here it's not interleaved
      size -= 0x200;
      memmove(rom, rom + 0x200, size);
    }
  }

  if (prom)  *prom = rom;
  if (psize) *psize = size;

  return 0;
}

// Insert a cartridge:
int PicoCartInsert(unsigned char *rom, unsigned int romsize, const char *carthw_cfg)
{
  // notaz: add a 68k "jump one op back" opcode to the end of ROM.
  // This will hang the emu, but will prevent nasty crashes.
  // note: 4 bytes are padded to every ROM
  if (rom != NULL)
    *(unsigned long *)(rom+romsize) = 0xFFFE4EFA; // 4EFA FFFE byteswapped

  Pico.rom=rom;
  Pico.romsize=romsize;

  if (SRam.data) {
    free(SRam.data);
    SRam.data = NULL;
  }

  PicoAHW &= PAHW_MCD|PAHW_SMS;

  PicoCartMemSetup = NULL;
  PicoDmaHook = NULL;
  PicoResetHook = NULL;
  PicoLineHook = NULL;

  if (!(PicoAHW & (PAHW_MCD|PAHW_SMS)))
    PicoCartDetect(carthw_cfg);

  // setup correct memory map for loaded ROM
  switch (PicoAHW) {
    default:
      elprintf(EL_STATUS|EL_ANOMALY, "starting in unknown hw configuration: %x", PicoAHW);
    case 0:
    case PAHW_SVP:  PicoMemSetup(); break;
    case PAHW_MCD:  PicoMemSetupCD(); break;
    case PAHW_PICO: PicoMemSetupPico(); break;
    case PAHW_SMS:  PicoMemSetupMS(); break;
  }

  if (PicoCartMemSetup != NULL)
    PicoCartMemSetup();

  if (PicoAHW & PAHW_SMS)
    PicoPowerMS();
  else
    PicoPower();

  PicoGameLoaded = 1;
  return 0;
}

static unsigned int rom_crc32(void)
{
  unsigned int crc;
  elprintf(EL_STATUS, "caclulating CRC32..");

  // have to unbyteswap for calculation..
  Byteswap(Pico.rom, Pico.rom, Pico.romsize);
  crc = crc32(Pico.rom, Pico.romsize);
  Byteswap(Pico.rom, Pico.rom, Pico.romsize);
  return crc;
}

static int rom_strcmp(int rom_offset, const char *s1)
{
  int i, len = strlen(s1);
  const char *s_rom = (const char *)Pico.rom;
  if (rom_offset + len > Pico.romsize)
    return 0;
  for (i = 0; i < len; i++)
    if (s1[i] != s_rom[(i + rom_offset) ^ 1])
      return 1;
  return 0;
}

static unsigned int rom_read32(int addr)
{
  unsigned short *m = (unsigned short *)(Pico.rom + addr);
  return (m[0] << 16) | m[1];
}

static char *sskip(char *s)
{
  while (*s && isspace_(*s))
    s++;
  return s;
}

static void rstrip(char *s)
{
  char *p;
  for (p = s + strlen(s) - 1; p >= s; p--)
    if (isspace_(*p))
      *p = 0;
}

static int parse_3_vals(char *p, int *val0, int *val1, int *val2)
{
  char *r;
  *val0 = strtoul(p, &r, 0);
  if (r == p)
    goto bad;
  p = sskip(r);
  if (*p++ != ',')
    goto bad;
  *val1 = strtoul(p, &r, 0);
  if (r == p)
    goto bad;
  p = sskip(r);
  if (*p++ != ',')
    goto bad;
  *val2 = strtoul(p, &r, 0);
  if (r == p)
    goto bad;

  return 1;
bad:
  return 0;
}

static int is_expr(const char *expr, char **pr)
{
  int len = strlen(expr);
  char *p = *pr;

  if (strncmp(expr, p, len) != 0)
    return 0;
  p = sskip(p + len);
  if (*p != '=')
    return 0; // wrong or malformed

  *pr = sskip(p + 1);
  return 1;
}

#include "carthw_cfg.inc"

static void parse_carthw(const char *carthw_cfg, int *fill_sram)
{
  int line = 0, any_checks_passed = 0, skip_sect = 0;
  const char *s, *builtin = builtin_carthw_cfg;
  int tmp, rom_crc = 0;
  char buff[256], *p, *r;
  FILE *f;

  f = fopen(carthw_cfg, "r");
  if (f == NULL)
    f = fopen("pico/carthw.cfg", "r");
  if (f == NULL)
    elprintf(EL_STATUS, "couldn't open carthw.cfg!");

  for (;;)
  {
    if (f != NULL) {
      p = fgets(buff, sizeof(buff), f);
      if (p == NULL)
        break;
    }
    else {
      if (*builtin == 0)
        break;
      for (s = builtin; *s != 0 && *s != '\n'; s++)
        ;
      while (*s == '\n')
        s++;
      tmp = s - builtin;
      if (tmp > sizeof(buff) - 1)
        tmp = sizeof(buff) - 1;
      memcpy(buff, builtin, tmp);
      buff[tmp] = 0;
      p = buff;
      builtin = s;
    }

    line++;
    p = sskip(p);
    if (*p == 0 || *p == '#')
      continue;

    if (*p == '[') {
      any_checks_passed = 0;
      skip_sect = 0;
      continue;
    }
    
    if (skip_sect)
      continue;

    /* look for checks */
    if (is_expr("check_str", &p))
    {
      int offs;
      offs = strtoul(p, &r, 0);
      if (offs < 0 || offs > Pico.romsize) {
        elprintf(EL_STATUS, "carthw:%d: check_str offs out of range: %d\n", line, offs);
	goto bad;
      }
      p = sskip(r);
      if (*p != ',')
        goto bad;
      p = sskip(p + 1);
      if (*p != '"')
        goto bad;
      p++;
      r = strchr(p, '"');
      if (r == NULL)
        goto bad;
      *r = 0;

      if (rom_strcmp(offs, p) == 0)
        any_checks_passed = 1;
      else
        skip_sect = 1;
      continue;
    }
    else if (is_expr("check_size_gt", &p))
    {
      int size;
      size = strtoul(p, &r, 0);
      if (r == p || size < 0)
        goto bad;

      if (Pico.romsize > size)
        any_checks_passed = 1;
      else
        skip_sect = 1;
      continue;
    }
    else if (is_expr("check_csum", &p))
    {
      int csum;
      csum = strtoul(p, &r, 0);
      if (r == p || (csum & 0xffff0000))
        goto bad;

      if (csum == (rom_read32(0x18c) & 0xffff))
        any_checks_passed = 1;
      else
        skip_sect = 1;
      continue;
    }
    else if (is_expr("check_crc32", &p))
    {
      unsigned int crc;
      crc = strtoul(p, &r, 0);
      if (r == p)
        goto bad;

      if (rom_crc == 0)
        rom_crc = rom_crc32();
      if (crc == rom_crc)
        any_checks_passed = 1;
      else
        skip_sect = 1;
      continue;
    }

    /* now time for actions */
    if (is_expr("hw", &p)) {
      if (!any_checks_passed)
        goto no_checks;
      rstrip(p);

      if      (strcmp(p, "svp") == 0)
        PicoSVPStartup();
      else if (strcmp(p, "pico") == 0)
        PicoInitPico();
      else if (strcmp(p, "prot") == 0)
        carthw_sprot_startup();
      else if (strcmp(p, "ssf2_mapper") == 0)
        carthw_ssf2_startup();
      else if (strcmp(p, "x_in_1_mapper") == 0)
        carthw_Xin1_startup();
      else if (strcmp(p, "realtec_mapper") == 0)
        carthw_realtec_startup();
      else if (strcmp(p, "radica_mapper") == 0)
        carthw_radica_startup();
      else if (strcmp(p, "piersolar_mapper") == 0)
        carthw_pier_startup();
      else if (strcmp(p, "prot_lk3") == 0)
        carthw_prot_lk3_startup();
      else {
        elprintf(EL_STATUS, "carthw:%d: unsupported mapper: %s", line, p);
        skip_sect = 1;
      }
      continue;
    }
    if (is_expr("sram_range", &p)) {
      int start, end;

      if (!any_checks_passed)
        goto no_checks;
      rstrip(p);

      start = strtoul(p, &r, 0);
      if (r == p)
        goto bad;
      p = sskip(r);
      if (*p != ',')
        goto bad;
      p = sskip(p + 1);
      end = strtoul(p, &r, 0);
      if (r == p)
        goto bad;
      if (((start | end) & 0xff000000) || start > end) {
        elprintf(EL_STATUS, "carthw:%d: bad sram_range: %08x - %08x", line, start, end);
        goto bad_nomsg;
      }
      SRam.start = start;
      SRam.end = end;
      continue;
    }
    else if (is_expr("prop", &p)) {
      if (!any_checks_passed)
        goto no_checks;
      rstrip(p);

      if      (strcmp(p, "no_sram") == 0)
        SRam.flags &= ~SRF_ENABLED;
      else if (strcmp(p, "no_eeprom") == 0)
        SRam.flags &= ~SRF_EEPROM;
      else if (strcmp(p, "filled_sram") == 0)
        *fill_sram = 1;
      else if (strcmp(p, "force_6btn") == 0)
        PicoQuirks |= PQUIRK_FORCE_6BTN;
      else {
        elprintf(EL_STATUS, "carthw:%d: unsupported prop: %s", line, p);
        goto bad_nomsg;
      }
      elprintf(EL_STATUS, "game prop: %s", p);
      continue;
    }
    else if (is_expr("eeprom_type", &p)) {
      int type;
      if (!any_checks_passed)
        goto no_checks;
      rstrip(p);

      type = strtoul(p, &r, 0);
      if (r == p || type < 0)
        goto bad;
      SRam.eeprom_type = type;
      SRam.flags |= SRF_EEPROM;
      continue;
    }
    else if (is_expr("eeprom_lines", &p)) {
      int scl, sda_in, sda_out;
      if (!any_checks_passed)
        goto no_checks;
      rstrip(p);

      if (!parse_3_vals(p, &scl, &sda_in, &sda_out))
        goto bad;
      if (scl < 0 || scl > 15 || sda_in < 0 || sda_in > 15 ||
          sda_out < 0 || sda_out > 15)
        goto bad;

      SRam.eeprom_bit_cl = scl;
      SRam.eeprom_bit_in = sda_in;
      SRam.eeprom_bit_out= sda_out;
      continue;
    }
    else if ((tmp = is_expr("prot_ro_value16", &p)) || is_expr("prot_rw_value16", &p)) {
      int addr, mask, val;
      if (!any_checks_passed)
        goto no_checks;
      rstrip(p);

      if (!parse_3_vals(p, &addr, &mask, &val))
        goto bad;

      carthw_sprot_new_location(addr, mask, val, tmp ? 1 : 0);
      continue;
    }


bad:
    elprintf(EL_STATUS, "carthw:%d: unrecognized expression: %s", line, buff);
bad_nomsg:
    skip_sect = 1;
    continue;

no_checks:
    elprintf(EL_STATUS, "carthw:%d: command without any checks before it: %s", line, buff);
    skip_sect = 1;
    continue;
  }

  if (f != NULL)
    fclose(f);
}

/*
 * various cart-specific things, which can't be handled by generic code
 */
static void PicoCartDetect(const char *carthw_cfg)
{
  int fill_sram = 0;

  memset(&SRam, 0, sizeof(SRam));
  if (Pico.rom[0x1B1] == 'R' && Pico.rom[0x1B0] == 'A')
  {
    SRam.start =  rom_read32(0x1B4) & ~0xff000001; // align
    SRam.end   = (rom_read32(0x1B8) & ~0xff000000) | 1;
    if (Pico.rom[0x1B2] & 0x40)
      // EEPROM
      SRam.flags |= SRF_EEPROM;
    SRam.flags |= SRF_ENABLED;
  }
  if (SRam.end == 0 || SRam.start > SRam.end)
  {
    // some games may have bad headers, like S&K and Sonic3
    // note: majority games use 0x200000 as starting address, but there are some which
    // use something else (0x300000 by HardBall '95). Luckily they have good headers.
    SRam.start = 0x200000;
    SRam.end   = 0x203FFF;
    SRam.flags |= SRF_ENABLED;
  }

  // set EEPROM defaults, in case it gets detected
  SRam.eeprom_type   = 0; // 7bit (24C01)
  SRam.eeprom_bit_cl = 1;
  SRam.eeprom_bit_in = 0;
  SRam.eeprom_bit_out= 0;

  if (carthw_cfg != NULL)
    parse_carthw(carthw_cfg, &fill_sram);

  if (SRam.flags & SRF_ENABLED)
  {
    if (SRam.flags & SRF_EEPROM)
      SRam.size = 0x2000;
    else
      SRam.size = SRam.end - SRam.start + 1;

    SRam.data = calloc(SRam.size, 1);
    if (SRam.data == NULL)
      SRam.flags &= ~SRF_ENABLED;

    if (SRam.eeprom_type == 1)	// 1 == 0 in PD EEPROM code
      SRam.eeprom_type = 0;
  }

  if ((SRam.flags & SRF_ENABLED) && fill_sram)
  {
    elprintf(EL_STATUS, "SRAM fill");
    memset(SRam.data, 0xff, SRam.size);
  }

  // Unusual region 'code'
  if (rom_strcmp(0x1f0, "EUROPE") == 0 || rom_strcmp(0x1f0, "Europe") == 0)
    *(int *) (Pico.rom + 0x1f0) = 0x20204520;
}
