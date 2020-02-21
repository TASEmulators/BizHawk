#include <stdio.h>
#include <stddef.h>
#include <stdint.h>

uint8_t font[128][8];
uint8_t font80[8][2][128];

int main(int argc, char **argv) {
	if (argc < 2)
		return 20;
		
	FILE *f = fopen(argv[1], "rb");
	if (!f)
		return 20;
		
	if (1 != fread(&font[0][0], 1024, 1, f))
		return 20;
		
	fclose(f);
	
	// ATASCII -> INTERNAL table
	static constexpr uint8_t kConv[4]={
		0x40, 0x20, 0x60, 0x00
	};
	
	for(int i=0; i<128; ++i) {
		int dc = i ^ kConv[i >> 5];
		bool linedraw = false;
		
		switch(i) {
			case 0x01:
			case 0x02:
			case 0x03:
			case 0x04:
			case 0x05:
			case 0x0D:
			case 0x0E:
			case 0x11:
			case 0x12:
			case 0x13:
			case 0x15:
			case 0x16:
			case 0x17:
			case 0x18:
			case 0x19:
			case 0x1A:
				linedraw = true;
				break;
		}
		
		for(int j=0; j<8; ++j) {
			uint8_t even = 0;
			uint8_t odd = 0;
			uint8_t data = font[dc][j] << 1;
			
			if (linedraw && (data & 0x02))
				++data;
			
			if (data & 0x01) even += 0x11;
			if (data & 0x02) odd += 0x11;
			if (data & 0x04) even += 0x22;
			if (data & 0x08) odd += 0x22;
			if (data & 0x10) even += 0x44;
			if (data & 0x20) odd += 0x44;
			if (data & 0x40) even += 0x88;
			if (data & 0x80) odd += 0x88;
			
			odd ^= 0xFF;
			even ^= 0xFF;
			
			if (j & 1) {
				font80[j][0][i] = odd;
				font80[j][1][i] = even;
			} else {
				font80[j][1][i] = odd;
				font80[j][0][i] = even;
			}
		}
	}
	
	for(int row = 0; row < 8; ++row) {
		for(int phase = 0; phase < 2; ++phase) {
			printf("\nfont80_%s%d:\n", phase ? "odd" : "even", row);
		
			for(int offset = 0; offset < 128; offset += 16) {
				const uint8_t *src = &font80[row][phase][offset];
				
				printf("\t\tdta\t\t$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X,$%02X\n"
					, src[0]
					, src[1]
					, src[2]
					, src[3]
					, src[4]
					, src[5]
					, src[6]
					, src[7]
					, src[8]
					, src[9]
					, src[10]
					, src[11]
					, src[12]
					, src[13]
					, src[14]
					, src[15]);
			}
		}
	}
}
