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

#include "sljitLir.h"
#include "regexJIT.h"

#include <stdlib.h>

#ifdef REGEX_MATCH_VERBOSE
#include <stdio.h>
#endif

/* Extra, hidden flags:
   {id!} where id > 0 found in the code. */
#define REGEX_ID_CHECK		0x100
/* When REGEX_NEWLINE && REGEX_MATCH_BEGIN defined, the pattern turn to a normal search,
   which starts with [\r\n] character range. */
#define REGEX_FAKE_MATCH_BEGIN	0x200
/* When REGEX_NEWLINE && REGEX_MATCH_END defined, the pattern turn to a normal search,
   which ends with [\r\n] character range. */
#define REGEX_FAKE_MATCH_END	0x400

/* --------------------------------------------------------------------- */
/*  Structures for JIT-ed pattern matching                               */
/* --------------------------------------------------------------------- */

struct regex_machine
{
	/* flags. */
	int flags;
	/* Number of state descriptors for one term. */
	sljit_sw no_states;
	/* Total size. */
	sljit_sw size;

	union {
		void *init_match;
		sljit_sw (SLJIT_FUNC *call_init)(void *next, void* match);
	} u;
#if (defined SLJIT_INDIRECT_CALL && SLJIT_INDIRECT_CALL)
	struct sljit_function_context context;
#endif

	void *continue_match;

	/* Variable sized array to contain the handler addresses. */
	sljit_uw entry_addrs[1];
};

struct regex_match
{
	/* Current and next state array. */
	sljit_sw *current;
	sljit_sw *next;
	/* Starting. */
	sljit_sw head;
	/* String character index (ever increasing). */
	sljit_sw index;
	/* Best match found so far (members in priority order). */
	sljit_sw best_begin;
	sljit_sw best_end;
	sljit_sw best_id;
	/* Bool flags (encoded as word). */
	sljit_sw fast_quit;
	sljit_sw fast_forward;
	/* Machine. */
	struct regex_machine *machine;

	union {
		void *continue_match;
		void (SLJIT_FUNC *call_continue)(struct regex_match *match, const regex_char_t *input_string, int length);
	} u;

	/* Variable sized array to contain the state arrays. */
	sljit_sw states[1];
};

/* State vector
    ITEM[0] - pointer to the address inside the machine code
    ITEM[1] - next pointer
    ITEM[2] - string started from (optional)
    ITEM[3] - max ID (optional) */

/* Register allocation. */
/* Current state array (loaded & stored: regex_match->current). */
#define R_CURR_STATE	SLJIT_S0
/* Next state array (loaded & stored: regex_match->next). */
#define R_NEXT_STATE	SLJIT_S1
/* Head (loaded & stored: regex_match->head). */
#define R_NEXT_HEAD	SLJIT_S2
/* String fragment pointer. */
#define R_STRING	SLJIT_S3
/* String fragment length. */
#define R_LENGTH	SLJIT_S4
/* 'struct regex_match*' */
#define R_REGEX_MATCH	SLJIT_R0
/* Current character. */
#define R_CURR_CHAR	SLJIT_R1
/* Temporary register. */
#define R_TEMP		SLJIT_R2
/* Caches the regex_match->best_begin. */
#define R_BEST_BEGIN	SLJIT_R3
/* Current character index. */
#define R_CURR_INDEX	SLJIT_R4

/* --------------------------------------------------------------------- */
/*  Stack management                                                     */
/* --------------------------------------------------------------------- */

/* Try to allocate 2^n blocks. */
#define STACK_FRAGMENT_SIZE (((64 * sizeof(struct stack_item)) - (sizeof(struct stack_fragment_data))) / (sizeof(struct stack_item)))

struct stack_item {
	int type;
	int value;
};

struct stack_fragment_data {
	struct stack_fragment *next;
	struct stack_fragment *prev;
};

struct stack_fragment {
	struct stack_fragment_data data;
	struct stack_item items[STACK_FRAGMENT_SIZE];
};

struct stack {
	struct stack_fragment *first;
	struct stack_fragment *last;
	sljit_uw index;
	sljit_uw count;
};

#if (defined SLJIT_DEBUG && SLJIT_DEBUG)

static void stack_check(struct stack *stack)
{
	struct stack_fragment *curr;
	int found;

	if (!stack)
		return;

	SLJIT_ASSERT(stack->index < STACK_FRAGMENT_SIZE);

	if (stack->first == NULL) {
		SLJIT_ASSERT(stack->first == NULL && stack->last == NULL);
		SLJIT_ASSERT(stack->index == STACK_FRAGMENT_SIZE - 1 && stack->count == 0);
		return;
	}

	found = 0;
	if (stack->last == NULL) {
		SLJIT_ASSERT(stack->index == STACK_FRAGMENT_SIZE - 1 && stack->count == 0);
		found = 1;
	}

	SLJIT_ASSERT(stack->first->data.prev == NULL);
	curr = stack->first;
	while (curr) {
		if (curr == stack->last)
			found = 1;
		if (curr->data.next)
			SLJIT_ASSERT(curr->data.next->data.prev == curr);
		curr = curr->data.next;
	}
	SLJIT_ASSERT(found);
}

#endif

static void stack_init(struct stack *stack)
{
	stack->first = NULL;
	stack->last = NULL;
	stack->index = STACK_FRAGMENT_SIZE - 1;
	stack->count = 0;
}

static void stack_destroy(struct stack *stack)
{
	struct stack_fragment *curr = stack->first;
	struct stack_fragment *prev;

#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
	stack_check(stack);
#endif

	while (curr) {
		prev = curr;
		curr = curr->data.next;
		SLJIT_FREE(prev, NULL);
	}
}

static SLJIT_INLINE struct stack_item* stack_top(struct stack *stack)
{
	SLJIT_ASSERT(stack->last);
	return stack->last->items + stack->index;
}

static int stack_push(struct stack *stack, int type, int value)
{
	if (stack->last) {
		stack->index++;
		if (stack->index >= STACK_FRAGMENT_SIZE) {
			stack->index = 0;
			if (!stack->last->data.next) {
				stack->last->data.next = (struct stack_fragment*)SLJIT_MALLOC(sizeof(struct stack_fragment), NULL);
				if (!stack->last->data.next)
					return 1;
				stack->last->data.next->data.next = NULL;
				stack->last->data.next->data.prev = stack->last;
			}
			stack->last = stack->last->data.next;
		}
	}
	else if (!stack->first) {
		stack->last = (struct stack_fragment*)SLJIT_MALLOC(sizeof(struct stack_fragment), NULL);
		if (!stack->last)
			return 1;
		stack->last->data.prev = NULL;
		stack->last->data.next = NULL;
		stack->first = stack->last;
		stack->index = 0;
	}
	else {
		stack->last = stack->first;
		stack->index = 0;
	}
	stack->last->items[stack->index].type = type;
	stack->last->items[stack->index].value = value;
	stack->count++;
#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
	stack_check(stack);
#endif
	return 0;
}

static struct stack_item* stack_pop(struct stack *stack)
{
	struct stack_item *ret = stack_top(stack);

	if (stack->index > 0)
		stack->index--;
	else {
		stack->last = stack->last->data.prev;
		stack->index = STACK_FRAGMENT_SIZE - 1;
	}

	stack->count--;
#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
	stack_check(stack);
#endif
	return ret;
}

static SLJIT_INLINE void stack_clone(struct stack *src, struct stack *dst)
{
	*dst = *src;
}

static int stack_push_copy(struct stack *stack, int items, int length)
{
	struct stack_fragment *frag1;
	struct stack_fragment *frag2;
	sljit_uw ind1, ind2;
	sljit_uw counter;

	SLJIT_ASSERT(stack->count >= (sljit_uw)length && items <= length && items > 0);

	/* Allocate the necessary elements. */
	counter = (sljit_uw)items;
	frag1 = stack->last;
	ind1 = stack->index;
	while (counter > 0) {
		if (stack->index + counter >= STACK_FRAGMENT_SIZE) {
			SLJIT_ASSERT(counter >= STACK_FRAGMENT_SIZE - stack->index - 1 + 1);
			counter -= STACK_FRAGMENT_SIZE - stack->index - 1 + 1;
			stack->index = 0;
			if (!stack->last->data.next) {
				stack->last->data.next = (struct stack_fragment*)SLJIT_MALLOC(sizeof(struct stack_fragment), NULL);
				if (!stack->last->data.next)
					return 1;
				stack->last->data.next->data.next = NULL;
				stack->last->data.next->data.prev = stack->last;
			}
			stack->last = stack->last->data.next;
		}
		else {
			stack->index += counter;
			counter = 0;
		}
	}

	frag2 = stack->last;
	ind2 = stack->index;
	while (length > 0) {
		frag2->items[ind2] = frag1->items[ind1];

		if (ind1 == 0) {
			ind1 = STACK_FRAGMENT_SIZE;
			frag1 = frag1->data.prev;
		}
		if (ind2 == 0) {
			ind2 = STACK_FRAGMENT_SIZE;
			frag2 = frag2->data.prev;
		}

		ind1--;
		ind2--;
		length--;
	}

#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
	stack_check(stack);
#endif
	stack->count += (sljit_uw)items;
	return 0;
}

/* --------------------------------------------------------------------- */
/*  Parser                                                               */
/* --------------------------------------------------------------------- */

enum {
	/* Common. */
	type_begin,
	type_end,
	type_char,
	type_newline,
	type_id,
	type_rng_start,
	type_rng_end,
	type_rng_char,
	type_rng_left,
	type_rng_right,

	/* generator only. */
	type_branch,
	type_jump,

	/* Parser only. */
	type_open_br,
	type_close_br,
	type_select,
	type_asterisk,
	type_plus_sign,
	type_qestion_mark
};

struct compiler_common {
	/* Temporary stacks. */
	struct stack stack;
	struct stack depth;
	/* REGEX_ flags. */
	int flags;
	/* Encoded size of the dfa representation. */
	sljit_uw dfa_size;
	/* Number of terms. */
	sljit_sw terms_size;
	/* Number of state descriptors for one term (same as machine->no_states). */
	sljit_sw no_states;
	/* Number of type_rng_(char|left)-s in the longest character range. */
	sljit_sw longest_range_size;

	/* DFA linear representation (size: dfa_size). */
	struct stack_item *dfa_transitions;
	/* Term id and search state pairs (size: dfa_size). */
	struct stack_item *search_states;

	/* sljit compiler */
	struct sljit_compiler *compiler;
	/* Machine data, which must be kept for later use. */
	struct regex_machine *machine;
	/* Temporary space for jumps (size: longest_range_size). */
	struct sljit_jump **range_jump_list;
};

static const regex_char_t* decode_number(const regex_char_t *regex_string, int length, int *result)
{
	int value = 0;

	SLJIT_ASSERT(length > 0);
	if (*regex_string < '0' || *regex_string > '9') {
		*result = -1;
		return regex_string;
	}

	while (length > 0 && *regex_string >= '0' && *regex_string <= '9') {
		value = value * 10 + (*regex_string - '0');
		length--;
		regex_string++;
	}

	*result = value;
	return regex_string;
}

