CC = x86_64-nt64-midipix-g++

CCFLAGS:= -I. -I../emulibc \
	-Wall -Werror=pointer-to-int-cast -Werror=int-to-pointer-cast -Werror=implicit-function-declaration \
	-std=c++0x -fomit-frame-pointer -fvisibility=hidden -fno-exceptions -fno-rtti \
	-DLSB_FIRST \
	-O3 -flto

TARGET = ss.wbx

LDFLAGS = -Wl,--dynamicbase,--export-all-symbols

ROOT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
SRCS:=$(shell find $(ROOT_DIR) -type f -name '*.cpp')
OBJ_DIR:=$(ROOT_DIR)/obj

_OBJS:=$(SRCS:.cpp=.o)
OBJS:=$(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

$(OBJ_DIR)/%.o: %.cpp
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

print-%:
	@echo $* = $($*)

#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
