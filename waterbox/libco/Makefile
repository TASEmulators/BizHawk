CC = x86_64-nt64-midipix-gcc
AS = nasm

CCFLAGS:= -Wall -O3 -std=c99
ASFLAGS:= -f elf64

TARGET = libco.so

LDFLAGS = -shared

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c')
ASRCS:=$(shell find $(ROOT_DIR) -type f -name '*.s')

OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.c=.c.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))
_AOBJS:=$(ASRCS:.s=.s.o)
AOBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_AOBJS))

$(OBJ_DIR)/%.c.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

$(OBJ_DIR)/%.s.o: %.s
	@mkdir -p $(@D)
	@$(AS) $(ASFLAGS) -o $@ $<

all: $(TARGET)

.PHONY: clean all

$(TARGET): $(OBJS) $(AOBJS)
	@$(CC) -o $@ $(LDFLAGS) $(CCFLAGS) $(OBJS) $(AOBJS)
	@mv $(TARGET) $(TARGET).in
	@strip $(TARGET).in -o $(TARGET) -R /4 -R /14 -R /29 -R /41 -R /55 -R /67 -R /78 -R /89 -R /104

clean:
	@rm -rf $(OBJ_DIR)
	@rm -f $(TARGET).in
	@rm -f $(TARGET)

#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
