## A C# library for parsing ISO9660 format disk images

Modified copy of `iso-parser`, [archived](https://code.google.com/archive/p/iso-parser/) from Google Code, licensed under the MIT License by [Craig Prince](https://www.craigprince.com/code.html). See `LICENSE.md` for the full text.

The repo description from the Google Code Archive follows.

---

The purpose of this class library is to allow the parsing of ISO9660 format disk images and navigating the file system present in the image.

The result is the ability to find the sector offset and length of file data within the disk image so that the file data can be read easily from that image.

NOTE: We do not currently support any of the extensions to the ISO format, such as Joliet; nor does this code support other image formats, such as UDF.
