/*
 * Troy's 6502 Emulator - Test program
 *
 * Copyright (c) 2022 Troy Schrapel
 *
 * This code is licensed under the MIT license
 *
 * https://github.com/visrealm/vrEmu6502
 *
 */

#include "vrEmu6502.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <inttypes.h>


 /* ------------------------------------------------------------------
  * GLOBALS
  */

  /* keep track of the number of instructions processed */
uint64_t instructionCount = 0;
uint64_t cycleCount = 0;
uint64_t outputCount = 0;
const char* filename = NULL;

uint64_t filterInstructionCount = 0;
uint16_t showMemFrom = 0;
uint16_t showMemBytes = 0;
uint16_t runAddress = 0;
bool quietMode = false;
uint64_t verboseFrom = (uint64_t)-1;
clock_t startTime = 0;
vrEmu6502Model cpuModel = CPU_65C02;

/* ------------------------------------------------------------------
 * FUNCTION DECLARATIONS
 */
void processArgs(int argc, char* argv[]);
void outputStep(VrEmu6502* vr6502);
void banner();
void argError(const char* opt, const char* arg);
void usage(int status);
void beginReport();
void endReport(int status);
int readHexFile(const char* hexFilename);


/* ------------------------------------------------------------------
 * 6502 MEMORY
 */

uint8_t ram[0x10000];

uint8_t MemRead(uint16_t addr, bool isDbg)
{
  (void)isDbg;
  return ram[addr];
}

void MemWrite(uint16_t addr, uint8_t val)
{
  ram[addr] = val;
}

/* ------------------------------------------------------------------
 * program entry point
 */
int main(int argc, char* argv[])
{
  /* set a large output buffer */

#ifdef _WIN32
  static char buf[1 << 22];
  setvbuf(stdout, buf, _IOFBF, sizeof(buf));
#endif

  banner();

  processArgs(argc, argv);

  if (!readHexFile(filename))
    return 1;

  beginReport();

  int status = 0;

  /*
   * build and test the cpu
   */
  VrEmu6502* vr6502 = vrEmu6502New(cpuModel, MemRead, MemWrite);
  if (vr6502)
  {
    /* reset the cpu (technically don't need to do this as vrEmu6502New does reset it) */
    vrEmu6502Reset(vr6502);

    vrEmu6502SetPC(vr6502, (uint16_t)runAddress);

    uint16_t lastPc = 0;

    startTime = clock();

    while (1)
    {
      if (vrEmu6502GetOpcodeCycle(vr6502) == 0)
      {
        /* trap detection */
        uint16_t pc = vrEmu6502GetCurrentOpcodeAddr(vr6502);
        if (lastPc == pc)
        {
          verboseFrom = outputCount = 0;
          printf("\nFinal instruction:\n");
          outputStep(vr6502);
          status = vrEmu6502GetCurrentOpcode(vr6502) == 0x4c ? 0 : -1;
          break;
        }
        lastPc = pc;

        ++instructionCount;

        /* break on STP instruction */
        if (vrEmu6502GetCurrentOpcode(vr6502) == 0xdb)
        {
          verboseFrom = outputCount = 0;
          printf("\nFinal instruction:\n");
          outputStep(vr6502);
          status = 0;
          break;
        }

        outputStep(vr6502);
      }

      /* call me once for each clock cycle (eg. 1,000,000 times per second for a 1MHz clock) */
      cycleCount += vrEmu6502InstCycle(vr6502);
    }

    vrEmu6502Destroy(vr6502);
    vr6502 = NULL;
  }
  else
  {
    printf("Error creating VrEmu6502\n");
    return 1;
  }

  endReport(status);

  return status;
}

const char* processorModel()
{
  switch (cpuModel)
  {
    case CPU_6502:
      return "Standard NMOS 6502";

    case CPU_6502U:
      return "Standard NMOS 6502 (with undocumented opcodes)";

    case CPU_65C02:
      return "Standard CMOS 65C02";

    case CPU_W65C02:
      return "Western Design Centre 65C02";

    case CPU_R65C02:
      return "Rockwell 65C02";

    default:
      return "Unknown";
  }
}


