/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef CONFIG_H_
#define CONFIG_H_
#include "tern.h"
#include "system.h"

tern_node *parse_config_file(char *config_path);
tern_node *parse_bundled_config(char *config_name);
tern_node *load_overrideable_config(char *name, char *bundled_name, uint8_t *used_config_dir);
tern_node *load_config();
char *serialize_config(tern_node *config, uint32_t *size_out);
uint8_t serialize_config_file(tern_node *config, char *path);
void persist_config_at(tern_node *app_config, tern_node *to_save, char *fname);
void persist_config(tern_node *config);
void delete_custom_config_at(char *fname);
void delete_custom_config(void);
char **get_extension_list(tern_node *config, uint32_t *num_exts_out);
uint32_t get_lowpass_cutoff(tern_node *config);
tern_node *get_systems_config(void);
tern_node *get_model(tern_node *config, system_type stype);

#endif //CONFIG_H_

