use std::io::*;
use std::mem::{transmute, size_of, zeroed};
use anyhow::anyhow;
use sha2::{Sha256, Digest};

pub fn write<T>(stream: &mut dyn Write, val: &T) -> Result<()> {
	let s = unsafe { std::slice::from_raw_parts(transmute::<&T, *const u8>(val), size_of::<T>()) };
	stream.write_all(s)?;
	Ok(())
}
pub fn read<T>(stream: &mut dyn Read, val: &mut T) -> Result<()> {
	let s = unsafe { std::slice::from_raw_parts_mut(transmute::<&mut T, *mut u8>(val), size_of::<T>()) };
	stream.read_exact(s)?;
	Ok(())
}
pub fn writeval<T>(stream: &mut dyn Write, val: T) -> Result<()> {
	let s = unsafe { std::slice::from_raw_parts(transmute::<&T, *const u8>(&val), size_of::<T>()) };
	stream.write_all(s)?;
	Ok(())
}
pub fn readval<T>(stream: &mut dyn Read) -> Result<T> {
	let mut v = unsafe { zeroed::<T>() };
	read(stream, &mut v)?;
	Ok(v)
}
pub fn write_magic(stream: &mut dyn Write, magic: &str) -> anyhow::Result<()> {
	stream.write_all(magic.as_bytes())?;
	Ok(())
}
pub fn verify_magic(stream: &mut dyn Read, magic: &str) -> anyhow::Result<()> {
	let mut read_tag = vec![0u8; magic.len()];
	stream.read_exact(&mut read_tag[..])?;
	match std::str::from_utf8(&read_tag[..]) {
		Ok(s) if s == magic => Ok(()),
		_ => Err(anyhow!("Bad magic for {} state", magic))
	}
}
pub fn write_hash(stream: &mut dyn Write, hash: &[u8]) -> anyhow::Result<()> {
	stream.write_all(hash)?;
	Ok(())
}
pub fn verify_hash(stream: &mut dyn Read, hash: &[u8]) -> anyhow::Result<()> {
	let mut read_hash = vec![0u8; hash.len()];
	stream.read_exact(&mut read_hash[..])?;
	if read_hash == hash {
		Ok(())
	} else {
		Err(anyhow!("Bad hash for state"))
	}
}
pub fn hash(data: &[u8]) -> Vec<u8> {
	let mut hasher = Sha256::new();
	hasher.update(data);
	hasher.finalize()[..].to_owned()	
}
