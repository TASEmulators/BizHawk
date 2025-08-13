using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.SavestatesTests
{
	[TestClass]
	public class ZipStateSaverCompressionTests
	{
		[TestMethod]
		public void PutLump_SmallTextFile_SkipsCompression()
		{
			// Arrange
			string tempFilePath = Path.GetTempFileName();
			string smallText = "Small text content"; // Less than 256 bytes
			
			try
			{
				using (var saver = new ZipStateSaver(tempFilePath, compressionLevel: 3))
				{
					// Act
					saver.PutLump(BinaryStateLump.BizVersion, tw => tw.Write(smallText));
				}
				
				// Assert - verify the file was saved uncompressed
				using (var loader = ZipStateLoader.LoadAndDetect(tempFilePath))
				{
					Assert.IsNotNull(loader, "Should be able to load the saved state");
					
					string loadedText = null;
					bool found = loader.GetLump(BinaryStateLump.BizVersion, false, tr => loadedText = tr.ReadToEnd());
					
					Assert.IsTrue(found, "Should find the BizVersion lump");
					Assert.AreEqual(smallText, loadedText, "Loaded text should match saved text");
				}
			}
			finally
			{
				if (File.Exists(tempFilePath))
				{
					File.Delete(tempFilePath);
				}
			}
		}
		
		[TestMethod]
		public void PutLump_LargeTextFile_UsesCompression()
		{
			// Arrange
			string tempFilePath = Path.GetTempFileName();
			// Create text larger than 256 bytes
			string largeText = new string('A', 500) + "\nSome varied content to help compression work\n" + new string('B', 300);
			
			try
			{
				using (var saver = new ZipStateSaver(tempFilePath, compressionLevel: 3))
				{
					// Act
					saver.PutLump(BinaryStateLump.Input, tw => tw.Write(largeText));
				}
				
				// Assert - verify the file was saved and can be loaded back
				using (var loader = ZipStateLoader.LoadAndDetect(tempFilePath))
				{
					Assert.IsNotNull(loader, "Should be able to load the saved state");
					
					string loadedText = null;
					bool found = loader.GetLump(BinaryStateLump.Input, false, tr => loadedText = tr.ReadToEnd());
					
					Assert.IsTrue(found, "Should find the Input lump");
					Assert.AreEqual(largeText, loadedText, "Loaded text should match saved text");
				}
			}
			finally
			{
				if (File.Exists(tempFilePath))
				{
					File.Delete(tempFilePath);
				}
			}
		}
		
		[TestMethod]
		public void PutLump_ThresholdBoundaryTest_256Bytes()
		{
			// Arrange
			string tempFilePath = Path.GetTempFileName();
			// Create text exactly at the 256 byte threshold
			string thresholdText = new string('X', 256);
			
			try
			{
				using (var saver = new ZipStateSaver(tempFilePath, compressionLevel: 3))
				{
					// Act
					saver.PutLump(BinaryStateLump.UserData, tw => tw.Write(thresholdText));
				}
				
				// Assert - verify the file was saved and can be loaded back
				using (var loader = ZipStateLoader.LoadAndDetect(tempFilePath))
				{
					Assert.IsNotNull(loader, "Should be able to load the saved state");
					
					string loadedText = null;
					bool found = loader.GetLump(BinaryStateLump.UserData, false, tr => loadedText = tr.ReadToEnd());
					
					Assert.IsTrue(found, "Should find the UserData lump");
					Assert.AreEqual(thresholdText, loadedText, "Loaded text should match saved text");
				}
			}
			finally
			{
				if (File.Exists(tempFilePath))
				{
					File.Delete(tempFilePath);
				}
			}
		}
	}
}