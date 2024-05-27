use crate::chd_file;
use chd::header::{Header, HeaderV1, HeaderV3, HeaderV4, HeaderV5};
use chd::map::Map;
use std::mem;

pub const CHD_MD5_BYTES: usize = 16;
pub const CHD_SHA1_BYTES: usize = 20;

#[repr(C)]
#[allow(non_camel_case_types)]
/// libchdr-compatible CHD header struct.
/// This struct is ABI-compatible with [chd.h](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/include/libchdr/chd.h#L302)
pub struct chd_header {
    length: u32,
    version: u32,
    flags: u32,
    compression: [u32; 4],
    hunkbytes: u32,
    totalhunks: u32,
    logicalbytes: u64,
    metaoffset: u64,
    mapoffset: u64,
    md5: [u8; CHD_MD5_BYTES],
    parentmd5: [u8; CHD_MD5_BYTES],
    sha1: [u8; CHD_SHA1_BYTES],
    rawsha1: [u8; CHD_SHA1_BYTES],
    parentsha1: [u8; CHD_SHA1_BYTES],
    unitbytes: u32,
    unitcount: u64,
    hunkcount: u32,
    mapentrybytes: u32,
    rawmap: *mut u8,
    obsolete_cylinders: u32,
    obsolete_sectors: u32,
    obsolete_heads: u32,
    obsolete_hunksize: u32,
}

impl From<&HeaderV1> for chd_header {
    fn from(header: &HeaderV1) -> Self {
        chd_header {
            length: header.length,
            version: header.version as u32,
            flags: header.flags,
            compression: [header.compression, 0, 0, 0],
            hunkbytes: header.hunk_bytes,
            totalhunks: header.total_hunks,
            logicalbytes: header.logical_bytes,
            metaoffset: 0,
            mapoffset: 0,
            md5: header.md5,
            parentmd5: header.parent_md5,
            sha1: [0u8; CHD_SHA1_BYTES],
            rawsha1: [0u8; CHD_SHA1_BYTES],
            parentsha1: [0u8; CHD_SHA1_BYTES],
            unitbytes: header.unit_bytes,
            unitcount: header.unit_count,
            hunkcount: header.total_hunks,
            mapentrybytes: 0,
            rawmap: std::ptr::null_mut(),
            obsolete_cylinders: header.cylinders,
            obsolete_sectors: header.sectors,
            obsolete_heads: header.heads,
            obsolete_hunksize: header.hunk_size,
        }
    }
}

impl From<&HeaderV3> for chd_header {
    fn from(header: &HeaderV3) -> Self {
        chd_header {
            length: header.length,
            version: header.version as u32,
            flags: header.flags,
            compression: [header.compression, 0, 0, 0],
            hunkbytes: header.hunk_bytes,
            totalhunks: header.total_hunks,
            logicalbytes: header.logical_bytes,
            metaoffset: header.meta_offset,
            mapoffset: 0,
            md5: header.md5,
            parentmd5: header.parent_md5,
            sha1: header.sha1,
            rawsha1: [0u8; CHD_SHA1_BYTES],
            parentsha1: header.parent_sha1,
            unitbytes: header.unit_bytes,
            unitcount: header.unit_count,
            hunkcount: header.total_hunks,
            mapentrybytes: 0,
            rawmap: std::ptr::null_mut(),
            obsolete_cylinders: 0,
            obsolete_sectors: 0,
            obsolete_heads: 0,
            obsolete_hunksize: 0,
        }
    }
}

impl From<&HeaderV4> for chd_header {
    fn from(header: &HeaderV4) -> Self {
        chd_header {
            length: header.length,
            version: header.version as u32,
            flags: header.flags,
            compression: [header.compression, 0, 0, 0],
            hunkbytes: header.hunk_bytes,
            totalhunks: header.total_hunks,
            logicalbytes: header.logical_bytes,
            metaoffset: header.meta_offset,
            mapoffset: 0,
            md5: [0u8; CHD_MD5_BYTES],
            parentmd5: [0u8; CHD_MD5_BYTES],
            sha1: header.sha1,
            rawsha1: header.raw_sha1,
            parentsha1: header.parent_sha1,
            unitbytes: header.unit_bytes,
            unitcount: header.unit_count,
            hunkcount: header.total_hunks,
            mapentrybytes: 0,
            rawmap: std::ptr::null_mut(),
            obsolete_cylinders: 0,
            obsolete_sectors: 0,
            obsolete_heads: 0,
            obsolete_hunksize: 0,
        }
    }
}

pub(crate) fn get_v5_header(chd: &chd_file) -> chd_header {
    let header: HeaderV5 = match chd.header() {
        Header::V5Header(h) => h.clone(),
        _ => unreachable!(),
    };
    let mut map_data: Vec<u8> = match chd.map() {
        Map::V5(map) => map.into(),
        _ => unreachable!(),
    };
    let version = header.version;
    let map_ptr = map_data.as_mut_ptr();
    mem::forget(map_data);

    chd_header {
        length: header.length,
        version: version as u32,
        // libchdr just reads garbage for V5 flags, we will give it as 0.
        flags: 0,
        compression: header.compression,
        hunkbytes: header.hunk_bytes,
        totalhunks: header.hunk_count,
        logicalbytes: header.logical_bytes,
        metaoffset: header.meta_offset,
        mapoffset: header.map_offset,
        md5: [0u8; CHD_MD5_BYTES],
        parentmd5: [0u8; CHD_MD5_BYTES],
        sha1: header.sha1,
        rawsha1: header.raw_sha1,
        parentsha1: header.parent_sha1,
        unitbytes: header.unit_bytes,
        unitcount: header.unit_count,
        hunkcount: header.hunk_count,
        mapentrybytes: header.map_entry_bytes,
        rawmap: map_ptr,
        obsolete_cylinders: 0,
        obsolete_sectors: 0,
        obsolete_heads: 0,
        obsolete_hunksize: 0,
    }
}