static int iterate(struct stack *stack, int min, int max)
{
	struct stack it;
	struct stack_item *item;
	int count = -1;
	int len = 0;
	int depth = 0;

	stack_clone(stack, &it);

	/* Calculate size. */
	while (count < 0) {
		item = stack_pop(&it);
		switch (item->type) {
		case type_id:
		case type_rng_end:
		case type_rng_char:
		case type_rng_left:
		case type_rng_right:
		case type_plus_sign:
		case type_qestion_mark:
			len++;
			break;

		case type_asterisk:
			len += 2;
			break;

		case type_close_br:
			depth++;
			break;

		case type_open_br:
			SLJIT_ASSERT(depth > 0);
			depth--;
			if (depth == 0)
				count = (int)it.count;
			break;

		case type_select:
			SLJIT_ASSERT(depth > 0);
			len += 2;
			break;

		default:
			SLJIT_ASSERT(item->type != type_begin && item->type != type_end);
			if (depth == 0)
				count = (int)it.count;
			len++;
			break;
		}
	}

	if (min == 0 && max == 0) {
		/* {0,0} case, not {0,} case: delete subtree. */
		stack_clone(&it, stack);
		/* and put an empty bracket expression instead of it. */
		if (stack_push(stack, type_open_br, 0))
			return REGEX_MEMORY_ERROR;
		if (stack_push(stack, type_close_br, 0))
			return REGEX_MEMORY_ERROR;
		return len;
	}

	count = (int)stack->count - count;

	/* Put an open bracket before the sequence. */
	if (stack_push_copy(stack, 1, count))
		return -1;

#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
	SLJIT_ASSERT(stack_push(&it, type_open_br, 0) == 0);
#else
	stack_push(&it, type_open_br, 0);
#endif

	/* Copy the data. */
	if (max > 0) {
		len = len * (max - 1);
		max -= min;
		/* Insert ? operators. */
		len += max;

		if (min > 0) {
			min--;
			while (min > 0) {
				if (stack_push_copy(stack, count, count))
					return -1;
				min--;
			}
			if (max > 0) {
				if (stack_push_copy(stack, count, count))
					return -1;
				if (stack_push(stack, type_qestion_mark, 0))
					return REGEX_MEMORY_ERROR;
				count++;
				max--;
			}
		}
		else {
			SLJIT_ASSERT(max > 0);
			max--;
			count++;
			if (stack_push(stack, type_qestion_mark, 0))
				return REGEX_MEMORY_ERROR;
		}

		while (max > 0) {
			if (stack_push_copy(stack, count, count))
				return -1;
			max--;
		}
	}
	else {
		SLJIT_ASSERT(min > 0);
		min--;
		/* Insert + operator. */
		len = len * min + 1;
		while (min > 0) {
			if (stack_push_copy(stack, count, count))
				return -1;
			min--;
		}

		if (stack_push(stack, type_plus_sign, 0))
			return REGEX_MEMORY_ERROR;
	}

	/* Close the opened bracket. */
	if (stack_push(stack, type_close_br, 0))
		return REGEX_MEMORY_ERROR;

	return len;
}

static int parse_iterator(const regex_char_t *regex_string, int length, struct stack *stack, sljit_uw *dfa_size, int begin)
{
	/* We only know that *regex_string == { . */
	int val1, val2;
	const regex_char_t *base_from = regex_string;
	const regex_char_t *from;

	length--;
	regex_string++;

	/* Decode left value. */
	val2 = -1;
	if (length == 0)
		return -2;
	if (*regex_string == ',') {
		val1 = 0;
		length--;
		regex_string++;
	}
	else {
		from = regex_string;
		regex_string = decode_number(regex_string, length, &val1);
		if (val1 < 0)
			return -2;
		length -= (int)(regex_string - from);

		if (length == 0)
			return -2;
		if (*regex_string == '}') {
			val2 = val1;
			if (val1 == 0)
				val1 = -1;
		}
		else if (length >= 2 && *regex_string == '!' && regex_string[1] == '}') {
			/* Non posix extension. */
			if (stack_push(stack, type_id, val1))
				return -1;
			(*dfa_size)++;
			return (int)(regex_string - base_from) + 1;
		}
		else {
			if (*regex_string != ',')
				return -2;
			length--;
			regex_string++;
		}
	}

	if (begin)
		return -2;

	/* Decode right value. */
	if (val2 == -1) {
		if (length == 0)
			return -2;
		if (*regex_string == '}')
			val2 = 0;
		else {
			from = regex_string;
			regex_string = decode_number(regex_string, length, &val2);
			length -= (int)(regex_string - from);
			if (val2 < 0 || length == 0 || *regex_string != '}' || val2 < val1)
				return -2;
			if (val2 == 0) {
				SLJIT_ASSERT(val1 == 0);
				val1 = -1;
			}
		}
	}

	/* Fast cases. */
	if (val1 > 1 || val2 > 1) {
		val1 = iterate(stack, val1, val2);
		if (val1 < 0)
			return -1;
		*dfa_size += (sljit_uw)val1;
	}
	else if (val1 == 0 && val2 == 0) {
		if (stack_push(stack, type_asterisk, 0))
			return -1;
		*dfa_size += 2;
	}
	else if (val1 == 1 && val2 == 0) {
		if (stack_push(stack, type_plus_sign, 0))
			return -1;
		(*dfa_size)++;
	}
	else if (val1 == 0 && val2 == 1) {
		if (stack_push(stack, type_qestion_mark, 0))
			return -1;
		(*dfa_size)++;
	}
	else if (val1 == -1) {
		val1 = iterate(stack, 0, 0);
		if (val1 < 0)
			return -1;
		*dfa_size -= (sljit_uw)val1;
		SLJIT_ASSERT(*dfa_size >= 2);
	}
	else {
		/* Ignore. */
		SLJIT_ASSERT(val1 == 1 && val2 == 1);
	}
	return (int)(regex_string - base_from);
}

static int parse_char_range(const regex_char_t *regex_string, int length, struct compiler_common *compiler_common)
{
	struct stack* stack = &compiler_common->stack;
	const regex_char_t *base_from = regex_string;
	regex_char_t left_char, right_char, tmp_char;

	length--;
	regex_string++;

	if (length == 0)
		return -2;

	if (*regex_string != '^') {
		if (stack_push(stack, type_rng_start, 0))
			return -1;
	}
	else {
		length--;
		regex_string++;

		if (length == 0)
			return -2;

		if (stack_push(stack, type_rng_start, 1))
			return -1;
	}
	/* For both the type_rng_start & type_rng_end. */
	compiler_common->dfa_size += 2;

	/* Range must be at least 1 character. */
	if (*regex_string == ']') {
		length--;
		regex_string++;
		if (stack_push(stack, type_rng_char, ']'))
			return -1;
		compiler_common->dfa_size++;
	}

	while (1) {
		if (length == 0)
			return -2;

		if (*regex_string == ']')
			break;

		if (*regex_string != '\\')
			left_char = *regex_string;
		else {
			regex_string++;
			length--;
			if (length == 0)
				return -2;
			left_char = *regex_string;
		}
		regex_string++;
		length--;

		/* Is a range here? */
		if (length >= 3 && *regex_string == '-' && *(regex_string + 1) != ']') {
			regex_string++;
			length--;

			if (*regex_string != '\\')
				right_char = *regex_string;
			else {
				regex_string++;
				length--;
				if (length == 0)
					return -2;
				right_char = *regex_string;
			}
			regex_string++;
			length--;

			if (left_char > right_char) {
				/* Swap if necessary. */
				tmp_char = left_char;
				left_char = right_char;
				right_char = tmp_char;
			}

			if (stack_push(stack, type_rng_left, left_char))
				return -1;
			if (stack_push(stack, type_rng_right, right_char))
				return -1;
			compiler_common->dfa_size += 2;
		}
		else {
			if (stack_push(stack, type_rng_char, left_char))
				return -1;
			compiler_common->dfa_size++;
		}
	}

	if (stack_push(stack, type_rng_end, 0))
		return -1;
	return (int)(regex_string - base_from);
}

static int parse(const regex_char_t *regex_string, int length, struct compiler_common *compiler_common)
{
	/* Depth of bracketed expressions. */
	int depth = 0;
	/* Have we already found a term? '1' if not yet. */
	int begin = 1;
	/* Cache stack pointer. */
	struct stack* stack = &compiler_common->stack;
	int tmp;

	/* Type_begin and type_end. */
	compiler_common->dfa_size = 2;
	stack_init(stack);
	if (stack_push(stack, type_begin, 0))
		return REGEX_MEMORY_ERROR;

	if (length > 0 && *regex_string == '^') {
		compiler_common->flags |= REGEX_MATCH_BEGIN;
		length--;
		regex_string++;
	}

	if ((compiler_common->flags & (REGEX_MATCH_BEGIN | REGEX_NEWLINE)) == (REGEX_MATCH_BEGIN | REGEX_NEWLINE)) {
		/* Replace REGEX_MATCH_BEGIN flag to REGEX_FAKE_MATCH_BEGIN */
		compiler_common->flags &= ~REGEX_MATCH_BEGIN;
		compiler_common->flags |= REGEX_FAKE_MATCH_BEGIN;
		/* and append a new-line search. */
		if (stack_push(stack, type_newline, 0))
			return REGEX_MEMORY_ERROR;
		compiler_common->dfa_size++;
		/* Begin intentionally kept as 1. */
	}

	while (length > 0) {
		switch (*regex_string) {
		case '\\' :
			length--;
			regex_string++;
			if (length == 0)
				return REGEX_INVALID_REGEX;
			if (stack_push(stack, type_char, *regex_string))
				return REGEX_MEMORY_ERROR;
			begin = 0;
			compiler_common->dfa_size++;
			break;

		case '.' :
			if (stack_push(stack, type_rng_start, 1))
				return REGEX_MEMORY_ERROR;
			if (compiler_common->flags & REGEX_NEWLINE) {
				if (stack_push(stack, type_rng_char, '\n'))
					return REGEX_MEMORY_ERROR;
				if (stack_push(stack, type_rng_char, '\r'))
					return REGEX_MEMORY_ERROR;
				compiler_common->dfa_size += 2;
			}
			if (stack_push(stack, type_rng_end, 1))
				return REGEX_MEMORY_ERROR;
			begin = 0;
			compiler_common->dfa_size += 2;
			break;

		case '(' :
			depth++;
			if (stack_push(stack, type_open_br, 0))
				return REGEX_MEMORY_ERROR;
			begin = 1;
			break;

		case ')' :
			if (depth == 0)
				return REGEX_INVALID_REGEX;
			depth--;
			if (stack_push(stack, type_close_br, 0))
				return REGEX_MEMORY_ERROR;
			begin = 0;
			break;

		case '|' :
			if (stack_push(stack, type_select, 0))
				return REGEX_MEMORY_ERROR;
			begin = 1;
			compiler_common->dfa_size += 2;
			break;

		case '*' :
			if (begin)
				return REGEX_INVALID_REGEX;
			if (stack_push(stack, type_asterisk, 0))
				return REGEX_MEMORY_ERROR;
			compiler_common->dfa_size += 2;
			break;

		case '?' :
		case '+' :
			if (begin)
				return REGEX_INVALID_REGEX;
			if (stack_push(stack, (*regex_string == '+') ? type_plus_sign : type_qestion_mark, 0))
				return REGEX_MEMORY_ERROR;
			compiler_common->dfa_size++;
			break;

		case '{' :
			tmp = parse_iterator(regex_string, length, stack, &compiler_common->dfa_size, begin);

			if (tmp >= 0) {
				length -= tmp;
				regex_string += tmp;
			}
			else if (tmp == -1)
				return REGEX_MEMORY_ERROR;
			else {
				/* Not a valid range expression. */
				SLJIT_ASSERT(tmp == -2);
				if (stack_push(stack, type_char, '{'))
					return REGEX_MEMORY_ERROR;
				compiler_common->dfa_size++;
			}
			break;

		case '[' :
			tmp = parse_char_range(regex_string, length, compiler_common);
			if (tmp >= 0) {
				length -= tmp;
				regex_string += tmp;
			}
			else if (tmp == -1)
				return REGEX_MEMORY_ERROR;
			else {
				SLJIT_ASSERT(tmp == -2);
				return REGEX_INVALID_REGEX;
			}
			begin = 0;
			break;

		default:
			if (length == 1 && *regex_string == '$') {
				compiler_common->flags |= REGEX_MATCH_END;
				break;
			}
			if (stack_push(stack, type_char, *regex_string))
				return REGEX_MEMORY_ERROR;
			begin = 0;
			compiler_common->dfa_size++;
			break;
		}
		length--;
		regex_string++;
	}

	if (depth != 0)
		return REGEX_INVALID_REGEX;

	if ((compiler_common->flags & (REGEX_MATCH_END | REGEX_NEWLINE)) == (REGEX_MATCH_END | REGEX_NEWLINE)) {
		/* Replace REGEX_MATCH_END flag to REGEX_FAKE_MATCH_END */
		compiler_common->flags &= ~REGEX_MATCH_END;
		compiler_common->flags |= REGEX_FAKE_MATCH_END;
		/* and append a new-line search. */
		if (stack_push(stack, type_newline, 1))
			return REGEX_MEMORY_ERROR;
		compiler_common->dfa_size++;
		/* Begin intentionally kept as 1. */
	}

	if (stack_push(stack, type_end, 0))
		return REGEX_MEMORY_ERROR;

	return REGEX_NO_ERROR;
}

