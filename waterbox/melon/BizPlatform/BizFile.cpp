#include "BizFile.h"

#include <emulibc.h>

#include <stdarg.h>

namespace melonDS::Platform
{

struct FileHandle
{
public:
	virtual ~FileHandle() = default;
	virtual bool IsEndOfFile() = 0;
	virtual bool ReadLine(char* str, int count) = 0;
	virtual bool Seek(s64 offset, FileSeekOrigin origin) = 0;
	virtual void Rewind() = 0;
	virtual size_t Read(void* data, u64 size, u64 count) = 0;
	virtual bool Flush() = 0;
	virtual size_t Write(const void* data, u64 size, u64 count) = 0;
	virtual int WriteFormatted(const char* fmt, va_list args) = 0;
	virtual size_t Length() = 0;
	virtual size_t Position() = 0;
};

struct MemoryFile final : FileHandle
{
public:
	MemoryFile(std::unique_ptr<u8[]> data_, size_t size_)
		: data(std::move(data_))
		, pos(0)
		, size(size_)
	{
	}

	~MemoryFile() = default;

	bool IsEndOfFile()
	{
		return pos == size;
	}

	bool ReadLine(char* str, int count)
	{
		if (count < 1)
		{
			return false;
		}

		size_t len = std::min(size - pos, (size_t)(count - 1));
		u8* end = (u8*)memchr(&data[pos], '\n', len);
		len = end ? (end + 1) - &data[pos] : len;
		memcpy(str, &data[pos], len);
		pos += len;
		str[len] = '\0';
		return true;
	}

	bool Seek(s64 offset, FileSeekOrigin origin)
	{
		size_t newPos;
		switch (origin)
		{
			case FileSeekOrigin::Start:
				newPos = offset;
				break;
			case FileSeekOrigin::Current:
				newPos = pos + offset;
				break;
			case FileSeekOrigin::End:
				newPos = size + offset;
				break;
			default:
				return false;
		}

		if (newPos > size)
		{
			return false;
		}

		pos = newPos;
		return true;
	}

	void Rewind()
	{
		pos = 0;
	}

	size_t Read(void* data_, u64 size_, u64 count)
	{
		u64 len = std::min(size_ * count, (u64)(size - pos) / size_ * size_);
		if (len == 0)
		{
			return 0;
		}

		memcpy(data_, &data[pos], len);
		pos += len;
		return len / size_;
	}

	bool Flush()
	{
		return true;
	}

	size_t Write(const void* data_, u64 size_, u64 count)
	{
		u64 len = std::min(size_ * count, (u64)(size - pos) / size_ * size_);
		if (len == 0)
		{
			return 0;
		}

		memcpy(&data[pos], data_, len);
		pos += len;
		return len / size_;
	}

	int WriteFormatted(const char* fmt, va_list args)
	{
		if (pos == size)
		{
			return -1;
		}

		// vsnprintf writes a null terminator, while vfprintf does not
		// save the old character and restore it after writing characters

		va_list argsCopy;
		va_copy(argsCopy, args);
		int numBytes = vsnprintf(nullptr, 0, fmt, argsCopy);
		va_end(argsCopy);

		if (numBytes <= 0)
		{
			return numBytes;
		}

		numBytes = (int)std::min((u64)numBytes, (u64)(size - pos - 1));
		u8 oldChar = data[pos + numBytes];
		int ret = vsnprintf((char*)&data[pos], size - pos, fmt, args);
		data[pos + numBytes] = oldChar;
		if (ret >= 0)
		{
			pos += ret;
		}

		return ret;
	}

	size_t Length()
	{
		return size;
	}

	size_t Position()
	{
		return pos;
	}

private:
	std::unique_ptr<u8[]> data;
	size_t pos, size;
};

// private memory file creation API
FileHandle* CreateMemoryFile(u8* fileData, u32 fileLength)
{
	std::unique_ptr<u8[]> data(new u8[fileLength]);
	memcpy(data.get(), fileData, fileLength);
	return new MemoryFile(std::move(data), fileLength);
}

struct CFile final : FileHandle
{
public:
	CFile(FILE* file_)
		: file(file_)
	{
	}

	~CFile()
	{
		fclose(file);
	}

	bool IsEndOfFile()
	{
		return feof(file) != 0;
	}

