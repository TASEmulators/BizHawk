# disable built-in rules and variables
MAKEFLAGS := Rr
.SUFFIXES:

[0-9] = 0 1 2 3 4 5 6 7 8 9
[A-Z] = A B C D E F G H I J K L M N O P Q R S T U V W X Y Z
[a-z] = a b c d e f g h i j k l m n o p q r s t u v w x y z
[markup] = ` ~ ! @ \# $$ % ^ & * ( ) - _ = + [ { ] } \ | ; : ' " , < . > / ?
[all] = $([0-9]) $([A-Z]) $([a-z]) $([markup])
[empty] :=
[space] := $([empty]) $([empty])

# platform detection
ifeq ($(platform),)
  ifeq ($(OS),Windows_NT)
    platform := windows
  endif
endif

ifeq ($(platform),)
  uname := $(shell uname)
  ifeq ($(uname),)
    platform := windows
  else ifneq ($(findstring Windows,$(uname)),)
    platform := windows
  else ifneq ($(findstring NT,$(uname)),)
    platform := windows
  else ifneq ($(findstring Darwin,$(uname)),)
    platform := macos
  else ifneq ($(findstring Linux,$(uname)),)
    platform := linux
  else ifneq ($(findstring BSD,$(uname)),)
    platform := bsd
  else
    $(error unknown platform, please specify manually.)
  endif
endif

# common commands
ifeq ($(shell echo ^^),^)
  # cmd
  delete  = $(info Deleting $1 ...) @del /q $(subst /,\,$1)
  rdelete = $(info Deleting $1 ...) @del /s /q $(subst /,\,$1) && if exist $(subst /,\,$1) (rmdir /s /q $(subst /,\,$1))
else
  # sh
  delete  = $(info Deleting $1 ...) @rm -f $1
  rdelete = $(info Deleting $1 ...) @rm -rf $1
endif

compiler.c      = $(compiler) -x c -std=c11
compiler.cpp    = $(compiler) -x c++ -std=c++17 -fno-operator-names
compiler.objc   = $(compiler) -x objective-c -std=c11
compiler.objcpp = $(compiler) -x objective-c++ -std=c++17 -fno-operator-names

flags.c      = -x c -std=c11
flags.cpp    = -x c++ -std=c++17 -fno-operator-names
flags.objc   = -x objective-c -std=c11
flags.objcpp = -x objective-c++ -std=c++17 -fno-operator-names
flags.deps   = -MMD -MP -MF $(@:.o=.d)

# compiler detection
ifeq ($(compiler),)
  ifeq ($(platform),windows)
    compiler := g++
    compiler.cpp = $(compiler) -x c++ -std=gnu++17 -fno-operator-names
    flags.cpp = -x c++ -std=gnu++17 -fno-operator-names
  else ifeq ($(platform),macos)
    compiler := clang++
  else ifeq ($(platform),linux)
    compiler := g++
  else ifeq ($(platform),bsd)
    compiler := clang++
  else
    compiler := g++
  endif
endif

# architecture detection
ifeq ($(arch),)
  machine := $(shell $(compiler) -dumpmachine)
  ifneq ($(filter amd64-% x86_64-%,$(machine)),)
    arch := amd64
  else ifneq ($(filter arm64-% aarch64-%,$(machine)),)
    arch := arm64
  else
    $(error unknown arch, please specify manually.)
  endif
endif

# build optimization levels
ifeq ($(build),debug)
  symbols = true
  flags += -Og -DBUILD_DEBUG
else ifeq ($(build),stable)
  lto = true
  flags += -O1 -DBUILD_STABLE
else ifeq ($(build),minified)
  lto = true
  flags += -Os -DBUILD_MINIFIED
else ifeq ($(build),release)
  lto = true
  flags += -O2 -DBUILD_RELEASE
else ifeq ($(build),optimized)
  lto = true
  flags += -O3 -fomit-frame-pointer -DBUILD_OPTIMIZED
else
  $(error unrecognized build type.)
endif

# debugging information
ifeq ($(symbols),true)
  flags += -g
  ifeq ($(platform),windows)
    ifeq ($(findstring clang++,$(compiler)),clang++)
      flags   += -gcodeview
      options += -Wl,-pdb=
    endif
  endif
endif

# link-time optimization
ifeq ($(lto),true)
  flags   += -flto
  options += -fwhole-program
  ifneq ($(findstring clang++,$(compiler)),clang++)
    flags   += -fwhole-program -fno-fat-lto-objects
    options += -flto=jobserver
  else
    options += -flto=thin
  endif
endif

# openmp support
ifeq ($(openmp),true)
  # macOS Xcode does not ship with OpenMP support
  ifneq ($(platform),macos)
    flags   += -fopenmp
    options += -fopenmp
  endif
endif

# clang settings
ifeq ($(findstring clang++,$(compiler)),clang++)
  flags += -fno-strict-aliasing -fwrapv
  ifeq ($(arch),arm64)
    # work around bad interaction with alignas(n) when n >= 4096
    flags += -mno-global-merge
  endif
# gcc settings
else ifeq ($(findstring g++,$(compiler)),g++)
  flags += -fno-strict-aliasing -fwrapv -Wno-trigraphs
endif

# windows settings
ifeq ($(platform),windows)
  options += -mthreads -lpthread -lws2_32 -lole32
  options += $(if $(findstring clang++,$(compiler)),-fuse-ld=lld)
  options += $(if $(findstring g++,$(compiler)),-static -static-libgcc -static-libstdc++)
  options += $(if $(findstring true,$(console)),-mconsole,-mwindows)
  windres := windres
endif

# macos settings
ifeq ($(platform),macos)
  flags   += -stdlib=libc++ -mmacosx-version-min=10.9 -Wno-auto-var-id -fobjc-arc
  options += -lc++ -lobjc -mmacosx-version-min=10.9
  # allow mprotect() on dynamic recompiler code blocks
  options += -Wl,-segprot,__DATA,rwx,rw
endif

# linux settings
ifeq ($(platform),linux)
  options += -ldl
endif

# bsd settings
ifeq ($(platform),bsd)
  flags   += -I/usr/local/include
  options += -Wl,-rpath=/usr/local/lib
  options += -Wl,-rpath=/usr/local/lib/gcc8
  options += -lstdc++ -lm
endif

# threading support
ifeq ($(threaded),true)
  ifneq ($(filter $(platform),linux bsd),)
    flags   += -pthread
    options += -pthread -lrt
  endif
endif

# paths
ifeq ($(object.path),)
  object.path := obj
endif

ifeq ($(output.path),)
  output.path := out
endif

# rules
default: all;

nall.verbose:
	$(info Compiler Flags:)
	$(foreach n,$(sort $(call unique,$(flags))),$(if $(filter-out -I%,$n),$(info $([space]) $n)))
	$(info Linker Options:)
	$(foreach n,$(sort $(call unique,$(options))),$(if $(filter-out -l%,$n),$(info $([space]) $n)))

%.o: $<
	$(info Compiling $(subst ../,,$<) ...)
	@$(call compile)

# function compile([arguments])
compile = \
  $(strip \
    $(if $(filter %.c,$<), \
      $(compiler.c)   $(flags.deps) $(flags) $1 -c $< -o $@ \
   ,$(if $(filter %.cpp,$<), \
      $(compiler.cpp) $(flags.deps) $(flags) $1 -c $< -o $@ \
    )) \
  )

# function rwildcard(directory, pattern)
rwildcard = \
  $(strip \
    $(filter $(if $2,$2,%), \
      $(foreach f, \
        $(wildcard $1*), \
        $(eval t = $(call rwildcard,$f/)) \
        $(if $t,$t,$f) \
      ) \
    ) \
  )

# function unique(source)
unique = \
  $(eval __temp :=) \
  $(strip \
    $(foreach s,$1,$(if $(filter $s,$(__temp)),,$(eval __temp += $s))) \
    $(__temp) \
  )

# function strtr(source, from, to)
strtr = \
  $(eval __temp := $1) \
  $(strip \
    $(foreach c, \
      $(join $(addsuffix :,$2),$3), \
      $(eval __temp := \
        $(subst $(word 1,$(subst :, ,$c)),$(word 2,$(subst :, ,$c)),$(__temp)) \
      ) \
    ) \
    $(__temp) \
  )

# function strupper(source)
strupper = $(call strtr,$1,$([a-z]),$([A-Z]))

# function strlower(source)
strlower = $(call strtr,$1,$([A-Z]),$([a-z]))

# function strlen(source)
strlen = \
  $(eval __temp := $(subst $([space]),_,$1)) \
  $(words \
    $(strip \
      $(foreach c, \
        $([all]), \
        $(eval __temp := \
          $(subst $c,$c ,$(__temp)) \
        ) \
      ) \
      $(__temp) \
    ) \
  )

# function streq(source)
streq = $(if $(filter-out xx,x$(subst $1,,$2)$(subst $2,,$1)x),,1)

# function strne(source)
strne = $(if $(filter-out xx,x$(subst $1,,$2)$(subst $2,,$1)x),1,)

# prefix
ifeq ($(platform),windows)
  prefix := $(subst $([space]),\$([space]),$(strip $(call strtr,$(LOCALAPPDATA),\,/)))
else
  prefix := $(HOME)/.local
endif
