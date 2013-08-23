/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus-ui-console - osal_files_win32.c                           *
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
#include <windows.h>

#include "main.h"
#include "m64p_types.h"
#include "osal_preproc.h"
#include "osal_files.h"

/* definitions for system directories to search when looking for mupen64plus plugins */
const int  osal_libsearchdirs = 1;
const char *osal_libsearchpath[1] = { ".\\" };

osal_lib_search *osal_library_search(const char *searchpath)
{
    osal_lib_search *head = NULL, *curr = NULL;
    WIN32_FIND_DATA entry;
    HANDLE hDir;

    char *pchSearchPath = (char *) malloc(strlen(searchpath) + 16);
    if (pchSearchPath == NULL)
    {
        DebugMessage(M64MSG_ERROR, "Couldn't allocate memory for file search path in osal_library_search()!");
        return NULL;
    }
    sprintf(pchSearchPath, "%s\\*.dll", searchpath);
    hDir = FindFirstFile(pchSearchPath, &entry);
    free(pchSearchPath);
    if (hDir == INVALID_HANDLE_VALUE)
        return NULL;

    /* look for any shared libraries in this folder */
    do
    {
        osal_lib_search *newlib = NULL;
        /* this is a .dll file, so add it to the list */
        newlib = (osal_lib_search *) malloc(sizeof(osal_lib_search));
        if (newlib == NULL)
        {
            DebugMessage(M64MSG_ERROR, "Memory allocation error in osal_library_search()!");
            osal_free_lib_list(head);
            FindClose(hDir);
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
        if (curr->filepath[strlen(curr->filepath)-1] != '\\')
            strcat(curr->filepath, "\\");
        int pathlen = (int) strlen(curr->filepath);
        curr->filename = curr->filepath + pathlen;
        strncat(curr->filepath, entry.cFileName, PATH_MAX - pathlen - 1);
        curr->filepath[PATH_MAX-1] = 0;
        /* set plugin_type and next pointer */
        curr->plugin_type = (m64p_plugin_type) 0;
        curr->next = NULL;
    } while (FindNextFile(hDir, &entry));

    FindClose(hDir);
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
