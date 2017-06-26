#include "bitmap.h"
#include <string.h>
#include <stdlib.h>

void clear(Bitmap* b)
{
	memset(b->dat, 0, b->w * b->h);
}

Bitmap* NewBitmap(int w, int h)
{
	Bitmap* b = malloc(sizeof(Bitmap) + w * h);
	b->w = w;
	b->h = h;
	b->line = malloc(sizeof(uint8_t*) * h);
	b->dat = malloc(w * h);
	for (int i = 0; i < h; i++)
		b->line[i] = b->dat + w * i;
	
	clear(b);
	return b;
}
