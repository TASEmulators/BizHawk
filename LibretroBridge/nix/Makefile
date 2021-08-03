
LDLIBS := -ldl
LDFLAGS := -shared
CFLAGS := -fPIC -Os
CPPFLAGS := -fPIC -Os -fvisibility=hidden

ROOT_DIR := ..
OBJ_DIR := obj
sources := $(ROOT_DIR)/features_cpu.c $(ROOT_DIR)/nix/LibretroBridge.cpp $(ROOT_DIR)/libco/amd64.c
_OBJS := $(addsuffix .o,$(sources))
OBJS := $(patsubst $(ROOT_DIR)%,$(OBJ_DIR)%,$(_OBJS))

target := $(OBJ_DIR)/LibretroBridge.so
all: $(target)

$(OBJ_DIR)/%.c.o: $(ROOT_DIR)/%.c
	@mkdir -p $(@D)
	$(COMPILE.c) $< -o $@
$(OBJ_DIR)/%.cpp.o: $(ROOT_DIR)/%.cpp
	@mkdir -p $(@D)
	$(COMPILE.cpp) $< -o $@

$(target): $(OBJS)
	$(CXX) $(LDFLAGS) $^ $(LDLIBS) -o $@
	cp $(target) ../../Assets/dll
	if [ -d ../../output/dll ]; then \
	    cp $(target) ../../output/dll; \
	fi;

clean:
	rm -rf $(OBJ_DIR)