/* --------------------------------------------------------------------- */
/*  Generating machine state transitions                                 */
/* --------------------------------------------------------------------- */

#define PUT_TRANSITION(typ, val) \
	do { \
		--transitions_ptr; \
		transitions_ptr->type = typ; \
		transitions_ptr->value = val; \
	} while (0)

static struct stack_item* handle_iteratives(struct stack_item *transitions_ptr, struct stack_item *transitions, struct stack *depth)
{
	struct stack_item *item;

	while (1) {
		item = stack_top(depth);

		switch (item->type) {
		case type_asterisk:
			SLJIT_ASSERT(transitions[item->value].type == type_branch);
			transitions[item->value].value = (int)(transitions_ptr - transitions);
			PUT_TRANSITION(type_branch, item->value + 1);
			break;

		case type_plus_sign:
			SLJIT_ASSERT(transitions[item->value].type == type_branch);
			transitions[item->value].value = (int)(transitions_ptr - transitions);
			break;

		case type_qestion_mark:
			PUT_TRANSITION(type_branch, item->value);
			break;

		default:
			return transitions_ptr;
		}
		stack_pop(depth);
	}
}

static int generate_transitions(struct compiler_common *compiler_common)
{
	struct stack *stack = &compiler_common->stack;
	struct stack *depth = &compiler_common->depth;
	struct stack_item *transitions_ptr;
	struct stack_item *item;

	stack_init(depth);
	compiler_common->dfa_transitions = SLJIT_MALLOC(sizeof(struct stack_item) * compiler_common->dfa_size, NULL);
	if (!compiler_common->dfa_transitions)
		return REGEX_MEMORY_ERROR;

	/* Go through the items of the stack and generate the necessary branches and jumps (edges of DFA). */
	transitions_ptr = compiler_common->dfa_transitions + compiler_common->dfa_size;
	while (stack->count > 0) {
		item = stack_pop(stack);
		switch (item->type) {
		case type_begin:
		case type_open_br:
			item = stack_pop(depth);
			if (item->type == type_select)
				PUT_TRANSITION(type_branch, item->value + 1);
			else
				SLJIT_ASSERT(item->type == type_close_br);
			if (stack->count == 0)
				PUT_TRANSITION(type_begin, 0);
			else
				transitions_ptr = handle_iteratives(transitions_ptr, compiler_common->dfa_transitions, depth);
			break;

		case type_end:
		case type_close_br:
			if (item->type == type_end)
				*--transitions_ptr = *item;
			if (stack_push(depth, type_close_br, (int)(transitions_ptr - compiler_common->dfa_transitions)))
				return REGEX_MEMORY_ERROR;
			break;

		case type_select:
			item = stack_top(depth);
			if (item->type == type_select) {
				SLJIT_ASSERT(compiler_common->dfa_transitions[item->value].type == type_jump);
				PUT_TRANSITION(type_branch, item->value + 1);
				PUT_TRANSITION(type_jump, item->value);
				item->value = (int)(transitions_ptr - compiler_common->dfa_transitions);
			}
			else {
				SLJIT_ASSERT(item->type == type_close_br);
				item->type = type_select;
				PUT_TRANSITION(type_jump, item->value);
				item->value = (int)(transitions_ptr - compiler_common->dfa_transitions);
			}
			break;

		case type_asterisk:
		case type_plus_sign:
		case type_qestion_mark:
			if (item->type != type_qestion_mark)
				PUT_TRANSITION(type_branch, 0);
			if (stack_push(depth, item->type, (int)(transitions_ptr - compiler_common->dfa_transitions)))
				return REGEX_MEMORY_ERROR;
			break;

		case type_char:
		case type_newline:
		case type_rng_start:
			/* Requires handle_iteratives. */
			*--transitions_ptr = *item;
			transitions_ptr = handle_iteratives(transitions_ptr, compiler_common->dfa_transitions, depth);
			break;

		default:
			*--transitions_ptr = *item;
			break;
		}
	}

	SLJIT_ASSERT(compiler_common->dfa_transitions == transitions_ptr);
	SLJIT_ASSERT(depth->count == 0);
	return REGEX_NO_ERROR;
}

#undef PUT_TRANSITION

#ifdef REGEX_MATCH_VERBOSE

static void verbose_transitions(struct compiler_common *compiler_common)
{
	struct stack_item *transitions_ptr = compiler_common->dfa_transitions;
	struct stack_item *transitions_end = transitions_ptr + compiler_common->dfa_size;
	struct stack_item *search_states_ptr = compiler_common->search_states;
	int pos;

	printf("-----------------\nTransitions\n-----------------\n");
	pos = 0;
	while (transitions_ptr < transitions_end) {
		printf("[%3d] ", pos++);
		if (search_states_ptr->type >= 0)
			printf("(%3d) ", search_states_ptr->type);
		switch (transitions_ptr->type) {
		case type_begin:
			printf("type_begin\n");
			break;

		case type_end:
			printf("type_end\n");
			break;

		case type_char:
			if (transitions_ptr->value >= ' ')
				printf("type_char '%c'\n", transitions_ptr->value);
			else
				printf("type_char 0x%x\n", transitions_ptr->value);
			break;

		case type_newline:
			printf("type_newline %s\n", transitions_ptr->value ? "(end)" : "(begin)");
			break;

		case type_id:
			printf("type_id %d\n", transitions_ptr->value);
			break;

		case type_rng_start:
			printf("type_rng_start %s\n", transitions_ptr->value ? "(invert)" : "(normal)");
			break;

		case type_rng_end:
			printf("type_rng_end\n");
			break;

		case type_rng_char:
			if (transitions_ptr->value >= ' ')
				printf("type_rng_char '%c'\n", transitions_ptr->value);
			else
				printf("type_rng_char 0x%x\n", transitions_ptr->value);
			break;

		case type_rng_left:
			if (transitions_ptr->value >= ' ')
				printf("type_rng_left '%c'\n", transitions_ptr->value);
			else
				printf("type_rng_left 0x%x\n", transitions_ptr->value);
			break;

		case type_rng_right:
			if (transitions_ptr->value >= ' ')
				printf("type_rng_right '%c'\n", transitions_ptr->value);
			else
				printf("type_rng_right 0x%x\n", transitions_ptr->value);
			break;

		case type_branch:
			printf("type_branch -> %d\n", transitions_ptr->value);
			break;

		case type_jump:
			printf("type_jump -> %d\n", transitions_ptr->value);
			break;

		default:
			printf("UNEXPECTED TYPE\n");
			break;
		}
		transitions_ptr++;
		search_states_ptr++;
	}
	printf("flags: ");
	if (!(compiler_common->flags & (REGEX_MATCH_BEGIN | REGEX_MATCH_END | REGEX_NEWLINE | REGEX_ID_CHECK | REGEX_FAKE_MATCH_BEGIN | REGEX_FAKE_MATCH_END)))
		printf("none ");
	if (compiler_common->flags & REGEX_MATCH_BEGIN)
		printf("REGEX_MATCH_BEGIN ");
	if (compiler_common->flags & REGEX_MATCH_END)
		printf("REGEX_MATCH_END ");
	if (compiler_common->flags & REGEX_NEWLINE)
		printf("REGEX_NEWLINE ");
	if (compiler_common->flags & REGEX_ID_CHECK)
		printf("REGEX_ID_CHECK ");
	if (compiler_common->flags & REGEX_FAKE_MATCH_BEGIN)
		printf("REGEX_FAKE_MATCH_BEGIN ");
	if (compiler_common->flags & REGEX_FAKE_MATCH_END)
		printf("REGEX_FAKE_MATCH_END ");
	if (compiler_common->longest_range_size > 0)
		printf("(longest range: %ld) ", (long)compiler_common->longest_range_size);
	printf("\n");
}

#endif

/* --------------------------------------------------------------------- */
/*  Utilities                                                            */
/* --------------------------------------------------------------------- */

static int generate_search_states(struct compiler_common *compiler_common)
{
	struct stack_item *transitions_ptr = compiler_common->dfa_transitions;
	struct stack_item *transitions_end = transitions_ptr + compiler_common->dfa_size;
	struct stack_item *search_states_ptr;
	struct stack_item *rng_start = NULL;

	compiler_common->terms_size = !(compiler_common->flags & REGEX_FAKE_MATCH_END) ? 1 : 2;
	compiler_common->longest_range_size = 0;
	compiler_common->search_states = SLJIT_MALLOC(sizeof(struct stack_item) * compiler_common->dfa_size, NULL);
	if (!compiler_common->search_states)
		return REGEX_MEMORY_ERROR;

	search_states_ptr = compiler_common->search_states;
	while (transitions_ptr < transitions_end) {
		switch (transitions_ptr->type) {
		case type_begin:
		case type_end:
			search_states_ptr->type = 0;
			break;

		case type_char:
			search_states_ptr->type = (int)compiler_common->terms_size++;
			break;

		case type_newline:
			if (transitions_ptr->value)
				search_states_ptr->type = 1;
			else
				search_states_ptr->type = (int)compiler_common->terms_size++;
			SLJIT_ASSERT(search_states_ptr->type == 1 || search_states_ptr->type == 2);
			break;

		case type_id:
			if (transitions_ptr->value > 0)
				compiler_common->flags |= REGEX_ID_CHECK;
			search_states_ptr->type = -1;
			break;

		case type_rng_start:
			search_states_ptr->type = (int)compiler_common->terms_size;
			rng_start = search_states_ptr;
			break;

		case type_rng_end:
			search_states_ptr->type = (int)compiler_common->terms_size++;
			/* This is an over estimation. */
			if (compiler_common->longest_range_size < search_states_ptr - rng_start)
				compiler_common->longest_range_size = search_states_ptr - rng_start;
			break;

		default:
			search_states_ptr->type = -1;
			break;
		}
		search_states_ptr->value = -1;
		search_states_ptr++;
		transitions_ptr++;
	}
	return REGEX_NO_ERROR;
}

