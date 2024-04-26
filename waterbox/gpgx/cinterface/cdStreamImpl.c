#include <emulibc.h>
#include <shared.h>

#include "callbacks.h"

#define SECTOR_DATA_SIZE 2352
#define SECTOR_SUBCODE_SIZE 96

ECL_INVISIBLE toc_t hotswap_toc;
int8 cd_index = 0;

struct cdStream_t
{
	int sector_size;
	int num_sectors;
	int64_t current_sector;
	int64_t current_offset;
	int64_t end_offset;
	uint8_t* cache_buffer;
	uint8_t* sectors_cached;
};

static cdStream cd_streams[128];
static cdStream subcode_streams[128];
ECL_INVISIBLE static int cache_is_allocated[256];

#define ALLOC_CACHE(stream) do \
{ \
	if (UNLIKELY(!cache_is_allocated[(uint8)cd_index + stream->sector_size == SECTOR_SUBCODE_SIZE ? 128 : 0])) \
	{ \
		stream->cache_buffer = alloc_invisible(stream->end_offset); \
		stream->sectors_cached = alloc_invisible(stream->num_sectors); \
		memset(stream->sectors_cached, 0, stream->num_sectors); \
		cache_is_allocated[(uint8)cd_index + stream->sector_size == SECTOR_SUBCODE_SIZE ? 128 : 0] = 1; \
	} \
} while (0);

static void cdStreamInit(cdStream* stream, toc_t* toc, int is_subcode)
{
	stream->sector_size = is_subcode ? SECTOR_SUBCODE_SIZE : SECTOR_DATA_SIZE;
	stream->num_sectors = toc->end;
	stream->current_sector = 0;
	stream->current_offset = 0;
	stream->end_offset = stream->sector_size * (int64_t)stream->num_sectors;
	ALLOC_CACHE(stream);

	if (!is_subcode)
	{
		for (int i = 0; i < toc->last; i++)
		{
			toc->tracks[i].fd = stream;
		}
	}
}

cdStream* cdStreamOpen(const char* fname)
{
	// This shouldn't happen
	if (cd_index < 0)
	{
		return NULL;
	}

	char* fext = strrchr(fname, '.');
	if (!fext)
	{
		if (!strcmp(fname, "PRIMARY_CD"))
		{
			if (load_archive("PRIMARY_CD", (unsigned char*)&cdd.toc, sizeof(toc_t), NULL))
			{
				cd_index = 0;
				cdStreamInit(&cd_streams[0], &cdd.toc, 0);
				return &cd_streams[0];
			}
		}
		else if (!strcmp(fname, "HOTSWAP_CD"))
		{
			memcpy(&cdd.toc, &hotswap_toc, sizeof(toc_t));
			cdStreamInit(&cd_streams[cd_index], &cdd.toc, 0);
			return &cd_streams[cd_index];
		}
	}
	else if (!strcmp(fext, ".iso"))
	{
		// an .iso will attempt to be loaded for the "secondary" CD
		if (load_archive("SECONDARY_CD", (unsigned char*)&cdd.toc, sizeof(toc_t), NULL))
		{
			cd_index = 0;
			cdStreamInit(&cd_streams[0], &cdd.toc, 0);
			return &cd_streams[0];
		}
	}
	else if (!strcmp(fext, ".sub"))
	{
		// separate stream for subcode
		cdStreamInit(&subcode_streams[cd_index], &cdd.toc, 1);
		return &subcode_streams[cd_index];
	}

	return NULL;
}

void cdStreamClose(cdStream* stream)
{
	// nothing to do
}

static uint8_t* cdStreamGetSector(cdStream* restrict stream, unsigned* offset)
{
	if (stream->current_sector >= stream->num_sectors)
	{
		static uint8_t empty_sector[SECTOR_DATA_SIZE];
		*offset = 0;
		return empty_sector;
	}

	*offset = stream->current_offset - (stream->current_sector * stream->sector_size);
	uint8_t* sector_cache = &stream->cache_buffer[stream->current_offset - *offset];

	if (!stream->sectors_cached[stream->current_sector])
	{
		cdd_readcallback(stream->current_sector, sector_cache, stream->sector_size == SECTOR_SUBCODE_SIZE, 0);
		stream->sectors_cached[stream->current_sector] = 1;
	}

	return sector_cache;
}

size_t cdStreamRead(void* restrict buffer, size_t size, size_t count, cdStream* restrict stream)
{
	ALLOC_CACHE(stream);

	size_t bytes_to_read = size * count; // in practice, this shouldn't ever overflow

	// we'll 0 fill the bytes past EOF, although we'll still report the bytes actually read 
	size_t ret = bytes_to_read;
	if (stream->current_offset + ret > stream->end_offset)
	{
		ret = stream->end_offset - stream->current_offset;
	}

	while (bytes_to_read > 0)
	{
		unsigned offset;
		uint8_t* sector = cdStreamGetSector(stream, &offset);

		unsigned bytes_to_copy = stream->sector_size - offset;
		if (bytes_to_copy > bytes_to_read)
		{
			bytes_to_copy = bytes_to_read;
		}

		memcpy(buffer, sector + offset, bytes_to_copy);
		bytes_to_read -= bytes_to_copy;

		stream->current_offset += bytes_to_copy;
		if (bytes_to_copy + offset >= stream->sector_size)
		{
			stream->current_sector++;
		}

		if (UNLIKELY(stream->current_offset >= stream->end_offset))
		{
			stream->current_offset = stream->end_offset;
			stream->current_sector = stream->num_sectors;
		}
	}

	// signal that the read has finished and the drive light should be turned on
	cdd_readcallback(0, NULL, 0, 1);
	return ret;
}

int cdStreamSeek(cdStream* stream, int64_t offset, int origin)
{
	switch (origin)
	{
		case SEEK_SET:
			stream->current_offset = offset;
			break;
		case SEEK_CUR:
			stream->current_offset += offset;
			break;
		case SEEK_END:
			stream->current_offset = stream->end_offset + offset;
			break;
	}

	if (stream->current_offset < 0)
	{
		stream->current_offset = 0;
	}

	if (stream->current_offset > stream->end_offset)
	{
		stream->current_offset = stream->end_offset;
	}

	stream->current_sector = stream->current_offset / stream->sector_size;
	return 0;
}

int64_t cdStreamTell(cdStream* stream)
{
	return stream->current_offset;
}

char* cdStreamGets(char* restrict str, int count, cdStream* restrict stream)
{
	// This is only used for GPGX's .cue file parsing, which is not used in our case
	return NULL;
}