/* ------------------------------------------------------------------
 * process command-line arguments
 */
void processArgs(int argc, char* argv[])
{
  for (int i = 1; i < argc; ++i)
  {
    if (argv[i][0] != '-')
    {
      filename = argv[i];
      continue;
    }

    int ch = 1 + (argv[i][1] == '-');

    switch (argv[i][ch])
    {
      case 'c':
        if (++i < argc)
        {
          if (strcmp(argv[i], "6502u") == 0)
          {
            cpuModel = CPU_6502U;
          }
          else if (strcmp(argv[i], "6502") == 0)
          {
            cpuModel = CPU_6502;
          }
          else if (strcmp(argv[i], "65c02") == 0)
          {
            cpuModel = CPU_65C02;
          }
          else if (strcmp(argv[i], "w65c02") == 0)
          {
            cpuModel = CPU_W65C02;
          }
          else if (strcmp(argv[i], "r65c02") == 0)
          {
            cpuModel = CPU_R65C02;
          }
          else
          {
            argError(argv[i - 1], argv[i]);
          }
        }
        else
        {
          argError(argv[i - 1], "<undefined>");
        }
        break;

      case 'f':
        if (++i < argc)
        {
          filterInstructionCount = strtol(argv[i], NULL, 0);
          if (filterInstructionCount <= 0)
          {
            argError(argv[i - 1], argv[i]);
          }
        }
        else
        {
          argError(argv[i - 1], "<undefined>");
        }
        break;

      case 'h':
        usage(0);
        break;

      case 'm':
        if (++i < argc)
        {
          char* tok = strchr(argv[i], ':');

          if (tok)
          {
            showMemFrom = (uint16_t)strtol(argv[i], NULL, 0);
            uint16_t to = (uint16_t)strtol(tok + 1, NULL, 0);
            if (showMemFrom <= to)
            {
              showMemBytes = (to - showMemFrom) + 1;
            }
          }
          else
          {
            showMemFrom = (uint16_t)strtol(argv[i], NULL, 0);
            showMemBytes = 1;
          }

          if (showMemBytes == 0)
          {
            argError(argv[i - 1], argv[i]);
          }

        }
        else
        {
          argError(argv[i - 1], "<undefined>");
        }
        break;

      case 'q':
        quietMode = true;
        if (i + 1 < argc)
        {
          verboseFrom = strtol(argv[i + 1], NULL, 10);
          if (verboseFrom == 0) verboseFrom = (uint64_t)-1; else ++i;
        }
        break;

      case 'r':
        if (++i < argc)
        {
          runAddress = (uint16_t)strtol(argv[i], NULL, 0);
        }
        else
        {
          argError(argv[i - 1], "<undefined>");
        }
        break;

      default:
        argError(argv[i], NULL);
        break;
    }
  }

  if (!filename)
  {
    argError(NULL, NULL);
  }
}

/*
  * output cpu state
  */