static int trace_transitions(int from, struct compiler_common *compiler_common)
{
	int id = 0;
	struct stack *stack = &compiler_common->stack;
	struct stack *depth = &compiler_common->depth;
	struct stack_item *dfa_transitions = compiler_common->dfa_transitions;
	struct stack_item *search_states = compiler_common->search_states;

	SLJIT_ASSERT(search_states[from].type >= 0);

	from++;

	/* Be prepared for any paths (loops, etc). */
	while (1) {
		if (dfa_transitions[from].type == type_id)
			if (id < dfa_transitions[from].value)
				id = dfa_transitions[from].value;

		if (search_states[from].value < id) {
			/* Forward step. */
			if (search_states[from].value == -1)
				if (stack_push(stack, 0, from))
					return REGEX_MEMORY_ERROR;
			search_states[from].value = id;

			if (dfa_transitions[from].type == type_branch) {
				if (stack_push(depth, id, from))
					return REGEX_MEMORY_ERROR;
				from++;
				continue;
			}
			else if (dfa_transitions[from].type == type_jump) {
				from = dfa_transitions[from].value;
				continue;
			}
			else if (search_states[from].type < 0) {
				from++;
				continue;
			}
		}

		/* Back tracking. */
		if (depth->count > 0) {
			id = stack_top(depth)->type;
			from = dfa_transitions[stack_pop(depth)->value].value;
			continue;
		}
		return 0;
	}
}

/* --------------------------------------------------------------------- */
/*  Code generator                                                       */
/* --------------------------------------------------------------------- */

#define TERM_OFFSET_OF(index, offs)	(((index) * no_states + (offs)) * (sljit_sw)sizeof(sljit_sw))
#define TERM_REL_OFFSET_OF(base, offs)	((base) + ((offs) * (sljit_sw)sizeof(sljit_sw)))

#define EMIT_OP1(type, arg1, arg2, arg3, arg4) \
	CHECK(sljit_emit_op1(compiler, type, arg1, arg2, arg3, arg4))

#define EMIT_OP2(type, arg1, arg2, arg3, arg4, arg5, arg6) \
	CHECK(sljit_emit_op2(compiler, type, arg1, arg2, arg3, arg4, arg5, arg6))

#define EMIT_OP2U(type, arg1, arg2, arg3, arg4) \
	CHECK(sljit_emit_op2u(compiler, type, arg1, arg2, arg3, arg4))

#define EMIT_LABEL(label) \
	label = sljit_emit_label(compiler); \
	CHECK(!label)

#define EMIT_JUMP(jump, type) \
	jump = sljit_emit_jump(compiler, type); \
	CHECK(!jump)

#define EMIT_CMP(jump, type, arg1, arg2, arg3, arg4) \
	jump = sljit_emit_cmp(compiler, type, arg1, arg2, arg3, arg4); \
	CHECK(!jump)

/* CHECK depends on the use case. */

#define CHECK(exp) \
	if (SLJIT_UNLIKELY(exp)) \
		return REGEX_MEMORY_ERROR

static int compile_uncond_tran(struct compiler_common *compiler_common, int reg)
{
	struct sljit_compiler *compiler = compiler_common->compiler;
	struct stack *stack = &compiler_common->stack;
	struct stack_item *search_states = compiler_common->search_states;
	int flags = compiler_common->flags;
	sljit_sw no_states = compiler_common->no_states;
	sljit_sw head = 0;
	sljit_sw offset, value;

	if (reg != R_CURR_STATE || !(compiler_common->flags & REGEX_FAKE_MATCH_BEGIN)) {
		CHECK(trace_transitions(0, compiler_common));
	}
	else {
		CHECK(trace_transitions(1, compiler_common));
	}

	while (stack->count > 0) {
		value = stack_pop(stack)->value;
		if (search_states[value].type >= 0) {
			offset = TERM_OFFSET_OF(search_states[value].type, 0);
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 1), SLJIT_IMM, head);
			if (offset > 0)
				head = offset;

			if (!(flags & REGEX_MATCH_BEGIN)) {
				EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 2), R_TEMP, 0);
				if (flags & REGEX_ID_CHECK) {
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 3), SLJIT_IMM, search_states[value].value);
				}
			}
			else if (flags & REGEX_ID_CHECK) {
				EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 2), SLJIT_IMM, search_states[value].value);
			}
		}
		search_states[value].value = -1;
	}
	if (reg == R_NEXT_STATE) {
		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_NEXT_HEAD, 0);
	}
	else if (flags & REGEX_FAKE_MATCH_BEGIN) {
		SLJIT_ASSERT(compiler_common->dfa_transitions[1].type == type_newline && !compiler_common->dfa_transitions[1].value);
		offset = TERM_OFFSET_OF(search_states[1].type, 0);

		SLJIT_ASSERT(!(flags & REGEX_MATCH_BEGIN));

		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 1), SLJIT_IMM, head);
		head = offset;

		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 2), SLJIT_IMM, 1);
		if (flags & REGEX_ID_CHECK) {
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(reg), TERM_REL_OFFSET_OF(offset, 3), SLJIT_IMM, 0);
		}
	}
	EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, head);
	return REGEX_NO_ERROR;
}

static int compile_cond_tran(struct compiler_common *compiler_common, sljit_sw curr_index)
{
	struct sljit_compiler *compiler = compiler_common->compiler;
	struct stack *stack = &compiler_common->stack;
	struct stack_item *search_states = compiler_common->search_states;
	sljit_sw offset;
	int flags;
	sljit_sw no_states;
	sljit_sw value;
	struct sljit_jump *jump1;
	struct sljit_jump *jump2;
	struct sljit_jump *jump3;
	struct sljit_jump *jump4;
	struct sljit_jump *jump5;
	struct sljit_label *label1;

	flags = compiler_common->flags;
	no_states = compiler_common->no_states;

	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_forward), SLJIT_IMM, 0);
	if (!(flags & (REGEX_ID_CHECK | REGEX_MATCH_BEGIN))) {
		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(curr_index, 2));
	}

	while (stack->count > 0) {
		value = stack_pop(stack)->value;
		if (search_states[value].type >= 0) {
#ifdef REGEX_MATCH_VERBOSE
			if (flags & REGEX_MATCH_VERBOSE)
				printf("-> (%3d:%3d) ", search_states[value].type, search_states[value].value);
#endif
			offset = TERM_OFFSET_OF(search_states[value].type, 0);

			if (!(flags & REGEX_ID_CHECK)) {
				if (!(flags & REGEX_MATCH_BEGIN)) {
					/* Check whether item is inserted. */
					EMIT_CMP(jump1, SLJIT_NOT_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), SLJIT_IMM, -1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), R_NEXT_HEAD, 0);
					if (offset > 0) {
						EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, offset);
					}
					EMIT_JUMP(jump2, SLJIT_JUMP);

					/* Check whether old index <= index. */
					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);

					EMIT_CMP(jump1, SLJIT_LESS_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + 2 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);

					EMIT_LABEL(label1);
					sljit_set_label(jump2, label1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + 2 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);

					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);
				}
				else {
					/* Check whether item is inserted. */
					EMIT_CMP(jump1, SLJIT_NOT_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), SLJIT_IMM, -1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), R_NEXT_HEAD, 0);
					if (offset > 0) {
						EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, offset);
					}
					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);
				}
			}
			else {
				if (!(flags & REGEX_MATCH_BEGIN)) {
					EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(curr_index, 2));

					/* Check whether item is inserted. */
					EMIT_CMP(jump1, SLJIT_NOT_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), SLJIT_IMM, -1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), R_NEXT_HEAD, 0);
					if (offset > 0) {
						EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, offset);
					}
					EMIT_JUMP(jump2, SLJIT_JUMP);

					/* Check whether old index != index. */
					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);

					EMIT_OP2U(SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_MEM1(R_NEXT_STATE), offset + 2 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);
					EMIT_JUMP(jump1, SLJIT_LESS);
					EMIT_JUMP(jump3, SLJIT_NOT_EQUAL); /* Greater. */

					/* Old index == index. */
					EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(curr_index, 3));
					if (search_states[value].value > 0) {
						EMIT_CMP(jump4, SLJIT_GREATER, R_TEMP, 0, SLJIT_IMM, search_states[value].value);

						EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_IMM, search_states[value].value);
						EMIT_LABEL(label1);
						sljit_set_label(jump4, label1);
					}

					EMIT_OP2U(SLJIT_SUB | SLJIT_SET_GREATER_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + 3 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);
					EMIT_JUMP(jump4, SLJIT_GREATER_EQUAL);
					EMIT_JUMP(jump5, SLJIT_JUMP);

					/* Overwrite index & id. */
					EMIT_LABEL(label1);
					sljit_set_label(jump3, label1);
					sljit_set_label(jump2, label1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + 2 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);

					EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(curr_index, 3));
					if (search_states[value].value > 0) {
						EMIT_CMP(jump3, SLJIT_GREATER, R_TEMP, 0, SLJIT_IMM, search_states[value].value);

						EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_IMM, search_states[value].value);
						EMIT_LABEL(label1);
						sljit_set_label(jump3, label1);
					}

					EMIT_LABEL(label1);
					sljit_set_label(jump5, label1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + 3 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);

					/* Exit. */
					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);
					sljit_set_label(jump4, label1);
				}
				else {
					EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(curr_index, 2));

					if (search_states[value].value > 0) {
						EMIT_CMP(jump1, SLJIT_GREATER, R_TEMP, 0, SLJIT_IMM, search_states[value].value);

						EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_IMM, search_states[value].value);
						EMIT_LABEL(label1);
						sljit_set_label(jump1, label1);
					}

					/* Check whether item is inserted. */
					EMIT_CMP(jump1, SLJIT_NOT_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), SLJIT_IMM, -1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + (sljit_sw)sizeof(sljit_sw), R_NEXT_HEAD, 0);
					if (offset > 0) {
						EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, offset);
					}
					EMIT_JUMP(jump2, SLJIT_JUMP);

					/* Check whether old id >= id. */
					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);

					EMIT_CMP(jump1, SLJIT_GREATER_EQUAL, SLJIT_MEM1(R_NEXT_STATE), offset + 2 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);

					EMIT_LABEL(label1);
					sljit_set_label(jump2, label1);
					EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), offset + 2 * (sljit_sw)sizeof(sljit_sw), R_TEMP, 0);

					EMIT_LABEL(label1);
					sljit_set_label(jump1, label1);
				}
			}
		}
		search_states[value].value = -1;
	}

#ifdef REGEX_MATCH_VERBOSE
	if (flags & REGEX_MATCH_VERBOSE)
		printf("\n");
