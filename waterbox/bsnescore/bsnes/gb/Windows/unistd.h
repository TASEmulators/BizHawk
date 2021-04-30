#include <io.h>
#define STDIN_FILENO 0
#define STDOUT_FILENO 1
#define STDERR_FILENO 2

#define read(...) _read(__VA_ARGS__)
#define write(...) _write(__VA_ARGS__)
