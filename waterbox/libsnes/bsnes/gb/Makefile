# Make hacks
.INTERMEDIATE:

# Set target, configuration, version and destination folders

PLATFORM := $(shell uname -s)
ifneq ($(findstring MINGW,$(PLATFORM)),)
PLATFORM := windows32
USE_WINDRES := true
endif

ifneq ($(findstring MSYS,$(PLATFORM)),)
PLATFORM := windows32
endif

ifeq ($(PLATFORM),windows32)
_ := $(shell chcp 65001)
EXESUFFIX:=.exe
NATIVE_CC = clang -IWindows -Wno-deprecated-declarations --target=i386-pc-windows
else
EXESUFFIX:=
NATIVE_CC := cc
endif

PB12_COMPRESS := build/pb12$(EXESUFFIX)

ifeq ($(PLATFORM),Darwin)
DEFAULT := cocoa
else
DEFAULT := sdl
endif

default: $(DEFAULT)

ifeq ($(MAKECMDGOALS),)
MAKECMDGOALS := $(DEFAULT)
endif

include version.mk
export VERSION
CONF ?= debug
SDL_AUDIO_DRIVER ?= sdl

BIN := build/bin
OBJ := build/obj
BOOTROMS_DIR ?= $(BIN)/BootROMs

ifdef DATA_DIR
CFLAGS += -DDATA_DIR="\"$(DATA_DIR)\""
endif

# Set tools

# Use clang if it's available.
ifeq ($(origin CC),default)
ifneq (, $(shell which clang))
CC := clang 
endif
endif

# Find libraries with pkg-config if available.
ifneq (, $(shell which pkg-config))
PKG_CONFIG := pkg-config
endif

ifeq ($(PLATFORM),windows32)
# To force use of the Unix version instead of the Windows version
MKDIR := $(shell which mkdir)
else
MKDIR := mkdir
endif

ifeq ($(CONF),native_release)
override CONF := release
LDFLAGS += -march=native -mtune=native
CFLAGS += -march=native -mtune=native
endif

ifeq ($(CONF),fat_release)
override CONF := release
FAT_FLAGS += -arch x86_64 -arch arm64
endif



# Set compilation and linkage flags based on target, platform and configuration

OPEN_DIALOG = OpenDialog/gtk.c
NULL := /dev/null

ifeq ($(PLATFORM),windows32)
OPEN_DIALOG = OpenDialog/windows.c
NULL := NUL
endif

ifeq ($(PLATFORM),Darwin)
OPEN_DIALOG = OpenDialog/cocoa.m
endif

# These must come before the -Wno- flags
WARNINGS += -Werror -Wall -Wno-unknown-warning -Wno-unknown-warning-option
WARNINGS += -Wno-nonnull -Wno-unused-result -Wno-strict-aliasing -Wno-multichar -Wno-int-in-bool-context

# Only add this flag if the compiler supports it
ifeq ($(shell $(CC) -x c -c $(NULL) -o $(NULL) -Werror -Wpartial-availability 2> $(NULL); echo $$?),0)
WARNINGS += -Wpartial-availability
endif

# GCC's implementation of this warning has false positives, so we skip it
ifneq ($(shell $(CC) --version 2>&1 | grep "gcc"), )
WARNINGS += -Wno-maybe-uninitialized
endif

CFLAGS += $(WARNINGS)

CFLAGS += -std=gnu11 -D_GNU_SOURCE -DVERSION="$(VERSION)" -I. -D_USE_MATH_DEFINES

