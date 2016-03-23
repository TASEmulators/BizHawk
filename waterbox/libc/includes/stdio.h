/* Input/output <stdio.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_STDIO_H
#define _PDCLIB_STDIO_H _PDCLIB_STDIO_H
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifndef _PDCLIB_SIZE_T_DEFINED
#define _PDCLIB_SIZE_T_DEFINED _PDCLIB_SIZE_T_DEFINED
typedef _PDCLIB_size_t size_t;
#endif

#ifndef _PDCLIB_NULL_DEFINED
#define _PDCLIB_NULL_DEFINED _PDCLIB_NULL_DEFINED
#define NULL _PDCLIB_NULL
#endif

/* See setvbuf(), third argument */
#define _IOFBF 1
#define _IOLBF 2
#define _IONBF 4

/* The following are platform-dependant, and defined in _PDCLIB_config.h. */
typedef _PDCLIB_fpos_t fpos_t;
typedef _PDCLIB_file_t FILE;
#define EOF -1
#define BUFSIZ _PDCLIB_BUFSIZ
#define FOPEN_MAX _PDCLIB_FOPEN_MAX
#define FILENAME_MAX _PDCLIB_FILENAME_MAX
#define L_tmpnam _PDCLIB_L_tmpnam
#define TMP_MAX _PDCLIB_TMP_MAX

/* See fseek(), third argument
 *
 * Some system headers (e.g. windows) also define the SEEK_* values. Check for
 * this and validate that they're the same value
 */
#if !defined(SEEK_CUR)
    #define SEEK_CUR _PDCLIB_SEEK_CUR
#elif SEEK_CUR != _PDCLIB_SEEK_CUR
    #error SEEK_CUR != _PDCLIB_SEEK_CUR
#endif

#if !defined(SEEK_END)
    #define SEEK_END _PDCLIB_SEEK_END
#elif SEEK_END != _PDCLIB_SEEK_END
    #error SEEK_END != _PDCLIB_SEEK_END
#endif

#if !defined(SEEK_SET)
    #define SEEK_SET _PDCLIB_SEEK_SET
#elif SEEK_SET != _PDCLIB_SEEK_SET
    #error SEEK_SET != _PDCLIB_SEEK_SET
#endif

extern FILE * stdin;
extern FILE * stdout;
extern FILE * stderr;

/* Operations on files */

/* Remove the given file.
   Returns zero if successful, non-zero otherwise.
   This implementation does detect if a file of that name is currently open,
   and fails the remove in this case. This does not detect two distinct names
   that merely result in the same file (e.g. "/home/user/foo" vs. "~/foo").
*/
int remove( const char * filename ) _PDCLIB_nothrow;

/* Rename the given old file to the given new name.
   Returns zero if successful, non-zero otherwise.
   This implementation does detect if the old filename corresponds to an open
   file, and fails the rename in this case.
   If there already is a file with the new filename, behaviour is defined by
   the glue code (see functions/_PDCLIB/rename.c).
*/
int rename( const char * old, const char * newn ) _PDCLIB_nothrow;

/* Open a temporary file with mode "wb+", i.e. binary-update. Remove the file
   automatically if it is closed or the program exits normally (by returning
   from main() or calling exit()).
   Returns a pointer to a FILE handle for this file.
   This implementation does not remove temporary files if the process aborts
   abnormally (e.g. abort()).
*/
FILE * tmpfile( void ) _PDCLIB_nothrow;

/* Generate a file name that is not equal to any existing filename AT THE TIME
   OF GENERATION. Generate a different name each time it is called.
   Returns a pointer to an internal static buffer containing the filename if s
   is a NULL pointer. (This is not thread-safe!)
   Returns s if it is not a NULL pointer (s is then assumed to point to an array
   of at least L_tmpnam characters).
   Returns NULL if unable to generate a suitable name (because all possible
   names already exist, or the function has been called TMP_MAX times already).
   Note that this implementation cannot guarantee a file of the name generated
   is not generated between the call to this function and a subsequent fopen().
*/
char * tmpnam( char * s ) _PDCLIB_nothrow;

/* File access functions */

/* Close the file associated with the given stream (after flushing its buffers).
   Returns zero if successful, EOF if any errors occur.
*/
int fclose( FILE * stream ) _PDCLIB_nothrow;

/* Flush the buffers of the given output stream. If the stream is an input
   stream, or an update stream with the last operation being an input operation,
   behaviour is undefined.
   If stream is a NULL pointer, perform the buffer flushing for all applicable
   streams.
   Returns zero if successful, EOF if a write error occurs.
   Sets the error indicator of the stream if a write error occurs.
*/
int fflush( FILE * stream ) _PDCLIB_nothrow;