void outputStep(VrEmu6502* vr6502)
{
  if (instructionCount < verboseFrom)
  {
    if (filterInstructionCount)
    {
      if (instructionCount % filterInstructionCount != 0)
        return;
    }
    else if (quietMode)
    {
      return;
    }
  }

  char buffer[32];
  uint16_t pc = vrEmu6502GetCurrentOpcodeAddr(vr6502);
  vrEmu6502DisassembleInstruction(vr6502, pc, sizeof(buffer), buffer, NULL, NULL);
  uint8_t a = vrEmu6502GetAcc(vr6502);
  uint8_t x = vrEmu6502GetX(vr6502);
  uint8_t y = vrEmu6502GetY(vr6502);
  uint8_t sp = vrEmu6502GetStackPointer(vr6502);
  uint8_t status = vrEmu6502GetStatus(vr6502);

  if (outputCount++ % 40 == 0)
  {
    putchar('\n');

    printf("Step #      | PC    | Instruction    | Acc | InX | InY | SP   Top |   Status    ");
    if (showMemBytes > 1)
    {
      printf("| $%04x - $%04x", showMemFrom, showMemFrom + showMemBytes - 1);
    }
    else if (showMemBytes)
    {
      printf("| $%04x", showMemFrom);
    }

    printf("\n------------+-------+----------------+-----+-----+-----+----------+-------------");
    if (showMemBytes)
    {
      printf("+------");
      if (showMemBytes > 1) printf("--------");
    }
    putchar('\n');
  }


  printf("#%-10"PRId64" | $%04x | %-14s | $%02x | $%02x | $%02x | $%02x: $%02x | $%02x: %c%c%c%c%c%c ",
    instructionCount, pc, buffer, a, x, y, sp, MemRead(0x100 + ((sp + 1) & 0xff), 0), status,
    status & FlagN ? 'N' : '.',
    status & FlagV ? 'V' : '.',
    status & FlagD ? 'D' : '.',
    status & FlagI ? 'I' : '.',
    status & FlagZ ? 'Z' : '.',
    status & FlagC ? 'C' : '.');

  if (showMemBytes) printf("| ");

  for (int i = 0; i < showMemBytes; ++i)
  {
    printf("$%02x ", MemRead((showMemFrom + i) & 0xffff, 0));
  }
  putchar('\n');
}

/* ------------------------------------------------------------------
 * startup banner
 */
void banner()
{
  printf("\n  -------------------------------------\n");
  printf("          vrEmu6502 Test Runner\n");
  printf("  -------------------------------------\n");
  printf("    Copyright (c) 2022 Troy Schrapel\n");
  printf("  https://github.com/visrealm/vrEmu6502\n");
  printf("  -------------------------------------\n\n");
}

/* ------------------------------------------------------------------
 * output errors
 */
void argError(const char* opt, const char* arg)
{
  if (arg == NULL)
  {
    if (opt == NULL)
    {
      printf("ERROR: Intel HEX file not provided\n\n");
    }
    else
    {
      printf("ERROR: Invalid option '%s'\n\n", opt);
    }
  }
  else if (opt != NULL)
  {
    printf("ERROR: Invalid value '%s' supplied for option '%s'\n\n", arg, opt);
  }

  usage(1);
}

/* ------------------------------------------------------------------
 * output program usage
 */
void usage(int status)
{
  printf("Usage:\n");
  printf("vrEmu6502Test [OPTION...] <testfile.hex>\n\n");
  printf("Options:\n");
  printf("  -c, --cpu <cpumodel>     one of \"6502\",  \"6502u\", \"65c02\", \"w65c02\", \"r65c02\". defaults to 65c02.\n");
  printf("  -f, --filter <lines>     filter output to every #<lines> lines\n");
  printf("  -h, --help               output help and exit\n");
  printf("  -m, --mem <from>[:<to>]  output given memory address or range\n");
  printf("  -q, --quiet [<count>]    quiet mode - until <count> instructions processed\n");
  printf("  -r, --run <addr>         override run address\n");

  exit(-status);
}

/* ------------------------------------------------------------------
 * output current run options
 */
void beginReport()
{
  printf("Options:\n");
  printf("  Processor model:            %s\n", processorModel());
  printf("  Output filtering:           ");

  if (verboseFrom == (uint64_t)-1)
  {
    if (filterInstructionCount)
    {
      printf("Output every #%"PRId64" instructions\n", filterInstructionCount);
    }
    else if (quietMode)
    {
      printf("Quiet mode\n");
    }
    else
    {
      printf("Verbose\n");
    }
  }
  else
  {
    if (filterInstructionCount)
    {
      printf("Output every #%"PRId64" instructions until #%"PRId64"\n", filterInstructionCount, verboseFrom);
    }
    else
    {
      printf("Quiet until #%"PRId64"\n", verboseFrom);
    }
  }

  if (showMemBytes)
  {
    printf("  Output memory:              $%04x", showMemFrom);
    if (showMemBytes > 1)
    {
      printf(" - $%04x", showMemFrom + showMemBytes - 1);
    }
    putchar('\n');
  }
  printf("  Start address:              $%04x\n\n", runAddress);

  printf("Running test:                 %s\n\n", filename);
}

