/*
 Copyright 2015 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include <stdlib.h>
#include <stdint.h>
#include "arena.h"

struct arena {
	void **used_blocks;
	void **free_blocks;

	size_t used_count;
	size_t used_storage;
	size_t free_count;
	size_t free_storage;
};

#define DEFAULT_STORAGE_SIZE 8

static arena *current_arena;

arena *get_current_arena()
{
	if (!current_arena) {
		current_arena = calloc(1, sizeof(arena));
	}
	return current_arena;
}

arena *set_current_arena(arena *a)
{
	arena *tmp = current_arena;
	current_arena = a;
	return tmp;
}

arena *start_new_arena()
{
	arena *tmp = current_arena;
	current_arena = NULL;
	return tmp;
}

void track_block(void *block)
{
	arena *cur = get_current_arena();
	if (cur->used_count == cur->used_storage) {
		if (cur->used_storage) {
			cur->used_storage *= 2;
		} else {
			cur->used_storage = DEFAULT_STORAGE_SIZE;
		}
		cur->used_blocks = realloc(cur->used_blocks, cur->used_storage * sizeof(void *));
	}
	cur->used_blocks[cur->used_count++] = block;
}

void mark_all_free()
{
	arena *cur = get_current_arena();
	if (!cur->free_blocks) {
		cur->free_blocks = cur->used_blocks;
		cur->free_storage = cur->used_storage;
		cur->free_count = cur->used_count;
		cur->used_count = cur->used_storage = 0;
		cur->used_blocks = NULL;
	} else {
		if (cur->free_storage < cur->used_count + cur->free_count) {
			cur->free_storage = cur->used_count + cur->free_count;
			cur->free_blocks = realloc(cur->free_blocks, cur->free_storage * sizeof(void*));
		}
		for (; cur->used_count > 0; cur->used_count--)
		{
			cur->free_blocks[cur->free_count++] = cur->used_blocks[cur->used_count-1];
		}
	}
}

void *try_alloc_arena()
{
	if (!current_arena || !current_arena->free_count) {
		return NULL;
	}
	void *ret = current_arena->free_blocks[--current_arena->free_count];
	track_block(ret);
	return ret;
}
