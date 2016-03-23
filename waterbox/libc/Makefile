CC:=gcc
TARGET:=emulibc.a

CFLAGS:=-Wall -Wextra -pedantic -Wno-unused-parameter -Wshadow \
	-Wpointer-arith -Wwrite-strings -Wmissing-declarations \
	-Wno-long-long -Wuninitialized -Wno-deprecated-declarations \
	-Wredundant-decls -Winline -Wcast-align -Wno-format \
	-Wnested-externs -Wstrict-prototypes -Wmissing-prototypes \
	-Wno-unused-but-set-variable -Wno-parentheses \
	-Iinternals -Iincludes -Icompileincludes \
	-ffreestanding -std=c11 -D_PDCLIB_BUILD -nostdinc -nostdlib \
	-mcmodel=large -O2

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c' -o -name '*.s')
OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.c=.o)
_OBJS:=$(_OBJS:.s=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CFLAGS)

$(OBJ_DIR)/%.o: %.s
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CFLAGS)

$(TARGET): $(OBJS)
	@ar rcs $@ $^

all: $(TARGET)

.PHONY: clean all

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET)

print-% : ; @echo $* = $($*)
