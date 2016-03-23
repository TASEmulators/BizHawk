/* _PDCLIB_print( const char *, struct _PDCLIB_status_t * )

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#include <stdio.h>
#include <stdint.h>
#include <stdarg.h>
#include <string.h>
#include <stdlib.h>
#include <stddef.h>
#include <stdbool.h>
#include <limits.h>

#ifndef REGTEST
#include "_PDCLIB_io.h"

/* Using an integer's bits as flags for both the conversion flags and length
   modifiers.
*/
/* FIXME: one too many flags to work on a 16-bit machine, join some (e.g. the
          width flags) into a combined field.
*/
#define E_minus    (1<<0)
#define E_plus     (1<<1)
#define E_alt      (1<<2)
#define E_space    (1<<3)
#define E_zero     (1<<4)
#define E_done     (1<<5)

#define E_char     (1<<6)
#define E_short    (1<<7)
#define E_long     (1<<8)
#define E_llong    (1<<9)
#define E_intmax   (1<<10)
#define E_size     (1<<11)
#define E_ptrdiff  (1<<12)
#define E_intptr   (1<<13)

#define E_ldouble  (1<<14)

#define E_lower    (1<<15)
#define E_unsigned (1<<16)

#define E_TYPES (E_char | E_short | E_long | E_llong | E_intmax \
                | E_size | E_ptrdiff | E_intptr)

/* returns true if callback-based output succeeded; else false */
static inline bool cbout(
    struct _PDCLIB_status_t * status,
    const void * buf,
    size_t size )
{
    size_t rv = status->write( status->ctx, buf, size );
    status->i       += rv;
    status->current += rv;
    return rv == size;
}

/* repeated output of a single character */
static inline bool cbrept(
    struct _PDCLIB_status_t * status,
    char c,
    size_t times )
{
    if ( sizeof(size_t) == 8 && CHAR_BIT == 8)
    {
        uint64_t spread = UINT64_C(0x0101010101010101) * c;
        while ( times )
        {
            size_t n = times > 8 ? 8 : times;
            if ( !cbout( status, &spread, n ) )
                return false;
            times -= n;
        }
        return true;
    }
    else if ( sizeof(size_t) == 4  && CHAR_BIT == 8)
    {
        uint32_t spread = UINT32_C(0x01010101) * c;
        while ( times )
        {
            size_t n = times > 4 ? 4 : times;
            if ( !cbout( status, &spread, n ) )
                return false;
            times -= n;
        }
        return true;
    }
    else
    {
        while ( times )
        {
            if ( !cbout( status, &c, 1) )
                return false;
            times--;
        }
        return true;
    }
}


/* Maximum number of output characters =
 *   number of bits in (u)intmax_t / number of bits per character in smallest
 *   base. Smallest base is octal, 3 bits/char.
 *
 * Additionally require 2 extra characters for prefixes
 *
 * Returns false if an I/O error occured.
 */
static const size_t maxIntLen = sizeof(intmax_t) * CHAR_BIT / 3 + 1;

