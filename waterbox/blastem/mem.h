/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm. 
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef MEM_H_
#define MEM_H_

#include <stddef.h>

#define PAGE_SIZE 4096

void * alloc_code(size_t *size);

#endif //MEM_H_

