/* PDCLib I/O support <_PDCLIB_io.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef __PDCLIB_IO_H
#define __PDCLIB_IO_H __PDCLIB_IO_H

#include "_PDCLIB_int.h"
#include "_PDCLIB_threadconfig.h"

/* Flags for representing mode (see fopen()). Note these must fit the same
   status field as the _IO?BF flags in <stdio.h> and the internal flags below.
*/
#define _PDCLIB_FREAD     8u
#define _PDCLIB_FWRITE   16u
#define _PDCLIB_FAPPEND  32u
#define _PDCLIB_FRW      64u
#define _PDCLIB_FBIN    128u

/* Internal flags, made to fit the same status field as the flags above. */
/* -------------------------------------------------------------------------- */
/* free() the buffer memory on closing (false for user-supplied buffer) */
#define _PDCLIB_FREEBUFFER   512u
/* stream has encountered error / EOF */
#define _PDCLIB_ERRORFLAG   1024u
#define _PDCLIB_EOFFLAG     2048u
/* stream is wide-oriented */
#define _PDCLIB_WIDESTREAM  4096u
/* stream is byte-oriented */
#define _PDCLIB_BYTESTREAM  8192u
/* file associated with stream should be remove()d on closing (tmpfile()) */
#define _PDCLIB_DELONCLOSE 16384u
/* stream handle should not be free()d on close (stdin, stdout, stderr) */
#define _PDCLIB_STATIC     32768u

union _PDCLIB_fd
{
#if defined(_PDCLIB_OSFD_T)
    _PDCLIB_OSFD_T      osfd;
#endif
    void *              pointer;
    _PDCLIB_uintptr_t   uval;
    _PDCLIB_intptr_t    sval;
};

/******************************************************************************/
/* Internal functions                                                         */
/******************************************************************************/

/* The worker for all printf() type of functions. The pointer spec should point
   to the introducing '%' of a conversion specifier. The status structure is to
   be that of the current printf() function, of which the members n, s, stream
   and arg will be preserved; i will be updated; and all others will be trashed
   by the function.
   Returns the number of characters parsed as a conversion specifier (0 if none
   parsed); returns -1 if the underlying I/O callback returns failure.
*/
int _PDCLIB_print( const char * spec, struct _PDCLIB_status_t * status );

/* The worker for all scanf() type of functions. The pointer spec should point
   to the introducing '%' of a conversion specifier. The status structure is to
   be that of the current scanf() function, of which the member stream will be
   preserved; n, i, and s will be updated; and all others will be trashed by
   the function.
   Returns a pointer to the first character not parsed as conversion specifier,
   or NULL in case of error.
   FIXME: Should distinguish between matching and input error
*/
const char * _PDCLIB_scan( const char * spec, struct _PDCLIB_status_t * status );

/* Parsing any fopen() style filemode string into a number of flags. */
unsigned int _PDCLIB_filemode( const char * mode );

/* Sanity checking and preparing of read buffer, should be called first thing
   by any stdio read-data function.
   Returns 0 on success, EOF on error.
   On error, EOF / error flags and errno are set appropriately.
*/
int _PDCLIB_prepread( _PDCLIB_file_t * stream );

/* Sanity checking, should be called first thing by any stdio write-data
   function.
   Returns 0 on success, EOF on error.
   On error, error flags and errno are set appropriately.
*/
int _PDCLIB_prepwrite( _PDCLIB_file_t * stream );

/* Closing all streams on program exit */
void _PDCLIB_closeall( void );

/* Writes a stream's buffer.
   Returns 0 on success, EOF on write error.
   Sets stream error flags and errno appropriately on error.
*/
int _PDCLIB_flushbuffer( _PDCLIB_file_t * stream );

/* Fills a stream's buffer.
   Returns 0 on success, EOF on read error / EOF.
   Sets stream EOF / error flags and errno appropriately on error.
*/
int _PDCLIB_fillbuffer( _PDCLIB_file_t * stream );

/* Repositions within a file. Returns new offset on success,
   -1 / errno on error.
*/
_PDCLIB_int_fast64_t _PDCLIB_seek( _PDCLIB_file_t * stream,
                                  _PDCLIB_int_fast64_t offset, int whence );

/* File backend I/O operations
 *
 * PDCLib will call through to these methods as needed to implement the stdio
 * functions.
 */