static bool int2base( uintmax_t value, struct _PDCLIB_status_t * status )
{
    char sign = 0;
    if ( ! ( status->flags & E_unsigned ) )
    {
        intmax_t signval = (intmax_t) value;
        bool negative = signval < 0;
        value = signval < 0 ? -signval : signval;

        if ( negative )
        {
            sign = '-';
        }
        else if ( status->flags & E_plus )
        {
            sign = '+';
        }
        else if (status->flags & E_space )
        {
            sign = ' ';
        }
    }

    // The user could theoretically ask for a silly buffer length here.
    // Perhaps after a certain size we should malloc? Or do we refuse to protect
    // them from their own stupidity?
    size_t bufLen = (status->width > maxIntLen ? status->width : maxIntLen) + 2;
    char outbuf[bufLen];
    char * outend = outbuf + bufLen;
    int written = 0;

    // Build up our output string - backwards
    {
        const char * digits = (status->flags & E_lower) ?
                                _PDCLIB_digits : _PDCLIB_Xdigits;
        uintmax_t remaining = value;
        if(status->prec != 0 || remaining != 0) do {
            uintmax_t digit = remaining % status->base;
            remaining /= status->base;

            outend[-++written] = digits[digit];
        } while(remaining != 0);
    }

    // Pad field out to the precision specification
    while( (long) written < status->prec ) outend[-++written] = '0';

    // If a field width specified, and zero padding was requested, then pad to
    // the field width
    unsigned padding = 0;
    if ( ( ! ( status->flags & E_minus ) ) && ( status->flags & E_zero ) )
    {
        while( written < (int) status->width )
        {
            outend[-++written] = '0';
            padding++;
        }
    }

    // Prefixes
    if ( sign != 0 )
    {
        if ( padding == 0 ) written++;
        outend[-written] = sign;
    }
    else if ( status->flags & E_alt )
    {
        switch ( status->base )
        {
            case 8:
                if ( outend[-written] != '0' ) outend[-++written] = '0';
                break;
            case 16:
                // No prefix if zero
                if ( value == 0 ) break;

                written += padding < 2 ? 2 - padding : 0;
                outend[-written    ] = '0';
                outend[-written + 1] = (status->flags & E_lower) ? 'x' : 'X';
                break;
            default:
                break;
        }
    }

    // Space padding to field width
    if ( ! ( status->flags & ( E_minus | E_zero ) ) )
    {
        while( written < (int) status->width ) outend[-++written] = ' ';
    }

    // Write output
    return cbout( status, outend - written, written );
}

/* print a string. returns false if an I/O error occured */
static bool printstr( const char * str, struct _PDCLIB_status_t * status )
{
    size_t len = status->prec >= 0 ? strnlen( str, status-> prec)
                                   : strlen(str);

    if ( status->width == 0 || status->flags & E_minus )
    {
        // Simple case or left justification
        if ( status->prec > 0 )
        {
            len = (unsigned) status->prec < len ? (unsigned)  status->prec : len;
        }

        if ( !cbout( status, str, len ) )
            return false;

        /* right padding */
        if ( status->width > status->current ) {
            len = status->width - status->current;

            if ( !cbrept( status, ' ', len ) )
                return false;
        }
    } else {
        // Right justification

        if ( status->width > len ) {
            size_t padding = status->width - len;

            if ( !cbrept( status, ' ', padding ))
                return false;
        }

        if ( !cbout( status, str, len ) )
            return false;
    }

    return true;
}

static bool printchar( char chr, struct _PDCLIB_status_t * status )
{
    if( ! ( status->flags & E_minus ) )
    {
        // Right justification
        if ( status-> width ) {
            size_t justification = status->width - status->current - 1;
            if ( !cbrept( status, ' ', justification ))
                return false;
        }

        if ( !cbout( status, &chr, 1 ))
            return false;
    } else {
        // Left justification

        if ( !cbout( status, &chr, 1 ))
            return false;

        if ( status->width > status->current ) {
            if ( !cbrept( status, ' ', status->width - status->current ) )
                return false;
        }
    }

    return true;
}

