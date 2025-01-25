#ifndef MEMMAP_H_
#define MEMMAP_H_

typedef enum {
	READ_16,
	READ_8,
	WRITE_16,
	WRITE_8
} ftype;

#define MMAP_READ      0x01
#define MMAP_WRITE     0x02
#define MMAP_CODE      0x04
#define MMAP_PTR_IDX   0x08
#define MMAP_ONLY_ODD  0x10
#define MMAP_ONLY_EVEN 0x20
#define MMAP_FUNC_NULL 0x40
#define MMAP_BYTESWAP  0x80
#define MMAP_AUX_BUFF  0x100
#define MMAP_READ_CODE 0x200

typedef uint16_t (*read_16_fun)(uint32_t address, void * context);
typedef uint8_t (*read_8_fun)(uint32_t address, void * context);
typedef void * (*write_16_fun)(uint32_t address, void * context, uint16_t value);
typedef void * (*write_8_fun)(uint32_t address, void * context, uint8_t value);

typedef struct {
	uint32_t     start;
	uint32_t     end;
	uint32_t     mask;
	uint32_t     aux_mask;
	uint16_t     ptr_index;
	uint16_t     flags;
	void *       buffer;
	read_16_fun  read_16;
	write_16_fun write_16;
	read_8_fun   read_8;
	write_8_fun  write_8;
} memmap_chunk;

#endif //MEMMAP_H_