	bool ReadLine(char* str, int count)
	{
		return fgets(str, count, file) != nullptr;
	}

	bool Seek(s64 offset, FileSeekOrigin origin)
	{
		int forigin;
		switch (origin)
		{
			case FileSeekOrigin::Start:
				forigin = SEEK_SET;
				break;
			case FileSeekOrigin::Current:
				forigin = SEEK_CUR;
				break;
			case FileSeekOrigin::End:
				forigin = SEEK_END;
				break;
			default:
				return false;
		}

		return fseek(file, offset, forigin) == 0;
	}

	void Rewind()
	{
		rewind(file);
	}

	size_t Read(void* data, u64 size, u64 count)
	{
		return fread(data, size, count, file);
	}

	bool Flush()
	{
		return fflush(file) == 0;
	}

	size_t Write(const void* data, u64 size, u64 count)
	{
		return fwrite(data, size, count, file);
	}

	int WriteFormatted(const char* fmt, va_list args)
	{
		return vfprintf(file, fmt, args);
	}

	size_t Length()
	{
		long pos = ftell(file);
		fseek(file, 0, SEEK_END);
		long len = ftell(file);
		fseek(file, pos, SEEK_SET);
		return len;
	}

	size_t Position()
	{
		return ftell(file);
	}

private:
	FILE* file;
};

std::string GetLocalFilePath(const std::string& filename)
{
	return filename;
}

// public APIs open C files
FileHandle* OpenFile(const std::string& path, FileMode mode)
{
	if (path == "dldi.bin" || path == "dsisd.bin")
	{
		// SD card files opened will be new memory files (always 256MiBs currently)
		constexpr u32 SD_CARD_SIZE = 256 * 1024 * 1024;
		std::unique_ptr<u8[]> data(new u8[SD_CARD_SIZE]);
		memset(data.get(), 0xFF, SD_CARD_SIZE);
		return new MemoryFile(std::move(data), SD_CARD_SIZE);
	}

	const char* fmode;
	if (mode & FileMode::Write)
	{
		fmode = "rb+";
	}
	else
	{
		fmode = "rb";
	}

	FILE* f = fopen(path.c_str(), fmode);
	if (!f)
	{
		return nullptr;
	}

	return new CFile(f);
}

FileHandle* OpenLocalFile(const std::string& path, FileMode mode)
{
	return OpenFile(path, mode);
}

bool FileExists(const std::string& name)
{
	if (name == "dldi.bin" || name == "dsisd.bin")
	{
		// these always return false (always consider opening these a "new" file)
		return false;
	}

	FILE* f = fopen(name.c_str(), "rb");
	bool exists = f != nullptr;
	fclose(f);
	return exists;
}

bool LocalFileExists(const std::string& name)
{
	return FileExists(name);
}

bool CheckFileWritable(const std::string& filepath)
{
	if (filepath == "dldi.bin" || filepath == "dsisd.bin")
	{
		return true;
	}

	FILE* f = fopen(filepath.c_str(), "rb+");
	bool exists = f != nullptr;
	fclose(f);
	return exists;
}

bool CheckLocalFileWritable(const std::string& filepath)
{
	return CheckFileWritable(filepath);
}

bool CloseFile(FileHandle* file)
{
	delete file;
	return true;
}

bool IsEndOfFile(FileHandle* file)
{
	return file->IsEndOfFile();
}

bool FileReadLine(char* str, int count, FileHandle* file)
{
	return file->ReadLine(str, count);
}

u64 FilePosition(FileHandle* file)
{
	return file->Position();
}

bool FileSeek(FileHandle* file, s64 offset, FileSeekOrigin origin)
{
	return file->Seek(offset, origin);
}

void FileRewind(FileHandle* file)
{
	file->Rewind();
}

u64 FileRead(void* data, u64 size, u64 count, FileHandle* file)
{
	return file->Read(data, size, count);
}

bool FileFlush(FileHandle* file)
{
	return file->Flush();
}

u64 FileWrite(const void* data, u64 size, u64 count, FileHandle* file)
{
	return file->Write(data, size, count);
}

u64 FileWriteFormatted(FileHandle* file, const char* fmt, ...)
{
	va_list args;
	va_start(args, fmt);
	int ret = file->WriteFormatted(fmt, args);
	va_end(args);
	return ret;
}

u64 FileLength(FileHandle* file)
{
	return file->Length();
}

}
