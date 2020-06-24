mod empty_read;
mod sys_out;
mod regular_file;

use crate::syscall_defs::*;
use crate::*;
use std::io::{Write, Read};
use empty_read::EmptyRead;
use sys_out::SysOutObj;
use regular_file::RegularFile;

#[derive(Clone, Copy, PartialEq, Eq)]
#[repr(transparent)]
pub struct FileDescriptor(pub i32);

const BAD_FD: FileDescriptor = FileDescriptor(-1);

pub trait FileObject: IStateable {
	fn stat(&self, statbuff: &mut KStat) -> SyscallResult;
	fn truncate(&mut self, size: i64) -> SyscallResult;
	fn can_read(&self) -> bool;
	fn read(&mut self, buf: &mut [u8]) -> Result<i64, SyscallError>;
	fn can_write(&self) -> bool;
	fn write(&mut self, buf: &[u8]) -> Result<i64, SyscallError>;
	fn seek(&mut self, offset: i64, whence: i32) -> Result<i64, SyscallError>;
	fn reset(&mut self);
	fn can_unmount(&self) -> bool;
	fn unmount(self: Box<Self>) -> Vec<u8>;
}

fn fill_stat(s: &mut KStat, can_read: bool, can_write: bool, can_seek: bool, length: i64) -> SyscallResult {
	s.st_dev = 1;
	s.st_ino = 1;
	s.st_nlink = 0;

	let mut flags = 0;
	if can_read {
		flags |= S_IRUSR | S_IRGRP | S_IROTH;
	}
	if can_write {
		flags |= S_IWUSR | S_IWGRP | S_IWOTH;
	}
	if can_seek {
		flags |= S_IFREG;
	} else {
		flags |= S_IFIFO;
	}
	s.st_mode = flags;
	s.st_uid = 0;
	s.st_gid = 0;
	s.__pad0 = 0;
	s.st_rdev = 0;
	if can_seek {
		s.st_size = length;
	} else {
		s.st_size = 0;
	}
	s.st_blksize = 4096;
	s.st_blocks = (s.st_size + 511) / 512;

	s.st_atime_sec = 1262304000000;
	s.st_atime_nsec = 1000000000 / 2;
	s.st_mtime_sec = 1262304000000;
	s.st_mtime_nsec = 1000000000 / 2;
	s.st_ctime_sec = 1262304000000;
	s.st_ctime_nsec = 1000000000 / 2;

	Ok(())
}

struct MountedFile {
	name: String,
	fd: FileDescriptor,
	obj: Box<dyn FileObject>,
}
impl IStateable for MountedFile {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		bin::write_magic(stream, "MountedFile")?;
		bin::write_magic(stream, &self.name)?;
		bin::write(stream, &self.fd)?;
		self.obj.save_state(stream)?;
		Ok(())
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		bin::verify_magic(stream, "MountedFile")?;
		bin::verify_magic(stream, &self.name)?;
		bin::read(stream, &mut self.fd)?;
		self.obj.load_state(stream)?;
		Ok(())
	}
}

