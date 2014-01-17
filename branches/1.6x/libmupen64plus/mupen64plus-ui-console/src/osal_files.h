/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus-ui-console - osal_files.h                                 *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Copyright (C) 2009 Richard Goedeken                                   *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.          *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

/* This header file is for all kinds of system-dependent file handling
 *
 */

#if !defined(OSAL_FILES_H)
#define OSAL_FILES_H

#include "m64p_types.h"
#include "osal_preproc.h"

/* data structure for linked list of shared libraries found in a directory */
typedef struct _osal_lib_search {
  char                     filepath[PATH_MAX];
  char                    *filename;
  m64p_plugin_type         plugin_type;
  struct _osal_lib_search *next;
  } osal_lib_search;

/* const definitions for system directories to search when looking for mupen64plus plugins */
extern const int   osal_libsearchdirs;
extern const char *osal_libsearchpath[];

/* functions for searching for shared libraries in a given directory */
extern osal_lib_search *osal_library_search(const char *searchpath);
extern void             osal_free_lib_list(osal_lib_search *head);

#endif /* #define OSAL_FILES_H */