#endif
	return REGEX_NO_ERROR;
}

static int compile_end_check(struct compiler_common *compiler_common, struct sljit_label *end_check_label)
{
	struct sljit_compiler *compiler = compiler_common->compiler;
	struct sljit_jump *jump;
	struct sljit_jump *clear_states_jump;
	struct sljit_label *label;
	struct sljit_label *leave_label;
	struct sljit_label *begin_loop_label;

	/* Priority order: best_begin > best_end > best_id.
	   In other words:
	       if (new best_begin > old test_begin) do nothing
	       otherwise we know that new_end > old_end, since R_CURR_INDEX ever increasing
	       therefore we must overwrite all best_* variables (new_id also contains the highest id for this turn). */

	/* Both R_CURR_CHAR and R_BEST_BEGIN used as temporary registers. */

	if (!(compiler_common->flags & REGEX_MATCH_BEGIN)) {
		EMIT_OP1(SLJIT_MOV, R_CURR_CHAR, 0, SLJIT_MEM1(R_CURR_STATE), TERM_REL_OFFSET_OF(0, 2));
		EMIT_CMP(jump, !(compiler_common->flags & REGEX_MATCH_NON_GREEDY) ? SLJIT_LESS : SLJIT_LESS_EQUAL, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_begin), R_CURR_CHAR, 0);
		sljit_set_label(jump, end_check_label);

		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_begin), R_CURR_CHAR, 0);
		if (!(compiler_common->flags & (REGEX_FAKE_MATCH_BEGIN | REGEX_FAKE_MATCH_END))) {
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_end), R_CURR_INDEX, 0);
		}
		else {
			if ((compiler_common->flags & (REGEX_FAKE_MATCH_BEGIN | REGEX_FAKE_MATCH_END)) == (REGEX_FAKE_MATCH_BEGIN | REGEX_FAKE_MATCH_END)) {
				EMIT_OP2(SLJIT_SUB, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_end), R_CURR_INDEX, 0, SLJIT_IMM, 2);
			}
			else {
				EMIT_OP2(SLJIT_SUB, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_end), R_CURR_INDEX, 0, SLJIT_IMM, 1);
			}
		}
		if (compiler_common->flags & REGEX_ID_CHECK) {
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_id), SLJIT_MEM1(R_CURR_STATE), TERM_REL_OFFSET_OF(0, 3));
		}

		EMIT_CMP(clear_states_jump, SLJIT_LESS, R_CURR_CHAR, 0, R_BEST_BEGIN, 0);

		EMIT_LABEL(leave_label);
		EMIT_OP1(SLJIT_MOV, R_BEST_BEGIN, 0, R_CURR_CHAR, 0);
		EMIT_JUMP(jump, SLJIT_JUMP);
		sljit_set_label(jump, end_check_label);

		/* A loop to clear all states, which are > (or >=) than R_CURR_CHAR. */
		EMIT_LABEL(label);
		sljit_set_label(clear_states_jump, label);

		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_NEXT_HEAD, 0);
		EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, 0);

		/* Begin of the loop. */
		EMIT_LABEL(begin_loop_label);
		EMIT_CMP(jump, SLJIT_EQUAL, R_TEMP, 0, SLJIT_IMM, 0);
		sljit_set_label(jump, leave_label);

		EMIT_OP2(SLJIT_ADD, R_TEMP, 0, R_TEMP, 0, R_CURR_STATE, 0);
		EMIT_OP1(SLJIT_MOV, R_BEST_BEGIN, 0, SLJIT_MEM1(R_TEMP), sizeof(sljit_sw));
		EMIT_CMP(clear_states_jump, !(compiler_common->flags & REGEX_MATCH_NON_GREEDY) ? SLJIT_GREATER : SLJIT_GREATER_EQUAL, SLJIT_MEM1(R_TEMP), 2 * sizeof(sljit_sw), R_CURR_CHAR, 0);

		/* Case 1: keep this case. */
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_TEMP), sizeof(sljit_sw), R_NEXT_HEAD, 0);
		EMIT_OP2(SLJIT_SUB, R_NEXT_HEAD, 0, R_TEMP, 0, R_CURR_STATE, 0);

		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_BEST_BEGIN, 0);
		EMIT_JUMP(jump, SLJIT_JUMP);
		sljit_set_label(jump, begin_loop_label);

		/* Case 2: remove this case. */
		EMIT_LABEL(label);
		sljit_set_label(clear_states_jump, label);

		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_TEMP), sizeof(sljit_sw), SLJIT_IMM, -1);

		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_BEST_BEGIN, 0);
		EMIT_JUMP(jump, SLJIT_JUMP);
		sljit_set_label(jump, begin_loop_label);
	}
	else {
		EMIT_OP1(SLJIT_MOV, R_BEST_BEGIN, 0, SLJIT_IMM, 0);
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_begin), SLJIT_IMM, 0);
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_end), R_CURR_INDEX, 0);
		if (compiler_common->flags & REGEX_ID_CHECK) {
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_id), SLJIT_MEM1(R_CURR_STATE), TERM_REL_OFFSET_OF(0, 2));
		}
		EMIT_JUMP(jump, SLJIT_JUMP);
		sljit_set_label(jump, end_check_label);
	}
	return REGEX_NO_ERROR;
}

static int compile_leave_fast_forward(struct compiler_common *compiler_common, struct sljit_label *fast_forward_label)
{
	struct sljit_compiler *compiler = compiler_common->compiler;
	struct stack *stack = &compiler_common->stack;
	struct stack_item *dfa_transitions = compiler_common->dfa_transitions;
	struct stack_item *search_states = compiler_common->search_states;
	int ind;
	struct sljit_jump *jump;
	int init_range = 1, prev_value = 0;

	while (stack->count > 0) {
		ind = stack_pop(stack)->value;
		search_states[ind].value = -1;
		if (search_states[ind].type >= 0) {
			if (dfa_transitions[ind].type == type_char) {
				EMIT_CMP(jump, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, dfa_transitions[ind].value);
				sljit_set_label(jump, fast_forward_label);
			}
			else if (dfa_transitions[ind].type == type_rng_start) {
				SLJIT_ASSERT(!dfa_transitions[ind].value);
				ind++;
				while (dfa_transitions[ind].type != type_rng_end) {
					if (dfa_transitions[ind].type == type_rng_char) {
						EMIT_CMP(jump, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, dfa_transitions[ind].value);
						sljit_set_label(jump, fast_forward_label);
					}
					else {
						SLJIT_ASSERT(dfa_transitions[ind].type == type_rng_left);
						if (init_range) {
							EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_CURR_CHAR, 0);
							init_range = 0;
						}
						if (dfa_transitions[ind].value != prev_value) {
							/* Best compatibility to all archs. */
							prev_value -= dfa_transitions[ind].value;
							if (prev_value < 0) {
								EMIT_OP2(SLJIT_SUB, R_TEMP, 0, R_TEMP, 0, SLJIT_IMM, -prev_value);
							}
							else {
								EMIT_OP2(SLJIT_ADD, R_TEMP, 0, R_TEMP, 0, SLJIT_IMM, prev_value);
							}
							prev_value = dfa_transitions[ind].value;
						}
						EMIT_CMP(jump, SLJIT_LESS_EQUAL, R_TEMP, 0, SLJIT_IMM, dfa_transitions[ind + 1].value - dfa_transitions[ind].value);
						sljit_set_label(jump, fast_forward_label);
						ind++;
					}
					ind++;
				}
			}
			else {
				SLJIT_ASSERT(dfa_transitions[ind].type == type_newline);
				EMIT_CMP(jump, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, '\n');
				sljit_set_label(jump, fast_forward_label);
				EMIT_CMP(jump, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, '\r');
				sljit_set_label(jump, fast_forward_label);
			}
		}
	}
	return REGEX_NO_ERROR;
}

static int compile_newline_check(struct compiler_common *compiler_common, sljit_sw ind)
{
	struct sljit_compiler *compiler = compiler_common->compiler;
	struct sljit_jump *jump1;
	struct sljit_jump *jump2;
	struct sljit_label *label;
	sljit_sw no_states;
	sljit_sw offset;

	/* Check whether a new-line character is found. */
	EMIT_CMP(jump1, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, '\n');
	EMIT_CMP(jump2, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, '\r');

	no_states = compiler_common->no_states;
	offset = TERM_OFFSET_OF(compiler_common->search_states[ind].type, 1);
	EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), offset);
	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_CURR_STATE), offset, SLJIT_IMM, -1);
	CHECK(sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_MEM2(R_CURR_STATE, R_TEMP), 0));

	EMIT_LABEL(label);
	sljit_set_label(jump1, label);
	sljit_set_label(jump2, label);
	return REGEX_NO_ERROR;
}

#undef CHECK

#define CHECK(exp) \
	if (SLJIT_UNLIKELY(exp)) \
		return 0

static SLJIT_INLINE void range_set_label(struct sljit_jump **range_jump_list, struct sljit_label *label)
{
	while (*range_jump_list) {
		sljit_set_label(*range_jump_list, label);
		range_jump_list++;
	}
}

static sljit_sw compile_range_check(struct compiler_common *compiler_common, sljit_sw ind)
{
	struct sljit_compiler *compiler = compiler_common->compiler;
	struct stack_item *dfa_transitions = compiler_common->dfa_transitions;
	struct sljit_jump **range_jump_list = compiler_common->range_jump_list;
	int invert = dfa_transitions[ind].value;
	struct sljit_label *label;
	sljit_sw no_states;
	sljit_sw offset;
	int init_range = 1, prev_value = 0;

	ind++;

	while (dfa_transitions[ind].type != type_rng_end) {
		if (dfa_transitions[ind].type == type_rng_char) {
			EMIT_CMP(*range_jump_list, SLJIT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, dfa_transitions[ind].value);
			range_jump_list++;
		}
		else {
			SLJIT_ASSERT(dfa_transitions[ind].type == type_rng_left);
			if (init_range) {
				EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_CURR_CHAR, 0);
				init_range = 0;
			}
			if (dfa_transitions[ind].value != prev_value) {
				/* Best compatibility to all archs. */
				prev_value -= dfa_transitions[ind].value;
				if (prev_value < 0) {
					EMIT_OP2(SLJIT_SUB, R_TEMP, 0, R_TEMP, 0, SLJIT_IMM, -prev_value);
				}
				else {
					EMIT_OP2(SLJIT_ADD, R_TEMP, 0, R_TEMP, 0, SLJIT_IMM, prev_value);
				}
				prev_value = dfa_transitions[ind].value;
			}
			EMIT_CMP(*range_jump_list, SLJIT_LESS_EQUAL, R_TEMP, 0, SLJIT_IMM, dfa_transitions[ind + 1].value - dfa_transitions[ind].value);
			range_jump_list++;
			ind++;
		}
		ind++;
	}

	*range_jump_list = NULL;

	if (!invert) {
		no_states = compiler_common->no_states;
		offset = TERM_OFFSET_OF(compiler_common->search_states[ind].type, 1);
		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), offset);
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_CURR_STATE), offset, SLJIT_IMM, -1);
		CHECK(sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_MEM2(R_CURR_STATE, R_TEMP), 0));

		EMIT_LABEL(label);
		range_set_label(compiler_common->range_jump_list, label);
		/* Clears the jump list. */
		*compiler_common->range_jump_list = NULL;
	}
	return ind;
}

