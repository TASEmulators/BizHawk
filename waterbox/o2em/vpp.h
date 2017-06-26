#ifndef __VPP_H
#define __VPP_H

Byte read_PB(Byte p);
void write_PB(Byte p, Byte val);
Byte vpp_read(ADDRESS adr);
void vpp_write(Byte dat, ADDRESS adr);
void vpp_finish_bmp(Byte *vmem, int offx, int offy, int w, int h, int totw, int toth);
void init_vpp(void);
void load_colplus(Byte *col);

#endif
