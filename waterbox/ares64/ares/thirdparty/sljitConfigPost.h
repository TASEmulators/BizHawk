#ifdef __cplusplus
extern "C" {
#endif

//custom allocator
void* sljit_nall_malloc_exec(sljit_uw size, void* exec_allocator_data);

#ifdef __cplusplus
}
#endif
