#include "open_dialog.h"
#include <stdlib.h>
#include <stdio.h>
#include <dlfcn.h>
#include <stdbool.h>
#include <string.h>

#define GTK_FILE_CHOOSER_ACTION_OPEN 0
#define GTK_RESPONSE_ACCEPT -3
#define GTK_RESPONSE_CANCEL -6


void *_gtk_file_chooser_dialog_new (const char *title,
                                    void *parent,
                                    int action,
                                    const char *first_button_text,
                                    ...);
bool _gtk_init_check (int *argc, char ***argv);
int _gtk_dialog_run(void *);
void _g_free(void *);
void _gtk_widget_destroy(void *);
char *_gtk_file_chooser_get_filename(void *);
void _g_log_set_default_handler (void *function, void *data);
void *_gtk_file_filter_new(void);
void _gtk_file_filter_add_pattern(void *filter, const char *pattern);
void _gtk_file_filter_set_name(void *filter, const char *name);
void _gtk_file_chooser_add_filter(void *dialog, void *filter);
void _gtk_main_iteration(void);
bool _gtk_events_pending(void);


#define LAZY(symbol) static typeof(_##symbol) *symbol = NULL;\
if (symbol == NULL) symbol = dlsym(handle, #symbol);\
if (symbol == NULL) goto lazy_error
#define TRY_DLOPEN(name) handle = handle? handle : dlopen(name, RTLD_NOW)

void nop(){}

char *do_open_rom_dialog(void)
{
    static void *handle = NULL;
    
    TRY_DLOPEN("libgtk-3.so");
    TRY_DLOPEN("libgtk-3.so.0");
    TRY_DLOPEN("libgtk-2.so");
    TRY_DLOPEN("libgtk-2.so.0");
    
    if (!handle) {
        goto lazy_error;
    }
    
    
    LAZY(gtk_init_check);
    LAZY(gtk_file_chooser_dialog_new);
    LAZY(gtk_dialog_run);
    LAZY(g_free);
    LAZY(gtk_widget_destroy);
    LAZY(gtk_file_chooser_get_filename);
    LAZY(g_log_set_default_handler);
    LAZY(gtk_file_filter_new);
    LAZY(gtk_file_filter_add_pattern);
    LAZY(gtk_file_filter_set_name);
    LAZY(gtk_file_chooser_add_filter);
    LAZY(gtk_events_pending);
    LAZY(gtk_main_iteration);
    
    /* Shut up GTK */
    g_log_set_default_handler(nop, NULL);
    
    gtk_init_check(0, 0);
    
    
    void *dialog = gtk_file_chooser_dialog_new("Open ROM",
                                               0,
                                               GTK_FILE_CHOOSER_ACTION_OPEN,
                                               "_Cancel", GTK_RESPONSE_CANCEL,
                                               "_Open", GTK_RESPONSE_ACCEPT,
                                               NULL );
    
    
    void *filter = gtk_file_filter_new();
    gtk_file_filter_add_pattern(filter, "*.gb");
    gtk_file_filter_add_pattern(filter, "*.gbc");
    gtk_file_filter_add_pattern(filter, "*.sgb");
    gtk_file_filter_add_pattern(filter, "*.isx");
    gtk_file_filter_set_name(filter, "Game Boy ROMs");
    gtk_file_chooser_add_filter(dialog, filter);
    
    int res = gtk_dialog_run (dialog);
    char *ret = NULL;
    
    if (res == GTK_RESPONSE_ACCEPT) { 
        char *filename;
        filename = gtk_file_chooser_get_filename(dialog);
        ret = strdup(filename);
        g_free(filename);
    }

    while (gtk_events_pending()) {
        gtk_main_iteration();
    }

    gtk_widget_destroy(dialog);

    while (gtk_events_pending()) {
        gtk_main_iteration();
    }
    return ret;
    
lazy_error:
    fprintf(stderr, "Failed to display GTK dialog\n");
    return NULL;
}
