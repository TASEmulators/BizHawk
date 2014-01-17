/*  src/psp/osk.c: PSP on-screen keyboard management
    Copyright 2010 Andrew Church

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

#include "osk.h"
#include "sys.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Is the on-screen keyboard active? */
static uint8_t osk_active;

/* Has a close been requested? */
static uint8_t osk_closing;

/* Parameter block pointer (only valid when osk_active != 0).  Pointers to
 * subsidiary data structures such as string buffers are stored within the
 * appropriate parent data structure. */
static SceUtilityOskParams *osk_params;

/*-----------------------------------------------------------------------*/

/* Local function declarations. */
static void reset_osk(void);
static uint16_t *utf8to16(const char *str);
static char *utf16to8(const uint16_t *str);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * osk_open:  Open the on-screen keyboard with the given prompt string and
 * default text.
 *
 * [Parameters]
 *      prompt: Prompt string
 *     deftext: Default text
 *      maxlen: Maximum length (number of _characters_, not bytes) of
 *                 entered text, not including the trailing null
 * [Return value]
 *     Nonzero on success, zero on failure
 */
int osk_open(const char *prompt, const char *deftext, unsigned int maxlen)
{
    PRECOND(prompt != NULL, goto error_return);
    PRECOND(deftext != NULL, goto error_return);
    PRECOND(maxlen > 0, goto error_return);
    PRECOND(strlen(deftext) <= maxlen, goto error_return);

    if (osk_active) {
        DMSG("Tried to start a second OSK while one was already active!");
        goto error_return;
    }

    osk_params = calloc(1, sizeof(*osk_params));
    if (!osk_params) {
        DMSG("No memory for osk_params");
        goto error_return;
    }
    osk_params->base.size = sizeof(*osk_params);
    osk_params->base.graphicsThread = THREADPRI_UTILITY + 1;
    osk_params->base.accessThread   = THREADPRI_UTILITY + 3;
    osk_params->base.fontThread     = THREADPRI_UTILITY + 2;
    osk_params->base.soundThread    = THREADPRI_UTILITY;
    osk_params->datacount = 1;

    osk_params->data = calloc(1, sizeof(*osk_params->data));
    if (!osk_params->data) {
        DMSG("No memory for data");
        goto error_free_osk_params;
    }
    osk_params->data->language = PSP_UTILITY_OSK_LANGUAGE_ENGLISH;
    osk_params->data->inputtype = PSP_UTILITY_OSK_INPUTTYPE_LATIN_UPPERCASE
                                | PSP_UTILITY_OSK_INPUTTYPE_LATIN_LOWERCASE
                                | PSP_UTILITY_OSK_INPUTTYPE_LATIN_DIGIT
                                | PSP_UTILITY_OSK_INPUTTYPE_LATIN_SYMBOL;
    osk_params->data->lines = 1;
    /* The order of these field names is apparently reversed in the current
     * (r2493) PSPSDK.  Set both to maxlen+1 just to be safe; the null
     * terminator in "deftext" will keep the OSK from overrunning the end
     * of the default text. */
    osk_params->data->outtextlength = maxlen + 1;
    osk_params->data->outtextlimit = maxlen + 1;

    osk_params->data->desc = utf8to16(prompt);
    if (!osk_params->data->desc) {
        DMSG("No memory for prompt buffer");
        goto error_free_data;
    }

    osk_params->data->intext = utf8to16(deftext);
    if (!osk_params->data->intext) {
        DMSG("No memory for default text buffer");
        goto error_free_desc;
    }

    osk_params->data->outtext = malloc(2 * (maxlen + 1));
    if (!osk_params->data->outtext) {
        DMSG("No memory for output text buffer");
        goto error_free_intext;
    }

    int res = sceUtilityOskInitStart(osk_params);
    if (res < 0) {
        DMSG("sceUtilityOskInitStart() failed: %s", psp_strerror(res));
        return 0;
    }

    osk_active = 1;
    osk_closing = 0;
    return 1;

  error_free_intext:
    free(osk_params->data->intext);
  error_free_desc:
    free(osk_params->data->desc);
  error_free_data:
    free(osk_params->data);
  error_free_osk_params:
    free(osk_params);
    osk_params = NULL;
  error_return:
    return 0;
}

/*************************************************************************/

