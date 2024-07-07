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
	virtual size_t Read(void* data, u64 count) = 0;
	virtual bool Flush() = 0;
	virtual size_t Write(const void* data, u64 count) = 0;
	virtual size_t Length() = 0;
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

	size_t Read(void* data_, u64 count)
	{
		count = std::min(count, (u64)(size - pos));
		memcpy(data_, &data[pos], count);
		pos += count;
		return count;
	}

	bool Flush()
	{
		return true;
	}

	size_t Write(const void* data_, u64 count)
	{
		count = std::min(count, (u64)(size - pos));
		memcpy(&data[pos], data_, count);
		pos += count;
		return count;
	}

	size_t Length()
	{
		return size;
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

	size_t Read(void* data, u64 count)
	{
		return fread(data, 1, count, file);
	}

	bool Flush()
	{
		return fflush(file) == 0;
	}

	size_t Write(const void* data, u64 count)
	{
		return fwrite(data, 1, count, file);
	}

	size_t Length()
	{
		long pos = ftell(file);
		fseek(file, 0, SEEK_END);
		long len = ftell(file);
		fseek(file, pos, SEEK_SET);
		return len;
	}

private:
	FILE* file;
};

// public APIs open C files
FileHandle* OpenFile(const std::string& path, FileMode mode)
{
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
	FILE* f = fopen(name.c_str(), "rb");
	bool exists = f != nullptr;
	fclose(f);
	return exists;
}

bool LocalFileExists(const std::string& name)
{
	return FileExists(name);
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
	return file->Read(data, size * count);
}

bool FileFlush(FileHandle* file)
{
	return file->Flush();
}

u64 FileWrite(const void* data, u64 size, u64 count, FileHandle* file)
{
	return file->Write(data, size * count);
}

// only used for FATStorage (i.e. SD cards), not supported
u64 FileWriteFormatted(FileHandle* file, const char* fmt, ...)
{
	return 0;
}

u64 FileLength(FileHandle* file)
{
	return file->Length();
}

}