/* ------------------------------------------------------------------
 * output end of run results
 */
void endReport(int status)
{
  clock_t endTime = clock();
  double totalSeconds = ((double)endTime - startTime) / (double)CLOCKS_PER_SEC;
  if (totalSeconds < 1e-3) totalSeconds = 1e-3;

  printf("\nTest results:                 %s\n\n", filename);
  printf("  Processor model:            %s\n\n", processorModel());
  printf("  Instructions executed:      %0f Mil\n", instructionCount / 1000000.0);
  printf("  Total clock cycles:         %0f Mil\n\n", cycleCount / 1000000.0);
  printf("  Elapsed time:               %.4f sec\n", totalSeconds);
  printf("  Average clock rate:         %.4f MHz\n", (cycleCount / totalSeconds) / 1000000);
  printf("  Average instruction rate:   %.4f MIPS\n", (instructionCount / totalSeconds) / 1000000);
  printf("  Average clocks/instruction: %.4f\n", (cycleCount / (double)instructionCount));

  printf("\nTest result:                  %s\n\n", status ? "FAILED" : "PASSED");
}


/* ------------------------------------------------------------------
 * read the hex file
 */
int readHexFile(const char* hexFilename)
{

#ifndef HAVE_STRNCPY_S
#define strncpy_s(A, B, C, D) strncpy((A), (C), (D)); (A)[(D)] = 0
#endif

  /*
   * load the INTEL HEX file
   */

  FILE* hexFile = NULL;
#ifndef HAVE_FOPEN_S
  hexFile = fopen(hexFilename, "r");
#else
  fopen_s(&hexFile, hexFilename, "r");
#endif

  if (hexFile)
  {
    char lineBuffer[1024];
    char tmpBuffer[10];

    int totalBytesRead = 0;

    while (fgets(lineBuffer, sizeof(lineBuffer), hexFile))
    {
      if (lineBuffer[0] != ':') continue;

      strncpy_s(tmpBuffer, sizeof(tmpBuffer), lineBuffer + 1, 2);
      int numBytes = (int)strtol(tmpBuffer, NULL, 16);
      totalBytesRead += numBytes;
      strncpy_s(tmpBuffer, sizeof(tmpBuffer), lineBuffer + 3, 4);
      int destAddr = (int)strtol(tmpBuffer, NULL, 16);

      strncpy_s(tmpBuffer, sizeof(tmpBuffer), lineBuffer + 7, 2);
      int recType = (int)strtol(tmpBuffer, NULL, 16);

      if (recType == 0)
      {
        for (int i = 0; i < numBytes; ++i)
        {
          strncpy_s(tmpBuffer, sizeof(tmpBuffer), lineBuffer + 9 + (i * 2), 2);
          ram[destAddr + i] = (uint8_t)strtol(tmpBuffer, NULL, 16);
        }
      }
      else if (runAddress == 0 && recType == 1)
      {
        runAddress = (uint16_t)destAddr;
        break;
      }
    }

    fclose(hexFile);

    if (totalBytesRead == 0)
    {
      printf("ERROR: Invalid Intel HEX file: %s\n", hexFilename);
      return 0;
    }
    else if (runAddress == 0)
    {
      printf("WARNING: Run address not set from Intel HEX file: %s\n", hexFilename);
      return 0;
    }
  }
  else
  {
    printf("ERROR: Unable to open HEX file: %s\n", hexFilename);
    return 0;
  }
  return 1;
}