int _PDCLIB_print( const char * spec, struct _PDCLIB_status_t * status )
{
    const char * orig_spec = spec;
    if ( *(++spec) == '%' )
    {
        /* %% -> print single '%' */
        if ( !cbout(status, spec, 1) )
            return -1;
        ++spec;
        return (spec - orig_spec);
    }
    /* Initializing status structure */
    status->flags = 0;
    status->base  = 0;
    status->current  = 0;
    status->width = 0;
    status->prec  = EOF;

    /* First come 0..n flags */
    do
    {
        switch ( *spec )
        {
            case '-':
                /* left-aligned output */
                status->flags |= E_minus;
                ++spec;
                break;
            case '+':
                /* positive numbers prefixed with '+' */
                status->flags |= E_plus;
                ++spec;
                break;
            case '#':
                /* alternative format (leading 0x for hex, 0 for octal) */
                status->flags |= E_alt;
                ++spec;
                break;
            case ' ':
                /* positive numbers prefixed with ' ' */
                status->flags |= E_space;
                ++spec;
                break;
            case '0':
                /* right-aligned padding done with '0' instead of ' ' */
                status->flags |= E_zero;
                ++spec;
                break;
            default:
                /* not a flag, exit flag parsing */
                status->flags |= E_done;
                break;
        }
    } while ( ! ( status->flags & E_done ) );

    /* Optional field width */
    if ( *spec == '*' )
    {
        /* Retrieve width value from argument stack */
        int width = va_arg( status->arg, int );
        if ( width < 0 )
        {
            status->flags |= E_minus;
            status->width = abs( width );
        }
        else
        {
            status->width = width;
        }
        ++spec;
    }
    else
    {
        /* If a width is given, strtol() will return its value. If not given,
           strtol() will return zero. In both cases, endptr will point to the
           rest of the conversion specifier - just what we need.
        */
        status->width = (int)strtol( spec, (char**)&spec, 10 );
    }

    /* Optional precision */
    if ( *spec == '.' )
    {
        ++spec;
        if ( *spec == '*' )
        {
            /* Retrieve precision value from argument stack. A negative value
               is as if no precision is given - as precision is initalized to
               EOF (negative), there is no need for testing for negative here.
            */
            status->prec = va_arg( status->arg, int );
            ++spec;
        }
        else
        {
            status->prec = (int)strtol( spec, (char**) &spec, 10 );
        }
        /* Having a precision cancels out any zero flag. */
        status->flags &= ~E_zero;
    }

    /* Optional length modifier
       We step one character ahead in any case, and step back only if we find
       there has been no length modifier (or step ahead another character if it
       has been "hh" or "ll").
    */
    switch ( *(spec++) )
    {
        case 'h':
            if ( *spec == 'h' )
            {
                /* hh -> char */
                status->flags |= E_char;
                ++spec;
            }
            else
            {
                /* h -> short */
                status->flags |= E_short;
            }
            break;
        case 'l':
            if ( *spec == 'l' )
            {
                /* ll -> long long */
                status->flags |= E_llong;
                ++spec;
            }
            else
            {
                /* k -> long */
                status->flags |= E_long;
            }
            break;
        case 'j':
            /* j -> intmax_t, which might or might not be long long */
            status->flags |= E_intmax;
            break;
        case 'z':
            /* z -> size_t, which might or might not be unsigned int */
            status->flags |= E_size;
            break;
        case 't':
            /* t -> ptrdiff_t, which might or might not be long */
            status->flags |= E_ptrdiff;
            break;
        case 'L':
            /* L -> long double */
            status->flags |= E_ldouble;
            break;
        default:
            --spec;
            break;
    }

    /* Conversion specifier */
    switch ( *spec )
    {
        case 'd':
            /* FALLTHROUGH */
        case 'i':
            status->base = 10;
            break;
        case 'o':
            status->base = 8;
            status->flags |= E_unsigned;
            break;
        case 'u':
            status->base = 10;
            status->flags |= E_unsigned;
            break;
        case 'x':
            status->base = 16;
            status->flags |= ( E_lower | E_unsigned );
            break;
        case 'X':
            status->base = 16;
            status->flags |= E_unsigned;
            break;
        case 'f':
        case 'F':
        case 'e':
        case 'E':
        case 'g':
        case 'G':
            break;
        case 'a':
        case 'A':
            break;
        case 'c':
            /* TODO: wide chars. */
            if ( !printchar( va_arg( status->arg, int ), status ) )
                return -1;
            ++spec;
            return (spec - orig_spec);
        case 's':
            /* TODO: wide chars. */
            {
                char * s = va_arg( status->arg, char * );
                if ( !printstr( s, status ) )
                    return -1;
                ++spec;
                return (spec - orig_spec);
            }
        case 'p':
            status->base = 16;
            status->flags |= ( E_lower | E_unsigned | E_alt | E_intptr );
            break;
        case 'n':
           {
               int * val = va_arg( status->arg, int * );
               *val = status->i;
               ++spec;
               return (spec - orig_spec);
           }
        default:
            /* No conversion specifier. Bad conversion. */
            return 0;
    }
    /* Do the actual output based on our findings */
    if ( status->base != 0 )
    {
        /* Integer conversions */
        /* TODO: Check for invalid flag combinations. */
        if ( status->flags & E_unsigned )
        {
            /* TODO: Marking the default case _PDCLIB_UNREACHABLE breaks %ju test driver? */
            uintmax_t value = 0;
            switch ( status->flags & E_TYPES )
            {
                case E_char:
                    value = (uintmax_t)(unsigned char)va_arg( status->arg, int );
                    break;
                case E_short:
                    value = (uintmax_t)(unsigned short)va_arg( status->arg, int );
                    break;
                case 0:
                    value = (uintmax_t)va_arg( status->arg, unsigned int );
                    break;
                case E_long:
                    value = (uintmax_t)va_arg( status->arg, unsigned long );
                    break;
                case E_llong:
                    value = (uintmax_t)va_arg( status->arg, unsigned long long );
                    break;
                case E_size:
                    value = (uintmax_t)va_arg( status->arg, size_t );
                    break;
                case E_intptr:
                    value = (uintmax_t)va_arg( status->arg, uintptr_t );
                    break;
                case E_ptrdiff:
                    value = (uintmax_t)va_arg( status->arg, ptrdiff_t );
                    break;
                case E_intmax:
                    value = va_arg( status->arg, uintmax_t );
            }
            if ( !int2base( value, status ) )
                return -1;
        }
        else
        {
            intmax_t value = 0;
            switch ( status->flags & E_TYPES )
            {
                case E_char:
                    value = (intmax_t)(char)va_arg( status->arg, int );
                    break;
                case E_short:
                    value = (intmax_t)(short)va_arg( status->arg, int );
                    break;
                case 0:
                    value = (intmax_t)va_arg( status->arg, int );
                    break;
                case E_long:
                    value = (intmax_t)va_arg( status->arg, long );
                    break;
                case E_llong:
                    value = (intmax_t)va_arg( status->arg, long long );
                    break;
                case E_size:
                    value = (intmax_t)va_arg( status->arg, size_t );
                    break;
                case E_intptr:
                    value = (intmax_t)va_arg( status->arg, intptr_t );
                    break;
                case E_ptrdiff:
                    value = (intmax_t)va_arg( status->arg, ptrdiff_t );
                    break;
                case E_intmax:
                    value = va_arg( status->arg, intmax_t );
                    break;
                default:
                    _PDCLIB_UNREACHABLE;
            }

            if (!int2base( value, status ) )
                return -1;
        }

        if ( status->flags & E_minus && status->current < status->width )
        {
            if (!cbrept( status, ' ', status->width - status->current ))
                return -1;
        }
    }
    ++spec;
    return spec - orig_spec;
}