#undef TERM_OFFSET_OF
#undef EMIT_OP1
#undef EMIT_OP2
#undef EMIT_LABEL
#undef EMIT_JUMP
#undef EMIT_CMP
#undef CHECK

/* --------------------------------------------------------------------- */
/*  Main compiler                                                        */
/* --------------------------------------------------------------------- */

#define TERM_OFFSET_OF(ind, offs) (((ind) * compiler_common.no_states + (offs)) * (sljit_sw)sizeof(sljit_sw))

#define EMIT_OP1(type, arg1, arg2, arg3, arg4) \
	CHECK(sljit_emit_op1(compiler_common.compiler, type, arg1, arg2, arg3, arg4))

#define EMIT_OP2(type, arg1, arg2, arg3, arg4, arg5, arg6) \
	CHECK(sljit_emit_op2(compiler_common.compiler, type, arg1, arg2, arg3, arg4, arg5, arg6))

#define EMIT_LABEL(label) \
	label = sljit_emit_label(compiler_common.compiler); \
	CHECK(!label)

#define EMIT_JUMP(jump, type) \
	jump = sljit_emit_jump(compiler_common.compiler, type); \
	CHECK(!jump)

#define EMIT_CMP(jump, type, arg1, arg2, arg3, arg4) \
	jump = sljit_emit_cmp(compiler_common.compiler, type, arg1, arg2, arg3, arg4); \
	CHECK(!jump)

/* A do {} while(0) expression helps to avoid goto statements. */
#define BEGIN_GUARD \
	do {

#define END_GUARD \
	} while(0);

#define CHECK(exp) \
	if (SLJIT_UNLIKELY(exp)) \
		break;

struct regex_machine* regex_compile(const regex_char_t *regex_string, int length, int re_flags, int *error)
{
	struct compiler_common compiler_common;
	sljit_sw ind;
	int error_code, done, suggest_fast_forward;
	/* ID of an empty match (-1 if not reachable). */
	int empty_match_id;

	struct sljit_jump *jump;
	struct sljit_jump *best_match_found_jump;
	struct sljit_jump *fast_forward_jump = NULL;
	struct sljit_jump *length_is_zero_jump;
	struct sljit_jump *end_check_jump = NULL;
	struct sljit_jump *best_match_check_jump = NULL;
	struct sljit_jump *non_greedy_end_jump = NULL;
	struct sljit_label *label;
	struct sljit_label *end_check_label = NULL;
	struct sljit_label *start_label;
	struct sljit_label *fast_forward_label;
	struct sljit_label *fast_forward_return_label;

	if (error)
		*error = REGEX_NO_ERROR;
#ifdef REGEX_MATCH_VERBOSE
	compiler_common.flags = re_flags & (REGEX_MATCH_BEGIN | REGEX_MATCH_END | REGEX_MATCH_NON_GREEDY | REGEX_NEWLINE | REGEX_MATCH_VERBOSE);
#else
	compiler_common.flags = re_flags & (REGEX_MATCH_BEGIN | REGEX_MATCH_END | REGEX_MATCH_NON_GREEDY | REGEX_NEWLINE);
#endif

	/* Step 1: parsing (Left->Right).
	   Syntax check and AST generator. */
	error_code = parse(regex_string, length, &compiler_common);
	if (error_code) {
		stack_destroy(&compiler_common.stack);
		if (error)
			*error = error_code;
		return NULL;
	}

	/* Step 2: generating branches (Right->Left). */
	error_code = generate_transitions(&compiler_common);
	stack_destroy(&compiler_common.stack);
	stack_destroy(&compiler_common.depth);
	if (error_code) {
		if (compiler_common.dfa_transitions)
			SLJIT_FREE(compiler_common.dfa_transitions, NULL);
		if (error)
			*error = error_code;
		return NULL;
	}

	/* Step 3: Generate necessary data for depth-first search (Left->Right). */
	error_code = generate_search_states(&compiler_common);
	if (error_code) {
		SLJIT_FREE(compiler_common.dfa_transitions, NULL);
		if (error)
			*error = error_code;
		return NULL;
	}

#ifdef REGEX_MATCH_VERBOSE
	if (compiler_common.flags & REGEX_MATCH_VERBOSE)
		verbose_transitions(&compiler_common);
#endif

	/* Step 4: Left->Right generate code. */
	stack_init(&compiler_common.stack);
	stack_init(&compiler_common.depth);
	done = 0;
	compiler_common.machine = NULL;
	compiler_common.compiler = NULL;
	compiler_common.range_jump_list = NULL;

	BEGIN_GUARD

	compiler_common.machine = (struct regex_machine*)SLJIT_MALLOC(sizeof(struct regex_machine) + (sljit_uw)(compiler_common.terms_size - 1) * sizeof(sljit_uw), NULL);
	CHECK(!compiler_common.machine);

	compiler_common.compiler = sljit_create_compiler(NULL, NULL);
	CHECK(!compiler_common.compiler);

	if (compiler_common.longest_range_size > 0) {
		compiler_common.range_jump_list = (struct sljit_jump**)SLJIT_MALLOC(sizeof(struct sljit_jump*) * (sljit_uw)compiler_common.longest_range_size, NULL);
		CHECK(!compiler_common.range_jump_list);
	}

	if ((compiler_common.flags & REGEX_ID_CHECK) && !(compiler_common.flags & REGEX_MATCH_BEGIN))
		compiler_common.no_states = 4;
	else if (!(compiler_common.flags & REGEX_ID_CHECK) && (compiler_common.flags & REGEX_MATCH_BEGIN))
		compiler_common.no_states = 2;
	else
		compiler_common.no_states = 3;

	compiler_common.machine->flags = compiler_common.flags;
	compiler_common.machine->no_states = compiler_common.no_states;
	compiler_common.machine->size = compiler_common.machine->no_states * compiler_common.terms_size;

	/* Study the regular expression. */
	empty_match_id = -1;
	suggest_fast_forward = 1;
	if (!(compiler_common.flags & REGEX_FAKE_MATCH_BEGIN)) {
		CHECK(trace_transitions(0, &compiler_common));
		while (compiler_common.stack.count > 0) {
			ind = stack_pop(&compiler_common.stack)->value;
			if (compiler_common.search_states[ind].type == 0) {
				SLJIT_ASSERT(compiler_common.dfa_transitions[ind].type == type_end);
				suggest_fast_forward = 0;
				empty_match_id = compiler_common.search_states[ind].value;
			}
			else if (compiler_common.search_states[ind].type > 0) {
				SLJIT_ASSERT(compiler_common.dfa_transitions[ind].type != type_end);
				if (compiler_common.dfa_transitions[ind].type == type_rng_start && compiler_common.dfa_transitions[ind].value)
					suggest_fast_forward = 0;
			}
			compiler_common.search_states[ind].value = -1;
		}
	}
	else {
		SLJIT_ASSERT(compiler_common.dfa_transitions[1].type == type_newline);
		CHECK(trace_transitions(1, &compiler_common));
		while (compiler_common.stack.count > 0) {
			ind = stack_pop(&compiler_common.stack)->value;
			if (compiler_common.search_states[ind].type == 0) {
				SLJIT_ASSERT(compiler_common.dfa_transitions[ind].type == type_end);
				suggest_fast_forward = 0;
				empty_match_id = compiler_common.search_states[ind].value;
			}
			compiler_common.search_states[ind].value = -1;
		}
	}

	/* Step 4.1: Generate entry. */
	CHECK(sljit_emit_enter(compiler_common.compiler, 0, SLJIT_ARGS3(VOID, P, P, 32), 5, 5, 0, 0, 0));

	/* Copy arguments to their place. */
	EMIT_OP1(SLJIT_MOV, R_REGEX_MATCH, 0, SLJIT_S0, 0);
	EMIT_OP1(SLJIT_MOV, R_STRING, 0, SLJIT_S1, 0);
	EMIT_OP2(SLJIT_ADD, R_LENGTH, 0, SLJIT_S2, 0, SLJIT_IMM, 1);

	/* Init global registers. */
	EMIT_OP1(SLJIT_MOV, R_CURR_STATE, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, current));
	EMIT_OP1(SLJIT_MOV, R_NEXT_STATE, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, next));
	EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, head));
	EMIT_OP1(SLJIT_MOV, R_BEST_BEGIN, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_begin));
	EMIT_OP1(SLJIT_MOV, R_CURR_INDEX, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, index));

	/* Check whether the best match has already found in a previous frame. */
	EMIT_CMP(jump, SLJIT_EQUAL, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_quit), SLJIT_IMM, 0);
	EMIT_JUMP(best_match_found_jump, SLJIT_JUMP);

#ifdef REGEX_MATCH_VERBOSE
	if (compiler_common.flags & REGEX_MATCH_VERBOSE)
		printf("\n-----------------\nTrace\n-----------------\n");
#endif

	/* Step 4.2: Generate code for state 0. */
	EMIT_LABEL(label);
	sljit_emit_op0(compiler_common.compiler, SLJIT_ENDBR);
	compiler_common.machine->entry_addrs[0] = (sljit_uw)label;

	/* Swapping current and next. */
	EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_CURR_STATE, 0);
	EMIT_OP1(SLJIT_MOV, R_CURR_STATE, 0, R_NEXT_STATE, 0);
	EMIT_OP1(SLJIT_MOV, R_NEXT_STATE, 0, R_TEMP, 0);

	/* Checking whether the best case needs to be updated. */
	if (!(compiler_common.flags & REGEX_MATCH_END)) {
		EMIT_CMP(end_check_jump, SLJIT_NOT_EQUAL, SLJIT_MEM1(R_CURR_STATE), TERM_REL_OFFSET_OF(0, 1), SLJIT_IMM, -1);
		EMIT_LABEL(end_check_label);
	}
	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_NEXT_STATE), TERM_REL_OFFSET_OF(0, 1), SLJIT_IMM, -1);
	EMIT_OP2(SLJIT_ADD, R_CURR_INDEX, 0, R_CURR_INDEX, 0, SLJIT_IMM, 1);

	/* Checking whether best case has already found. */
	if (!(compiler_common.flags & REGEX_MATCH_END) || (compiler_common.flags & REGEX_MATCH_BEGIN)) {
		if (!(compiler_common.flags & REGEX_MATCH_BEGIN)) {
			/* We can bail out if no more active states remain and R_BEST_BEGIN != -1. */
			EMIT_CMP(best_match_check_jump, SLJIT_NOT_EQUAL, R_BEST_BEGIN, 0, SLJIT_IMM, -1);
		}
		else {
			/* We can bail out if no more active states remain (regardless of R_BEST_BEGIN). */
			EMIT_CMP(best_match_check_jump, SLJIT_EQUAL, R_NEXT_HEAD, 0, SLJIT_IMM, 0);
		}
	}

	EMIT_LABEL(start_label);
	sljit_set_label(jump, start_label);

	if (!(compiler_common.flags & REGEX_MATCH_BEGIN) && suggest_fast_forward) {
		EMIT_CMP(fast_forward_jump, SLJIT_NOT_EQUAL, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_forward), SLJIT_IMM, 0);
	}

	/* Loading the next character. */
	EMIT_OP2(SLJIT_SUB | SLJIT_SET_Z, R_LENGTH, 0, R_LENGTH, 0, SLJIT_IMM, 1);
	EMIT_JUMP(length_is_zero_jump, SLJIT_EQUAL);

	EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_STRING, 0);
