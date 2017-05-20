CC = x86_64-nt64-midipix-gcc

CCFLAGS:= -Wall -O2# -mcmodel=large 

TARGET = libemuhost.so

LDFLAGS = -shared

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

$(TARGET) : $(OBJS)
	@$(CC) -o $@ $(LDFLAGS) $(CCFLAGS) $(OBJS)

clean:
	rm -rf $(OBJ_DIR)
	rm -f $(TARGET)

#install:
#	$(CP) $(TARGET) $(DEST_$(ARCH))
