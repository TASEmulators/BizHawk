include ../common.mak

CCFLAGS := $(CCFLAGS) -Wall -O3 -flto

SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c')
OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.c=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

all: $(OBJS)

.PHONY: clean all

clean:
	rm -rf $(OBJ_DIR)
