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

/* Must be the first one. Must not depend on any other include. */
#include "sljitLir.h"
#include "regexJIT.h"

#include <stdio.h>

#if defined _WIN32 || defined _WIN64
#define COLOR_RED
#define COLOR_GREEN
#define COLOR_ARCH
#define COLOR_DEFAULT
#else
#define COLOR_RED "\33[31m"
#define COLOR_GREEN "\33[32m"
#define COLOR_ARCH "\33[33m"
#define COLOR_DEFAULT "\33[0m"
#endif

#ifdef REGEX_USE_8BIT_CHARS
#define S(str)	str
#else
#define S(str)	L##str
#endif

#ifdef REGEX_MATCH_VERBOSE
void verbose_test(regex_char_t *pattern, regex_char_t *string)
{
	int error;
	regex_char_t *ptr;
	struct regex_machine* machine;
	struct regex_match* match;
	int begin, end, id;

	ptr = pattern;
	while (*ptr)
		ptr++;

	printf("Start test '%s' matches to '%s'\n", pattern, string);
	machine = regex_compile(pattern, (int)(ptr - pattern), REGEX_MATCH_VERBOSE | REGEX_NEWLINE, &error);

	if (error) {
		printf("WARNING: Error %d\n", error);
		return;
	}
	if (!machine) {
		printf("ERROR: machine must be exists. Report this bug, please\n");
		return;
	}

	match = regex_begin_match(machine);
	if (!match) {
		printf("WARNING: Not enough memory for matching\n");
		regex_free_machine(machine);
		return;
	}

	ptr = string;
	while (*ptr)
		ptr++;

	regex_continue_match_debug(match, string, (int)(ptr - string));

	begin = regex_get_result(match, &end, &id);
	printf("Math returns: %3d->%3d [%3d]\n", begin, end, id);

	regex_free_match(match);
	regex_free_machine(machine);
}
#endif

struct test_case {
	int begin;	/* Expected begin. */
	int end;	/* Expected end. */
	int id;		/* Expected id. */
	int finished;	/* -1 : don't care, 0 : false, 1 : true. */
	int flags;	/* REGEX_MATCH_* */
	const regex_char_t *pattern;	/* NULL : use the previous pattern. */
	const regex_char_t *string;	/* NULL : end of tests. */
};

static void run_tests(struct test_case* test, int verbose, int silent)
{
	int error;
	const regex_char_t *ptr;
	struct regex_machine* machine = NULL;
	struct regex_match* match;
	int begin, end, id, finished;
	int success = 0, fail = 0;

	if (!verbose && !silent)
		printf("Pass -v to enable verbose, -s to disable this hint.\n\n");

	for ( ; test->string ; test++) {
		if (verbose)
			printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
		fail++;

		if (test->pattern) {
			if (machine)
				regex_free_machine(machine);

			ptr = test->pattern;
			while (*ptr)
				ptr++;

			machine = regex_compile(test->pattern, (int)(ptr - test->pattern), test->flags, &error);

			if (error) {
				if (!verbose)
					printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
				printf("ABORT: Error %d\n", error);
				return;
			}
			if (!machine) {
				if (!verbose)
					printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
				printf("ABORT: machine must be exists. Report this bug, please\n");
				return;
			}
		}
		else if (test->flags != 0) {
			if (!verbose)
				printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
			printf("ABORT: flag must be 0 if no pattern\n");
			return;
		}

		ptr = test->string;
		while (*ptr)
			ptr++;

		match = regex_begin_match(machine);
#ifdef REGEX_MATCH_VERBOSE
		if (!match) {
			if (!verbose)
				printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
			printf("ABORT: Not enough memory for matching\n");
			regex_free_machine(machine);
			return;
		}
		regex_continue_match_debug(match, test->string, (int)(ptr - test->string));
		begin = regex_get_result(match, &end, &id);
		finished = regex_is_match_finished(match);

		if (begin != test->begin || end != test->end || id != test->id) {
			if (!verbose)
				printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
			printf("FAIL A: begin: %d != %d || end: %d != %d || id: %d != %d\n", test->begin, begin, test->end, end, test->id, id);
			continue;
		}
		if (test->finished != -1 && test->finished != !!finished) {
			if (!verbose)
				printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
			printf("FAIL A: finish check\n");
			continue;
		}
#endif

		regex_reset_match(match);
		regex_continue_match(match, test->string, (int)(ptr - test->string));
		begin = regex_get_result(match, &end, &id);
		finished = regex_is_match_finished(match);
		regex_free_match(match);

		if (begin != test->begin || end != test->end || id != test->id) {
			if (!verbose)
				printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
			printf("FAIL B: begin: %d != %d || end: %d != %d || id: %d != %d\n", test->begin, begin, test->end, end, test->id, id);
			continue;
		}
		if (test->finished != -1 && test->finished != !!finished) {
			if (!verbose)
				printf("test: '%s' '%s': ", test->pattern ? test->pattern : "[[REUSE]]", test->string);
			printf("FAIL B: finish check\n");
			continue;
		}

		if (verbose)
			printf("SUCCESS\n");
		fail--;
		success++;
	}
	if (machine)
		regex_free_machine(machine);

	printf("REGEX tests: ");
	if (fail == 0)
		printf("all tests " COLOR_GREEN "PASSED" COLOR_DEFAULT " ");
	else
		printf(COLOR_RED "%d" COLOR_DEFAULT " (" COLOR_RED "%d%%" COLOR_DEFAULT ") tests failed ", fail, fail * 100 / (success + fail));
	printf("on " COLOR_ARCH "%s" COLOR_DEFAULT "\n", regex_get_platform_name());
}

