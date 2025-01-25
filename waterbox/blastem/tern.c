/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include "tern.h"
#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "util.h"

tern_node * tern_insert(tern_node * head, char const * key, tern_val value, uint8_t valtype)
{
	tern_node ** cur = &head;
	while(*key)
	{
		if (*cur) {
			while(*cur && (*cur)->el != *key)
			{
				if (*key < (*cur)->el) {
					cur = &(*cur)->left;
				} else {
					cur = &(*cur)->right;
				}
			}
		}
		if (!*cur) {
			*cur = malloc(sizeof(tern_node));
			(*cur)->left = NULL;
			(*cur)->right = NULL;
			(*cur)->straight.next = NULL;
			(*cur)->el = *key;
			(*cur)->valtype = TVAL_NONE;
		}
		cur = &((*cur)->straight.next);
		key++;
	}
	while(*cur && (*cur)->el)
	{
		cur = &(*cur)->left;
	}
	if (!*cur) {
		*cur = malloc(sizeof(tern_node));
		(*cur)->left = NULL;
		(*cur)->right = NULL;
		(*cur)->el = 0;
		(*cur)->valtype = TVAL_NONE;
	}
	if ((*cur)->valtype == TVAL_PTR) {
		//not freeing tern nodes can also cause leaks, but handling freeing those here is problematic
		//since updating a sub-tree may involve creating a new root node
		free((*cur)->straight.value.ptrval);
	}
	(*cur)->straight.value = value;
	(*cur)->valtype = valtype;
	return head;
}

uint8_t tern_find(tern_node * head, char const * key, tern_val *ret)
{
	tern_node * cur = head;
	while (cur)
	{
		if (cur->el == *key) {
			if (*key) {
				cur = cur->straight.next;
				key++;
			} else {
				*ret = cur->straight.value;
				return cur->valtype;
			}
		} else if (*key < cur->el) {
			cur = cur->left;
		} else {
			cur = cur->right;
		}
	}
	return TVAL_NONE;
}

tern_node * tern_find_prefix(tern_node * head, char const * key)
{
	tern_node * cur = head;
	while (cur && *key)
	{
		if (cur->el == *key) {
			cur = cur->straight.next;
			key++;
		} else if (*key < cur->el) {
			cur = cur->left;
		} else {
			cur = cur->right;
		}
	}
	return cur;
}

intptr_t tern_find_int(tern_node * head, char const * key, intptr_t def)
{
	tern_val ret;
	uint8_t valtype = tern_find(head, key, &ret);
	if (valtype == TVAL_INT) {
		return ret.intval;
	}
	return def;
}

tern_node * tern_insert_int(tern_node * head, char const * key, intptr_t value)
{
	tern_val val;
	val.intval = value;
	return tern_insert(head, key, val, TVAL_INT);
}

void * tern_find_ptr_default(tern_node * head, char const * key, void * def)
{
	tern_val ret;
	uint8_t valtype = tern_find(head, key, &ret);
	if (valtype == TVAL_PTR) {
		return ret.ptrval;
	}
	return def;
}

void * tern_find_ptr(tern_node * head, char const * key)
{
	return tern_find_ptr_default(head, key, NULL);
}

tern_node *tern_find_node(tern_node *head, char const *key)
{
	tern_val ret;
	uint8_t valtype = tern_find(head, key, &ret);
	if (valtype == TVAL_NODE) {
		return ret.ptrval;
	}
	return NULL;
}

uint8_t tern_delete(tern_node **head, char const *key, tern_val *out)
{
	tern_node *cur = *head, **last = head;
	while (cur)
	{
		if (cur->el == *key) {
			if (*key) {
				last = &cur->straight.next;
				cur = cur->straight.next;
				key++;
			} else {
				break;
			}
		} else if (*key < cur->el) {
			last = &cur->left;
			cur = cur->left;
		} else {
			last = &cur->right;
			cur = cur->right;
		}
	}
	if (!cur) {
		return TVAL_NONE;
	}
	*last = cur->right;
	uint8_t valtype = cur->valtype;
	if (out) {
		*out = cur->straight.value;
	}
	free(cur);
	return valtype;
}