/* Open the file with the given filename in the given mode, and return a stream
   handle for it in which error and end-of-file indicator are cleared. Defined
   values for mode are:

   READ MODES
                      text files        binary files
   without update     "r"               "rb"
   with update        "r+"              "rb+" or "r+b"

   Opening in read mode fails if no file with the given filename exists, or if
   cannot be read.

   WRITE MODES
                      text files        binary files
   without update     "w"               "wb"
   with update        "w+"              "wb+" or "w+b"

   With write modes, if a file with the given filename already exists, it is
   truncated to zero length.

   APPEND MODES
                      text files        binary files
   without update     "a"               "ab"
   with update        "a+"              "ab+" or "a+b"

   With update modes, if a file with the given filename already exists, it is
   not truncated to zero length, but all writes are forced to end-of-file (this
   regardless to fseek() calls). Note that binary files opened in append mode
   might have their end-of-file padded with '\0' characters.

   Update modes mean that both input and output functions can be performed on
   the stream, but output must be terminated with a call to either fflush(),
   fseek(), fsetpos(), or rewind() before input is performed, and input must
   be terminated with a call to either fseek(), fsetpos(), or rewind() before
   output is performed, unless input encountered end-of-file.

   If a text file is opened with update mode, the implementation is at liberty
   to open a binary stream instead. This implementation honors the exact mode
   given.

   The stream is fully buffered if and only if it can be determined not to
   refer to an interactive device.

   If the mode string begins with but is longer than one of the above sequences
   the implementation is at liberty to ignore the additional characters, or do
   implementation-defined things. This implementation only accepts the exact
   modes above.

   Returns a pointer to the stream handle if successfull, NULL otherwise.
*/
FILE * fopen( const char * _PDCLIB_restrict filename,
              const char * _PDCLIB_restrict mode ) _PDCLIB_nothrow;

/* Creates a stream connected to the file descriptor \p fd with mode \p mode.
   Mode must match the mode with which the file descriptor was opened.
*/
FILE * _PDCLIB_fvopen( _PDCLIB_fd_t fd, const _PDCLIB_fileops_t * ops,
                       int mode, const char * filename ) _PDCLIB_nothrow;

/* Close any file currently associated with the given stream. Open the file
   identified by the given filename with the given mode (equivalent to fopen()),
   and associate it with the given stream. If filename is a NULL pointer,
   attempt to change the mode of the given stream.
   This implementation allows any mode changes on "real" files, and associating
   of the standard streams with files. It does *not* support mode changes on
   standard streams.
   (Primary use of this function is to redirect stdin, stdout, and stderr.)
*/
FILE * freopen( const char * _PDCLIB_restrict filename, const char * _PDCLIB_restrict mode, FILE * _PDCLIB_restrict stream ) _PDCLIB_nothrow;

/* If buf is a NULL pointer, call setvbuf( stream, NULL, _IONBF, BUFSIZ ).
   If buf is not a NULL pointer, call setvbuf( stream, buf, _IOFBF, BUFSIZ ).
*/
void setbuf( FILE * _PDCLIB_restrict stream, char * _PDCLIB_restrict buf ) _PDCLIB_nothrow;

/* Set the given stream to the given buffering mode. If buf is not a NULL
   pointer, use buf as file buffer (of given size). If buf is a NULL pointer,
   use a buffer of given size allocated internally. _IONBF causes unbuffered
   behaviour, _IOLBF causes line-buffered behaviour, _IOFBF causes fully
   buffered behaviour. Calling this function is only valid right after a file is
   opened, and before any other operation (except for any unsuccessful calls to
   setvbuf()) has been performed.
   Returns zero if successful, nonzero otherwise.
*/
int setvbuf( FILE * _PDCLIB_restrict stream, char * _PDCLIB_restrict buf, int mode, size_t size ) _PDCLIB_nothrow;

/* Formatted input/output functions */

