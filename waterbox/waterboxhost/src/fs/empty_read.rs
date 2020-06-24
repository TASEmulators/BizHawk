use crate::syscall_defs::*;
use crate::*;
use std::io::{Write, Read};
use super::*;

/// stdin
pub struct EmptyRead {
}
impl IStateable for EmptyRead {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		bin::write_magic(stream, "EmptyRead")?;
		Ok(())
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		bin::verify_magic(stream, "EmptyRead")?;
		Ok(())
	}
}
impl FileObject for EmptyRead {
	fn can_read(&self) -> bool {
		true
	}
	fn read(&mut self, _buf: &mut [u8]) -> Result<i64, SyscallError> {
		Ok(0)
	}
	fn can_write(&self) -> bool {
		false
	}
	fn write(&mut self, _buf: &[u8]) -> Result<i64, SyscallError> {
		Err(EBADF)
	}
	fn seek(&mut self, _offset: i64, _whence: i32) -> Result<i64, SyscallError> {
		Err(ESPIPE)
	}
	fn truncate(&mut self, _size: i64) -> SyscallResult {
		Err(EINVAL)
	}
	fn stat(&self, statbuff: &mut KStat) -> SyscallResult {
		fill_stat(statbuff, true, false, false, 0)
	}
	fn can_unmount(&self) -> bool {
		false
	}
	fn unmount(self: Box<Self>) -> Vec<u8> {
		panic!()
	}
	fn reset(&mut self) {}
}