struct _PDCLIB_fileops
{
    /*! Read length bytes from the file into buf; returning the number of bytes
     *  actually read in *numBytesRead.
     *
     *  Returns true if bytes were read successfully; on end of file, returns
     *  true with *numBytesRead == 0.
     *
     *  On error, returns false and sets errno appropriately. *numBytesRead is
     *  ignored in this situation.
     */
    _PDCLIB_bool (*read)( _PDCLIB_fd_t self,
                          void * buf,
                          _PDCLIB_size_t length,
                          _PDCLIB_size_t * numBytesRead );

    /*! Write length bytes to the file from buf; returning the number of bytes
     *  actually written in *numBytesWritten
     *
     *  Returns true if bytes were written successfully. On error, returns false
     *  and setss errno appropriately (as with read, *numBytesWritten is
     *  ignored)
     */
    _PDCLIB_bool (*write)( _PDCLIB_fd_t self, const void * buf,
                   _PDCLIB_size_t length, _PDCLIB_size_t * numBytesWritten );

    /* Seek to the file offset specified by offset, from location whence, which
     * may be one of the standard constants SEEK_SET/SEEK_CUR/SEEK_END
     */
    _PDCLIB_bool (*seek)( _PDCLIB_fd_t self, _PDCLIB_int_fast64_t offset,
                          int whence, _PDCLIB_int_fast64_t *newPos );

    void (*close)( _PDCLIB_fd_t self );

    /*! Behaves as read does, except for wide characters. Both length and
     *  *numCharsRead represent counts of characters, not bytes.
     *
     *  This function is optional; if missing, PDCLib will buffer the character
     *  data as bytes and perform translation directly into the user's buffers.
     *  It is useful if your backend can directly take wide characters (for
     *  example, the Windows console)
     */
    _PDCLIB_bool (*wread)( _PDCLIB_fd_t self, _PDCLIB_wchar_t * buf,
                     _PDCLIB_size_t length, _PDCLIB_size_t * numCharsRead );

    /* Behaves as write does, except for wide characters. As with wread, both
     * length and *numCharsWritten are character counts.
     *
     * This function is also optional; if missing, PDCLib will buffer the
     * character data as bytes and do translation directly from the user's
     * buffers. You only need to implement this if your backend can directly
     * take wide characters (for example, the Windows console)
     */
    _PDCLIB_bool (*wwrite)( _PDCLIB_fd_t self, const _PDCLIB_wchar_t * buf,
                     _PDCLIB_size_t length, _PDCLIB_size_t * numCharsWritten );
};

/* struct _PDCLIB_file structure */
struct _PDCLIB_file
{
    const _PDCLIB_fileops_t * ops;
    _PDCLIB_fd_t              handle;   /* OS file handle */
    _PDCLIB_MTX_T             lock;     /* file lock */
    char *                    buffer;   /* Pointer to buffer memory */
    _PDCLIB_size_t            bufsize;  /* Size of buffer */
    _PDCLIB_size_t            bufidx;   /* Index of current position in buffer */
    _PDCLIB_size_t            bufend;   /* Index of last pre-read character in buffer */
#ifdef _PDCLIB_NEED_EOL_TRANSLATION
    _PDCLIB_size_t            bufnlexp; /* Current position of buffer newline expansion */
#endif
    _PDCLIB_size_t            ungetidx; /* Number of ungetc()'ed characters */
    unsigned char *           ungetbuf; /* ungetc() buffer */
    unsigned int              status;   /* Status flags; see above */
    /* multibyte parsing status to be added later */
    _PDCLIB_fpos_t            pos;      /* Offset and multibyte parsing state */
    char *                    filename; /* Name the current stream has been opened with */
    _PDCLIB_file_t *          next;     /* Pointer to next struct (internal) */
};

