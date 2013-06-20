/*  src/psp/filesel.c: Simple file selector for use with PSP menu
    Copyright 2009 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include "common.h"

#include "display.h"
#include "filesel.h"
#include "font.h"
#include "sys.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* File selector data structure (exported as opaque type FileSelector) */

struct FileSelector_ {
    char *title;            // Window title

    char *dir;              // Current directory (note that we don't support
                            //     changing directories at present)
    SceUID scandir_thread;  // If nonzero, thread ID of directory scan thread;
                            //     if zero, indicates that scan is complete.
    /* The file list MUST NOT be accessed by the main thread while
     * scandir_thread is nonzero */
    char **file_list;       // List of files in the current directory (each
                            //    entry is malloc'd)
    int num_files;          // Number of files in file_list[]; negative
                            //    indicates an error scanning the directory

    int top_file;           // Index of top file shown in window
    int selected_file;      // Selected file index (negative = cancelled)
    int num_lines;          // Number of lines visible in file selector window
                            //    (set after first filesel_draw() call)
    uint8_t cursor_timer;   // Frame counter for cursor flashing

    uint8_t done;           // Nonzero when user selects a file or cancels
};

/*************************************************************************/

/* Display parameters */

/* Window size */
#define WINDOW_WIDTH    360
#define WINDOW_HEIGHT   208

/* Window colors */
#define BGCOLOR_TITLE   0xFF804060  // Title bar
#define BGCOLOR_LIST    0xFF604040  // File list area
#define BORDER_COLOR_HI 0xFFFFECEC  // Border lines (bright)
#define BORDER_COLOR_LO 0xFF000000  // Border lines (shadow)

/* Cursor parameters */
#define CURSOR_COLOR    0x80FFECEC
#define CURSOR_PERIOD   60  // frames

/* Text colors */
#define TEXTCOLOR_TITLE     0xFFFFECEC
#define TEXTCOLOR_FILE      0xFFFFECEC
#define TEXTCOLOR_DISABLED  0xFF807474  // "Scanning directory...", etc.
#define TEXTCOLOR_ERROR     0xFF5540FF

/*************************************************************************/

/* Local function declarations */

static void scandir_thread(SceSize args, void *argp);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * filesel_create:  Create a new file selector.
 *
 * [Parameters]
 *     title: File selector window title
 *       dir: Directory to select file from
 * [Return value]
 *     Newly-created file selector, or NULL on error
 */
FileSelector *filesel_create(const char *title, const char *dir)
{
    PRECOND(title != NULL, goto error_return);
    PRECOND(dir != NULL, goto error_return);

    FileSelector *filesel = malloc(sizeof(*filesel));
    if (UNLIKELY(!filesel)) {
        DMSG("No memory for FileSelector");
        goto error_return;
    }
    filesel->title = strdup(title);
    if (UNLIKELY(!filesel->title)) {
        DMSG("No memory for title");
        goto error_free_filesel;
    }
    filesel->dir = strdup(dir);
    if (UNLIKELY(!filesel->dir)) {
        DMSG("No memory for current directory");
        goto error_free_title;
    }
    filesel->scandir_thread = 0;
    filesel->file_list = NULL;
    filesel->num_files = 0;
    filesel->top_file = 0;
    filesel->selected_file = 0;
    filesel->num_lines = 0;
    filesel->cursor_timer = 0;
    filesel->done = 0;

    /* We only ever have one file selector open at once, so we just use a
     * static thread name */
    int32_t thread = sys_start_thread("YabauseScanDirThread", scandir_thread,
                                      THREADPRI_MAIN+1, 0x1000,
                                      sizeof(filesel), &filesel);
    if (thread & 0x80000000) {
        DMSG("Failed to start scandir thread: %s", psp_strerror(thread));
        goto error_free_dir;
    }

    filesel->scandir_thread = thread;
    return filesel;

  error_free_dir:
    free(filesel->dir);
  error_free_title:
    free(filesel->title);
  error_free_filesel:
    free(filesel);
  error_return:
    return NULL;
}

/*************************************************************************/

/**
 * filesel_process:  Process input for a file selector.
 *
 * [Parameters]
 *     filesel: File selector
 *     buttons: Newly-pressed (or repeating) buttons (PSP_CTRL_* bitmask)
 * [Return value]
 *     None
 */