/**
 * osk_update:  Update the on-screen keyboard if it is active.  Must be
 * called once per frame while the on-screen keyboard is active; may be
 * called at any other time (the function does nothing in that case).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void osk_update(void)
{
    if (osk_active) {
        const int status = sceUtilityOskGetStatus();
        if (status == PSP_UTILITY_DIALOG_VISIBLE) {
            int res = sceUtilityOskUpdate(1);
            if (res < 0) {
                DMSG("sceUtilityOskUpdate() failed: %s", psp_strerror(res));
            }
        } else if (sceUtilityOskGetStatus() == PSP_UTILITY_DIALOG_QUIT) {
            sceUtilityOskShutdownStart();
        } else if (sceUtilityOskGetStatus() == PSP_UTILITY_DIALOG_FINISHED) {
            if (osk_closing) {
                reset_osk();
            }
        }
    }
}

/*************************************************************************/

/**
 * osk_status:  Return whether the on-screen keyboard is currently active.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the on-screen keyboard is active, else zero
 */
int osk_status(void)
{
    return osk_active;
}

/*************************************************************************/

/**
 * osk_result:  Return the result status from the on-screen keyboard.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Result status (OSK_RESULT_*)
 */
OSKResult osk_result(void)
{
    if (!osk_active || osk_closing) {
        return OSK_RESULT_NONE;
    } else if (sceUtilityOskGetStatus() != PSP_UTILITY_DIALOG_FINISHED) {
        return OSK_RESULT_RUNNING;
    } else if (osk_params->data->result == PSP_UTILITY_OSK_RESULT_UNCHANGED) {
        return OSK_RESULT_UNCHANGED;
    } else if (osk_params->data->result == PSP_UTILITY_OSK_RESULT_CHANGED) {
        return OSK_RESULT_CHANGED;
    } else if (osk_params->data->result == PSP_UTILITY_OSK_RESULT_CANCELLED) {
        return OSK_RESULT_CANCELLED;
    } else {
        DMSG("Weird result value %d from OSK", osk_params->data->result);
        return OSK_RESULT_ERROR;
    }
}

/*************************************************************************/

/**
 * osk_get_text:  Return the text entered by the user from the on-screen
 * keyboard in a newly malloc()ed buffer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Entered text, or NULL if not available (e.g., if the OSK was cancelled)
 */
char *osk_get_text(void)
{
    int result = osk_result();
    if (result != OSK_RESULT_UNCHANGED && result != OSK_RESULT_CHANGED) {
        return NULL;
    }

    char *text = utf16to8(osk_params->data->outtext);
    if (!text) {
        DMSG("Failed to convert entered text to UTF-8");
        return NULL;
    }

    return text;
}

/*************************************************************************/

/**
 * osk_close:  Close the on-screen keyboard and discard all associated
 * resources (including the entered text).  If the on-screen keyboard is
 * not active, this function does nothing.
 *
 * Even after calling this function, the caller MUST continue to call
 * osk_update() once per frame until osk_status() returns zero.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void osk_close(void)
{
    if (osk_active) {
        if (sceUtilityOskGetStatus() == PSP_UTILITY_DIALOG_FINISHED) {
            /* Free all resources immediately. */
            reset_osk();
        } else {
            /* Request a close, and free resources when the OS is done. */
            sceUtilityOskShutdownStart();
            osk_closing = 1;
        }
    }
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * reset_osk:  Free all local resources and reset the OSK to the inactive
 * state.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void reset_osk(void)
{
    PRECOND(osk_active, return);

    free(osk_params->data->outtext);
    free(osk_params->data->intext);
    free(osk_params->data->desc);
    free(osk_params->data);
    free(osk_params);
    osk_params = NULL;
    osk_closing = 0;
    osk_active = 0;
}

/*************************************************************************/

/**
 * utf8to16, utf16to8:  Convert a string between 8-bit and 16-bit Unicode
 * formats.  The returned string is stored in a newly malloc()ed buffer.
 *
 * [Parameters]
 *     str: String to convert
 * [Return value]
 *     Converted string, or NULL on error
 * [Notes]
 *     These functions only support Unicode characters with codepoints in
 *     the range 0-65535 (16-bit characters).
 */
