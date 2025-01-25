#ifndef UTIL_H_
#define UTIL_H_

#include <stdio.h>
#include <time.h>
#include "tern.h"

typedef struct {
	char    *name;
	uint8_t is_dir;
} dir_entry;

#ifdef _WIN32
#define PATH_SEP "\\"
#else
#define PATH_SEP "/"
#endif

//Utility functions

//Allocates a new string containing the concatenation of first and second
char * alloc_concat(char const * first, char const * second);
//Allocates a new string containing the concatenation of the strings pointed to by parts
char * alloc_concat_m(int num_parts, char const ** parts);
//Returns a newly allocated string in which all variables in based are replaced with values from vars or the environment
char *replace_vars(char *base, tern_node *vars, uint8_t allow_env);
//Byteswaps a ROM image in memory
void byteswap_rom(int filesize, uint16_t *cart);
//Returns the size of a file using fseek and ftell
long file_size(FILE * f);
//Strips whitespace and non-printable characters from the beginning and end of a string
char * strip_ws(char * text);
//Inserts a null after the first word, returns a pointer to the second word
char * split_keyval(char * text);
//Checks if haystack starts with prefix
uint8_t startswith(const char *haystack, const char *prefix);
//Takes a binary byte buffer and produces a lowercase hex string
void bin_to_hex(uint8_t *output, uint8_t *input, uint64_t size);
//Takes an (optionally) null-terminated UTF16-BE string and converts a maximum of max_size code-units to UTF-8
char *utf16be_to_utf8(uint8_t *buf, uint32_t max_size);
//Returns the next Unicode codepoint from a utf-8 string
int utf8_codepoint(const char **text);
//Determines whether a character is a valid path separator for the current platform
char is_path_sep(char c);
//Determines whether a path is considered an absolute path on the current platform
char is_absolute_path(char *path);
//Returns the basename of a path with th extension (if any) stripped
char * basename_no_extension(const char *path);
//Returns the extension from a path or NULL if there is no extension
char *path_extension(char const *path);
//Returns true if the given path matches one of the extensions in the list
uint8_t path_matches_extensions(char *path, char **ext_list, uint32_t num_exts);
//Returns the directory portion of a path or NULL if there is no directory part
char *path_dirname(const char *path);
//Gets the smallest power of two that is >= a certain value, won't work for values > 0x80000000
uint32_t nearest_pow2(uint32_t val);
//Should be called by main with the value of argv[0] for use by get_exe_dir
void set_exe_str(char * str);
//Returns the directory the executable is in
char * get_exe_dir();
//Returns the user's home directory
char * get_home_dir();
//Returns an appropriate path for storing config files
char const *get_config_dir();
//Returns an appropriate path for saving non-config data like savestates
char const *get_userdata_dir();
//Reads a file bundled with the executable
char *read_bundled_file(char *name, uint32_t *sizeret);
//Retunrs an array of normal files and directories residing in a directory
dir_entry *get_dir_list(char *path, size_t *numret);
//Frees a dir list returned by get_dir_list
void free_dir_list(dir_entry *list, size_t numentries);
//Performs a case-insensitive sort by file name on a dir list
void sort_dir_list(dir_entry *list, size_t num_entries);
//Gets the modification time of a file
time_t get_modification_time(char *path);
//Recusrively creates a directory if it does not exist
int ensure_dir_exists(const char *path);
//Returns the contents of a symlink in a newly allocated string
char * readlink_alloc(char * path);
//Prints an error message to stderr and to a message box if not in headless mode and then exits
void fatal_error(char *format, ...);
//Prints an information message to stdout and to a message box if not in headless mode and not attached to a console
void info_message(char *format, ...);
//Prints an information message to stderr and to a message box if not in headless mode and not attached to a console
void warning(char *format, ...);
//Prints a debug message to stdout
void debug_message(char *format, ...);
//Disables output of info and debug messages to stdout
void disable_stdout_messages(void);
//Returns stdout disable status
uint8_t is_stdout_enabled(void);
//Deletes a file, returns true on success, false on failure
uint8_t delete_file(char *path);
//Initializes the socket library on platforms that need it
void socket_init(void);
//Sets a sockt to blocking or non-blocking mode
int socket_blocking(int sock, int should_block);
//Close a socket
void socket_close(int sock);
//Return the last error on a socket operation
int socket_last_error(void);
//Returns if the last socket error was EAGAIN/EWOULDBLOCK
int socket_error_is_wouldblock(void);

#endif //UTIL_H_
