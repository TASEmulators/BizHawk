#pragma once

/**
 * @file file.hpp
 * @brief Contains common functions related to file manipulation
 */

#include <fstream>
#include <sstream>
#include <string>

namespace jaffarCommon
{

// Taken from https://stackoverflow.com/questions/116038/how-do-i-read-an-entire-file-into-a-stdstring-in-c/116220#116220
static inline std::string slurp(std::ifstream &in)
{
  std::ostringstream sstr;
  sstr << in.rdbuf();
  return sstr.str();
}

static inline bool loadStringFromFile(std::string &dst, const std::string& fileName)
{
  std::ifstream fi(fileName);

  // If file not found or open, return false
  if (fi.good() == false) return false;

  // Reading entire file
  dst = slurp(fi);

  // Closing file
  fi.close();

  return true;
}

// Save string to a file
static inline bool saveStringToFile(const std::string &src, const std::string& fileName)
{
  FILE *fid = fopen(fileName.c_str(), "w");
  if (fid != NULL)
  {
    fwrite(src.c_str(), 1, src.size(), fid);
    fclose(fid);
    return true;
  }
  return false;
}

} // namespace jaffarCommon
