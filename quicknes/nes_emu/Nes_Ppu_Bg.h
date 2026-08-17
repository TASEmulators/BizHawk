while ( true )
{
	while ( count-- )
	{
		int attrib = attr_table [addr >> 2 & 0x07];
		attrib >>= (addr >> 4 & 4) | (addr & 2);
		unsigned long offset = (attrib & 3) * attrib_factor + this->palette_offset;
		
		// draw one tile
		cache_t const* lines = this->get_bg_tile( nametable [addr] + bg_bank );
		byte* p = pixels;
		addr++;
		pixels += 8; // next tile
		
		if ( !clipped )
		{
			// optimal case: no clipping
			for ( int n = 4; n--; )
			{
				unsigned long line = *lines++;
				((uint32_t*) p) [0] = (line >> 4 & mask) + offset;
				((uint32_t*) p) [1] = (line      & mask) + offset;
				p += row_bytes;
				((uint32_t*) p) [0] = (line >> 6 & mask) + offset;
				((uint32_t*) p) [1] = (line >> 2 & mask) + offset;
				p += row_bytes;
			}
		}
		else
		{
			lines += fine_y >> 1;
			
			if ( fine_y & 1 )
			{
				unsigned long line = *lines++;
				((uint32_t*) p) [0] = (line >> 6 & mask) + offset;
				((uint32_t*) p) [1] = (line >> 2 & mask) + offset;
				p += row_bytes;
			}
			
			for ( int n = height >> 1; n--; )
			{
				unsigned long line = *lines++;
				((uint32_t*) p) [0] = (line >> 4 & mask) + offset;
				((uint32_t*) p) [1] = (line      & mask) + offset;
				p += row_bytes;
				((uint32_t*) p) [0] = (line >> 6 & mask) + offset;
				((uint32_t*) p) [1] = (line >> 2 & mask) + offset;
				p += row_bytes;
			}
			
			if ( height & 1 )
			{
				unsigned long line = *lines;
				((uint32_t*) p) [0] = (line >> 4 & mask) + offset;
				((uint32_t*) p) [1] = (line      & mask) + offset;
			}
		} 
	}
	
	count = count2;
	count2 = 0;
	addr -= 32;
	attr_table = attr_table - nametable + nametable2;
	nametable = nametable2;
	if ( !count )
		break;
}
