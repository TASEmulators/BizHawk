include ../common.mak

CCFLAGS := $(CCFLAGS) -Icore -Iutil -Icore/m68k -Icore/z80 -Icore/input_hw \
	-Icore/cart_hw -Icore/cart_hw/svp -Icore/sound -Icore/ntsc -Icore/cd_hw \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer \
	-DLSB_FIRST -DUSE_32BPP_RENDERING -DINLINE=static\ __inline__ \
	-O3 -flto

TARGET := gpgx.wbx

LDFLAGS := $(LDFLAGS)

SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c')
OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.c=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

all: $(TARGET)

.PHONY: clean all

$(TARGET): $(OBJS) $(EMULIBC_OBJS)
	@$(CC) -o $@ $(LDFLAGS) $(CCFLAGS) $(OBJS) $(EMULIBC_OBJS)

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET)

install: $(TARGET)
	cp -f $< $(OUTPUTDLL_DIR)
	gzip --stdout --best $< > $(OUTPUTDLL_DIR)/$<.gz