static uint16_t *utf8to16(const char *str)
{
    /* Allocate a buffer big enough for the longest possible result; we'll
     * shrink it if necessary when we're done. */
    const uint32_t bufsize = (strlen(str) + 1) * 2;
    uint16_t *out = malloc(bufsize);
    if (!out) {
        DMSG("Can't allocate %u bytes", bufsize);
        return NULL;
    }

    /* Convert the string, character by character. */
    uint32_t pos = 0, len = 0;
    while (str[pos] != 0) {
        const uint8_t ch = (uint8_t)str[pos++];
        if (ch < 0x80) {
            out[len++] = ch;
        } else if (ch < 0xC0) {
            /* Continuation bytes are invalid as the first byte of a UTF-8
             * sequence. */
            DMSG("Invalid continuation byte 0x%02X at offset %u", ch, pos-1);
            goto fail;
        } else if (ch < 0xE0) {
            const uint8_t ch_1 = (uint8_t)str[pos++];
            if (ch_1 < 0x80 || ch_1 >= 0xC0) {
                /* The required continuation byte is missing, so treat this
                 * character as unknown and restart processing on the
                 * second byte (which we just checked). */
                DMSG("Missing continuation byte at offset %u (got 0x%02X)",
                     pos-1, ch_1);
                goto fail;
            } else if (ch < 0xC2) {
                /* Characters with codepoints less than 128 must be coded
                 * using the single-byte format; for example, C1 9C for the
                 * backslash character (U+005C) is invalid.  This is a
                 * common attack vector against security vulnerabilities,
                 * so we explicitly disallow such invalid forms. */
                DMSG("Invalid extended form 0x%02X 0x%02X at offset %u",
                     pos-2, ch, ch_1);
                goto fail;
            } else {
                out[len++] = (ch   & 0x1F) << 6
                           | (ch_1 & 0x3F) << 0;
            }
        } else if (ch < 0xF0) {
            const uint8_t ch_1 = (uint8_t)str[pos++];
            const uint8_t ch_2 = (uint8_t)str[pos++];
            if (ch_1 < 0x80 || ch_1 >= 0xC0) {
                DMSG("Missing continuation byte at offset %u (got 0x%02X)",
                     pos-2, ch_1);
                goto fail;
            } else if (ch_2 < 0x80 || ch_2 >= 0xC0) {
                DMSG("Missing continuation byte at offset %u (got 0x%02X)",
                     pos-1, ch_2);
                goto fail;
            } else if (ch == 0xE0 && ch_1 < 0xA0) {
                DMSG("Invalid extended form 0x%02X 0x%02X 0x%02X at offset %u",
                     pos-3, ch, ch_1, ch_2);
                goto fail;
            } else {
                out[len++] = (ch   & 0x0F) << 12
                           | (ch_1 & 0x3F) <<  6
                           | (ch_2 & 0x3F) <<  0;
                if (out[len-1] >= 0xD800 && out[len-1] < 0xE000) {
                    DMSG("Invalid surrogate 0x%04X at offset %u",
                         out[len-1], pos-3);
                    goto fail;
                }
            }
        } else {
            DMSG("Out-of-range codepoint with first byte 0x%02X at offset %u",
                 ch, pos-1);
            goto fail;
        }
    }

    /* Append a terminating null. */
    out[len++] = 0;

    /* If we ended up with fewer characters than we allocated space for,
     * shrink the output buffer before returning it. */
    if (len*2 < bufsize) {
        /* This should never fail, but just in case, save the result in a
         * temporary variable and check for NULL first. */
        uint16_t *new_out = realloc(out, len*2);
        if (new_out) {
            out = new_out;
        }
    }

    return out;

  fail:
    free(out);
    return NULL;
}

/*----------------------------------*/

static char *utf16to8(const uint16_t *str)
{
    /* strlen() only works on byte streams, so we have to calculate the
     * input string length manually. */
    uint32_t str_len = 0;
    while (str[str_len] != 0) {
        str_len++;
    }

    const uint32_t bufsize = str_len*3 + 1;
    char *out = malloc(bufsize);
    if (!out) {
        DMSG("Can't allocate %u bytes", bufsize);
        return NULL;
    }

    uint32_t pos = 0, len = 0;
    while (str[pos] != 0) {
        const uint16_t ch = str[pos++];
        if (ch < 0x80) {
            out[len++] = ch;
        } else if (ch < 0x800) {
            out[len++] = 0xC0 | (ch>>6 & 0x1F);
            out[len++] = 0x80 | (ch>>0 & 0x3F);
        } else if (ch >= 0xD800 && ch < 0xE000) {
            DMSG("Surrogate 0x%04X found at offset %u (not supported)",
                 ch, pos-1);
            goto fail;
        } else {
            out[len++] = 0xE0 | (ch>>12 & 0x0F);
            out[len++] = 0x80 | (ch>> 6 & 0x3F);
            out[len++] = 0x80 | (ch>> 0 & 0x3F);
        }
    }

    out[len++] = 0;

    if (len < bufsize) {
        char *new_out = realloc(out, len);
        if (new_out) {
            out = new_out;
        }
    }

    return out;

  fail:
    free(out);
    return NULL;
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
