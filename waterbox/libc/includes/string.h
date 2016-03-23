/* String handling <string.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_STRING_H
#define _PDCLIB_STRING_H _PDCLIB_STRING_H
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

/* String function conventions */

/*
   In any of the following functions taking a size_t n to specify the length of
   an array or size of a memory region, n may be 0, but the pointer arguments to
   the call shall still be valid unless otherwise stated.
*/

/* Copying functions */

/* Copy a number of n characters from the memory area pointed to by s2 to the
   area pointed to by s1. If the two areas overlap, behaviour is undefined.
   Returns the value of s1.
*/
void * memcpy( void * _PDCLIB_restrict s1, const void * _PDCLIB_restrict s2, size_t n ) _PDCLIB_nothrow;

/* Copy a number of n characters from the memory area pointed to by s2 to the
   area pointed to by s1. The two areas may overlap.
   Returns the value of s1.
*/
void * memmove( void * s1, const void * , size_t n ) _PDCLIB_nothrow;

/* Copy the character array s2 (including terminating '\0' byte) into the
   character array s1.
   Returns the value of s1.
*/
char * strcpy( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2 ) _PDCLIB_nothrow;

/* Copy a maximum of n characters from the character array s2 into the character
   array s1. If s2 is shorter than n characters, '\0' bytes will be appended to
   the copy in s1 until n characters have been written. If s2 is longer than n
   characters, NO terminating '\0' will be written to s1. If the arrays overlap,
   behaviour is undefined.
   Returns the value of s1.
*/
char * strncpy( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2, size_t n ) _PDCLIB_nothrow;

/* Concatenation functions */

/* Append the contents of the character array s2 (including terminating '\0') to
   the character array s1 (first character of s2 overwriting the '\0' of s1). If
   the arrays overlap, behaviour is undefined.
   Returns the value of s1.
*/
char * strcat( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2 ) _PDCLIB_nothrow;

/* Append a maximum of n characters from the character array s1 to the character
   array s1 (first character of s2 overwriting the '\0' of s1). A terminating
   '\0' is ALWAYS appended, even if the full n characters have already been
   written. If the arrays overlap, behaviour is undefined.
   Returns the value of s1.
*/
char * strncat( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2, size_t n ) _PDCLIB_nothrow;

/* Comparison functions */

/* Compare the first n characters of the memory areas pointed to by s1 and s2.
   Returns 0 if s1 == s2, a negative number if s1 < s2, and a positive number if
   s1 > s2.
*/
int memcmp( const void * s1, const void * s2, size_t n ) _PDCLIB_nothrow;

/* Compare the character arrays s1 and s2.
   Returns 0 if s1 == s2, a negative number if s1 < s2, and a positive number if
   s1 > s2.
*/
int strcmp( const char * s1, const char * s2 ) _PDCLIB_nothrow;

/* Compare the character arrays s1 and s2, interpreted as specified by the
   LC_COLLATE category of the current locale.
   Returns 0 if s1 == s2, a negative number if s1 < s2, and a positive number if
   s1 > s2.
   TODO: Currently a dummy wrapper for strcmp() as PDCLib does not yet support
   locales.
*/
int strcoll( const char * s1, const char * s2 ) _PDCLIB_nothrow;

/* Compare no more than the first n characters of the character arrays s1 and
   s2.
   Returns 0 if s1 == s2, a negative number if s1 < s2, and a positive number if
   s1 > s2.
*/
int strncmp( const char * s1, const char * s2, size_t n ) _PDCLIB_nothrow;

/* Transform the character array s2 as appropriate for the LC_COLLATE setting of
   the current locale. If length of resulting string is less than n, store it in
   the character array pointed to by s1. Return the length of the resulting
   string.
*/
size_t strxfrm( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2, size_t n ) _PDCLIB_nothrow;

/* Search functions */

/* Search the first n characters in the memory area pointed to by s for the
   character c (interpreted as unsigned char).
   Returns a pointer to the first instance found, or NULL.
*/
void * memchr( const void * s, int c, size_t n ) _PDCLIB_nothrow;

/* Search the character array s (including terminating '\0') for the character c
   (interpreted as char).
   Returns a pointer to the first instance found, or NULL.
*/
char * strchr( const char * s, int c ) _PDCLIB_nothrow;

/* Determine the length of the initial substring of character array s1 which
   consists only of characters not from the character array s2.
   Returns the length of that substring.
*/
size_t strcspn( const char * s1, const char * s2 ) _PDCLIB_nothrow;

/* Search the character array s1 for any character from the character array s2.
   Returns a pointer to the first occurrence, or NULL.
*/
char * strpbrk( const char * s1, const char * s2 ) _PDCLIB_nothrow;

/* Search the character array s (including terminating '\0') for the character c
   (interpreted as char).
   Returns a pointer to the last instance found, or NULL.
*/
char * strrchr( const char * s, int c ) _PDCLIB_nothrow;

/* Determine the length of the initial substring of character array s1 which
   consists only of characters from the character array s2.
   Returns the length of that substring.
*/
size_t strspn( const char * s1, const char * s2 ) _PDCLIB_nothrow;

/* Search the character array s1 for the substring in character array s2.
   Returns a pointer to that sbstring, or NULL. If s2 is of length zero,
   returns s1.
*/
char * strstr( const char * s1, const char * s2 ) _PDCLIB_nothrow;

/* In a series of subsequent calls, parse a C string into tokens.
   On the first call to strtok(), the first argument is a pointer to the to-be-
   parsed C string. On subsequent calls, the first argument is NULL unless you
   want to start parsing a new string. s2 holds an array of seperator characters
   which can differ from call to call. Leading seperators are skipped, the first
   trailing seperator overwritten with '\0'.
   Returns a pointer to the next token.
   WARNING: This function uses static storage, and as such is not reentrant.
*/
char * strtok( char * _PDCLIB_restrict s1, const char * _PDCLIB_restrict s2 ) _PDCLIB_nothrow;

/* Miscellaneous functions */

/* Write the character c (interpreted as unsigned char) to the first n
   characters of the memory area pointed to by s.
   Returns s.
*/
void * memset( void * s, int c, size_t n ) _PDCLIB_nothrow;

/* Map an error number to a (locale-specific) error message string. Error
   numbers are typically errno values, but any number is mapped to a message.
   TODO: PDCLib does not yet support locales.
*/
char * strerror( int errnum ) _PDCLIB_nothrow;

/* Returns the length of the string s (excluding terminating '\0').
*/
size_t strlen( const char * s ) _PDCLIB_nothrow;

#if _PDCLIB_POSIX_MIN(2008098L)
/* Returns the length of the string s (excluding terminating '\0') or maxlen if
 * no terminating '\0' is found in the first maxlen characters.
 */
size_t strnlen( const char * s, size_t maxlen ) _PDCLIB_nothrow;
#endif

#if _PDCLIB_POSIX_MIN(2008098L) || _PDCLIB_XOPEN_MIN(0)
char * strdup( const char* src ) _PDCLIB_nothrow;
char * strndup( const char* src, size_t n ) _PDCLIB_nothrow;
#endif

#if _PDCLIB_BSD_SOURCE
size_t strlcpy(
   char *_PDCLIB_restrict _Dst,
   const char *_PDCLIB_restrict _Src,
   size_t _DstSize) _PDCLIB_nothrow;

size_t strlcat(
   char *_PDCLIB_restrict _Dst,
   const char *_PDCLIB_restrict _Src,
   size_t _DstSize) _PDCLIB_nothrow;
#endif

#ifdef __cplusplus
}
#endif

#endif
