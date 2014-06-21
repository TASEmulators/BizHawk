CXX = g++
CXXFLAGS = -Wall -DLSB_FIRST -I.. -Wno-multichar -O3
TARGET = bizswan.dll
LDFLAGS = -shared -static-libgcc -static-libstdc++ 
RM = rm
CP = cp

SRCS = \
	../eeprom.cpp \
	../gfx.cpp \
	../interrupt.cpp \
	../main.cpp \
	../memory.cpp \
	../rtc.cpp \
	../sound.cpp \
	../system.cpp \
	../tcache.cpp \
	../v30mz.cpp \
	../newstate.cpp \
	../Blip/Blip_Buffer.cpp

OBJS = $(SRCS:.cpp=.o)

all: $(TARGET)

%.o: %.cpp
	$(CXX) -c -o $@ $< $(CXXFLAGS)

$(TARGET) : $(OBJS)
	$(CXX) -o $@ $(LDFLAGS) $(OBJS)

clean:
	$(RM) $(OBJS)
	$(RM) $(TARGET)
	
install:
	$(CP) $(TARGET) ../../output/dll