void filesel_process(FileSelector *filesel, uint32_t buttons)
{
    PRECOND(filesel != NULL, return);

    /* If the user wants to cancel, give that top priority */
    if (buttons & PSP_CTRL_CROSS) {
        filesel->selected_file = -1;
        filesel->done = 1;
        return;
    }

    /* If the scanning thread is still running, there's nothing to do */
    if (filesel->scandir_thread) {
        return;
    }

    /* Update the cursor timer (assume we're called once per frame) */
    filesel->cursor_timer = (filesel->cursor_timer + 1) % CURSOR_PERIOD;

    /* If we have a file list, let the user move the cursor around or
     * select a file; note that scrolling is handled at drawing time, when
     * we know how many lines will fit in the file list display area */
    if (filesel->num_files > 0) {
        if (buttons & PSP_CTRL_UP) {
            if (filesel->selected_file > 0) {
                filesel->selected_file--;
                filesel->cursor_timer = 0;
            }
        } else if (buttons & PSP_CTRL_DOWN) {
            if (filesel->selected_file < filesel->num_files - 1) {
                filesel->selected_file++;
                filesel->cursor_timer = 0;
            }
        } else if (buttons & PSP_CTRL_LEFT) {
            /* Use the number of visible lines calculated in filesel_draw().
             * If we get here on the very first filesel_process() call,
             * filesel->num_lines will be zero, so this becomes a no-op. */
            int num_to_scroll = filesel->num_lines;
            if (num_to_scroll > filesel->top_file) {
                num_to_scroll = filesel->top_file;
            }
            filesel->top_file -= num_to_scroll;
            filesel->selected_file -= num_to_scroll;
            filesel->cursor_timer = 0;
        } else if (buttons & PSP_CTRL_RIGHT) {
            int max_top = filesel->num_files - filesel->num_lines;
            if (max_top < 0) {
                max_top = 0;
            }
            int num_to_scroll = filesel->num_lines;
            if (num_to_scroll > max_top - filesel->top_file) {
                num_to_scroll = max_top - filesel->top_file;
            }
            filesel->top_file += num_to_scroll;
            filesel->selected_file += num_to_scroll;
            filesel->cursor_timer = 0;
        } else if (buttons & PSP_CTRL_CIRCLE) {
            filesel->done = 1;
        }
    }
}

/*************************************************************************/

/**
 * filesel_draw:  Draw a file selector.
 *
 * [Parameters]
 *     filesel: File selector
 * [Return value]
 *     None
 */
