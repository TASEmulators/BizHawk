/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include "tern.h"
#include "util.h"
#include "paths.h"
#include "config.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef __MINGW64_VERSION_MAJOR
#define MINGW_W64_VERSION (__MINGW64_VERSION_MAJOR * 1000 + __MINGW64_VERSION_MINOR)
#else
#define MINGW_W64_VERSION 0
#endif

#if defined(_WIN32) && (MINGW_W64_VERSION < 3003)
char * strtok_r(char * input, char * sep, char ** state)
{
	if (input) {
		*state = input;
	}
	char * ret = *state;
	while (**state && **state != *sep)
	{
		++*state;
	}
	if (**state)
	{
		**state = 0;
		++*state;
		return ret;
	}
	return NULL;
}
#endif

static tern_node * parse_config_int(char **state, int started, int *line)
{
	char *config_data, *curline;
	tern_node * head = NULL;
	config_data = started ? NULL : *state;
	while ((curline = strtok_r(config_data, "\n", state)))
	{

		config_data = NULL;
		curline = strip_ws(curline);
		int len = strlen(curline);
		if (!len) {
			(*line)++;
			continue;
		}
		if (curline[0] == '#') {
			(*line)++;
			continue;
		}
		if (curline[0] == '}') {
			if (started) {
				return head;
			}
			fatal_error("unexpected } on line %d\n", *line);
		}

		char * end = curline + len - 1;
		if (*end == '{') {
			*end = 0;
			curline = strip_ws(curline);
			(*line)++;
			head = tern_insert_node(head, curline, parse_config_int(state, 1, line));
		} else {
			char * val = strip_ws(split_keyval(curline));
			char * key = curline;
			if (*val) {
				head = tern_insert_ptr(head, key, strdup(val));
			} else {
				fprintf(stderr, "Key %s is missing a value on line %d\n", key, *line);
			}
			(*line)++;
		}
	}
	return head;
}

tern_node *parse_config(char * config_data)
{
	int line = 1;
	return parse_config_int(&config_data, 0, &line);
}

typedef struct {
	char     *buf;
	uint32_t capacity;
	uint32_t size;
	uint32_t indent;
} serialize_state;

static void ensure_buf_capacity(uint32_t ensure, serialize_state *state)
{
	if (ensure + state->size > state->capacity) {
		state->capacity = state->capacity * 2;
		state->buf = realloc(state->buf, state->capacity);
	}
}

static void indent(serialize_state *state)
{
	memset(state->buf + state->size, '\t', state->indent);
	state->size += state->indent;
}

static void serialize_config_int(tern_node *config, serialize_state *state);

static void serialize_iter(char *key, tern_val val, uint8_t valtype, void *data)
{
	serialize_state *state = data;
	uint32_t keylen = strlen(key);
	uint32_t vallen = 0;
	if (valtype == TVAL_PTR) {
		vallen = strlen(val.ptrval);
	}
	ensure_buf_capacity(state->indent + keylen + 2 + vallen, state);
	state->buf[state->size++] = '\n';
	indent(state);
	memcpy(state->buf + state->size, key, keylen);
	state->size += keylen;
	state->buf[state->size++] = ' ';
	if (valtype == TVAL_PTR) {
		memcpy(state->buf + state->size, val.ptrval, vallen);
		state->size += vallen;
	} else {
		serialize_config_int(val.ptrval, state);
	}
}

static void serialize_config_int(tern_node *config, serialize_state *state)
{
	ensure_buf_capacity(1, state);
	state->buf[state->size++] = '{';
	state->indent++;
	
	tern_foreach(config, serialize_iter, state);

	--state->indent;
	ensure_buf_capacity(2 + state->indent, state);
	state->buf[state->size++] = '\n';
	indent(state);
	state->buf[state->size++] = '}';
}

char *serialize_config(tern_node *config, uint32_t *size_out)
{
	serialize_state state = {
		.size = 0,
		.capacity = 1024,
		.indent = 0
	};
	state.buf = malloc(state.capacity);
	tern_foreach(config, serialize_iter, &state);
	//serialize_config_int(config, &state);
	*size_out = state.size;
	return state.buf;
}

tern_node *parse_config_file(char *config_path)
{
	tern_node * ret = NULL;
	FILE * config_file = fopen(config_path, "rb");
	if (!config_file) {
		goto open_fail;
	}
	long config_size = file_size(config_file);
	if (!config_size) {
		goto config_empty;
	}
	char *config_data = calloc(config_size + 1, 1);
	if (fread(config_data, 1, config_size, config_file) != config_size) {
		goto config_read_fail;
	}

	ret = parse_config(config_data);
config_read_fail:
	free(config_data);
config_empty:
	fclose(config_file);
open_fail:
	return ret;
}

uint8_t serialize_config_file(tern_node *config, char *path)
{
	FILE *f = fopen(path, "w");
	if (!f) {
		return 0;
	}
	uint32_t buf_size;
	char *buffer = serialize_config(config, &buf_size);
	uint8_t ret = buf_size == fwrite(buffer, 1, buf_size, f);
	free(buffer);
	fclose(f);
	return ret;
}

