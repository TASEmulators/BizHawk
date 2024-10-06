#include <emulibc.h>
#include <shared.h>

#include "callbacks.h"

#define SECTOR_DATA_SIZE 2352
#define SECTOR_SUBCODE_SIZE 96

ECL_INVISIBLE toc_t pending_toc;
int8 cd_index = 0;

struct cdStream_t
{
	unsigned sector_size;
	unsigned num_sectors;
	unsigned current_sector;
	int64_t current_offset;
	int64_t end_offset;
};

static cdStream cd_streams[128];
static cdStream audio_streams[128];
static cdStream subcode_streams[128];

static void cdStreamInit(cdStream* stream, toc_t* toc, int is_subcode)
{
	stream->sector_size = is_subcode ? SECTOR_SUBCODE_SIZE : SECTOR_DATA_SIZE;
	stream->num_sectors = toc->end;
	stream->current_sector = 0;
	stream->current_offset = 0;
	stream->end_offset = stream->sector_size * (int64_t)stream->num_sectors;

	if (!is_subcode)
	{
		toc->tracks[0].fd = stream;

		// audio tracks should be given a separate stream (to avoid conflicts for seeking)
		cdStream* audio_stream = &audio_streams[cd_index];
		memcpy(audio_stream, stream, sizeof(cdStream));

		for (unsigned i = 1; i < toc->last; i++)
		{
			toc->tracks[i].fd = audio_stream;
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
			if (load_archive("PRIMARY_CD", (unsigned char*)&pending_toc, sizeof(toc_t), NULL))
			{
				cd_index = 0;
				cdStreamInit(&cd_streams[0], &pending_toc, 0);
				return &cd_streams[0];
			}
		}
		else if (!strcmp(fname, "HOTSWAP_CD"))
		{
			cdStreamInit(&cd_streams[cd_index], &pending_toc, 0);
			return &cd_streams[cd_index];
		}
	}
	else if (!strcmp(fext, ".iso"))
	{
		// an .iso will attempt to be loaded for the "secondary" CD
		if (load_archive("SECONDARY_CD", (unsigned char*)&pending_toc, sizeof(toc_t), NULL))
		{
			cd_index = 0;
			cdStreamInit(&cd_streams[0], &pending_toc, 0);
			return &cd_streams[0];
		}
	}
	else if (!strcmp(fext, ".cue"))
	{
		// .cue file will attempt to be loaded for parsing
		// we use this to know when to load in the TOC
		// (can't do it when PRIMARY_CD/HOTSWAP_CD/SECONDARY_CD is opened, due to GPGX assuming those are a single track)
		memcpy(&cdd.toc, &pending_toc, sizeof(toc_t));
		return NULL;
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

static void cdStreamGetSector(cdStream* restrict stream, uint8_t sector[SECTOR_DATA_SIZE], unsigned* offset)
{
	if (stream->current_sector >= stream->num_sectors)
	{
		memset(sector, 0, SECTOR_DATA_SIZE);
		*offset = 0;
		return;
	}

	cdd_readcallback(stream->current_sector, sector, stream->sector_size == SECTOR_SUBCODE_SIZE);
	*offset = stream->current_offset - (stream->current_sector * stream->sector_size);
}

size_t cdStreamRead(void* restrict buffer, size_t size, size_t count, cdStream* restrict stream)
{
	uint8_t* restrict dest = buffer;
	size_t bytes_to_read = size * count; // in practice, this shouldn't ever overflow

	// we'll 0 fill the bytes past EOF, although we'll still report the bytes actually read 
	size_t ret = bytes_to_read;
	if (stream->current_offset + ret > stream->end_offset)
	{
		ret = stream->end_offset - stream->current_offset;
	}

	while (bytes_to_read > 0)
	{
		uint8_t sector[SECTOR_DATA_SIZE];
		unsigned offset;
		cdStreamGetSector(stream, sector, &offset);

		unsigned bytes_to_copy = stream->sector_size - offset;
		if (bytes_to_copy > bytes_to_read)
		{
			bytes_to_copy = bytes_to_read;
		}

		memcpy(dest, &sector[offset], bytes_to_copy);
		dest += bytes_to_copy;
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
