#include <stdio.h>
#include <stdlib.h>

unsigned char bitmap[100][13];

int main() {
	FILE *f = fopen("drive.bmp", "rb");
	fseek(f, 0x36, SEEK_SET);

	for(int y=0; y<40; ++y) {
		for(int x=0; x<50; ++x) {
			int r = getc(f);
			int g = getc(f);
			int b = getc(f);

			int m = 0x40 >> ((x&3)*2);

//fprintf(stderr, "%d\n", r);
			if (r >= 0xc0) {
				bitmap[y][x>>2] += m*0;
			} else if (r >= 100) {
				bitmap[y][x>>2] += m*2;
			} else if (r >= 40) {
				bitmap[y][x>>2] += m*1;
			} else {
				bitmap[y][x>>2] += m*3;
			}
		}
		getc(f);
		getc(f);
	}

	for(int y=0; y<40; y += 1) {
		fputs("\tdta\t", stdout);
		for(int x=0; x<13; ++x) {
			int c = bitmap[y][x];

			if (x)
				putchar(',');

			putchar('%');
			putchar(c & 0x80 ? '1' : '0');
			putchar(c & 0x40 ? '1' : '0');
			putchar(c & 0x20 ? '1' : '0');
			putchar(c & 0x10 ? '1' : '0');
			putchar(c & 0x08 ? '1' : '0');
			putchar(c & 0x04 ? '1' : '0');
			putchar(c & 0x02 ? '1' : '0');
			putchar(c & 0x01 ? '1' : '0');
		}
		putchar('\n');
	}

}
