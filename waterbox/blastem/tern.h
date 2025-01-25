/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef TERN_H_
#define TERN_H_

#include <stdint.h>

#define MAX_INT_KEY_SIZE (sizeof(uint32_t) + 2)

typedef union {
	void     *ptrval;
	intptr_t intval;
} tern_val;

typedef struct tern_node {
	struct tern_node *left;
	union {
		struct tern_node *next;
		tern_val         value;
	} straight;
	struct tern_node *right;
	char             el;
	uint8_t          valtype;
} tern_node;

enum {
	TVAL_NONE=0,
	TVAL_INT,
	TVAL_PTR,
	TVAL_NODE
};

typedef void (*iter_fun)(char *key, tern_val val, uint8_t valtype, void *data);

tern_node * tern_insert(tern_node * head, char const * key, tern_val value, uint8_t valtype);
uint8_t tern_find(tern_node * head, char const * key, tern_val *ret);
tern_node * tern_find_prefix(tern_node * head, char const * key);
intptr_t tern_find_int(tern_node * head, char const * key, intptr_t def);
tern_node * tern_insert_int(tern_node * head, char const * key, intptr_t value);
void * tern_find_ptr_default(tern_node * head, char const * key, void * def);
void * tern_find_ptr(tern_node * head, char const * key);
tern_node *tern_find_node(tern_node *head, char const *key);
uint8_t tern_delete(tern_node **head, char const *key, tern_val *out);
tern_val tern_find_path_default(tern_node *head, char const *key, tern_val def, uint8_t req_valtype);
tern_val tern_find_path(tern_node *head, char const *key, uint8_t valtype);
uint8_t tern_delete_path(tern_node **head, char const *key, tern_val *out);
tern_node * tern_insert_ptr(tern_node * head, char const * key, void * value);
tern_node * tern_insert_node(tern_node *head, char const *key, tern_node *value);
tern_node *tern_insert_path(tern_node *head, char const *key, tern_val val, uint8_t valtype);
uint32_t tern_count(tern_node *head);
void tern_foreach(tern_node *head, iter_fun fun, void *data);
char * tern_int_key(uint32_t key, char * buf);
void tern_free(tern_node *head);

#endif //TERN_H_