/* Testing. */

static struct test_case tests[] = {
{ 3, 7, 0, -1, 0,
  S("text"), S("is textile") },
{ 0, 10, 0, -1, 0,
  S("^(ab|c)*?d+(es)?"), S("abccabddeses") },
{ -1, 0, 0, 1, 0,
  S("^a+"), S("saaaa") },
{ 3, 6, 0, 0, 0,
  S("(a+|b+)$"), S("saabbb") },
{ 1, 6, 0, 0, 0,
  S("(a+|b+){,2}$"), S("saabbb") },
{ 1, 6, 0, 1, 0,
  S("(abcde|bc)(a+*|(b|c){2}+){0}"), S("babcdeaaaaaaaa") },
{ 1, 6, 0, 1, 0,
  S("(abc(aa)?|(cab+){2})"), S("cabcaa") },
{ -1, 0, 0, 1, 0,
  S("^(abc(aa)?|(cab+){2})$"), S("cabcaa") },
{ 0, 3, 1, -1, 0,
  S("^(ab{001!})?c"), S("abcde") },
{ 1, 15, 2, -1, 0,
  S("(c?(a|bb{2!}){2,3}()+d){2,3}"), S("ccabbadbbadcaadcaad") },
{ 2, 9, 0, -1, 0,
  NULL, S("cacaadaadaa") },
{ -1, 0, 0, -1, REGEX_MATCH_BEGIN,
  S("(((ab?c|d{1})))"), S("ad") },
{ 0, 9, 3, -1, REGEX_MATCH_BEGIN,
  S("^((a{1!}|b{2!}|c{3!}){3,6}d)+"), S("cabadbacddaa") },
{ 1, 6, 0, 0, REGEX_MATCH_END,
  S("(a+(bb|cc?)?){4,}"), S("maaaac") },
{ 3, 12, 1, 0, REGEX_MATCH_END,
  S("(x+x+{02,03}(x+|{1!})){03,06}$"), S("aaaxxxxxxxxx") },
{ 1, 2, 3, -1, 0,
  S("((c{1!})?|x+{2!}|{3!})(a|c)"), S("scs") },
{ 1, 4, 2, 1, 0,
  NULL, S("sxxaxxxaccacca") },
{ 0, 2, 1, 1, 0,
  NULL, S("ccdcdcdddddcdccccd") },
{ 0, 3, 0, -1, REGEX_MATCH_NON_GREEDY,
  S("^a+a+a+"), S("aaaaaa") },
{ 2, 5, 0, -1, REGEX_MATCH_NON_GREEDY,
  S("a+a+a+"), S("bbaaaaaa") },
{ 1, 4, 0, 1, 0,
  S("baa|a+"), S("sbaaaaaa") },
{ 0, 6, 0, 1, 0,
  S("baaa|baa|sbaaaa"), S("sbaaaaa") },
{ 1, 4, 0, 1, REGEX_MATCH_NON_GREEDY,
  S("baaa|baa"), S("xbaaa") },
{ 0, 0, 3, 1, 0,
  S("{3!}"), S("xx") },
{ 0, 0, 1, 1, 0,
  S("{1!}(a{2!})*"), S("xx") },
{ 0, 2, 2, 0, 0,
  NULL, S("aa") },
{ 0, 0, 1, 1, REGEX_MATCH_NON_GREEDY,
  S("{1!}(a{2!})*"), S("aaxx") },
{ 4, 12, 0, 1, 0,
  S("(.[]-]){3}[^]-]{2}"), S("ax-xs-[][]lmn") },
{ 3, 7, 1, 1, 0,
  S("([ABC]|[abc]{1!}){3,5}"), S("AbSAabbx") },
{ 0, 8, 3, 0, 0,
  S("^[x\\-y[\\]]+([[\\]]{3!})*$"), S("x-y[-][]") },
{ 0, 9, 0, 0, 0,
  NULL, S("x-y[-][]x") },
{ 2, 8, 0, 1, 0,
  S("<(/{1!})?[^>]+>"), S("  <html></html> ") },
{ 2, 9, 1, 1, 0,
  NULL, S("  </html><html> ") },
{ 2, 9, 0, 1, 0,
  S("[A-Z0-9a-z]+"), S("[(Iden9aA)]") },
{ 1, 4, 0, 1, 0,
  S("[^x-y]+[a-c_]{2,3}"), S("x_a_y") },
{ 4, 11, 0, 0, 0,
  NULL, S("ssaymmaa_ccl") },
{ 3, 6, 0, 1, REGEX_NEWLINE,
  S(".a[^k]"), S("\na\nxa\ns") },
{ 0, 2, 0, 1, REGEX_NEWLINE,
  S("^a+"), S("aa\n") },
{ 1, 4, 0, 1, 0 /* =REGEX_NEWLINE */,
  NULL, S("\naaa\n") },
{ 2, 3, 0, 1, 0 /* =REGEX_NEWLINE */,
  NULL, S("\n\na\n") },
{ 0, 2, 0, 1, REGEX_NEWLINE,
  S("a+$"), S("aa\n") },
{ 0, 3, 0, 0, 0 /* =REGEX_NEWLINE */,
  NULL, S("aaa") },
{ 2, 4, 1, 1, REGEX_NEWLINE,
  S("^a(a{1!})*$"), S("\n\naa\n\n") },
{ 0, 1, 0, 0, 0 /* REGEX_NEWLINE */,
  NULL, S("a") },
{ -1, 0, 0, -1, 0 /* REGEX_NEWLINE */,
  NULL, S("ab\nba") },
{ -1, 0, 0, 0, 0,
  NULL, NULL }
};

int main(int argc, char* argv[])
{
	int has_arg = (argc >= 2 && argv[1][0] == '-' && argv[1][2] == '\0');

/*	verbose_test("a((b)((c|d))|)c|"); */
/*	verbose_test("Xa{009,0010}Xb{,7}Xc{5,}Xd{,}Xe{1,}Xf{,1}X"); */
/*	verbose_test("{3!}({3})({0!}){,"); */
/*	verbose_test("(s(ab){2,4}t){2,}*S(a*(b)(c()|)d+){3,4}{0,0}*M"); */
/*	verbose_test("^a({2!})*b+(a|{1!}b)+d$"); */
/*	verbose_test("((a|b|c)*(xy)+)+", "asbcxyxy"); */

	run_tests(tests, has_arg && argv[1][1] == 'v', has_arg && argv[1][1] == 's');

#if !(defined SLJIT_CONFIG_UNSUPPORTED && SLJIT_CONFIG_UNSUPPORTED)
	sljit_free_unused_memory_exec();
#endif /* !SLJIT_CONFIG_UNSUPPORTED */

	return 0;
}