static inline _PDCLIB_size_t _PDCLIB_getchars( char * out, _PDCLIB_size_t n,
                                               int stopchar,
                                               _PDCLIB_file_t * stream )
{
    _PDCLIB_size_t i = 0;
    int c;
    while ( stream->ungetidx > 0 && i != n )
    {
        c = (unsigned char)
                ( out[ i++ ] = stream->ungetbuf[ --(stream->ungetidx) ] );
        if( c == stopchar )
            return i;
    }

    while ( i != n )
    {
        while ( stream->bufidx != stream->bufend && i != n)
        {
            c = (unsigned char) stream->buffer[ stream->bufidx++ ];
#ifdef _PDCLIB_NEED_EOL_TRANSLATION
            if ( !( stream->status & _PDCLIB_FBIN ) && c == '\r' )
            {
                if ( stream->bufidx == stream->bufend )
                    break;

                if ( stream->buffer[ stream->bufidx ] == '\n' )
                {
                    c = '\n';
                    stream->bufidx++;
                }
            }
#endif
            out[ i++ ] = c;

            if( c == stopchar )
                return i;
        }

        if ( i != n )
        {
            if( _PDCLIB_fillbuffer( stream ) == -1 )
            {
                break;
            }
        }
    }

#ifdef _PDCLIB_NEED_EOL_TRANSLATION
    if ( i != n && stream->bufidx != stream->bufend )
    {
        // we must have EOF'd immediately after a \r
        out[ i++ ] = stream->buffer[ stream->bufidx++ ];
    }
#endif

    return i;
}

/* Unlocked functions - internal names
 *
 * We can't use the functions using their "normal" names internally because that
 * would cause namespace leakage. Therefore, we use them by prefixed internal
 * names
 */
void _PDCLIB_flockfile(struct _PDCLIB_file *file) _PDCLIB_nothrow;
int _PDCLIB_ftrylockfile(struct _PDCLIB_file *file) _PDCLIB_nothrow;
void _PDCLIB_funlockfile(struct _PDCLIB_file *file) _PDCLIB_nothrow;

int _PDCLIB_getc_unlocked(struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_getchar_unlocked(void) _PDCLIB_nothrow;
int _PDCLIB_putc_unlocked(int c, struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_putchar_unlocked(int c) _PDCLIB_nothrow;
void _PDCLIB_clearerr_unlocked(struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_feof_unlocked(struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_ferror_unlocked(struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_fflush_unlocked(struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_fgetc_unlocked(struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_fputc_unlocked(int c, struct _PDCLIB_file *stream) _PDCLIB_nothrow;
_PDCLIB_size_t _PDCLIB_fread_unlocked(void *ptr, _PDCLIB_size_t size, _PDCLIB_size_t n, struct _PDCLIB_file *stream) _PDCLIB_nothrow;
_PDCLIB_size_t _PDCLIB_fwrite_unlocked(const void *ptr, _PDCLIB_size_t size, _PDCLIB_size_t n, struct _PDCLIB_file *stream) _PDCLIB_nothrow;
char *_PDCLIB_fgets_unlocked(char *s, int n, struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_fputs_unlocked(const char *s, struct _PDCLIB_file *stream) _PDCLIB_nothrow;
int _PDCLIB_fgetpos_unlocked( struct _PDCLIB_file * _PDCLIB_restrict stream, _PDCLIB_fpos_t * _PDCLIB_restrict pos ) _PDCLIB_nothrow;
int _PDCLIB_fsetpos_unlocked( struct _PDCLIB_file * stream, const _PDCLIB_fpos_t * pos ) _PDCLIB_nothrow;
long int _PDCLIB_ftell_unlocked( struct _PDCLIB_file * stream ) _PDCLIB_nothrow;
int _PDCLIB_fseek_unlocked( struct _PDCLIB_file * stream, long int offset, int whence ) _PDCLIB_nothrow;
void _PDCLIB_rewind_unlocked( struct _PDCLIB_file * stream ) _PDCLIB_nothrow;

int _PDCLIB_puts_unlocked( const char * s ) _PDCLIB_nothrow;
int _PDCLIB_ungetc_unlocked( int c, struct _PDCLIB_file * stream ) _PDCLIB_nothrow;


int _PDCLIB_printf_unlocked( const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int _PDCLIB_vprintf_unlocked( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;
int _PDCLIB_fprintf_unlocked( struct _PDCLIB_file * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int _PDCLIB_vfprintf_unlocked( struct _PDCLIB_file * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;
int _PDCLIB_scanf_unlocked( const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int _PDCLIB_vscanf_unlocked( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;
int _PDCLIB_fscanf_unlocked( struct _PDCLIB_file * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int _PDCLIB_vfscanf_unlocked( struct _PDCLIB_file * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

#endif
