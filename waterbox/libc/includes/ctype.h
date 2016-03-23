/* Character handling <ctype.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_CTYPE_H
#define _PDCLIB_CTYPE_H _PDCLIB_CTYPE_H
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Character classification functions */

/* Note that there is a difference between "whitespace" (any printing, non-
   graph character, like horizontal and vertical tab), and "blank" (the literal
   ' ' space character).

   There will be masking macros for each of these later on, but right now I
   focus on the functions only.
*/

/* Returns isalpha( c ) || isdigit( c ) */
int isalnum( int c ) _PDCLIB_nothrow;

/* Returns isupper( c ) || islower( c ) in the "C" locale.
   In any other locale, also returns true for a locale-specific set of
   alphabetic characters which are neither control characters, digits,
   punctation, or whitespace.
*/
int isalpha( int c ) _PDCLIB_nothrow;

/* Returns true if the character isspace() and used for seperating words within
   a line of text. In the "C" locale, only ' ' and '\t' are considered blanks.
*/
int isblank( int c ) _PDCLIB_nothrow;

/* Returns true if the character is a control character. */
int iscntrl( int c ) _PDCLIB_nothrow;

/* Returns true if the character is a decimal digit. Locale-independent. */
int isdigit( int c ) _PDCLIB_nothrow;

/* Returns true for every printing character except space (' '). */
int isgraph( int c ) _PDCLIB_nothrow;

/* Returns true for lowercase letters in the "C" locale.
   In any other locale, also returns true for a locale-specific set of
   characters which are neither control characters, digits, punctation, or
   space (' '). In a locale other than the "C" locale, a character might test
   true for both islower() and isupper().
*/
int islower( int c ) _PDCLIB_nothrow;

/* Returns true for every printing character including space (' '). */
int isprint( int c ) _PDCLIB_nothrow;

/* Returns true for every printing character that is neither whitespace
   nor alphanumeric in the "C" locale. In any other locale, there might be
   characters that are printing characters, but neither whitespace nor
   alphanumeric.
*/
int ispunct( int c ) _PDCLIB_nothrow;

/* Returns true for every standard whitespace character (' ', '\f', '\n', '\r',
   '\t', '\v') in the "C" locale. In any other locale, also returns true for a
   locale-specific set of characters for which isalnum() is false.
*/ 
int isspace( int c ) _PDCLIB_nothrow;

/* Returns true for uppercase letters in the "C" locale.
   In any other locale, also returns true for a locale-specific set of
   characters which are neither control characters, digits, punctation, or
   space (' '). In a locale other than the "C" locale, a character might test
   true for both islower() and isupper().
*/
int isupper( int c ) _PDCLIB_nothrow;

/* Returns true for any hexadecimal-digit character. Locale-independent. */
int isxdigit( int c ) _PDCLIB_nothrow;

/* Character case mapping functions */

/* Converts an uppercase letter to a corresponding lowercase letter. Input that
   is not an uppercase letter remains unchanged.
*/
int tolower( int c ) _PDCLIB_nothrow;

/* Converts a lowercase letter to a corresponding uppercase letter. Input that
   is not a lowercase letter remains unchanged.
*/
int toupper( int c ) _PDCLIB_nothrow;

#ifdef __cplusplus
}
#endif

#endif
