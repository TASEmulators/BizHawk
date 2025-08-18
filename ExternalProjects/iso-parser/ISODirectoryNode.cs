﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ISOParser
{
	/// <summary>
	/// Representation of a directory in the file system.
	/// </summary>
	public class ISODirectoryNode : ISONode
	{
		/// <summary>
		/// The children in this directory.
		/// </summary>
		public Dictionary<string, ISONode> Children;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="record">The node for this directory.</param>
		public ISODirectoryNode(ISONodeRecord record)
			: base(record)
		{
			this.Children = new Dictionary<string, ISONode>();
		}

		/// <summary>
		/// Parse the children based on the data in this directory.
		/// </summary>
		/// <param name="s">The stream to parse from.</param>
		/// <param name="visited">The set of already handled 
		/// files/directories.</param>
		public void Parse(Stream s, Dictionary<long, ISONode> visited)
		{
			// Go to the beginning of the set of directories
			s.Seek(this.Offset * ISOFile.SECTOR_SIZE, SeekOrigin.Begin);

			List<ISONodeRecord> records = new List<ISONodeRecord>();
			
			// Read the directory entries
			while (s.Position < ((this.Offset * ISOFile.SECTOR_SIZE) + this.Length))
			{
				ISONodeRecord record;
				
				// Read the record
				record = new ISONodeRecord();
				if (ISOFile.Format == ISOFile.ISOFormat.CDInteractive)
					record.ParseCDInteractive(s);
				if (ISOFile.Format == ISOFile.ISOFormat.ISO9660)
					record.ParseISO9660(s);


				//zero 24-jun-2013 - improved validity checks
				//theres nothing here!
				if (record.Length == 0)
				{
					break;
				}
				else
				{
					// Check if we already have this node
					if (!visited.TryGetValue(record.OffsetOfData, out var node))
					{
						// Create the node from the record
						if (record.IsFile())
						{
							node = new ISOFileNode(record);
						}
						else if (record.IsDirectory())
						{
							node = new ISODirectoryNode(record);
						}
						else
						{
							node = new ISONode(record);
						}

						// Keep track that we've now seen the node and are parsing it
						visited.Add(node.Offset, node);
					}

					// Add the node as a child
					if (!this.Children.ContainsKey(record.Name))
						this.Children.Add(record.Name, node);
				}
			}

			long currentPosition = s.Position;

			// Iterate over directories...
			foreach (KeyValuePair<string, ISONode> child in this.Children)
			{
				// Parse this node
				if (child.Key != ISONodeRecord.CURRENT_DIRECTORY
					&& child.Key != ISONodeRecord.PARENT_DIRECTORY
					&& child.Value is ISODirectoryNode dirNode)
				{
					dirNode.Parse(s, visited);
				}
			}

			s.Seek(currentPosition, SeekOrigin.Begin);
		}

		/// <summary>
		/// Print out this node's children.
		/// </summary>
		/// <param name="depth">The number of "tabs" to indent this directory.</param>
		public void Print(int depth)
		{
			// Get the tabs string
			string tabs = "";
			for (int i = 0; i < depth; i++)
			{
				tabs += "  ";
			}

			// Print the directory names recursively, sorted alphabetically
			foreach (string s in this.Children.Keys.OrderBy(s => s))
			{
				ISONode n = this.Children[s];
				Console.WriteLine(tabs + s);
				if (s != ISONodeRecord.CURRENT_DIRECTORY
					&& s != ISONodeRecord.PARENT_DIRECTORY
					&& n is ISODirectoryNode dirNode)
				{
					dirNode.Print(depth + 1);
				}
			}
		}
	}
}