pub struct FileSystem {
	files: Vec<MountedFile>,
}
impl FileSystem {
	pub fn new() -> FileSystem {
		FileSystem {
			files: vec![
				MountedFile {
					name: "/dev/stdin".to_string(),
					fd: FileDescriptor(0),
					obj: Box::new(EmptyRead {})
				},
				MountedFile {
					name: "/dev/stdout".to_string(),
					fd: FileDescriptor(1),
					obj: Box::new(SysOutObj { host_handle: Box::new(std::io::stdout()) })
				},
				MountedFile {
					name: "/dev/stderr".to_string(),
					fd: FileDescriptor(2),
					obj: Box::new(SysOutObj { host_handle: Box::new(std::io::stderr()) })
				},
			],
		}
	}
	/// Accept a file from the outside world.  Writable files may never appear in a savestate,
	/// and readonly files must not be added or removed from savestate to savestate, so all uses
	/// are either transient or read only resources that last for the life of emulation.
	pub fn mount(&mut self, name: String, data: Vec<u8>, writable: bool) -> anyhow::Result<()> {
		if self.files.iter().any(|f| f.name == name) {
			return Err(anyhow!("File with name {} already mounted.", name))
		}
		self.files.push(MountedFile {
			name: name.to_string(),
			fd: BAD_FD,
			obj: Box::new(RegularFile::new(data, writable))
		});
		Ok(())
	}
	/// Remove a file previously loaded with mount().  Returns the content of the file at this time.
	/// Not possible if the guest has yet to close the file.
	pub fn unmount(&mut self, name: &str) -> anyhow::Result<Vec<u8>> {
		let idx = match self.files.iter().position(|f| f.name == name) {
			Some(f) => f,
			None => return Err(anyhow!("File with name {} not previously mounted.", name))
		};
		let file = &self.files[idx];
		if file.fd != BAD_FD {
			return Err(anyhow!("File {} is still open in the system", name))
		}
		if !file.obj.can_unmount() {
			return Err(anyhow!("File {} cannot be unmounted as it is permanently attached", name))
		}
		Ok(self.files.remove(idx).obj.unmount())
	}
	/// Implements a subset of open(2)
	pub fn open(&mut self, name: &str, flags: i32, _mode: i32) -> Result<FileDescriptor, SyscallError> {
		// TODO: Missing file callback
		let fd = {
			let mut i = 0;
			loop {
				if !self.files.iter().any(|f| f.fd.0 == i) {
					break FileDescriptor(i)
				}
				i += 1;
			}
		};
		let file = match self.files.iter_mut().find(|f| f.name == name) {
			Some(f) => f,
			None => return Err(ENOENT)
		};
		if file.fd != BAD_FD {
			return Err(EACCES)
		}
		// TODO: We should be doing more with flags and mode
		match flags & O_ACCMODE {
			O_RDONLY => {
				if !file.obj.can_read() {
					return Err(EACCES)
				}
			}
			O_WRONLY => {
				if !file.obj.can_write() {
					return Err(EACCES)
				}

			},
			O_RDWR => {
				if !file.obj.can_read() || !file.obj.can_write() {
					return Err(EACCES)
				}
			},
			_ => return Err(EINVAL)
		}
		// TODO: If the requested access was R on an RW file (transient), we still allow writing once opened
		file.fd = fd;
		Ok(fd)
	}
	/// Implements a subset of close(2)
	pub fn close(&mut self, fd: FileDescriptor) -> SyscallResult {
		let file = match self.files.iter_mut().find(|f| f.fd == fd) {
			Some(f) => f,
			None => return Err(EBADF)
		};
		file.obj.reset();
		file.fd = BAD_FD;
		Ok(())
	}
	fn wrap_action<T, P: FnOnce(&mut dyn FileObject) -> Result<T, SyscallError>>(&mut self, name: &str, action: P) -> Result<T, SyscallError> {
		match self.files.iter_mut().find(|f| f.name == name) {
			Some(f) => action(f.obj.as_mut()),
			None => Err(ENOENT)
		}
	}
	fn wrap_faction<T, P: FnOnce(&mut dyn FileObject) -> Result<T, SyscallError>>(&mut self, fd: FileDescriptor, action: P) -> Result<T, SyscallError> {
		match self.files.iter_mut().find(|f| f.fd == fd) {
			Some(f) => action(f.obj.as_mut()),
			None => Err(ENOENT)
		}
	}
	/// Implements a subset of stat(2)
	pub fn stat(&mut self, name: &str, statbuff: &mut KStat) -> SyscallResult {
		self.wrap_action(name, |f| f.stat(statbuff))
	}
	/// Implements a subset of fstat(2)
	pub fn fstat(&mut self, fd: FileDescriptor, statbuff: &mut KStat) -> SyscallResult {
		self.wrap_faction(fd, |f| f.stat(statbuff))
	}
	/// Implements a subset of truncate(2)
	pub fn truncate(&mut self, name: &str, size: i64) -> SyscallResult {
		self.wrap_action(name, |f| f.truncate(size))
	}
	/// Implements a subset of ftruncate(2)
	pub fn ftruncate(&mut self, fd: FileDescriptor, size: i64) -> SyscallResult {
		self.wrap_faction(fd, |f| f.truncate(size))
	}
	/// Implements a subset of read(2)
	pub fn read(&mut self, fd: FileDescriptor, buf: &mut [u8]) -> Result<i64, SyscallError> {
		self.wrap_faction(fd, |f| f.read(buf))
	}
	/// Implements a subset of write(2)
	pub fn write(&mut self, fd: FileDescriptor, buf: &[u8]) -> Result<i64, SyscallError> {
		self.wrap_faction(fd, |f| f.write(buf))
	}
	/// Implements a subset of lseek(2)
	pub fn seek(&mut self, fd: FileDescriptor, offset: i64, whence: i32) -> Result<i64, SyscallError> {
		self.wrap_faction(fd, |f| f.seek(offset, whence))
	}
}
impl IStateable for FileSystem {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		bin::write_magic(stream, "FileSystem")?;
		for f in self.files.iter_mut() {
			f.save_state(stream)?;
		}
		bin::write_magic(stream, "FileSystemEnd")?;
		Ok(())
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		bin::verify_magic(stream, "FileSystem")?;
		for f in self.files.iter_mut() {
			f.load_state(stream)?;
		}
		bin::verify_magic(stream, "FileSystemEnd")?;
		Ok(())
	}
}