tern_node *parse_bundled_config(char *config_name)
{
	tern_node *ret = NULL;
#ifdef CONFIG_PATH
	if (!strcmp("default.cfg", config_name) || !strcmp("blastem.cfg", config_name)) {
		char *confpath = path_append(CONFIG_PATH, config_name);
		ret = parse_config_file(confpath);
		free(confpath);
	} else {
#endif
	uint32_t confsize;
	char *confdata = read_bundled_file(config_name, &confsize);
	if (confdata) {
		confdata[confsize] = 0;
		ret = parse_config(confdata);
		free(confdata);
	}
#ifdef CONFIG_PATH
	}
#endif
	return ret;
}

tern_node *load_overrideable_config(char *name, char *bundled_name, uint8_t *used_config_dir)
{
	char const *confdir = get_config_dir();
	char *confpath = NULL;
	tern_node *ret;
	if (confdir) {
		confpath = path_append(confdir, name);
		ret = parse_config_file(confpath);
	}
	free(confpath);
	if (used_config_dir) {
		*used_config_dir = ret != NULL;
	}
	
	if (!ret) {
		ret = parse_bundled_config(name);
		if (!ret) {
			ret = parse_bundled_config(bundled_name);
		}
	}

	return ret;
}

static uint8_t app_config_in_config_dir;
tern_node *load_config()
{
	tern_node *ret = load_overrideable_config("blastem.cfg", "default.cfg", &app_config_in_config_dir);
	
	if (!ret) {
		if (get_config_dir()) {
			fatal_error("Failed to find a config file at %s or in the blastem executable directory\n", get_config_dir());
		} else {
			fatal_error("Failed to find a config file in the BlastEm executable directory and the config directory path could not be determined\n");
		}
	}
	return ret;
}

void persist_config_at(tern_node *app_config, tern_node *to_save, char *fname)
{
	char*use_exe_dir = tern_find_path_default(app_config, "ui\0config_in_exe_dir\0", (tern_val){.ptrval = "off"}, TVAL_PTR).ptrval;
	char *confpath;
	if (!strcmp(use_exe_dir, "on")) {
		confpath = path_append(get_exe_dir(), fname);
		if (app_config == to_save && app_config_in_config_dir) {
			//user switched to "portable" configs this session and there is an
			//existing config file in the user-specific config directory
			//delete it so we don't end up loading it next time
			char *oldpath = path_append(get_config_dir(), fname);
			delete_file(oldpath);
			free(oldpath);
		}
	} else {
		char const *confdir = get_config_dir();
		if (!confdir) {
			fatal_error("Failed to locate config file directory\n");
		}
		ensure_dir_exists(confdir);
		confpath = path_append(confdir, fname);
	}
	if (!serialize_config_file(to_save, confpath)) {
		fatal_error("Failed to write config to %s\n", confpath);
	}
	free(confpath);
}

void persist_config(tern_node *config)
{
	persist_config_at(config, config, "blastem.cfg");
}

void delete_custom_config_at(char *fname)
{
	char *confpath = path_append(get_exe_dir(), fname);
	delete_file(confpath);
	free(confpath);
	confpath = path_append(get_config_dir(), fname);
	delete_file(confpath);
	free(confpath);
}

void delete_custom_config(void)
{
	delete_custom_config_at("blastem.cfg");
}

char **get_extension_list(tern_node *config, uint32_t *num_exts_out)
{
	char *ext_filter = strdup(tern_find_path_default(config, "ui\0extensions\0", (tern_val){.ptrval = "bin gen md smd sms gg"}, TVAL_PTR).ptrval);
	uint32_t num_exts = 0, ext_storage = 5;
	char **ext_list = malloc(sizeof(char *) * ext_storage);
	char *cur_filter = ext_filter;
	while (*cur_filter)
	{
		if (num_exts == ext_storage) {
			ext_storage *= 2;
			ext_list = realloc(ext_list, sizeof(char *) * ext_storage);
		}
		ext_list[num_exts++] = cur_filter;
		cur_filter = split_keyval(cur_filter);
	}
	*num_exts_out = num_exts;
	return ext_list;
}

#define DEFAULT_LOWPASS_CUTOFF 3390
uint32_t get_lowpass_cutoff(tern_node *config)
{
	char * lowpass_cutoff_str = tern_find_path(config, "audio\0lowpass_cutoff\0", TVAL_PTR).ptrval;
	return lowpass_cutoff_str ? atoi(lowpass_cutoff_str) : DEFAULT_LOWPASS_CUTOFF;
}

tern_node *get_systems_config(void)
{
	static tern_node *systems;
	if (!systems) {
		systems = parse_bundled_config("systems.cfg");
	}
	return systems;
}

tern_node *get_model(tern_node *config, system_type stype)
{
	char *model = tern_find_path_default(config, "system\0model\0", (tern_val){.ptrval = "md1va3"}, TVAL_PTR).ptrval;
	return tern_find_node(get_systems_config(), model);
}