tern_val tern_find_path_default(tern_node *head, char const *key, tern_val def, uint8_t req_valtype)
{
	tern_val ret;
	while (*key)
	{
		uint8_t valtype = tern_find(head, key, &ret);
		if (!valtype) {
			return def;
		}
		key = key + strlen(key) + 1;
		if (*key) {
			if (valtype != TVAL_NODE) {
				return def;
			}
			head = ret.ptrval;
		} else if (req_valtype && req_valtype != valtype) {
			return def;
		}
	}
	return ret;
}

tern_val tern_find_path(tern_node *head, char const *key, uint8_t valtype)
{
	tern_val def;
	def.ptrval = NULL;
	return tern_find_path_default(head, key, def, valtype);
}

tern_node * tern_insert_ptr(tern_node * head, char const * key, void * value)
{
	tern_val val;
	val.ptrval = value;
	return tern_insert(head, key, val, TVAL_PTR);
}

tern_node * tern_insert_node(tern_node *head, char const *key, tern_node *value)
{
	tern_val val;
	val.ptrval = value;
	return tern_insert(head, key, val, TVAL_NODE);
}

tern_node *tern_insert_path(tern_node *head, char const *key, tern_val val, uint8_t valtype)
{
	const char *next_key = key + strlen(key) + 1;
	if (*next_key) {
		tern_node *child = tern_find_node(head, key);
		child = tern_insert_path(child, next_key, val, valtype);
		return tern_insert_node(head, key, child);
	} else {
		return tern_insert(head, key, val, valtype);
	}
}

uint8_t tern_delete_path(tern_node **head, char const *key, tern_val *out)
{
	const char *next_key = key + strlen(key) + 1;
	if (*next_key) {
		tern_node *child = tern_find_node(*head, key);
		if (!child) {
			return TVAL_NONE;
		}
		tern_node *tmp = child;
		uint8_t valtype = tern_delete_path(&tmp, next_key, out);
		if (tmp != child) {
			*head = tern_insert_node(*head, key, tmp);
		}
		return valtype;
	} else {
		return tern_delete(head, key, out);
	}
}

uint32_t tern_count(tern_node *head)
{
	uint32_t count = 0;
	if (head->left) {
		count += tern_count(head->left);
	}
	if (head->right) {
		count += tern_count(head->right);
	}
	if (!head->el) {
		count++;
	} else if (head->straight.next) {
		count += tern_count(head->straight.next);
	}
	return count;
}

#define MAX_ITER_KEY 127
void tern_foreach_int(tern_node *head, iter_fun fun, void *data, char *keybuf, int pos)
{
	if (!head->el) {
		keybuf[pos] = 0;
		fun(keybuf, head->straight.value, head->valtype, data);
	}
	if (head->left) {
		tern_foreach_int(head->left, fun, data, keybuf, pos);
	}
	if (head->el && head->straight.next) {
		if (pos == MAX_ITER_KEY) {
			fatal_error("tern_foreach_int: exceeded maximum key size");
		}
		keybuf[pos] = head->el;
		tern_foreach_int(head->straight.next, fun, data, keybuf, pos+1);
	}
	if (head->right) {
		tern_foreach_int(head->right, fun, data, keybuf, pos);
	}
}

void tern_foreach(tern_node *head, iter_fun fun, void *data)
{
	//lame, but good enough for my purposes
	char key[MAX_ITER_KEY+1];
	tern_foreach_int(head, fun, data, key, 0);
}

char * tern_int_key(uint32_t key, char * buf)
{
	char * cur = buf;
	while (key)
	{
		*(cur++) = (key & 0x7F) + 1;
		key >>= 7;
	}
	*cur = 0;
	return buf;
}

void tern_free(tern_node *head)
{
	if (!head) {
		return;
	}
	tern_free(head->left);
	tern_free(head->right);
	if (head->el) {
		tern_free(head->straight.next);
	}
	free(head);
}