void filesel_draw(FileSelector *filesel)
{
    PRECOND(filesel != NULL, return);

    const int x1 = DISPLAY_WIDTH/2 - WINDOW_WIDTH/2;
    const int y1 = DISPLAY_HEIGHT/2 - WINDOW_HEIGHT/2;
    const int x2 = x1 + (WINDOW_WIDTH - 1);
    const int y2 = y1 + (WINDOW_HEIGHT - 1);

    /* Draw the outside border */

    display_fill_box(x1, y1, x2-1, y1, BORDER_COLOR_HI);
    display_fill_box(x1+1, y1+1, x2, y1+1, BORDER_COLOR_LO);
    display_fill_box(x1, y1+1, x1, y2-1, BORDER_COLOR_HI);
    display_fill_box(x1+1, y1+2, x1+1, y2, BORDER_COLOR_LO);
    display_fill_box(x2-1, y1+1, x2-1, y2-1, BORDER_COLOR_HI);
    display_fill_box(x2, y2+2, x2, y2, BORDER_COLOR_LO);
    display_fill_box(x1+1, y2-1, x2-2, y2-1, BORDER_COLOR_HI);
    display_fill_box(x1, y2, x2-1, y2, BORDER_COLOR_LO);

    /* Draw the title bar */

    const int title_text_y = y1+4;
    const int title_y2 = title_text_y + FONT_HEIGHT + 2;
    display_fill_box(x1+1, title_y2, x2-2, title_y2, BORDER_COLOR_HI);
    display_fill_box(x1+2, title_y2+1, x2-1, title_y2+1, BORDER_COLOR_LO);
    display_fill_box(x1+2, y1+2, x2-2, title_y2-2, BGCOLOR_TITLE);
    font_printf((x1+x2)/2, title_text_y, 0, TEXTCOLOR_TITLE,
                "%s", filesel->title);

    /* Draw the file list */

    display_fill_box(x1+2, title_y2+2, x2-2, y2-2, BGCOLOR_LIST);
    const int list_x1 = x1 + 4;
    const int list_y1 = title_y2 + 4;
    const int list_x2 = x2 - 4;
    const int list_y2 = y2 - 4;
    const int line_height = FONT_HEIGHT + 2;
    const int num_lines = ((list_y2+1) - list_y1) / line_height;
    filesel->num_lines = num_lines;  // Save for reference in filesel_process()
    int y = list_y1 + (((list_y2+1)-list_y1) - (num_lines*line_height)) / 2;
    if (filesel->scandir_thread) {
        font_printf(list_x1, y, -1, TEXTCOLOR_DISABLED,
                    "(Scanning directory...)");
    } else if (filesel->num_files < 0) {
        font_printf(list_x1, y, -1, TEXTCOLOR_ERROR,
                    "(Error scanning directory!)");
    } else if (filesel->num_files == 0) {
        font_printf(list_x1, y, -1, TEXTCOLOR_DISABLED,
                    "(No files found.)");
    } else {
        /* Scroll the list if needed */
        if (filesel->selected_file < filesel->top_file) {
            filesel->top_file = filesel->selected_file;
        } else if (filesel->selected_file >= filesel->top_file + num_lines) {
            filesel->top_file = filesel->selected_file - (num_lines-1);
        }
        /* List as many files as will fit in the window */
        int i;
        for (i = filesel->top_file;
             i < filesel->num_files && i < filesel->top_file + num_lines;
             i++, y += line_height
        ) {
            // FIXME: handle overlength filenames in a more general way
            const int maxlen = ((list_x2+1) - list_x1) / 6;
            if (strlen(filesel->file_list[i]) > maxlen) {
                font_printf(list_x1, y, -1, TEXTCOLOR_FILE,
                            "%.*s...", maxlen-3, filesel->file_list[i]);
            } else {
                font_printf(list_x1, y, -1, TEXTCOLOR_FILE,
                            "%s", filesel->file_list[i]);
            }
            if (filesel->selected_file == i) {
                const float cursor_alpha =
                    (sinf((filesel->cursor_timer / (float)CURSOR_PERIOD)
                          * (float)M_TWOPI) + 1) / 2;
                const uint32_t cursor_alpha_byte =
                    floorf((CURSOR_COLOR>>24 & 0xFF) * cursor_alpha + 0.5f);
                display_fill_box(list_x1-1, y-1, list_x2+1, y+FONT_HEIGHT,
                                 cursor_alpha_byte<<24
                                     | (CURSOR_COLOR & 0x00FFFFFF));
            }
        }
    }
}

/*************************************************************************/

/**
 * filesel_done:  Return whether a file selector's work is done (i.e.,
 * whether the user has either selected a file or cancelled the selector).
 *
 * [Parameters]
 *     filesel: File selector
 * [Return value]
 *     True (nonzero) if any of the following are true:
 *         - The user selected a file
 *         - The user cancelled the file selector
 *         - An error occurred which prevents the file selector's
 *               processing from continuing normally
 *     False (zero) if none of the above are true
 */
int filesel_done(FileSelector *filesel)
{
    PRECOND(filesel != NULL, return 1);

    return filesel->done;
}

/*-----------------------------------------------------------------------*/

/**
 * filesel_selected_file:  Return the name of the file selected by the
 * user, if any.
 *
 * [Parameters]
 *     filesel: File selector
 * [Return value]
 *     The name of the file selected by the user, or NULL if the user has
 *     not selected a file (including if the user has cancelled the file
 *     selector)
 */
const char *filesel_selected_file(FileSelector *filesel)
{
    PRECOND(filesel != NULL, return NULL);

    if (filesel->done && filesel->selected_file >= 0) {
        return filesel->file_list[filesel->selected_file];
    } else {
        return NULL;
    }
}

/*************************************************************************/

/**
 * filesel_destroy:  Destroy a file selector.  Does nothing if filesel==NULL.
 *
 * [Parameters]
 *     filesel: File selector to destroy
 * [Return value]
 *     None
 */
