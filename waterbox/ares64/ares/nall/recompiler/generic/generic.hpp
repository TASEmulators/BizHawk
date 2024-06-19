#pragma once

#if defined(SLJIT)
namespace nall::recompiler {
  struct generic {
    static constexpr bool supported = Architecture::amd64 | Architecture::arm64 | Architecture::ppc64 | Architecture::rv64;

    bump_allocator& allocator;
    sljit_compiler* compiler = nullptr;
    sljit_label* epilogue = nullptr;

    generic(bump_allocator& alloc) : allocator(alloc) {}
    ~generic() { resetCompiler(); }

    auto beginFunction(int args) -> void {
      assert(args <= 3);
      resetCompiler();
      compiler = sljit_create_compiler(nullptr, &allocator);

      sljit_s32 options = 0;
      if(args >= 1) options |= SLJIT_ARG_VALUE(SLJIT_ARG_TYPE_W, 1);
      if(args >= 2) options |= SLJIT_ARG_VALUE(SLJIT_ARG_TYPE_W, 2);
      if(args >= 3) options |= SLJIT_ARG_VALUE(SLJIT_ARG_TYPE_W, 3);
      sljit_emit_enter(compiler, 0, options, 4, 3, 0, 0, 0);
      sljit_jump* entry = sljit_emit_jump(compiler, SLJIT_JUMP);
      epilogue = sljit_emit_label(compiler);
      sljit_emit_return_void(compiler);

      sljit_set_label(entry, sljit_emit_label(compiler));
    }

    auto endFunction() -> u8* {
      u8* code = (u8*)sljit_generate_code(compiler);
      allocator.reserve(sljit_get_generated_code_size(compiler));
      resetCompiler();
      return code;
    }

    auto resetCompiler() -> void {
      if(compiler) sljit_free_compiler(compiler);
      compiler = nullptr;
      epilogue = nullptr;
    }

    auto testJumpEpilog() -> void {
      sljit_set_label(sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL | SLJIT_32, SLJIT_RETURN_REG, 0, SLJIT_IMM, 0), epilogue);
    }

    auto jumpEpilog() -> void {
      sljit_set_label(sljit_emit_jump(compiler, SLJIT_JUMP), epilogue);
    }

    auto setLabel(sljit_jump* jump) -> void {
      sljit_set_label(jump, sljit_emit_label(compiler));
    }

    auto jump() -> sljit_jump* {
      return sljit_emit_jump(compiler, SLJIT_JUMP);
    }

    auto jump(sljit_s32 flag) -> sljit_jump* {
      return sljit_emit_jump(compiler, flag);
    }

    #include "constants.hpp"
    #include "encoder-instructions.hpp"
    #include "encoder-calls.hpp"
  };
}
#endif
