//custom allocator
#define SLJIT_EXECUTABLE_ALLOCATOR      0
#define SLJIT_MALLOC_EXEC(size, data)   sljit_nall_malloc_exec((size), (data))
#define SLJIT_FREE_EXEC(ptr, data)      0
#define SLJIT_EXEC_OFFSET(ptr)          0

//debug-only options
#if !defined(BUILD_DEBUG)
#define SLJIT_DEBUG     0
#define SLJIT_VERBOSE   0
#endif
