#ifndef CD_STREAM_H
#define CD_STREAM_H

#include <stddef.h>
#include <stdio.h>

struct cdStream_t;
typedef struct cdStream_t cdStream;

extern cdStream *cdStreamOpen(const char *fname);
extern void cdStreamClose(cdStream *stream);
extern size_t cdStreamRead(void *restrict buffer, size_t size, size_t count, cdStream *restrict stream);
extern int cdStreamSeek(cdStream *stream, int64_t offset, int origin);
extern int64_t cdStreamTell(cdStream *stream);
extern char *cdStreamGets(char *restrict str, int count, cdStream *restrict stream);

#endif