#endif

#ifdef TEST
#define _PDCLIB_FILEID "_PDCLIB/print.c"
#define _PDCLIB_STRINGIO

#include "_PDCLIB_test.h"

#ifndef REGTEST
static size_t testcb( void *p, const char *buf, size_t size )
{
    char **destbuf = p;
    memcpy(*destbuf, buf, size);
    *destbuf += size;
    return size;
}

static int testprintf( char * buffer, const char * format, ... )
{
    /* Members: base, flags, n, i, current, width, prec, ctx, cb, arg      */
    struct _PDCLIB_status_t status;
    status.base = 0;
    status.flags = 0;
    status.n = 100;
    status.i = 0;
    status.current = 0;
    status.width = 0;
    status.prec = 0;
    status.ctx = &buffer;
    status.write = testcb;
    va_start( status.arg, format );
    memset( buffer, '\0', 100 );
    if ( _PDCLIB_print( format, &status ) != (int)strlen( format ) )
    {
        printf( "_PDCLIB_print() did not return end-of-specifier on '%s'.\n", format );
        ++TEST_RESULTS;
    }
    va_end( status.arg );
    return status.i;
}
#endif

#define TEST_CONVERSION_ONLY

int main( void )
{
#ifndef REGTEST
    char target[100];
#include "printf_testcases.h"
#endif
    return TEST_RESULTS;
}

#endif