#[cfg(test)]
mod tests {
	use super::*;

	type TestResult = anyhow::Result<()>;

	#[test]
	fn test_create() -> TestResult {
		let mut fs = FileSystem::new();
		let mut state0 = Vec::new();
		fs.save_state(&mut state0)?;
		fs.load_state(&mut &state0[..])?;
		Ok(())
	}

	#[test]
	fn test_ro_state() -> TestResult {
		let mut fs = FileSystem::new();
		fs.mount("myfile".to_string(),
			"The quick brown fox jumps over the lazy dog.".to_string().into_bytes(), false)?;
		let fd = fs.open("myfile", O_RDONLY, 0)?;
		assert_eq!(fd.0, 3);
		let mut buff = vec![0u8; 8];
		assert!(fs.write(fd, &buff[..]).is_err());
		assert_eq!(fs.read(fd, &mut buff[..])?, 8);
		assert_eq!(buff, "The quic".as_bytes());
		let mut state0 = Vec::new();
		fs.save_state(&mut state0)?;
		assert_eq!(fs.read(fd, &mut buff[..])?, 8);
		assert_eq!(buff, "k brown ".as_bytes());
		fs.load_state(&mut &state0[..])?;
		assert_eq!(fs.read(fd, &mut buff[..])?, 8);
		assert_eq!(buff, "k brown ".as_bytes());
		assert_eq!(fs.read(fd, &mut buff[..])?, 8);
		assert_eq!(buff, "fox jump".as_bytes());
		assert_eq!(fs.read(fd, &mut buff[..])?, 8);
		assert_eq!(buff, "s over t".as_bytes());
		assert_eq!(fs.read(fd, &mut buff[..])?, 8);
		assert_eq!(buff, "he lazy ".as_bytes());
		assert_eq!(fs.read(fd, &mut buff[..])?, 4);
		assert_eq!(&buff[0..4], "dog.".as_bytes());
		Ok(())
	}

	#[test]
	fn test_negative() -> TestResult {
		let mut fs = FileSystem::new();
		assert!(fs.mount("/dev/stdin".to_string(), Vec::new(), false).is_err()); // overriding existing name
		assert!(fs.unmount("oopopo").is_err()); // unmounting nonexistant file
		assert!(fs.unmount("/dev/stdout").is_err()); // unmounting permanent file
		fs.mount("oopopo".to_string(), Vec::new(), true)?;
		let mut state0 = Vec::new();
		assert!(fs.save_state(&mut state0).is_err()); // save state with transient file
		state0.resize(0, 0);
		fs.unmount("oopopo")?;
		fs.mount("oopopo".to_string(), Vec::new(), false)?;
		fs.save_state(&mut state0)?;
		fs.unmount("oopopo")?;
		assert!(fs.load_state(&mut &state0[..]).is_err()); // loading state with different list of files
		// TODO: Our general contract is that after a failed loadstate, the entire core is poisoned.
		// Can we do better?  Should we do better?
		Ok(())
	}

	#[test]
	fn test_rw_unmount() -> TestResult {
		let mut fs = FileSystem::new();
		fs.mount("z".to_string(), Vec::new(), true)?;
		let fd = fs.open("z", O_RDWR, 0)?;
		fs.write(fd, "Big test".as_bytes())?;
		fs.seek(fd, 0, SEEK_SET)?;
		fs.write(fd, "Q".as_bytes())?;
		fs.seek(fd, 2, SEEK_CUR)?;
		fs.write(fd, ")".as_bytes())?;
		fs.seek(fd, -1, SEEK_END)?;
		fs.write(fd, "$$$$".as_bytes())?;
		let mut statbuff = Box::new(KStat::default());
		fs.fstat(fd, statbuff.as_mut())?;
		assert_eq!(statbuff.st_size, 11);
		fs.close(fd)?;
		let vec = fs.unmount("z")?;
		assert_eq!(vec, "Qig)tes$$$$".as_bytes());
		Ok(())
	}
}