#ifdef REGEX_USE_8BIT_CHARS
	EMIT_OP1(SLJIT_MOV_U8, R_CURR_CHAR, 0, SLJIT_MEM1(R_TEMP), 0);
	EMIT_OP2(SLJIT_ADD, R_TEMP, 0, R_TEMP, 0, SLJIT_IMM, 1);
#else
	EMIT_OP1(SLJIT_MOV_UH, R_CURR_CHAR, 0, SLJIT_MEM1(R_TEMP), 0);
	EMIT_OP2(SLJIT_ADD, R_TEMP, 0, R_TEMP, 0, SLJIT_IMM, 2);
#endif
	EMIT_OP1(SLJIT_MOV, R_STRING, 0, R_TEMP, 0);

#ifdef REGEX_MATCH_VERBOSE
	if (compiler_common.flags & REGEX_MATCH_VERBOSE) {
		printf("(%3d): ", 0);
		CHECK(trace_transitions(0, &compiler_common));
		while (compiler_common.stack.count > 0) {
			ind = stack_pop(&compiler_common.stack)->value;
			if (compiler_common.search_states[ind].type >= 0)
				printf("-> (%3d:%3d) ", compiler_common.search_states[ind].type, compiler_common.search_states[ind].value);
			compiler_common.search_states[ind].value = -1;
		}
		printf("\n");
	}
#endif

	EMIT_LABEL(fast_forward_return_label);
	if (!(compiler_common.flags & REGEX_MATCH_BEGIN)) {
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_forward), SLJIT_IMM, 1);
		if (!(compiler_common.flags & REGEX_MATCH_END)) {
			EMIT_CMP(jump, SLJIT_NOT_EQUAL, R_BEST_BEGIN, 0, SLJIT_IMM, -1);
		}

		EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_CURR_INDEX, 0);
		CHECK(compile_uncond_tran(&compiler_common, R_NEXT_STATE));
		/* And branching to the first state. */
		CHECK(sljit_emit_ijump(compiler_common.compiler, SLJIT_JUMP, SLJIT_MEM2(R_CURR_STATE, R_TEMP), 0));

		if (!(compiler_common.flags & REGEX_MATCH_END)) {
			EMIT_LABEL(label);
			sljit_set_label(jump, label);
		}
	}
	/* This is the case where we only have to reset the R_NEXT_HEAD. */
	EMIT_OP1(SLJIT_MOV, R_TEMP, 0, R_NEXT_HEAD, 0);
	EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, 0);
	CHECK(sljit_emit_ijump(compiler_common.compiler, SLJIT_JUMP, SLJIT_MEM2(R_CURR_STATE, R_TEMP), 0));

	/* Fast-forward loop. */
	if (fast_forward_jump) {
		/* Quit from fast-forward loop. */
		EMIT_LABEL(fast_forward_label);
		EMIT_OP2(SLJIT_SUB, R_TEMP, 0, R_NEXT_HEAD, 0, SLJIT_IMM, 1);
		EMIT_OP1(SLJIT_MOV, R_LENGTH, 0, R_NEXT_STATE, 0);
		EMIT_OP1(SLJIT_MOV, R_STRING, 0, R_CURR_STATE, 0);
		EMIT_OP1(SLJIT_MOV, R_CURR_INDEX, 0, R_NEXT_HEAD, 0);
		EMIT_OP1(SLJIT_MOV, R_NEXT_STATE, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, next));
		EMIT_OP1(SLJIT_MOV, R_CURR_STATE, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, current));
		EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, head));

		/* Update the start field of the locations. */
		CHECK(trace_transitions(0, &compiler_common));
		while (compiler_common.stack.count > 0) {
			ind = stack_pop(&compiler_common.stack)->value;
			if (compiler_common.search_states[ind].type >= 0) {
				EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(compiler_common.search_states[ind].type, 2), R_TEMP, 0);
			}
			compiler_common.search_states[ind].value = -1;
		}
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_forward), SLJIT_IMM, 0);
		EMIT_JUMP(jump, SLJIT_JUMP);
		sljit_set_label(jump, fast_forward_return_label);

		/* Start fast-forward. */
		EMIT_LABEL(label);
		sljit_set_label(fast_forward_jump, label);

		/* Moving everything to registers. */
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, next), R_NEXT_STATE, 0);
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, current), R_CURR_STATE, 0);
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, head), R_NEXT_HEAD, 0);
		EMIT_OP1(SLJIT_MOV, R_NEXT_STATE, 0, R_LENGTH, 0);
		EMIT_OP1(SLJIT_MOV, R_CURR_STATE, 0, R_STRING, 0);
		EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, R_CURR_INDEX, 0);

		/* Fast forward mainloop. */
		EMIT_LABEL(label);
		EMIT_OP2(SLJIT_SUB | SLJIT_SET_Z, R_NEXT_STATE, 0, R_NEXT_STATE, 0, SLJIT_IMM, 1);
		EMIT_JUMP(fast_forward_jump, SLJIT_EQUAL);

#ifdef REGEX_USE_8BIT_CHARS
		EMIT_OP1(SLJIT_MOV_U8, R_CURR_CHAR, 0, SLJIT_MEM1(R_CURR_STATE), 0);
		EMIT_OP2(SLJIT_ADD, R_CURR_STATE, 0, R_CURR_STATE, 0, SLJIT_IMM, 1);
#else
		EMIT_OP1(SLJIT_MOV_UH, R_CURR_CHAR, 0, SLJIT_MEM1(R_CURR_STATE), 0);
		EMIT_OP2(SLJIT_ADD, R_CURR_STATE, 0, R_CURR_STATE, 0, SLJIT_IMM, 2);
#endif

		CHECK(trace_transitions(0, &compiler_common));
		CHECK(compile_leave_fast_forward(&compiler_common, fast_forward_label));

		EMIT_OP2(SLJIT_ADD, R_NEXT_HEAD, 0, R_NEXT_HEAD, 0, SLJIT_IMM, 1);
		EMIT_JUMP(jump, SLJIT_JUMP);
		sljit_set_label(jump, label);

		/* String is finished. */
		EMIT_LABEL(label);
		sljit_set_label(fast_forward_jump, label);
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, index), R_NEXT_HEAD, 0);
		EMIT_JUMP(fast_forward_jump, SLJIT_JUMP);
	}

	/* End check. */
	if (end_check_jump) {
		EMIT_LABEL(label);
		sljit_set_label(end_check_jump, label);

		if (!(compiler_common.flags & REGEX_MATCH_NON_GREEDY) || !(compiler_common.flags & REGEX_MATCH_BEGIN)) {
			CHECK(compile_end_check(&compiler_common, end_check_label));
		}
		else {
			/* Since we leave, we do not need to update the R_BEST_BEGIN. */
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_begin), SLJIT_IMM, 0);
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_end), R_CURR_INDEX, 0);
			if (compiler_common.flags & REGEX_ID_CHECK) {
				EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, best_id), SLJIT_MEM1(R_CURR_STATE), TERM_REL_OFFSET_OF(0, 2));
			}
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_quit), SLJIT_IMM, 1);
			EMIT_JUMP(non_greedy_end_jump, SLJIT_JUMP);
		}
	}

	/* Finish check. */
	if (best_match_check_jump) {
		EMIT_LABEL(label);
		sljit_set_label(best_match_check_jump, label);

		if (!(compiler_common.flags & REGEX_MATCH_BEGIN)) {
			EMIT_CMP(jump, SLJIT_NOT_EQUAL, R_NEXT_HEAD, 0, SLJIT_IMM, 0);
			sljit_set_label(jump, start_label);
		}
		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, fast_quit), SLJIT_IMM, 1);
	}

	/* Leaving matching and storing the necessary values. */
	EMIT_LABEL(label);
	sljit_set_label(length_is_zero_jump, label);
	if (non_greedy_end_jump)
		sljit_set_label(non_greedy_end_jump, label);

	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, index), R_CURR_INDEX, 0);
	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, head), R_NEXT_HEAD, 0);
	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, next), R_NEXT_STATE, 0);
	EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_REGEX_MATCH), SLJIT_OFFSETOF(struct regex_match, current), R_CURR_STATE, 0);

	/* Exit from JIT. */
	EMIT_LABEL(label);
	sljit_set_label(best_match_found_jump, label);
	if (fast_forward_jump)
		sljit_set_label(fast_forward_jump, label);
	CHECK(sljit_emit_return_void(compiler_common.compiler));

	for (ind = 1; ind < (sljit_sw)compiler_common.dfa_size - 1; ind++) {
		if (compiler_common.search_states[ind].type >= 0) {
			SLJIT_ASSERT(compiler_common.search_states[ind].type < compiler_common.terms_size);
			EMIT_LABEL(label);
			sljit_emit_op0(compiler_common.compiler, SLJIT_ENDBR);
			compiler_common.machine->entry_addrs[compiler_common.search_states[ind].type] = (sljit_uw)label;

			if (compiler_common.dfa_transitions[ind].type == type_char) {
				EMIT_CMP(jump, SLJIT_NOT_EQUAL, R_CURR_CHAR, 0, SLJIT_IMM, compiler_common.dfa_transitions[ind].value);
			}
			else if (compiler_common.dfa_transitions[ind].type == type_rng_start) {
				ind = compile_range_check(&compiler_common, ind);
				CHECK(!ind);
			}
			else {
				SLJIT_ASSERT(compiler_common.dfa_transitions[ind].type == type_newline);
				CHECK(compile_newline_check(&compiler_common, ind));
			}

			CHECK(trace_transitions((int)ind, &compiler_common));
#ifdef REGEX_MATCH_VERBOSE
			if (compiler_common.flags & REGEX_MATCH_VERBOSE)
				printf("(%3d): ", compiler_common.search_states[ind].type);
#endif
			CHECK(compile_cond_tran(&compiler_common, compiler_common.search_states[ind].type));

			if (compiler_common.dfa_transitions[ind].type == type_char) {
				EMIT_LABEL(label);
				sljit_set_label(jump, label);
			}
			else if (compiler_common.dfa_transitions[ind].type == type_rng_end) {
				EMIT_LABEL(label);
				range_set_label(compiler_common.range_jump_list, label);
			}
			else {
				SLJIT_ASSERT(compiler_common.dfa_transitions[ind].type == type_newline);
			}

			/* Branch to the next item in the list. */
			EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(compiler_common.search_states[ind].type, 1));
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(R_CURR_STATE), TERM_OFFSET_OF(compiler_common.search_states[ind].type, 1), SLJIT_IMM, -1);
			CHECK(sljit_emit_ijump(compiler_common.compiler, SLJIT_JUMP, SLJIT_MEM2(R_CURR_STATE, R_TEMP), 0));
		}
	}

	if (ind == (sljit_sw)compiler_common.dfa_size - 1) {
		/* Generate an init stub function. */
		EMIT_LABEL(label);
		CHECK(sljit_emit_enter(compiler_common.compiler, 0, SLJIT_ARGS2(W, P, P), 3, 3, 0, 0, 0));

		if (empty_match_id == -1) {
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), SLJIT_OFFSETOF(struct regex_match, best_begin), SLJIT_IMM, -1);
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), SLJIT_OFFSETOF(struct regex_match, best_id), SLJIT_IMM, 0);
		}
		else {
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), SLJIT_OFFSETOF(struct regex_match, best_begin), SLJIT_IMM, 0);
			EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), SLJIT_OFFSETOF(struct regex_match, best_id), SLJIT_IMM, empty_match_id);
		}

		EMIT_OP1(SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), SLJIT_OFFSETOF(struct regex_match, index), SLJIT_IMM, !(compiler_common.flags & REGEX_FAKE_MATCH_BEGIN) ? 1 : 2);

		if (!(compiler_common.flags & REGEX_MATCH_NON_GREEDY) || empty_match_id == -1) {
			/* The else is a really rare event, so we still generate an empty function instead of a runtime pointer check. */
			SLJIT_ASSERT(R_CURR_STATE == SLJIT_S0);
			if (!(compiler_common.flags & REGEX_MATCH_BEGIN)) {
				/* R_CURR_INDEX (put to R_TEMP) is zero. */
				EMIT_OP1(SLJIT_MOV, R_TEMP, 0, SLJIT_IMM, 0);
			}
			CHECK(compile_uncond_tran(&compiler_common, R_CURR_STATE));
		}
		else {
			EMIT_OP1(SLJIT_MOV, R_NEXT_HEAD, 0, SLJIT_IMM, 0);
		}
		CHECK(sljit_emit_return(compiler_common.compiler, SLJIT_MOV, R_NEXT_HEAD, 0));

		compiler_common.machine->continue_match = sljit_generate_code(compiler_common.compiler);
