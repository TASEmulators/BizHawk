#pragma once
#include <stdint.h>

typedef struct
{
	uint8_t** line;
	uint8_t* dat;
	int w;
	int h;
} Bitmap;

void clear(Bitmap* b);
Bitmap* NewBitmap(int w, int h);
