CXX = g++
CXXFLAGS = -Wall -I.. -O3 -fno-exceptions -fomit-frame-pointer -std=c++11
TARGET = 6502XXX.dll
LDFLAGS = -shared -static-libgcc -static-libstdc++ $(CXXFLAGS)
RM = rm
CP = cp

SRCS = \
	../execute.cpp

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
	$(CP) $(TARGET) ../../../../../output/dll