#ifndef SLJIT_INDIRECT_CALL
		compiler_common.machine->u.init_match = (void*)(sljit_sw)sljit_get_label_addr(label);
#else
		sljit_set_function_context(&compiler_common.machine->u.init_match, &compiler_common.machine->context, sljit_get_label_addr(label), regex_compile);
#endif
#ifdef REGEX_MATCH_VERBOSE
		if (compiler_common.flags & REGEX_MATCH_VERBOSE)
			printf("Continue match: %p Init match: %p\n\n", compiler_common.machine->continue_match, compiler_common.machine->u.init_match);
#endif
		if (compiler_common.machine->continue_match) {
			for (ind = 0; ind < compiler_common.terms_size; ++ind)
				compiler_common.machine->entry_addrs[ind] = sljit_get_label_addr((struct sljit_label*)compiler_common.machine->entry_addrs[ind]);
			done = 1;
		}
	}
	END_GUARD

	stack_destroy(&compiler_common.stack);
	stack_destroy(&compiler_common.depth);
	SLJIT_FREE(compiler_common.dfa_transitions, NULL);
	SLJIT_FREE(compiler_common.search_states, NULL);
	if (compiler_common.range_jump_list)
		SLJIT_FREE(compiler_common.range_jump_list, NULL);
	if (compiler_common.compiler)
		sljit_free_compiler(compiler_common.compiler);
	if (done)
		return compiler_common.machine;

	if (compiler_common.machine) {
		SLJIT_FREE(compiler_common.machine, NULL);
	}
	if (error)
		*error = REGEX_MEMORY_ERROR;
	return NULL;
}

#undef TERM_OFFSET_OF
#undef EMIT_OP1
#undef EMIT_OP2
#undef EMIT_LABEL
#undef EMIT_JUMP
#undef EMIT_CMP
#undef BEGIN_GUARD
#undef END_GUARD
#undef CHECK

void regex_free_machine(struct regex_machine *machine)
{
	sljit_free_code(machine->continue_match, NULL);
	SLJIT_FREE(machine, NULL);
}

const char* regex_get_platform_name(void)
{
	return sljit_get_platform_name();
}

/* --------------------------------------------------------------------- */
/*  Mathching utilities                                                  */
/* --------------------------------------------------------------------- */

struct regex_match* regex_begin_match(struct regex_machine *machine)
{
	sljit_sw *ptr1;
	sljit_sw *ptr2;
	sljit_sw *end;
	sljit_sw *entry_addrs;

	struct regex_match *match = (struct regex_match*)SLJIT_MALLOC(sizeof(struct regex_match) + (sljit_uw)(machine->size * 2 - 1) * sizeof(sljit_sw), NULL);
	if (!match)
		return NULL;

	ptr1 = match->states;
	ptr2 = match->states + machine->size;
	end = ptr2;
	entry_addrs = (sljit_sw*)machine->entry_addrs;

	match->current = ptr1;
	match->next = ptr2;
	match->head = 0;
	match->machine = machine;

	/* Init machine states. */
	switch (machine->no_states) {
	case 2:
		while (ptr1 < end) {
			*ptr1++ = *entry_addrs;
			*ptr2++ = *entry_addrs++;
			*ptr1++ = -1;
			*ptr2++ = -1;
		}
		break;

	case 3:
		while (ptr1 < end) {
			*ptr1++ = *entry_addrs;
			*ptr2++ = *entry_addrs++;
			*ptr1++ = -1;
			*ptr2++ = -1;
			*ptr1++ = 0;
			*ptr2++ = 0;
		}
		break;

	case 4:
		while (ptr1 < end) {
			*ptr1++ = *entry_addrs;
			*ptr2++ = *entry_addrs++;
			*ptr1++ = -1;
			*ptr2++ = -1;
			*ptr1++ = 0;
			*ptr2++ = 0;
			*ptr1++ = 0;
			*ptr2++ = 0;
		}
		break;

	default:
		SLJIT_UNREACHABLE();
		break;
	}

	SLJIT_ASSERT(ptr1 == end);

	match->u.continue_match = machine->continue_match;

	regex_reset_match(match);
	return match;
}

void regex_reset_match(struct regex_match *match)
{
	struct regex_machine *machine = match->machine;
	sljit_sw current, ind;
	sljit_sw *current_ptr;

	match->best_end = 0;
	match->fast_quit = 0;
	match->fast_forward = 0;

	if (match->head != 0) {
		/* Clear the current state. */
		current = match->head;
		current_ptr = match->current;
		do {
			ind = (current / (sljit_sw)sizeof(sljit_sw)) + 1;
			current = current_ptr[ind];
			current_ptr[ind] = -1;
		} while (current != 0);
	}
	match->head = machine->u.call_init(match->current, match);
}

void regex_free_match(struct regex_match *match)
{
	SLJIT_FREE(match, NULL);
}

void regex_continue_match(struct regex_match *match, const regex_char_t *input_string, int length)
{
	match->u.call_continue(match, input_string, length);
}

int regex_get_result(struct regex_match *match, int *end, int *id)
{
	int flags = match->machine->flags;
	sljit_sw no_states;

	*end = (int)match->best_end;
	*id = (int)match->best_id;
	if (!(flags & (REGEX_MATCH_END | REGEX_FAKE_MATCH_END)))
		return (int)match->best_begin;

	if (flags & REGEX_FAKE_MATCH_END) {
		SLJIT_ASSERT(!(flags & (REGEX_MATCH_BEGIN | REGEX_MATCH_END)));
		if (match->best_begin != -1)
			return (int)match->best_begin;

		no_states = match->machine->no_states;
		if (match->current[no_states + 1] == -1)
			return -1;
		if (flags & REGEX_ID_CHECK)
			*id = (int)match->current[no_states + 3];
		if (!(flags & REGEX_FAKE_MATCH_BEGIN))
			*end = (int)match->index - 1;
		else
			*end = (int)match->index - 2;
		return (int)match->current[no_states + 2];
	}
	else {
		/* Check the status of the last code. */
		if (!(flags & REGEX_MATCH_BEGIN)) {
			/* No shortcut in this case. */
			if (!(flags & REGEX_ID_CHECK)) {
				if (match->current[1] == -1)
					return -1;
				*end = (int)match->index - 1;
				return (int)match->current[2];
			}

			if (match->current[1] == -1)
				return -1;
			*end = (int)match->index - 1;
			*id = (int)match->current[3];
			return (int)match->current[2];
		}

		/* Shortcut is possible in this case. */
		if (!(flags & REGEX_ID_CHECK)) {
			if (match->current[1] == -1 || match->head == -1)
				return -1;
			*end = (int)match->index - 1;
			return 0;
		}

		if (match->current[1] == -1 || match->head == -1)
			return -1;
		*end = (int)match->index - 1;
		*id = (int)match->current[2];
		return 0;
	}
}

int regex_is_match_finished(struct regex_match *match)
{
	return (int)match->fast_quit;
}

#ifdef REGEX_MATCH_VERBOSE
void regex_continue_match_debug(struct regex_match *match, const regex_char_t *input_string, int length)
{
	sljit_sw *ptr;
	sljit_sw *end;
	sljit_sw count;
#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
	sljit_sw current;
#endif
	sljit_sw no_states = match->machine->no_states;
	sljit_sw len = match->machine->size;

	while (length > 0) {
		match->u.call_continue(match, input_string, 1);

		if (match->fast_forward) {
			if (match->machine->flags & REGEX_MATCH_VERBOSE)
				printf("fast forward\n");
		}

		/* Verbose (first). */
		if (match->machine->flags & REGEX_MATCH_VERBOSE) {
			ptr = match->current;
			end = ptr + len;
			count = 0;
			printf("'%c' (%3ld->%3ld [%3ld]) ", *input_string, (long)match->best_begin, (long)match->best_end, (long)match->best_id);
			while (ptr < end) {
				printf("[%3ld:", (long)count++);
				switch (no_states) {
				case 2:
					if (ptr[1] != -1)
						printf("+] ");
					else
						printf(" ] ");
					break;

				case 3:
					if (ptr[1] != -1)
						printf("+,%3ld] ", (long)ptr[2]);
					else
						printf(" ,XXX] ");
					break;

				case 4:
					if (ptr[1] != -1)
						printf("+,%3ld,%3ld] ", (long)ptr[2], (long)ptr[3]);
					else
						printf(" ,XXX,XXX] ");
					break;
				}
				ptr += no_states;
			}
			printf("\n");
		}

#if (defined SLJIT_DEBUG && SLJIT_DEBUG)
		/* Sanity check (later). */
		ptr = match->next;
		end = ptr + len;
		while (ptr < end) {
			SLJIT_ASSERT(ptr[1] == -1);
			ptr += no_states;
		}

		/* Check number of active elements. */
		ptr = match->current + no_states;
		end = ptr + len - no_states;
		count = 0;
		while (ptr < end) {
			if (ptr[1] != -1)
				count++;
			ptr += no_states;
		}

		/* Check chain list. */
		current = match->head;
		ptr = match->current;
		while (current != 0) {
			SLJIT_ASSERT(current >= 0 && current < len * (sljit_sw)sizeof(sljit_sw));
			SLJIT_ASSERT((current % (no_states * (sljit_sw)sizeof(sljit_sw))) == 0);
			SLJIT_ASSERT(count > 0);
			current = ptr[(current / (sljit_sw)sizeof(sljit_sw)) + 1];
			count--;
		}
		SLJIT_ASSERT(count == 0);
#endif

		if (match->fast_quit) {
			/* the machine has stopped working. */
			if (match->machine->flags & REGEX_MATCH_VERBOSE)
				printf("Best match has found\n");
			break;
		}

		input_string++;
		length--;
	}
}
#endif