ifeq (,$(PKG_CONFIG))
SDL_CFLAGS := $(shell sdl2-config --cflags)
SDL_LDFLAGS := $(shell sdl2-config --libs)
else
SDL_CFLAGS := $(shell $(PKG_CONFIG) --cflags sdl2)
SDL_LDFLAGS := $(shell $(PKG_CONFIG) --libs sdl2)
endif
ifeq (,$(PKG_CONFIG))
GL_LDFLAGS := -lGL
else
GL_CFLAGS := $(shell $(PKG_CONFIG) --cflags gl)
GL_LDFLAGS := $(shell $(PKG_CONFIG) --libs gl || echo -lGL)
endif
ifeq ($(PLATFORM),windows32)
CFLAGS += -IWindows -Drandom=rand --target=i386-pc-windows
LDFLAGS += -lmsvcrt -lcomdlg32 -luser32 -lshell32 -lSDL2main -Wl,/MANIFESTFILE:NUL --target=i386-pc-windows
SDL_LDFLAGS := -lSDL2
GL_LDFLAGS := -lopengl32
else
LDFLAGS += -lc -lm -ldl
endif

ifeq ($(PLATFORM),Darwin)
SYSROOT := $(shell xcodebuild -sdk macosx -version Path 2> $(NULL))
ifeq ($(SYSROOT),)
SYSROOT := /Library/Developer/CommandLineTools/SDKs/$(shell ls /Library/Developer/CommandLineTools/SDKs/ | grep 10 | tail -n 1)
endif
ifeq ($(SYSROOT),/Library/Developer/CommandLineTools/SDKs/)
$(error Could not find a macOS SDK)
endif

CFLAGS += -F/Library/Frameworks -mmacosx-version-min=10.9 -isysroot $(SYSROOT)
OCFLAGS += -x objective-c -fobjc-arc -Wno-deprecated-declarations -isysroot $(SYSROOT)
LDFLAGS += -framework AppKit -framework PreferencePanes -framework Carbon -framework QuartzCore -weak_framework Metal -weak_framework MetalKit -mmacosx-version-min=10.9 -isysroot $(SYSROOT)
GL_LDFLAGS := -framework OpenGL
endif
CFLAGS += -Wno-deprecated-declarations
ifeq ($(PLATFORM),windows32)
CFLAGS += -Wno-deprecated-declarations # Seems like Microsoft deprecated every single LIBC function
LDFLAGS += -Wl,/NODEFAULTLIB:libcmt.lib
endif

ifeq ($(CONF),debug)
CFLAGS += -g
else ifeq ($(CONF), release)
CFLAGS += -O3 -DNDEBUG
STRIP := strip
ifeq ($(PLATFORM),Darwin)
LDFLAGS += -Wl,-exported_symbols_list,$(NULL)
STRIP := -@true
endif
ifneq ($(PLATFORM),windows32)
LDFLAGS += -flto
CFLAGS += -flto
LDFLAGS += -Wno-lto-type-mismatch # For GCC's LTO
endif

else
$(error Invalid value for CONF: $(CONF). Use "debug", "release" or "native_release")
endif

# Define our targets

ifeq ($(PLATFORM),windows32)
SDL_TARGET := $(BIN)/SDL/sameboy.exe $(BIN)/SDL/sameboy_debugger.exe $(BIN)/SDL/SDL2.dll
TESTER_TARGET := $(BIN)/tester/sameboy_tester.exe
else
SDL_TARGET := $(BIN)/SDL/sameboy
TESTER_TARGET := $(BIN)/tester/sameboy_tester
endif

cocoa: $(BIN)/SameBoy.app
quicklook: $(BIN)/SameBoy.qlgenerator
sdl: $(SDL_TARGET) $(BIN)/SDL/dmg_boot.bin $(BIN)/SDL/cgb_boot.bin $(BIN)/SDL/agb_boot.bin $(BIN)/SDL/sgb_boot.bin $(BIN)/SDL/sgb2_boot.bin $(BIN)/SDL/LICENSE $(BIN)/SDL/registers.sym $(BIN)/SDL/background.bmp $(BIN)/SDL/Shaders
bootroms: $(BIN)/BootROMs/agb_boot.bin $(BIN)/BootROMs/cgb_boot.bin $(BIN)/BootROMs/dmg_boot.bin $(BIN)/BootROMs/sgb_boot.bin $(BIN)/BootROMs/sgb2_boot.bin
tester: $(TESTER_TARGET) $(BIN)/tester/dmg_boot.bin $(BIN)/tester/cgb_boot.bin $(BIN)/tester/agb_boot.bin $(BIN)/tester/sgb_boot.bin $(BIN)/tester/sgb2_boot.bin
all: cocoa sdl tester libretro