/*
   Write output to the given stream, as defined by the given format string and
   0..n subsequent arguments (the argument stack).

   The format string is written to the given stream verbatim, except for any
   conversion specifiers included, which start with the letter '%' and are
   documented below. If the given conversion specifiers require more arguments
   from the argument stack than provided, behaviour is undefined. Additional
   arguments not required by conversion specifiers are evaluated but otherwise
   ignored.

   (The standard specifies the format string is allowed to contain multibyte
   character sequences as long as it starts and ends in initial shift state,
   but this is not yet supported by this implementation, which interprets the
   format string as sequence of char.)
   TODO: Add multibyte support to printf() functions.

   A conversion specifier consists of:
   - Zero or more flags (one of the characters "-+ #0").
   - Optional minimum field width as decimal integer. Default is padding to the
     left, using spaces. Note that 0 is taken as a flag, not the beginning of a
     field width. Note also that a small field width will not result in the
     truncation of a value.
   - Optional precision (given as ".#" with # being a decimal integer),
     specifying:
     - the min. number of digits to appear (diouxX),
     - the max. number of digits after the decimal point (aAeEfF),
     - the max. number of significant digits (gG),
     - the max. number of bytes to be written (s).
     - behaviour with other conversion specifiers is undefined.
   - Optional length modifier specifying the size of the argument (one of "hh",
     "ll", or one of the characters "hljztL").
   - Conversion specifier character specifying the type of conversion to be
     applied (and the type of the next argument from the argument stack). One
     of the characters "diouxXfFeEgGaAcspn%".

   Minimum field width and/or precision may be given as asterisk ('*') instead
   of a decimal integer. In this case, the next argument from the argument
   stack is assumed to be an int value specifying the width / precision. A
   negative field width is interpreted as flag '-' followed by a positive field
   width. A negative precision is interpreted as if no precision was given.

   FLAGS
   -     Left-justify the conversion result within its field width.
   +     Prefix a '+' on positive signed conversion results. Prefix a '-' on
         floating conversions resulting in negative zero, or negative values
         rounding to zero.
   space Prefix a space on positive signed conversion results, or if a signed
         conversion results in no characters. If both '+' and ' ' are given,
         ' ' is ignored.
   #     Use an "alternative form" for
         - 'o' conversion, increasing precision until the first digit of the
           result is a zero;
         - 'x' or 'X' conversion, prefixing "0x" or "0X" to nonzero results;
         - "aAeEfF" conversions, always printing a decimal point even if no
           digits are following;
         - 'g' or 'G' conversions, always printing a decimal point even if no
           digits are following, and not removing trailing zeroes.
         - behaviour for other conversions is unspecified.
   0     Use leading zeroes instead of spaces for field width padding. If both
         '-' and '0' are given, '0' is ignored. If a precision is specified for
         any of the "diouxX" conversions, '0' is ignored. Behaviour is only
         defined for "diouxXaAeEfFgG".

   LENGTH MODIFIERS
   hh  For "diouxX" conversions, the argument from the argument stack is
       assumed to be of char width. (It will have been subject to integer
       promotion but will be converted back.) For 'n' conversions, the argument
       is assumed to be a pointer to signed char.
   h   For "diouxX" conversions, the argument from the argument stack is
       assumed to be of short int width. (It will have been subject to integer
       promotion but will be converted back.) For 'n' conversions, the argument
       is assumed to be a pointer to short int.
   l   For "diouxX" conversions, the argument from the argument stack is
       assumed to be of long int width. For 'n' conversions, the argument is
       assumed to be a pointer to short int. For 'c' conversions, the argument
       is assumed to be a wint_t. For 's' conversions, the argument is assumed
       to be a pointer to wchar_t. No effect on "aAeEfFgG" conversions.
   ll  For "diouxX" conversions, the argument from the argument stack is
       assumed to be of long long int width. For 'n' conversions, the argument
       is assumed to be a pointer to long long int.
   j   For "diouxX" conversions, the argument from the argument stack is
       assumed to be of intmax_t width. For 'n' conversions, the argument is
       assumed to be a pointer to intmax_t.
   z   For "diouxX" conversions, the argument from the argument stack is
       assumed to be of size_t width. For 'n' conversions, the argument is
       assumed to be a pointer to size_t.
   t   For "diouxX" conversions, the argument from the argument stack is
       assumed to be of ptrdiff_t width. For 'n' conversions, the argument is
       assumed to be a pointer to ptrdiff_t.
   L   For "aAeEfFgG" conversions, the argument from the argument stack is
       assumed to be a long double.
   Length modifiers appearing for any conversions not mentioned above will have
   undefined behaviour.
   If a length modifier appears with any conversion specifier other than as
   specified above, the behavior is undefined.

   CONVERSION SPECIFIERS
   d,i The argument from the argument stack is assumed to be of type int, and
       is converted to a signed decimal value with a minimum number of digits
       as specified by the precision (default 1), padded with leading zeroes.
       A zero value converted with precision zero yields no output.
   o   The argument from the argument stack is assumed to be of type unsigned
       int, and is converted to an unsigned octal value, other behaviour being
       as above.
   u   The argument from the argument stack is assumed to be of type unsigned
       int, and converted to an unsigned decimal value, other behaviour being
       as above.
   x,X The argument from the argument stack is assumed to be of type unsigned
       int, and converted to an unsigned hexadecimal value, using lowercase
       "abcdef" for 'x' and uppercase "ABCDEF" for 'X' conversion, other
       behaviour being as above.
   f,F The argument from the argument stack is assumed to be of type double,
       and converted to a decimal floating point in decimal-point notation,
       with the number of digits after the decimal point as specified by the
       precision (default 6) and the value being rounded appropriately. If
       precision is zero (and the '#' flag is not given), no decimal point is
       printed. At least one digit is always printed before the decimal point.
       For 'f' conversions, an infinity value is printed as either [-]inf or
       [-]infinity (, depending on the configuration of this implementation. A
       NaN value is printed as [-]nan. For 'F' conversions uppercase characters
       are used for these special values. The flags '-', '+' and ' ' apply as
       usual to these special values, '#' and '0' have no effect.
   e,E The argument from the argument stack is assumed to be of type double,
       and converted to a decimal floating point in normalized exponential
       notation ([?]d.ddd edd). "Normalized" means one nonzero digit before
       the decimal point, unless the value is zero. The number of digits after
       the decimal point is specified by the precision (default 6), the value
       being rounded appropriately. If precision is zero (and the '#' flag is
       not given), no decimal point is printed. The exponent has at least two
       digits, and not more than necessary to represent the exponent. If the
       value is zero, the exponent is zero. The 'e' written to indicate the
       exponend is uppercase for 'E' conversions.
       Infinity or NaN values are represented as for 'f' and 'F' conversions,
       respectively.
   g,G The argument from the argument stack is assumed to be of type double,
       and converted according to either 'f' or 'e' format for 'g' conversions,
       or 'F' or 'E' format for 'G' conversions, respectively, with the actual
       conversion chosen depending on the value. 'e' / 'E' conversion is chosen
       if the resulting exponent is < -4 or >= the precision (default 1).
       Trailing zeroes are removed (unless the '#' flag is given). A decimal
       point appears only if followed by a digit.
       Infinity or NaN values are represented as for 'f' and 'F' conversions,
       respectively.
   a,A The argument from the argument stack is assumed to be of type double,
       and converted to a floating point hexadecimal notation ([?]0xh.hhhh pd)
       with one hexadecimal digit (being nonzero if the value is normalized,
       and otherwise unspecified) before the decimal point, and the number of
       digits after the decimal point being specified by the precision. If no
       precision is given, the default is to print as many digits as nevessary
       to give an exact representation of the value (if FLT_RADIX is a power of
       2). If no precision is given and FLT_RADIX is not a power of 2, the
       default is to print as many digits to distinguish values of type double
       (possibly omitting trailing zeroes). (A precision p is sufficient to
       distinguish values of the source type if 16^p-1 > b^n where b is
       FLT_RADIX and n is the number of digits in the significand (to base b)
       of the source type. A smaller p might suffice depending on the
       implementation's scheme for determining the digit to the left of the
       decimal point.) The error has the correct sign for the current rounding
       direction.
       Unless the '#' flag is given, no decimal-point is given for zero
       precision.
       The 'a' conversion uses lowercase "abcdef", "0x" and 'p', the 'A'
       conversion uppercase "ABCDEF", "0X" and 'P'.
       The exponent always has at least one digit, and not more than necessary
       to represent the decimal exponent of 2. If the value is zero, the
       exponent is zero.
       Infinity or NaN values are represented as for 'f' and 'F' conversions,
       respectively.
       Binary implementations are at liberty to chose the hexadecimal digit to
       the left of the decimal point so that subsequent digits align to nibble
       boundaries.
   c   The argument from the argument stack is assumed to be of type int, and
       converted to a character after the value has been cast to unsigned char.
       If the 'l' length modifier is given, the argument is assumed to be of
       type wint_t, and converted as by a "%ls" conversion with no precision
       and a pointer to a two-element wchar_t array, with the first element
       being the wint_t argument and the second a '\0' wide character.
   s   The argument from the argument stack is assumed to be a char array (i.e.
       pointer to char). Characters from that array are printed until a zero
       byte is encountered or as many bytes as specified by a given precision
       have been written.
       If the l length modifier is given, the argument from the argument stack
       is assumed to be a wchar_t array (i.e. pointer to wchar_t). Wide
       characters from that array are converted to multibyte characters as by
       calls to wcrtomb() (using a mbstate_t object initialized to zero prior
       to the first conversion), up to and including the terminating null wide
       character. The resulting multibyte character sequence is then printed up
       to but not including the terminating null character. If a precision is
       given, it specifies the maximum number of bytes to be written (including
       shift sequences). If the given precision would require access to a wide
       character one past the end of the array, the array shall contain a '\0'
       wide character. In no case is a partial multibyte character written.
       Redundant shift sequences may result if the multibyte characters have a
       state-dependent encoding.
       TODO: Clarify these statements regarding %ls.
   p   The argument from the argument stack is assumed to be a void pointer,
       and converted to a sequence of printing characters in an implementation-
       defined manner.
       This implementation casts the pointer to type intptr_t, and prints the
       value as if a %#x conversion specifier was given.
   n   The argument from the argument stack is assumed to be a pointer to a
       signed integer, into which the number of characters written so far by
       this call to fprintf is stored. The behaviour, should any flags, field
       widths, or precisions be given is undefined.
   %   A verbatim '%' character is written. No argument is taken from the
       argument stack.

   Returns the number of characters written if successful, a negative value
   otherwise.
*/
int fprintf( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* TODO: fscanf() documentation */
/*
   Read input from a given stream, as defined by the given format string, and
   store converted input in the objects pointed to by 0..n subsequent arguments
   (the argument stack).

   The format string contains a sequence of directives that are expected to
   match the input. If such a directive fails to match, the function returns
   (matching error). It also returns if an input error occurs (input error).

   Directives can be:
   - one or more whitespaces, matching any number of whitespaces in the input;
   - printing characters, matching the input verbatim;
   - conversion specifications, which convert an input sequence into a value as
     defined by the individual specifier, and store that value in a memory
     location pointed to by the next pointer on the argument stack. Details are
     documented below. If there is an insufficient number of pointers on the
     argument stack, behaviour is undefined. Additional arguments not required
     by any conversion specifications are evaluated, but otherwise ignored.

   (The standard specifies the format string is allowed to contain multibyte
   character sequences as long as it starts and ends in initial shift state,
   but this is not yet supported by this implementation, which interprets the
   format string as sequence of char.)
   TODO: Add multibyte support to scanf() functions.

   A conversion specifier consists of:
   - Optional assignment-suppressing character ('*') that makes the conversion
     read input as usual, but does not assign the conversion result.
   - Optional maximum field width as decimal integer.
   - Optional length modifier specifying the size of the argument (one of "hh",
     "ll", or one of the characters "hljztL").
   - Conversion specifier character specifying the type of conversion to be
     applied (and the type of the next argument from the argument stack). One
     of the characters "diouxXaAeEfFgGcs[pn%".

   LENGTH MODIFIERS
   hh  For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of of char width.
   h   For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of short int width.
   l   For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of long int width.
       For "aAeEfFgG" conversions, it is assumed to point to a variable of type
       double.
       For "cs[" conversions, it is assumed to point to a variable of type
       wchar_t.
   ll  For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of long long int width.
   j   For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of intmax_t width.
   z   For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of size_t width.
   t   For "diouxXn" conversions, the next pointer from the argument stack is
       assumed to point to a variable of ptrdiff_t width.
   L   For "aAeEfFgG" conversions, the next pointer from the argument stack is
       assumed to point to a variable of type long double.
   Length modifiers appearing for any conversions not mentioned above will have
   undefined behaviour.
   If a length modifier appears with any conversion specifier other than as
   specified above, the behavior is undefined.

   CONVERSION SPECIFIERS
   d    Matches an (optionally signed) decimal integer of the format expected
        by strtol() with base 10. The next pointer from the argument stack is
        assumed to point to a signed integer.
   i    Matches an (optionally signed) integer of the format expected by
        strtol() with base 0. The next pointer from the argument stack is
        assumed to point to a signed integer.
   o    Matches an (optionally signed) octal integer of the format expected by
        strtoul() with base 8. The next pointer from the argument stack is
        assumed to point to an unsigned integer.
   u    Matches an (optionally signed) decimal integer of the format expected
        by strtoul() with base 10. The next pointer from the argument stack is
        assumed to point to an unsigned integer.
   x    Matches an (optionally signed) hexadecimal integer of the format
        expected by strtoul() with base 16. The next pointer from the argument
        stack is assumed to point to an unsigned integer.
   aefg Matches an (optionally signed) floating point number, infinity, or not-
        a-number-value of the format expected by strtod(). The next pointer
        from the argument stack is assumed to point to a float.
   c    Matches a number of characters as specified by the field width (default
        1). The next pointer from the argument stack is assumed to point to a
        character array large enough to hold that many characters.
        If the 'l' length modifier is given, the input is assumed to match a
        sequence of multibyte characters (starting in the initial shift state),
        which will be converted to a wide character sequence as by successive
        calls to mbrtowc() with a mbstate_t object initialized to zero prior to
        the first conversion. The next pointer from the argument stack is
        assumed to point to a wchar_t array large enough to hold that many
        characters.
        In either case, note that no '\0' character is added to terminate the
        sequence.
   s    Matches a sequence of non-white-space characters. The next pointer from
        the argument stack is assumed to point to a character array large
        enough to hold the sequence including terminating '\0' character.
        If the 'l' length modifier is given, the input is assumed to match a
        sequence of multibyte characters (starting in the initial shift state),
        which will be converted to a wide character sequence as by a call to
        mbrtowc() with a mbstate_t object initialized to zero prior to the
        first conversion. The next pointer from the argument stack is assumed
        to point to a wchar_t array large enough to hold the sequence including
        terminating '\0' character.
   [    Matches a nonempty sequence consisting of any of those characters
        specified between itself and a corresponding closing bracket (']').
        If the first character in the list is a circumflex ('^'), this matches
        a nonempty sequence consisting of any characters NOT specified. If the
        closing bracket appears as the first character in the scanset ("[]" or
        "[^]", it is assumed to belong to the scanset, which then ends with the
        NEXT closing bracket.
        If there is a '-' character in the scanset which is not the first after
        the opening bracket (or the circumflex, see above) or the last in the
        scanset, behaviour is implementation-defined. This implementation
        handles this character like any other.

        The extend of the input field is determined byte-by-byte for the above
        conversions ('c', 's', '['), with no special provisions being made for
        multibyte characters. The resulting field is nevertheless a multibyte
        sequence begining in intial shift state.

   p    Matches a sequence of characters as produced by the printf() "%p"
        conversion. The next pointer from the argument stack is assumed to
        point to a void pointer, which will be filled with the same location
        as the pointer used in the printf() statement. Note that behaviour is
        undefined if the input value is not the result of an earlier printf()
        call.
   n    Does not read input. The next pointer from the argument stack is
        assumed to point to a signed integer, into which the number of
        characters read from input so far by this call to fscanf() is stored.
        This does not affect the return value of fscanf(). The behaviour,
        should an assignment-supressing character of field width be given,
        is undefined.
        This can be used to test the success of literal matches and suppressed
        assignments.
   %    Matches a single, verbatim '%' character.

   A, E, F, G and X are valid, and equivalent to their lowercase counterparts.

   All conversions except [, c, or n imply that whitespace characters from the
   input stream are consumed until a non-whitespace character is encountered.
   Such whitespaces do not count against a maximum field width.

   Conversions push at most one character back into the input stream. That
   implies that some character sequences converted by the strtol() and strtod()
   function families are not converted identically by the scnaf() function
   family.

   Returns the number of input items successfully assigned. This can be zero if
   an early mismatch occurs. Returns EOF if an input failure occurs before the
   first conversion.
*/
int fscanf( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* Equivalent to fprintf( stdout, format, ... ). */
int printf( const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* Equivalent to fscanf( stdin, format, ... ). */
int scanf( const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* Equivalent to fprintf( stdout, format, ... ), except that the result is
   written into the buffer pointed to by s, instead of stdout, and that any
   characters beyond the (n-1)th are discarded. The (n)th character is
   replaced by a '\0' character in this case.
   Returns the number of characters that would have been written (not counting
   the terminating '\0' character) if n had been sufficiently large, if
   successful, and a negative number if an encoding error ocurred.
*/
int snprintf( char * _PDCLIB_restrict s, size_t n, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* Equivalent to fprintf( stdout, format, ... ), except that the result is
   written into the buffer pointed to by s, instead of stdout.
*/
int sprintf( char * _PDCLIB_restrict s, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* Equivalent to fscanf( stdin, format, ... ), except that the input is read
   from the buffer pointed to by s, instead of stdin.
*/
int sscanf( const char * _PDCLIB_restrict s, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;

/* Equivalent to fprintf( stream, format, ... ), except that the argument stack
   is passed as va_list parameter. Note that va_list is not declared by
   <stdio.h>.
*/
int vfprintf( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Equivalent to fscanf( stream, format, ... ), except that the argument stack
   is passed as va_list parameter. Note that va_list is not declared by
   <stdio.h>.
*/
int vfscanf( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Equivalent to fprintf( stdout, format, ... ), except that the argument stack
   is passed as va_list parameter. Note that va_list is not declared by
   <stdio.h>.
*/
int vprintf( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Equivalent to fscanf( stdin, format, ... ), except that the argument stack
   is passed as va_list parameter. Note that va_list is not declared by
   <stdio.h>.
*/
int vscanf( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Equivalent to snprintf( s, n, format, ... ), except that the argument stack
   is passed as va_list parameter. Note that va_list is not declared by
   <stdio.h>.
   */
int vsnprintf( char * _PDCLIB_restrict s, size_t n, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Equivalent to fprintf( stdout, format, ... ), except that the argument stack
   is passed as va_list parameter, and the result is written to the buffer
   pointed to by s, instead of stdout. Note that va_list is not declared by
   <stdio.h>.
*/
int vsprintf( char * _PDCLIB_restrict s, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Equivalent to fscanf( stdin, format, ... ), except that the argument stack
   is passed as va_list parameter, and the input is read from the buffer
   pointed to by s, instead of stdin. Note that va_list is not declared by
   <stdio.h>.
*/
int vsscanf( const char * _PDCLIB_restrict s, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

/* Character input/output functions */

/* Retrieve the next character from given stream.
   Returns the character, EOF otherwise.
   If end-of-file is reached, the EOF indicator of the stream is set.
   If a read error occurs, the error indicator of the stream is set.
*/
int fgetc( FILE * stream ) _PDCLIB_nothrow;

/* Read at most n-1 characters from given stream into the array s, stopping at
   \n or EOF. Terminate the read string with \n. If EOF is encountered before
   any characters are read, leave the contents of s unchanged.
   Returns s if successful, NULL otherwise.
   If a read error occurs, the error indicator of the stream is set. In this
   case, the contents of s are indeterminate.
*/
char * fgets( char * _PDCLIB_restrict s, int n, FILE * _PDCLIB_restrict stream ) _PDCLIB_nothrow;

/* Write the value c (cast to unsigned char) to the given stream.
   Returns c if successful, EOF otherwise.
   If a write error occurs, sets the error indicator of the stream is set.
*/
int fputc( int c, FILE * stream ) _PDCLIB_nothrow;

/* Write the string s (not including the terminating \0) to the given stream.
   Returns a value >=0 if successful, EOF otherwise.
   This implementation does set the error indicator of the stream if a write
   error occurs.
*/
int fputs( const char * _PDCLIB_restrict s, FILE * _PDCLIB_restrict stream ) _PDCLIB_nothrow;

/* Equivalent to fgetc( stream ), but may be overloaded by a macro that
   evaluates its parameter more than once.
*/
int getc( FILE * stream ) _PDCLIB_nothrow;

/* Equivalent to fgetc( stdin ). */
int getchar( void ) _PDCLIB_nothrow;

#if _PDCLIB_C_MAX(1999)
/* Read characters from given stream into the array s, stopping at \n or EOF.
   The string read is terminated with \0. Returns s if successful. If EOF is
   encountered before any characters are read, the contents of s are unchanged,
   and NULL is returned. If a read error occurs, the contents of s are indeter-
   minate, and NULL is returned.

   This function is dangerous and has been a great source of security
   vulnerabilities. Do not use it. It was removed by C11.
*/
char * gets( char * s ) _PDCLIB_DEPRECATED _PDCLIB_nothrow;
#endif

/* Equivalent to fputc( c, stream ), but may be overloaded by a macro that
   evaluates its parameter more than once.
*/
int putc( int c, FILE * stream ) _PDCLIB_nothrow;

/* Equivalent to fputc( c, stdout ), but may be overloaded by a macro that
   evaluates its parameter more than once.
*/
int putchar( int c ) _PDCLIB_nothrow;

/* Write the string s (not including the terminating \0) to stdout, and append
   a newline to the output. Returns a value >= 0 when successful, EOF if a
   write error occurred.
*/
int puts( const char * s ) _PDCLIB_nothrow;

/* Push the value c (cast to unsigned char) back onto the given (input) stream.
   A character pushed back in this way will be delivered by subsequent read
   operations (and skipped by subsequent file positioning operations) as if it
   has not been read. The external representation of the stream is unaffected
   by this pushback (it is a buffer operation). One character of pushback is
   guaranteed, further pushbacks may fail. EOF as value for c does not change
   the input stream and results in failure of the function.
   For text files, the file position indicator is indeterminate until all
   pushed-back characters are read. For binary files, the file position
   indicator is decremented by each successful call of ungetc(). If the file
   position indicator for a binary file was zero before the call of ungetc(),
   behaviour is undefined. (Older versions of the library allowed such a call.)
   Returns the pushed-back character if successful, EOF if it fails.
*/
int ungetc( int c, FILE * stream ) _PDCLIB_nothrow;

/* Direct input/output functions */

/* Read up to nmemb elements of given size from given stream into the buffer
   pointed to by ptr. Returns the number of elements successfully read, which
   may be less than nmemb if a read error or EOF is encountered. If a read
   error is encountered, the value of the file position indicator is
   indeterminate. If a partial element is read, its value is indeterminate.
   If size or nmemb are zero, the function does nothing and returns zero.
*/
size_t fread( void * _PDCLIB_restrict ptr, size_t size, size_t nmemb, FILE * _PDCLIB_restrict stream ) _PDCLIB_nothrow;

/* Write up to nmemb elements of given size from buffer pointed to by ptr to
   the given stream. Returns the number of elements successfully written, which
   will be less than nmemb only if a write error is encountered. If a write
   error is encountered, the value of the file position indicator is
   indeterminate. If size or nmemb are zero, the function does nothing and
   returns zero.
*/
size_t fwrite( const void * _PDCLIB_restrict ptr, size_t size, size_t nmemb, FILE * _PDCLIB_restrict stream ) _PDCLIB_nothrow;

/* File positioning functions */

/* Store the current position indicator (and, where appropriate, the current
   mbstate_t status object) for the given stream into the given pos object. The
   actual contents of the object are unspecified, but it can be used as second
   parameter to fsetpos() to reposition the stream to the exact position and
   parse state at the time fgetpos() was called.
   Returns zero if successful, nonzero otherwise.
   TODO: Implementation-defined errno setting for fgetpos().
*/
int fgetpos( FILE * _PDCLIB_restrict stream, fpos_t * _PDCLIB_restrict pos ) _PDCLIB_nothrow;

/* Set the position indicator for the given stream to the given offset from:
   - the beginning of the file if whence is SEEK_SET,
   - the current value of the position indicator if whence is SEEK_CUR,
   - end-of-file if whence is SEEK_END.
   On text streams, non-zero offsets are only allowed with SEEK_SET, and must
   have been returned by ftell() for the same file.
   Any characters buffered by ungetc() are dropped, the end-of-file indicator
   for the stream is cleared. If the given stream is an update stream, the next
   operation after a successful fseek() may be either input or output.
   Returns zero if successful, nonzero otherwise. If a read/write error occurs,
   the error indicator for the given stream is set.
*/
int fseek( FILE * stream, long int offset, int whence ) _PDCLIB_nothrow;

/* Set the position indicator (and, where appropriate the mbstate_t status
   object) for the given stream to the given pos object (created by an earlier
   call to fgetpos() on the same file).
   Any characters buffered by ungetc() are dropped, the end-of-file indicator
   for the stream is cleared. If the given stream is an update stream, the next
   operation after a successful fsetpos() may be either input or output.
   Returns zero if successful, nonzero otherwise. If a read/write error occurs,
   the error indicator for the given stream is set.
   TODO: Implementation-defined errno setting for fsetpos().
*/
int fsetpos( FILE * stream, const fpos_t * pos ) _PDCLIB_nothrow;

/* Return the current offset of the given stream from the beginning of the
   associated file. For text streams, the exact value returned is unspecified
   (and may not be equal to the number of characters), but may be used in
   subsequent calls to fseek().
   Returns -1L if unsuccessful.
   TODO: Implementation-defined errno setting for ftell().
*/
long int ftell( FILE * stream ) _PDCLIB_nothrow;

/* Equivalent to (void)fseek( stream, 0L, SEEK_SET ), except that the error
   indicator for the stream is also cleared.
*/
void rewind( FILE * stream ) _PDCLIB_nothrow;

/* Error-handling functions */

/* Clear the end-of-file and error indicators for the given stream. */
void clearerr( FILE * stream ) _PDCLIB_nothrow;

/* Return zero if the end-of-file indicator for the given stream is not set,
   nonzero otherwise.
*/
int feof( FILE * stream ) _PDCLIB_nothrow;

/* Return zero if the error indicator for the given stream is not set, nonzero
   otherwise.
*/
int ferror( FILE * stream ) _PDCLIB_nothrow;

/* If s is neither a NULL pointer nor an empty string, print the string to
   stderr (with appended colon (':') and a space) first. In any case, print an
   error message depending on the current value of errno (being the same as if
   strerror( errno ) had been called).
*/
void perror( const char * s ) _PDCLIB_nothrow;

/* Unlocked I/O
 *
 * Since threading was introduced in C11, FILE objects have had implicit locks
 * to prevent data races and inconsistent output.
 *
 * PDCLib provides these functions from POSIX as an extension in order to enable
 * users to access the underlying unlocked functions.
 *
 * For each function defined in C11 where an _unlocked variant is defined below,
 * the behaviour of the _unlocked variant is the same except that it will not
 * take the lock associated with the stream.
 *
 * flockfile, ftrylockfile and funlockfile can be used to manually manipulate
 * the stream locks. The behaviour of the _unlocked functions if called when the
 * stream isn't locked by the calling thread is implementation defined.
 */
#if _PDCLIB_POSIX_MIN(200112L) || _PDCLIB_BSD_SOURCE || _PDCLIB_SVID_SOURCE
void flockfile(FILE *file) _PDCLIB_nothrow;
int ftrylockfile(FILE *file) _PDCLIB_nothrow;
void funlockfile(FILE *file) _PDCLIB_nothrow;

int getc_unlocked(FILE *stream) _PDCLIB_nothrow;
int getchar_unlocked(void) _PDCLIB_nothrow;
int putc_unlocked(int c, FILE *stream) _PDCLIB_nothrow;
int putchar_unlocked(int c) _PDCLIB_nothrow;
#endif

#if _PDCLIB_BSD_SOURCE || _PDCLIB_SVID_SOURCE
void clearerr_unlocked(FILE *stream) _PDCLIB_nothrow;
int feof_unlocked(FILE *stream) _PDCLIB_nothrow;
int ferror_unlocked(FILE *stream) _PDCLIB_nothrow;
int fflush_unlocked(FILE *stream) _PDCLIB_nothrow;
int fgetc_unlocked(FILE *stream) _PDCLIB_nothrow;
int fputc_unlocked(int c, FILE *stream) _PDCLIB_nothrow;
size_t fread_unlocked(void *ptr, size_t size, size_t n, FILE *stream) _PDCLIB_nothrow;
size_t fwrite_unlocked(const void *ptr, size_t size, size_t n, FILE *stream) _PDCLIB_nothrow;
#endif

#if _PDCLIB_GNU_SOURCE
char *fgets_unlocked(char *s, int n, FILE *stream) _PDCLIB_nothrow;
int fputs_unlocked(const char *s, FILE *stream) _PDCLIB_nothrow;
#endif

#if _PDCLIB_EXTENSIONS
int _vcbprintf(
    void *p,
    _PDCLIB_size_t ( *cb ) ( void *p, const char *buf, _PDCLIB_size_t size ),
    const char *format,
    _PDCLIB_va_list arg );

int _cbprintf(
    void *p,
    size_t ( *cb ) ( void *p, const char *buf, size_t size ),
    const char *format,
    ... );

int fgetpos_unlocked( FILE * _PDCLIB_restrict stream, fpos_t * _PDCLIB_restrict pos ) _PDCLIB_nothrow;
int fsetpos_unlocked( FILE * stream, const fpos_t * pos ) _PDCLIB_nothrow;
long int ftell_unlocked( FILE * stream ) _PDCLIB_nothrow;
int fseek_unlocked( FILE * stream, long int offset, int whence ) _PDCLIB_nothrow;
void rewind_unlocked( FILE * stream ) _PDCLIB_nothrow;

int puts_unlocked( const char * s ) _PDCLIB_nothrow;
int ungetc_unlocked( int c, FILE * stream ) _PDCLIB_nothrow;

int printf_unlocked( const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int vprintf_unlocked( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;
int fprintf_unlocked( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int vfprintf_unlocked( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;
int scanf_unlocked( const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int vscanf_unlocked( const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;
int fscanf_unlocked( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, ... ) _PDCLIB_nothrow;
int vfscanf_unlocked( FILE * _PDCLIB_restrict stream, const char * _PDCLIB_restrict format, _PDCLIB_va_list arg ) _PDCLIB_nothrow;

// Todo: remove prefix?
_PDCLIB_uint_fast64_t _PDCLIB_ftell64( FILE * stream ) _PDCLIB_nothrow;
_PDCLIB_uint_fast64_t _PDCLIB_ftell64_unlocked( FILE * stream ) _PDCLIB_nothrow;
#endif

#ifdef __cplusplus
}
#endif

#endif
