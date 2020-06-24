use crate::syscall_defs::*;
use crate::*;
use std::io::{Write, Read};
use super::*;

/// A file whose content is in memory and managed by the waterbox host
pub struct RegularFile {
	data: Vec<u8>,
	hash: Option<Vec<u8>>,
	position: usize,
}
impl RegularFile {
	pub fn new(data: Vec<u8>, writable: bool) -> RegularFile {
		let hash = if writable {
			None
		} else {
			Some(bin::hash(&data[..]))
		};
		RegularFile {
			data,
			hash,
			position: 0,
		}
	}
}
impl IStateable for RegularFile {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		match &self.hash {
			Some(hash) => {
				bin::write_magic(stream, "RegularFile")?;
				bin::write_hash(stream, &hash[..])?;
				bin::write(stream, &self.position)?;
				Ok(())	
	
			},
			None => Err(anyhow!("Cannot save state while transient files are active"))
		}
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		match &self.hash {
			Some(hash) => {
				bin::verify_magic(stream, "RegularFile")?;
				bin::verify_hash(stream, &hash[..])?;
				bin::read(stream, &mut self.position)?;
				Ok(())		
			}
			None => Err(anyhow!("Cannot load state while transient files are active"))
		}
	}
}
impl FileObject for RegularFile {
	fn can_read(&self) -> bool {
		true
	}
	fn read(&mut self, buf: &mut [u8]) -> Result<i64, SyscallError> {
		let n = std::cmp::min(buf.len(), self.data.len() - self.position);
		let dst = &mut buf[0..n];
		let src = &self.data[self.position..self.position + n];
		dst.copy_from_slice(src);
		self.position += n;
		Ok(n as i64)
	}
	fn can_write(&self) -> bool {
		match self.hash {
			None => true,
			Some(_) => false	
		}	
	}
	fn write(&mut self, buf: &[u8]) -> Result<i64, SyscallError> {
		if !self.can_write() {
			return Err(EBADF)
		}
		let n = buf.len();
		let newpos = self.position + n;
		if newpos > self.data.len() {
			self.data.resize(newpos, 0);
		}
		let dst = &mut self.data[self.position..newpos];

		dst.copy_from_slice(buf);
		self.position = newpos;
		Ok(n as i64)
	}
	fn seek(&mut self, offset: i64, whence: i32) -> Result<i64, SyscallError> {
		let newpos = match whence {
			SEEK_SET => {
				0
			},
			SEEK_CUR => {
				self.position as i64 + offset
			},
			SEEK_END => {
				self.data.len() as i64 + offset
			}
			_ => return Err(EINVAL)
		};
		if newpos < 0 || newpos > self.data.len() as i64 {
			return Err(EINVAL)
		}
		self.position = newpos as usize;
		Ok(newpos)
	}
	fn truncate(&mut self, size: i64) -> SyscallResult {
		if !self.can_write() {
			return Err(EBADF)
		}
		if size < 0 {
			return Err(EINVAL)
		}
		self.data.resize(size as usize, 0);
		self.position = std::cmp::min(self.position, size as usize);
		Ok(())
	}
	fn reset(&mut self) {
		self.position = 0;
	}
	fn stat(&self, statbuff: &mut KStat) -> SyscallResult {
		fill_stat(statbuff, true, self.can_write(), true, self.data.len() as i64)
	}
	fn can_unmount(&self) -> bool {
		true
	}
	fn unmount(self: Box<Self>) -> Vec<u8> {
		self.data
	}
}
