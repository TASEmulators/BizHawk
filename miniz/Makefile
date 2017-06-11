CXX = gcc
CXXFLAGS = -Wall -O3 -std=c99 -fomit-frame-pointer -Wno-attributes -s

MACHINE = $(shell $(CXX) -dumpmachine)
ifneq (,$(findstring i686,$(MACHINE)))
	ARCH = 32
else ifneq (,$(findstring x86_64,$(MACHINE)))
	ARCH = 64
else
	$(error Unknown arch)
endif

LDFLAGS_32 =
LDFLAGS_64 =
LDFLAGS = -shared $(LDFLAGS_$(ARCH)) $(CXXFLAGS)

TARGET = libminiz.dll
RM = rm
CP = cp

SRCS = miniz.c

OBJS = $(SRCS:.c=.o)
DEST_32 = ../output/dll
DEST_64 = ../output64/dll

all: $(TARGET)

%.o: %.c
	$(CXX) -c -o $@ $< $(CXXFLAGS)

$(TARGET) : $(OBJS)
	$(CXX) -o $@ $(LDFLAGS) $(OBJS)

clean:
	$(RM) $(OBJS)
	$(RM) $(TARGET)
	
install:
	$(CP) $(TARGET) $(DEST_$(ARCH))
