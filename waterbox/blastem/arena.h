/*
 Copyright 2015 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef ARENA_H_
#define ARENA_H_

typedef struct arena arena;

arena *get_current_arena();
arena *set_current_arena(arena *a);
arena *start_new_arena();
void track_block(void *block);
void mark_all_free();
void *try_alloc_arena();

#endif //ARENA_H_