void filesel_destroy(FileSelector *filesel)
{
    if (filesel) {
        if (filesel->scandir_thread) {
            sceKernelTerminateDeleteThread(filesel->scandir_thread);
        }
        int i;
        for (i = 0; i < filesel->num_files; i++) {
            free(filesel->file_list[i]);
        }
        free(filesel->file_list);
        free(filesel->dir);
        free(filesel->title);
        free(filesel);
    }
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * scandir_thread:  Thread used to scan a directory for files.  Updates the
 * file_list[] and num_files fields of the passed-in FileSelector structure.
 *
 * [Parameters]
 *     args: Parameter size (must be sizeof(FileSelector *))
 *     argp: Parameter pointer (must point to a valid FileSelector pointer)
 * [Return value]
 *     Does not return (terminates and deletes thread when finished)
 */
static void scandir_thread(SceSize args, void *argp)
{
    PRECOND(args == sizeof(FileSelector *), goto exit_thread);
    PRECOND(argp != NULL, goto exit_thread);

    FileSelector * const filesel = *(FileSelector **)argp;


    int dirfd = sceIoDopen(filesel->dir);
    if (dirfd < 0) {
        DMSG("sceIoDopen(%s): %s", filesel->dir, psp_strerror(dirfd));
        goto signal_error;
    }

    SceIoDirent dirent;
    memset(&dirent, 0, sizeof(dirent));
    int res;
    while ((res = sceIoDread(dirfd, &dirent)) > 0) {

        /* Ignore . (current directory) and .. (parent directory) entries */
        if (strcmp(dirent.d_name,".") == 0 || strcmp(dirent.d_name,"..") == 0){
            continue;
        }

        /* Ignore all directories, since we don't support changing directory
         * (we could subsume the above test into this one, but we leave them
         * separate for clarity) */
        char pathbuf[1000];
        if (UNLIKELY(snprintf(pathbuf, sizeof(pathbuf), "%s/%s", filesel->dir,
                              dirent.d_name) >= sizeof(pathbuf))) {
            DMSG("Pathname buffer overflow: %s/%s", filesel->dir,
                 dirent.d_name);
            continue;
        }
        struct SceIoStat st;
        memset(&st, 0, sizeof(st));
        res = sceIoGetstat(pathbuf, &st);
        if (UNLIKELY(res < 0)) {
            DMSG("sceIoGetstat(%s): %s", pathbuf, psp_strerror(res));
            continue;
        }
        if (FIO_S_ISDIR(st.st_mode)) {
            continue;
        }

        /* Make room for the new file in the array */
        char **new_file_list =
            realloc(filesel->file_list,
                    sizeof(*filesel->file_list) * (filesel->num_files + 1));
        if (UNLIKELY(!new_file_list)) {
            DMSG("No memory to expand file list to %d files",
                 filesel->num_files + 1);
            goto clear_file_list;
        }
        filesel->file_list = new_file_list;
        char *new_file = strdup(dirent.d_name);
        if (UNLIKELY(!new_file)) {
            DMSG("No memory to copy filename: %s", dirent.d_name);
            goto clear_file_list;
        }

        /* Insert the new file into the file list, keeping the list sorted.
         * We don't expect to see very many files in a directory, so we
         * don't bother with anything more complex than a linear search. */
        int i;
        for (i = 0; i < filesel->num_files; i++) {
            if (stricmp(new_file, filesel->file_list[i]) < 0) {
                break;
            }
        }
        if (i < filesel->num_files) {
            memmove(&filesel->file_list[i+1], &filesel->file_list[i],
                    sizeof(*filesel->file_list) * (filesel->num_files - i));
        }
        filesel->file_list[i] = new_file;
        filesel->num_files++;
    }

    res = sceIoDclose(dirfd);
    if (res != 0) {
        DMSG("sceIoDclose(%s): %s", filesel->dir, psp_strerror(dirfd));
        /* Not a critical error, so just let it slide */
    }

    filesel->scandir_thread = 0;
    goto exit_thread;

  clear_file_list:;
    int i;
    for (i = 0; i < filesel->num_files; i++) {
        free(filesel->file_list[i]);
    }
    free(filesel->file_list);
    filesel->file_list = NULL;
  signal_error:
    filesel->num_files = -1;
    filesel->scandir_thread = 0;
  exit_thread:
    sceKernelExitDeleteThread(0);
}

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
