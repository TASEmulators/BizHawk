/*
 *    Stack-less Just-In-Time compiler
 *
 *    Copyright Zoltan Herczeg (hzmester@freemail.hu). All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 *
 *   1. Redistributions of source code must retain the above copyright notice, this list of
 *      conditions and the following disclaimer.
 *
 *   2. Redistributions in binary form must reproduce the above copyright notice, this list
 *      of conditions and the following disclaimer in the documentation and/or other materials
 *      provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER(S) AND CONTRIBUTORS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
 * SHALL THE COPYRIGHT HOLDER(S) OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#ifndef _REGEX_JIT_H_
#define _REGEX_JIT_H_

#ifdef __cplusplus
extern "C" {
#endif

/* Character type config. */
#define REGEX_USE_8BIT_CHARS

#ifdef REGEX_USE_8BIT_CHARS
typedef char regex_char_t;
#else
typedef wchar_t regex_char_t;
#endif

/* Error codes. */
#define REGEX_NO_ERROR		0
#define REGEX_MEMORY_ERROR	1
#define REGEX_INVALID_REGEX	2

/* Note: large, nested {a,b} iterations can blow up the memory consumption
   a{n,m} is replaced by aa...aaa?a?a?a?a? (n >= 0, m > 0)
                         \__n__/\____m___/
   a{n,}  is replaced by aa...aaa+ (n > 0)
                         \_n-1_/
*/

/* The value returned by regex_compile. Can be used for multiple matching. */
struct regex_machine;

/* A matching state. */
struct regex_match;

/* Note: REGEX_MATCH_BEGIN and REGEX_MATCH_END does not change the parsing
     (Hence ^ and $ are parsed normally).
   Force matching to start from begining of the string (same as ^). */
#define REGEX_MATCH_BEGIN	0x01
/* Force matching to continue until the last character (same as $). */
#define REGEX_MATCH_END		0x02
/* Changes . to [^\r\n]
     Note: [...] and [^...] are NOT affected at all (as other regex engines do). */
#define REGEX_NEWLINE		0x04
/* Non greedy matching. In case of Thompson (non-recursive) algorithm,
   it (usually) does not have a significant speed gain. */
#define REGEX_MATCH_NON_GREEDY	0x08
/* Verbose. This define can be commented out, which disables all verbose features. */
#define REGEX_MATCH_VERBOSE	0x10

/* If error occures the function returns NULL, and the error code returned in error variable.
   You can pass NULL to error if you don't care about the error code.
   The re_flags argument contains the default REGEX_MATCH flags. See above. */
struct regex_machine* regex_compile(const regex_char_t *regex_string, int length, int re_flags, int *error);
void regex_free_machine(struct regex_machine *machine);

/* Create and init match structure for a given machine. */
struct regex_match* regex_begin_match(struct regex_machine *machine);
void regex_reset_match(struct regex_match *match);
void regex_free_match(struct regex_match *match);

/* Pattern matching.
   regex_continue_match does not support REGEX_MATCH_VERBOSE flag. */
void regex_continue_match(struct regex_match *match, const regex_char_t *input_string, int length);
int regex_get_result(struct regex_match *match, int *end, int *id);
/* Returns true, if the best match has already found. */
int regex_is_match_finished(struct regex_match *match);

/* Only exists if VERBOSE is defined in regexJIT.c
   Do both sanity check and verbose.
   (The latter only if REGEX_MATCH_VERBOSE was passed to regex_compile) */
void regex_continue_match_debug(struct regex_match *match, const regex_char_t *input_string, int length);

/* Misc. */
const char* regex_get_platform_name(void);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif
