/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus-ui-console - osal_files_unix.c                            *
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

/* This implements all kinds of system-dependent file handling
 *
 */

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <sys/types.h>
#include <dirent.h>  // for opendir(), readdir(), closedir()

#include "main.h"
#include "m64p_types.h"
#include "osal_preproc.h"
#include "osal_files.h"

/* definitions for system directories to search when looking for mupen64plus plugins */
#if defined(PLUGINDIR)
  const int   osal_libsearchdirs = 4;
  const char *osal_libsearchpath[4] = { PLUGINDIR, "/usr/local/lib/mupen64plus",  "/usr/lib/mupen64plus", "./" };
#else
  const int   osal_libsearchdirs = 3;
  const char *osal_libsearchpath[3] = { "/usr/local/lib/mupen64plus",  "/usr/lib/mupen64plus", "./" };
#endif

osal_lib_search *osal_library_search(const char *searchpath)
{
    osal_lib_search *head = NULL, *curr = NULL;
    DIR *dir;
    struct dirent *entry;
    
#ifdef __APPLE__
    const char* suffix = ".dylib";
#else
    const char* suffix = ".so";
#endif

    dir = opendir(searchpath);
    if (dir == NULL)
        return NULL;

    /* look for any shared libraries in this folder */
    while ((entry = readdir(dir)) != NULL)
    {
        osal_lib_search *newlib = NULL;
        if (strcmp(entry->d_name + strlen(entry->d_name) - strlen(suffix), suffix) != 0)
            continue;
        /* this is a .so file, so add it to the list */
        newlib = malloc(sizeof(osal_lib_search));
        if (newlib == NULL)
        {
            DebugMessage(M64MSG_ERROR, "Memory allocation error in osal_library_search()!");
            osal_free_lib_list(head);
            closedir(dir);
            return NULL;
        }
        if (head == NULL)
        {
            head = curr = newlib;
        }
        else
        {
            curr->next = newlib;
            curr = newlib;
        }
        /* set up the filepath and filename members */
        strncpy(curr->filepath, searchpath, PATH_MAX-2);
        curr->filepath[PATH_MAX-2] = 0;
        if (curr->filepath[strlen(curr->filepath)-1] != '/')
            strcat(curr->filepath, "/");
        int pathlen = strlen(curr->filepath);
        curr->filename = curr->filepath + pathlen;
        strncat(curr->filepath, entry->d_name, PATH_MAX - pathlen - 1);
        curr->filepath[PATH_MAX-1] = 0;
        /* set plugin_type and next pointer */
        curr->plugin_type = 0;
        curr->next = NULL;
    }

    closedir(dir);
    return head;
}

void osal_free_lib_list(osal_lib_search *head)
{
    while (head != NULL)
    {
        osal_lib_search *next = head->next;
        free(head);
        head = next;
    }
}

