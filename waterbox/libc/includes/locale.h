/* Localization <locale.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_LOCALE_H
#define _PDCLIB_LOCALE_H _PDCLIB_LOCALE_H
#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifndef _PDCLIB_NULL_DEFINED
#define _PDCLIB_NULL_DEFINED _PDCLIB_NULL_DEFINED
#define NULL _PDCLIB_NULL
#endif

/* The structure returned by localeconv().

   The values for *_sep_by_space:
   0 - no space
   1 - if symbol and sign are adjacent, a space seperates them from the value;
       otherwise a space seperates the symbol from the value
   2 - if symbol and sign are adjacent, a space seperates them; otherwise a
       space seperates the sign from the value

   The values for *_sign_posn:
   0 - Parentheses surround value and symbol
   1 - sign precedes value and symbol
   2 - sign succeeds value and symbol
   3 - sign immediately precedes symbol
   4 - sign immediately succeeds symbol
*/
struct lconv
{
    char * decimal_point;      /* decimal point character                     */
    char * thousands_sep;      /* character for seperating groups of digits   */
    char * grouping;           /* string indicating the size of digit groups  */
    char * mon_decimal_point;  /* decimal point for monetary quantities       */
    char * mon_thousands_sep;  /* thousands_sep for monetary quantities       */
    char * mon_grouping;       /* grouping for monetary quantities            */
    char * positive_sign;      /* string indicating nonnegative mty. qty.     */
    char * negative_sign;      /* string indicating negative mty. qty.        */
    char * currency_symbol;    /* local currency symbol (e.g. '$')            */
    char * int_curr_symbol;    /* international currency symbol (e.g. "USD"   */
    char frac_digits;          /* fractional digits in local monetary qty.    */
    char p_cs_precedes;        /* if currency_symbol precedes positive qty.   */
    char n_cs_precedes;        /* if currency_symbol precedes negative qty.   */
    char p_sep_by_space;       /* if it is seperated by space from pos. qty.  */
    char n_sep_by_space;       /* if it is seperated by space from neg. qty.  */
    char p_sign_posn;          /* positioning of positive_sign for mon. qty.  */
    char n_sign_posn;          /* positioning of negative_sign for mon. qty.  */
    char int_frac_digits;      /* Same as above, for international format     */
    char int_p_cs_precedes;    /* Same as above, for international format     */
    char int_n_cs_precedes;    /* Same as above, for international format     */
    char int_p_sep_by_space;   /* Same as above, for international format     */
    char int_n_sep_by_space;   /* Same as above, for international format     */
    char int_p_sign_posn;      /* Same as above, for international format     */
    char int_n_sign_posn;      /* Same as above, for international format     */
};

/* First arguments to setlocale().
   TODO: Beware, values might change before v0.6 is released.
*/
/* Entire locale */
#define LC_ALL      -1
/* Collation (strcoll(), strxfrm()) */
#define LC_COLLATE  0
/* Character types (<ctype.h>) */
#define LC_CTYPE    1
/* Monetary formatting (as returned by localeconv) */
#define LC_MONETARY 2
/* Decimal-point character (for printf() / scanf() functions), string
   conversions, nonmonetary formatting as returned by localeconv              */
#define LC_NUMERIC  3
/* Time formats (strftime(), wcsftime()) */
#define LC_TIME     4

/* not supported! */
#define LC_MESSAGES 5

/* The category parameter can be any of the LC_* macros to specify if the call
   to setlocale() shall affect the entire locale or only a portion thereof.
   The category locale specifies which locale should be switched to, with "C"
   being the minimal default locale, and "" being the locale-specific native
   environment. A NULL pointer makes setlocale() return the *current* setting.
   Otherwise, returns a pointer to a string associated with the specified
   category for the new locale.
*/
char * setlocale( int category, const char * locale ) _PDCLIB_nothrow;

/* Returns a struct lconv initialized to the values appropriate for the current
   locale setting.
*/
struct lconv * localeconv( void ) _PDCLIB_nothrow;

#if _PDCLIB_POSIX_MIN(2008)
#define LC_COLLATE_MASK  (1 << LC_COLLATE)
#define LC_CTYPE_MASK    (1 << LC_CTYPE)
#define LC_MONETARY_MASK (1 << LC_MONETARY)
#define LC_NUMERIC_MASK  (1 << LC_NUMERIC)
#define LC_TIME_MASK     (1 << LC_TIME)
#define LC_MESSAGES_MASK (1 << LC_MESSAGES)
#define LC_ALL_MASK      (LC_COLLATE_MASK | LC_CTYPE_MASK | LC_MONETARY_MASK | \
                          LC_NUMERIC_MASK | LC_TIME_MASK | LC_MESSAGES_MASK)


/* POSIX locale type */
typedef _PDCLIB_locale_t locale_t;

/* Global locale */
extern struct _PDCLIB_locale _PDCLIB_global_locale;
#define LC_GLOBAL_LOCALE (&_PDCLIB_global_locale)

#ifdef _PDCLIB_LOCALE_METHOD

locale_t newlocale(int category_mask, const char *locale, locale_t base);

/* Set the thread locale to newlocale
 *
 * If newlocale is (locale_t)0, then doesn't change the locale and just returns
 * the existing locale.
 *
 * If newlocale is LC_GLOBAL_LOCALE, resets the thread's locale to use the
 * global locale.
 *
 * Returns the previous thread locale. If the thread had no previous locale,
 * returns the global locale.
 */
locale_t uselocale( locale_t newlocale );

/* Returns a copy of loc */
locale_t duplocale( locale_t loc );

/* Frees the passed locale object */
void freelocale( locale_t loc );
#endif

#endif

#ifdef __cplusplus
}
#endif

#endif
