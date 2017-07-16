CC = x86_64-nt64-midipix-gcc

CCFLAGS:=-I. \
	-I../emulibc \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c99 -fomit-frame-pointer -fvisibility=hidden \
	-falign-functions=16 \
	-DLSB_FIRST -DNDEBUG -DEMU_F68K -D_USE_CZ80 \
	-O3 -flto

TARGET = picodrive.wbx

LDFLAGS = -Wl,--dynamicbase,--export-all-symbols

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c')
OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.c=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

all: $(TARGET)

.PHONY: clean all

$(TARGET).in: $(OBJS)
	@$(CC) -o $@ $(LDFLAGS) $(CCFLAGS) $(OBJS) ../emulibc/libemuhost.so

$(TARGET): $(TARGET).in
	strip $< -o $@ -R /4 -R /14 -R /29 -R /41 -R /55 -R /67 -R /78 -R /89 -R /104
#	cp $< $@

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET).in
	rm -f $(TARGET)

#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
