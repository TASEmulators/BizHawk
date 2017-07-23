CC = x86_64-nt64-midipix-gcc
CPP = x86_64-nt64-midipix-g++

FLAGS:=-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-Wno-multichar \
	-fomit-frame-pointer \
	-O3 -flto

CCFLAGS:=$(FLAGS) \
	-std=gnu99 \
	-DLSB_FIRST -D_GNU_SOURCE -DGB_INTERNAL

CPPFLAGS:=$(FLAGS) -DSPC_NO_COPY_STATE_FUNCS -std=c++0x -D_GNU_SOURCE -DGB_INTERNAL

TARGET = sameboy.wbx

LDFLAGS = -Wl,--dynamicbase,--export-all-symbols

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
C_SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.c')
CPP_SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.cpp')
SRCS:=$(C_SRCS) $(CPP_SRCS)
OBJ_DIR:=$(ROOT_DIR)/obj

__OBJS:=$(SRCS:.c=.o)
_OBJS:=$(__OBJS:.cpp=.opp)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.c
	@mkdir -p $(@D)
	@$(CC) -c -o $@ $< $(CCFLAGS)

$(OBJ_DIR)/%.opp: %.cpp
	@mkdir -p $(@D)
	@$(CPP) -c -o $@ $< $(CPPFLAGS)

all: $(TARGET)

.PHONY: clean all

$(TARGET).in: $(OBJS)
	@$(CPP) -o $@ $(LDFLAGS) $(FLAGS) $(OBJS) ../emulibc/libemuhost.so

$(TARGET): $(TARGET).in
	strip $< -o $@ -R /4 -R /14 -R /29 -R /41 -R /55 -R /67 -R /78 -R /89 -R /104
#	cp $< $@

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET).in
	rm -f $(TARGET)

#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
