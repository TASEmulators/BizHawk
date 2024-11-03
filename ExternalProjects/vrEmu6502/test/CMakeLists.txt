cmake_minimum_required(VERSION 3.16)

include(CheckSymbolExists)

enable_testing()

add_executable (vrEmu6502Test vrEmu6502Test.c)

target_link_libraries(vrEmu6502Test vrEmu6502)


check_symbol_exists(fopen_s "stdio.h" HAVE_FOPEN_S)
check_symbol_exists(strncpy_s "string.h" HAVE_STRNCPY_S)

if (HAVE_FOPEN_S)
    target_compile_definitions(vrEmu6502Test PRIVATE -DHAVE_FOPEN_S)
endif()

if (HAVE_STRNCPY_S)
    target_compile_definitions(vrEmu6502Test PRIVATE -DHAVE_STRNCPY_S)
endif()


set(TESTS_DIR ${CMAKE_CURRENT_SOURCE_DIR}/programs/)
set(TEST_RUNNER ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}vrEmu6502Test)

add_test(6502_functional_test ${TEST_RUNNER} -quiet -cpu 6502 ${TESTS_DIR}6502_functional_test.hex)
add_test(65c02_functional_test ${TEST_RUNNER} -quiet -cpu 65c02 ${TESTS_DIR}6502_functional_test.hex)
add_test(w65c02_functional_test ${TEST_RUNNER} -quiet -cpu w65c02 ${TESTS_DIR}6502_functional_test.hex)
add_test(r65c02_functional_test ${TEST_RUNNER} -quiet -cpu r65c02 ${TESTS_DIR}6502_functional_test.hex)

add_test(6502_decimal_test ${TEST_RUNNER} -quiet -cpu 6502 ${TESTS_DIR}6502_decimal_test.hex)
add_test(65c02_decimal_test ${TEST_RUNNER} -quiet -cpu 65c02 ${TESTS_DIR}65C02_decimal_test.hex)
add_test(w65c02_decimal_test ${TEST_RUNNER} -quiet -cpu w65c02 ${TESTS_DIR}65C02_decimal_test.hex)
add_test(r65c02_decimal_test ${TEST_RUNNER} -quiet -cpu r65c02 ${TESTS_DIR}6502_decimal_test.hex)

add_test(65c02_extended_test ${TEST_RUNNER} -quiet -cpu 65c02 ${TESTS_DIR}65C02_extended_opcodes_test.hex)
add_test(w65c02_extended_test ${TEST_RUNNER} -quiet -cpu w65c02 ${TESTS_DIR}W65C02_extended_opcodes_test.hex)
add_test(r65c02_extended_test ${TEST_RUNNER} -quiet -cpu r65c02 ${TESTS_DIR}R65C02_extended_opcodes_test.hex)

