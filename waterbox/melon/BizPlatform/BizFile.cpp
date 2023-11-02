#include "BizFile.h"

#include <emulibc.h>

#include <stdarg.h>

namespace Platform
{

struct FileCallbackInterface
{
	int (*GetLength)(const char* path);
	void (*GetData)(const char* path, u8* buffer);
};

ECL_INVISIBLE static FileCallbackInterface FileCallbacks;

void SetFileCallbacks(FileCallbackInterface& fileCallbackInterface)
{
	FileCallbacks = fileCallbackInterface;
}

struct FileHandle
{
public:
	FileHandle(std::shared_ptr<u8[]> data_, size_t size_, FileMode mode_)
		: data(data_)
		, pos(0)
		, size(size_)
		, mode(mode_)
	{
	}

	bool IsEndOfFile()
	{
		return pos == size;
	}

	bool ReadLine(char* str, int count)
	{
		if (!Readable())
		{
			return false;
		}

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
		if (!Readable())
		{
			return 0;
		}

		count = std::min(count, (u64)(size - pos));
		memcpy(data_, &data[pos], count);
		pos += count;
		return count;
	}

	size_t Write(const void* data_, u64 count)
	{
		if (!Writable())
		{
			return 0;
		}

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
	std::shared_ptr<u8[]> data;
	size_t pos, size;
	FileMode mode;

	bool Readable()
	{
		return (mode & FileMode::Read) == FileMode::Read;
	}

	bool Writable()
	{
		return (mode & FileMode::Write) == FileMode::Write;
	}
};

static std::unordered_map<std::string, std::pair<std::shared_ptr<u8[]>, size_t>> FileBufferCache;

FileHandle* OpenFile(const std::string& path, FileMode mode)
{
	if ((mode & FileMode::ReadWrite) == FileMode::None)
	{
		// something went wrong here
		return nullptr;
	}

	if (auto cache = FileBufferCache.find(path); cache != FileBufferCache.end())
	{
		return new FileHandle(cache->second.first, cache->second.second, mode);
	}

	size_t size = FileCallbacks.GetLength(path.c_str());
	if (size == 0)
	{
		return nullptr;
	}

	std::shared_ptr<u8[]> data(new u8[size]);
	FileCallbacks.GetData(path.c_str(), data.get());
	FileBufferCache.emplace(path, std::make_pair(data, size));
	return new FileHandle(data, size, mode);
}

FileHandle* OpenLocalFile(const std::string& path, FileMode mode)
{
	return OpenFile(path, mode);
}

bool FileExists(const std::string& name)
{
	if (auto cache = FileBufferCache.find(name); cache != FileBufferCache.end())
	{
		return true;
	}

	return FileCallbacks.GetLength(name.c_str()) > 0;
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
	return true;
}

u64 FileWrite(const void* data, u64 size, u64 count, FileHandle* file)
{
	return file->Write(data, size * count);
}

u64 FileWriteFormatted(FileHandle* file, const char* fmt, ...)
{
	va_list args;

	va_start(args, fmt);
	size_t bufferSize = vsnprintf(nullptr, 0, fmt, args);
	va_end(args);

	if ((int)bufferSize < 0)
	{
		return 0;
	}

	auto buffer = std::make_unique<char[]>(bufferSize + 1);

	va_start(args, fmt);
	vsnprintf(buffer.get(), bufferSize + 1, fmt, args);
	va_end(args);

	return file->Write(buffer.get(), bufferSize);
}

u64 FileLength(FileHandle* file)
{
	return file->Length();
}

}
