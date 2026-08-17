
int sprite_2 = sprite [2];

// pixels
ptrdiff_t next_row = this->scanline_row_bytes;
byte* out = this->scanline_pixels + sprite [3] +
		(top_minus_one + skip - begin_minus_one) * next_row;
cache_t const* lines = get_sprite_tile( sprite );

int dir = 1;
byte* scanlines = this->sprite_scanlines + 1 + top_minus_one + skip;

if ( sprite_2 & 0x80 )
{
	// vertical flip
	out -= next_row;
	out += visible * next_row;
	next_row = -next_row;
	dir = -1;
	scanlines += visible - 1;
	#if CLIPPED
		int height = this->sprite_height();
		skip = height - skip - visible;
		assert( skip + visible <= height );
	#endif
}

// attributes
unsigned long offset = (sprite_2 & 3) * 0x04040404 + (this->palette_offset + 0x10101010);

unsigned long const mask    = 0x03030303 + zero;
unsigned long const maskgen = 0x80808080 + zero;

#define DRAW_PAIR( shift ) {                    \
	int sprite_count = *scanlines;              \
	CALC_FOUR( ((uint32_t*) out) [0], (line >> (shift + 4)), out0 ) \
	CALC_FOUR( ((uint32_t*) out) [1], (line >> shift), out1 )       \
	if ( sprite_count < this->max_sprites ) {   \
		((uint32_t*) out) [0] = out0;           \
		((uint32_t*) out) [1] = out1;           \
	}                                           \
	if ( CLIPPED ) visible--;                   \
	out += next_row;                            \
	*scanlines = sprite_count + 1;              \
	scanlines += dir;                           \
	if ( CLIPPED && !visible ) break;           \
}

if ( !(sprite_2 & 0x20) )
{
	// front
	unsigned long const maskgen2 = 0x7f7f7f7f + zero;

	#define CALC_FOUR( in, line, out )                          \
		unsigned long out;                                      \
		{                                                       \
			unsigned long bg = in;                              \
			unsigned long sp = line & mask;                     \
			unsigned long bgm = maskgen2 + ((bg >> 4) & mask);  \
			unsigned long spm = (maskgen - sp) & maskgen2;      \
			unsigned long m = (bgm & spm) >> 2;                 \
			out = (bg & ~m) | ((sp + offset) & m);              \
		}
	
	#if CLIPPED
		lines += skip >> 1;
		unsigned long line = *lines++;
		if ( skip & 1 )
			goto front_skip;
		
		while ( true )
		{
			DRAW_PAIR( 0 )
		front_skip:
			DRAW_PAIR( 2 )
			line = *lines++;
		}
	#else
		for ( int n = visible >> 1; n--; )
		{
			unsigned long line = *lines++;
			DRAW_PAIR( 0 )
			DRAW_PAIR( 2 )
		}
	#endif
	
	#undef CALC_FOUR
}
else
{
	// behind
	unsigned long const omask = 0x20202020 + zero;
	unsigned long const bg_or = 0xc3c3c3c3 + zero;

	#define CALC_FOUR( in, line, out )                      \
		unsigned long out;                                  \
		{                                                   \
			unsigned long bg = in;                          \
			unsigned long sp = line & mask;                 \
			unsigned long bgm = maskgen - (bg & mask);      \
			unsigned long spm = maskgen - sp;               \
			out = (bg & (bgm | bg_or)) | (spm & omask) |    \
					(((offset & spm) + sp) & ~(bgm >> 2));  \
		}
	
	#if CLIPPED
		lines += skip >> 1;
		unsigned long line = *lines++;
		if ( skip & 1 )
			goto back_skip;
		
		while ( true )
		{
			DRAW_PAIR( 0 )
		back_skip:
			DRAW_PAIR( 2 )
			line = *lines++;
		}
	#else
		for ( int n = visible >> 1; n--; )
		{
			unsigned long line = *lines++;
			DRAW_PAIR( 0 )
			DRAW_PAIR( 2 )
		}
	#endif
	
	#undef CALC_FOUR
}

#undef CLIPPED
#undef DRAW_PAIR