# Get a list of our source files and their respective object file targets

CORE_SOURCES := $(shell ls Core/*.c)
SDL_SOURCES := $(shell ls SDL/*.c) $(OPEN_DIALOG) SDL/audio/$(SDL_AUDIO_DRIVER).c
TESTER_SOURCES := $(shell ls Tester/*.c)

ifeq ($(PLATFORM),Darwin)
COCOA_SOURCES := $(shell ls Cocoa/*.m) $(shell ls HexFiend/*.m) $(shell ls JoyKit/*.m)
QUICKLOOK_SOURCES := $(shell ls QuickLook/*.m) $(shell ls QuickLook/*.c)
endif

ifeq ($(PLATFORM),windows32)
CORE_SOURCES += $(shell ls Windows/*.c)
endif

CORE_OBJECTS := $(patsubst %,$(OBJ)/%.o,$(CORE_SOURCES))
COCOA_OBJECTS := $(patsubst %,$(OBJ)/%.o,$(COCOA_SOURCES))
QUICKLOOK_OBJECTS := $(patsubst %,$(OBJ)/%.o,$(QUICKLOOK_SOURCES))
SDL_OBJECTS := $(patsubst %,$(OBJ)/%.o,$(SDL_SOURCES))
TESTER_OBJECTS := $(patsubst %,$(OBJ)/%.o,$(TESTER_SOURCES))

# Automatic dependency generation

ifneq ($(filter-out clean bootroms libretro %.bin, $(MAKECMDGOALS)),)
-include $(CORE_OBJECTS:.o=.dep)
ifneq ($(filter $(MAKECMDGOALS),sdl),)
-include $(SDL_OBJECTS:.o=.dep)
endif
ifneq ($(filter $(MAKECMDGOALS),tester),)
-include $(TESTER_OBJECTS:.o=.dep)
endif
ifneq ($(filter $(MAKECMDGOALS),cocoa),)
-include $(COCOA_OBJECTS:.o=.dep)
endif
endif

$(OBJ)/SDL/%.dep: SDL/%
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) $(SDL_CFLAGS) $(GL_CFLAGS) -MT $(OBJ)/$^.o -M $^ -c -o $@

$(OBJ)/%.dep: %
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) -MT $(OBJ)/$^.o -M $^ -c -o $@

# Compilation rules

$(OBJ)/Core/%.c.o: Core/%.c
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) $(FAT_FLAGS) -DGB_INTERNAL -c $< -o $@

$(OBJ)/SDL/%.c.o: SDL/%.c
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) $(FAT_FLAGS) $(SDL_CFLAGS) $(GL_CFLAGS) -c $< -o $@

$(OBJ)/%.c.o: %.c
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) $(FAT_FLAGS) -c $< -o $@
	
# HexFiend requires more flags
$(OBJ)/HexFiend/%.m.o: HexFiend/%.m
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) $(FAT_FLAGS) $(OCFLAGS) -c $< -o $@ -fno-objc-arc -include HexFiend/HexFiend_2_Framework_Prefix.pch
	
$(OBJ)/%.m.o: %.m
	-@$(MKDIR) -p $(dir $@)
	$(CC) $(CFLAGS) $(FAT_FLAGS) $(OCFLAGS) -c $< -o $@

# Cocoa Port

$(BIN)/SameBoy.app: $(BIN)/SameBoy.app/Contents/MacOS/SameBoy \
                    $(shell ls Cocoa/*.icns Cocoa/*.png) \
                    Cocoa/License.html \
                    Cocoa/Info.plist \
                    Misc/registers.sym \
                    $(BIN)/SameBoy.app/Contents/Resources/dmg_boot.bin \
                    $(BIN)/SameBoy.app/Contents/Resources/cgb_boot.bin \
                    $(BIN)/SameBoy.app/Contents/Resources/agb_boot.bin \
                    $(BIN)/SameBoy.app/Contents/Resources/sgb_boot.bin \
                    $(BIN)/SameBoy.app/Contents/Resources/sgb2_boot.bin \
                    $(patsubst %.xib,%.nib,$(addprefix $(BIN)/SameBoy.app/Contents/Resources/Base.lproj/,$(shell cd Cocoa;ls *.xib))) \
                    $(BIN)/SameBoy.qlgenerator \
                    Shaders
	$(MKDIR) -p $(BIN)/SameBoy.app/Contents/Resources
	cp Cocoa/*.icns Cocoa/*.png Misc/registers.sym $(BIN)/SameBoy.app/Contents/Resources/
	sed s/@VERSION/$(VERSION)/ < Cocoa/Info.plist > $(BIN)/SameBoy.app/Contents/Info.plist
	cp Cocoa/License.html $(BIN)/SameBoy.app/Contents/Resources/Credits.html
	$(MKDIR) -p $(BIN)/SameBoy.app/Contents/Resources/Shaders
	cp Shaders/*.fsh Shaders/*.metal $(BIN)/SameBoy.app/Contents/Resources/Shaders
	$(MKDIR) -p $(BIN)/SameBoy.app/Contents/Library/QuickLook/
	cp -rf $(BIN)/SameBoy.qlgenerator $(BIN)/SameBoy.app/Contents/Library/QuickLook/

$(BIN)/SameBoy.app/Contents/MacOS/SameBoy: $(CORE_OBJECTS) $(COCOA_OBJECTS)
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS) $(FAT_FLAGS) -framework OpenGL -framework AudioUnit -framework AVFoundation -framework CoreVideo -framework CoreMedia -framework IOKit
ifeq ($(CONF), release)
	$(STRIP) $@
endif

$(BIN)/SameBoy.app/Contents/Resources/Base.lproj/%.nib: Cocoa/%.xib
	ibtool --compile $@ $^ 2>&1 | cat -
	
# Quick Look generator

$(BIN)/SameBoy.qlgenerator: $(BIN)/SameBoy.qlgenerator/Contents/MacOS/SameBoyQL \
                            $(shell ls QuickLook/*.png) \
                            QuickLook/Info.plist \
                            $(BIN)/SameBoy.qlgenerator/Contents/Resources/cgb_boot_fast.bin
	$(MKDIR) -p $(BIN)/SameBoy.qlgenerator/Contents/Resources
	cp QuickLook/*.png $(BIN)/SameBoy.qlgenerator/Contents/Resources/
	sed s/@VERSION/$(VERSION)/ < QuickLook/Info.plist > $(BIN)/SameBoy.qlgenerator/Contents/Info.plist

# Currently, SameBoy.app includes two "copies" of each Core .o file once in the app itself and
# once in the QL Generator. It should probably become a dylib instead.
$(BIN)/SameBoy.qlgenerator/Contents/MacOS/SameBoyQL: $(CORE_OBJECTS) $(QUICKLOOK_OBJECTS)
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS) $(FAT_FLAGS) -Wl,-exported_symbols_list,QuickLook/exports.sym -bundle -framework Cocoa -framework Quicklook

# cgb_boot_fast.bin is not a standard boot ROM, we don't expect it to exist in the user-provided
# boot ROM directory.
$(BIN)/SameBoy.qlgenerator/Contents/Resources/cgb_boot_fast.bin: $(BIN)/BootROMs/cgb_boot_fast.bin
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@
	
# SDL Port

# Unix versions build only one binary
$(BIN)/SDL/sameboy: $(CORE_OBJECTS) $(SDL_OBJECTS)
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS) $(FAT_FLAGS) $(SDL_LDFLAGS) $(GL_LDFLAGS)
ifeq ($(CONF), release)
	$(STRIP) $@
endif

# Windows version builds two, one with a conole and one without it
$(BIN)/SDL/sameboy.exe: $(CORE_OBJECTS) $(SDL_OBJECTS) $(OBJ)/Windows/resources.o
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS) $(SDL_LDFLAGS) $(GL_LDFLAGS) -Wl,/subsystem:windows

$(BIN)/SDL/sameboy_debugger.exe: $(CORE_OBJECTS) $(SDL_OBJECTS) $(OBJ)/Windows/resources.o
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS) $(SDL_LDFLAGS) $(GL_LDFLAGS) -Wl,/subsystem:console

ifneq ($(USE_WINDRES),)
$(OBJ)/%.o: %.rc
	-@$(MKDIR) -p $(dir $@)
	windres --preprocessor cpp -DVERSION=\"$(VERSION)\" $^ $@
else
$(OBJ)/%.res: %.rc
	-@$(MKDIR) -p $(dir $@)
	rc /fo $@ /dVERSION=\"$(VERSION)\" $^ 

%.o: %.res
	cvtres /OUT:"$@" $^
endif

# We must provide SDL2.dll with the Windows port.
$(BIN)/SDL/SDL2.dll:
	@$(eval MATCH := $(shell where $$LIB:SDL2.dll))
	cp "$(MATCH)" $@

# Tester

$(BIN)/tester/sameboy_tester: $(CORE_OBJECTS) $(TESTER_OBJECTS)
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS)
ifeq ($(CONF), release)
	$(STRIP) $@
endif

$(BIN)/tester/sameboy_tester.exe: $(CORE_OBJECTS) $(SDL_OBJECTS)
	-@$(MKDIR) -p $(dir $@)
	$(CC) $^ -o $@ $(LDFLAGS) -Wl,/subsystem:console

$(BIN)/SDL/%.bin: $(BOOTROMS_DIR)/%.bin
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@

$(BIN)/tester/%.bin: $(BOOTROMS_DIR)/%.bin
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@

$(BIN)/SameBoy.app/Contents/Resources/%.bin: $(BOOTROMS_DIR)/%.bin
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@

$(BIN)/SDL/LICENSE: LICENSE
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@

$(BIN)/SDL/registers.sym: Misc/registers.sym
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@

$(BIN)/SDL/background.bmp: SDL/background.bmp
	-@$(MKDIR) -p $(dir $@)
	cp -f $^ $@

$(BIN)/SDL/Shaders: Shaders
	-@$(MKDIR) -p $@
	cp -rf Shaders/*.fsh $@

# Boot ROMs

$(OBJ)/%.2bpp: %.png
	-@$(MKDIR) -p $(dir $@)
	rgbgfx -h -u -o $@ $<

$(OBJ)/BootROMs/SameBoyLogo.pb12: $(OBJ)/BootROMs/SameBoyLogo.2bpp $(PB12_COMPRESS)
	$(realpath $(PB12_COMPRESS)) < $< > $@
	
$(PB12_COMPRESS): BootROMs/pb12.c
	$(NATIVE_CC) -std=c99 -Wall -Werror $< -o $@

$(BIN)/BootROMs/agb_boot.bin: BootROMs/cgb_boot.asm
$(BIN)/BootROMs/cgb_boot_fast.bin: BootROMs/cgb_boot.asm
$(BIN)/BootROMs/sgb2_boot: BootROMs/sgb_boot.asm

$(BIN)/BootROMs/%.bin: BootROMs/%.asm $(OBJ)/BootROMs/SameBoyLogo.pb12
	-@$(MKDIR) -p $(dir $@)
	rgbasm -i $(OBJ)/BootROMs/ -i BootROMs/ -o $@.tmp $<
	rgblink -o $@.tmp2 $@.tmp
	dd if=$@.tmp2 of=$@ count=1 bs=$(if $(findstring dmg,$@)$(findstring sgb,$@),256,2304) 2> $(NULL)
	@rm $@.tmp $@.tmp2

# Libretro Core (uses its own build system)
libretro:
	CFLAGS="$(WARNINGS)" $(MAKE) -C libretro

# Clean
clean:
	rm -rf build

.PHONY: libretro tester
