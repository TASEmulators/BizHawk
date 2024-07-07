/////////////////////////////////////////////////////////////////////////////////
// Paint.NET
// Copyright (C) Rick Brewster, Chris Crosetto, Dennis Dietrich, Tom Jackson, 
//               Michael Kelsey, Brandon Ortiz, Craig Taylor, Chris Trevino, 
//               and Luke Walker
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.
// See src/setup/License.rtf for complete licensing and attribution information.
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Copied for Paint.NET PCX Plugin
// Copyright (C) Joshua Bell
/////////////////////////////////////////////////////////////////////////////////

// Based on: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnaspp/html/colorquant.asp

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace PcxFileTypePlugin.Quantize
{
	/// <summary>
	/// Quantize using an Octree
	/// </summary>
	public class OctreeQuantizer : Quantizer
	{
		/// <summary>
		/// Stores the tree
		/// </summary>
		private readonly Octree _octree;

		/// <summary>
		/// Maximum allowed color depth
		/// </summary>
		private readonly int _maxColors;

		/// <param name="maxColors">The maximum number of colors to return</param>
		/// <param name="maxColorBits">The number of significant bits</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxColors"/> ≥ <c>256</c> or <paramref name="maxColorBits"/> outside range <c>1..8</c></exception>
		/// <remarks>The Octree quantizer is a two pass algorithm. The initial pass sets up the octree, the second pass quantizes a color based on the nodes in the tree.</remarks>
		public OctreeQuantizer(int maxColors, int maxColorBits) : base(false)
		{
			if (maxColors > 255)
			{
				throw new ArgumentOutOfRangeException(nameof(maxColors), maxColors, "The number of colors should be less than 256");
			}

			if ((maxColorBits < 1) | (maxColorBits > 8))
			{
				throw new ArgumentOutOfRangeException(nameof(maxColorBits), maxColorBits, "This should be between 1 and 8");
			}

			_octree = new Octree(maxColorBits);
			_maxColors = maxColors;
		}

		/// <summary>
		/// Process the pixel in the first pass of the algorithm
		/// </summary>
		/// <param name="pixel">The pixel to quantize</param>
		/// <remarks>
		/// This function need only be overridden if your quantize algorithm needs two passes,
		/// such as an Octree quantizer.
		/// </remarks>
		protected override void InitialQuantizePixel(int pixel)
		{
			_octree.AddColor(pixel);
		}

		/// <summary>
		/// Override this to process the pixel in the second pass of the algorithm
		/// </summary>
		/// <param name="pixel">The pixel to quantize</param>
		/// <returns>The quantized value</returns>
		protected override byte QuantizePixel(int pixel)
		{
			byte paletteIndex = (byte)_maxColors;    // The color at [_maxColors] is set to transparent

			// Get the palette index if this non-transparent
			int a = (pixel >> 24) & 0xFF;

#if HANDLE_TRANSPARENCY
            if (a > 0)
#endif
			{
				paletteIndex = (byte)_octree.GetPaletteIndex(pixel);
			}

			return paletteIndex;
		}

		/// <summary>
		/// Retrieve the palette for the quantized image
		/// </summary>
		/// <param name="original">Any old palette, this is overwritten</param>
		/// <returns>The new color palette</returns>
		protected override ColorPalette GetPalette(ColorPalette original)
		{
			// First off convert the octree to _maxColors colors
			List<Color> palette = _octree.Palletize(_maxColors - 1);

			// Then convert the palette based on those colors
			for (int index = 0; index < palette.Count; index++)
			{
				original.Entries[index] = palette[index];
			}

#if ORIGINAL_CODE
            for (int i = palette.Count; i < original.Entries.Length; ++i)
            {
                original.Entries[i] = Color.FromArgb(255, 0, 0, 0);
            }

            // Add the transparent color
            original.Entries[_maxColors] = Color.FromArgb(0, 0, 0, 0);
#else // PCX Plugin
			// For PCX: Pad with transparency
			for (int i = palette.Count; i < original.Entries.Length; ++i)
			{
				original.Entries[i] = Color.Transparent;
			}
#endif
			return original;
		}

		/// <summary>
		/// Class which does the actual quantization
		/// </summary>
		private class Octree
		{
			/// <summary>
			/// Construct the octree
			/// </summary>
			/// <param name="maxColorBits">The maximum number of significant bits in the image</param>
			public Octree(int maxColorBits)
			{
				_maxColorBits = maxColorBits;
				_leafCount = 0;
				_reducibleNodes = new OctreeNode[9];
				_root = new OctreeNode(0, _maxColorBits, this);
				_previousColor = 0;
				_previousNode = null;
			}

			/// <summary>
			/// Add a given color value to the octree
			/// </summary>
			public void AddColor(int pixel)
			{
				// Check if this request is for the same color as the last
				if (_previousColor == pixel)
				{
					// If so, check if I have a previous node setup. This will only occur if the first color in the image
					// happens to be black, with an alpha component of zero.
					if (null == _previousNode)
					{
						_previousColor = pixel;
						_root.AddColor(pixel, _maxColorBits, 0, this);
					}
					else
					{
						// Just update the previous node
						_previousNode.Increment(pixel);
					}
				}
				else
				{
					_previousColor = pixel;
					_root.AddColor(pixel, _maxColorBits, 0, this);
				}
			}

			/// <summary>
			/// Reduce the depth of the tree
			/// </summary>
			private void Reduce()
			{
				int index;

				// Find the deepest level containing at least one reducible node
				for (index = _maxColorBits - 1; index > 0 && _reducibleNodes[index] == null; index--)
				{
					// intentionally blank
				}

				// Reduce the node most recently added to the list at level 'index'
				OctreeNode node = _reducibleNodes[index];
				_reducibleNodes[index] = node.NextReducible;

				// Decrement the leaf count after reducing the node
				_leafCount -= node.Reduce();

				// And just in case I've reduced the last color to be added, and the next color to
				// be added is the same, invalidate the previousNode...
				_previousNode = null;
			}

			/// <summary>
			/// Return the array of reducible nodes
			/// </summary>
			protected OctreeNode[] ReducibleNodes => _reducibleNodes;

			/// <summary>
			/// Keep track of the previous node that was quantized
			/// </summary>
			/// <param name="node">The node last quantized</param>
			protected void TrackPrevious(OctreeNode node)
			{
				_previousNode = node;
			}

			private Color[] _palette;
			private PaletteTable _paletteTable;

			/// <summary>
			/// Convert the nodes in the octree to a palette with a maximum of colorCount colors
			/// </summary>
			/// <param name="colorCount">The maximum number of colors</param>
			/// <returns>A list with the palettized colors</returns>
			public List<Color> Palletize(int colorCount)
			{
				while (_leafCount > colorCount)
				{
					Reduce();
				}

				// Now palletize the nodes
				var palette = new List<Color>(_leafCount);
				int paletteIndex = 0;

				_root.ConstructPalette(palette, ref paletteIndex);

				// And return the palette
				_palette = palette.ToArray();
				_paletteTable = null;

				return palette;
			}

			/// <summary>
			/// Get the palette index for the passed color
			/// </summary>
			public int GetPaletteIndex(int pixel)
			{
				var ret = _root.GetPaletteIndex(pixel, 0);

				if (ret < 0)
				{
					if (_paletteTable == null)
					{
						_paletteTable = new PaletteTable(_palette);
					}

					ret = _paletteTable.FindClosestPaletteIndex(Color.FromArgb(pixel));
				}

				return ret;
			}

			/// <summary>
			/// Mask used when getting the appropriate pixels for a given node
			/// </summary>
			private static readonly int[] Mask = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

			/// <summary>
			/// The root of the octree
			/// </summary>
			private readonly OctreeNode _root;

			/// <summary>
			/// Number of leaves in the tree
			/// </summary>
			private int _leafCount;

			/// <summary>
			/// Array of reducible nodes
			/// </summary>
			private readonly OctreeNode[] _reducibleNodes;

			/// <summary>
			/// Maximum number of significant bits in the image
			/// </summary>
			private readonly int _maxColorBits;

			/// <summary>
			/// Store the last node quantized
			/// </summary>
			private OctreeNode _previousNode;

			/// <summary>
			/// Cache the previous color quantized
			/// </summary>
			private int _previousColor;

			/// <summary>
			/// Class which encapsulates each node in the tree
			/// </summary>
			protected class OctreeNode
			{
				/// <summary>
				/// Construct the node
				/// </summary>
				/// <param name="level">The level in the tree = 0 - 7</param>
				/// <param name="colorBits">The number of significant color bits in the image</param>
				/// <param name="octree">The tree to which this node belongs</param>
				public OctreeNode(int level, int colorBits, Octree octree)
				{
					// Construct the new node
					_leaf = (level == colorBits);

					_red = 0;
					_green = 0;
					_blue = 0;
					_pixelCount = 0;

					// If a leaf, increment the leaf count
					if (_leaf)
					{
						octree._leafCount++;
						_nextReducible = null;
						_children = null;
					}
					else
					{
						// Otherwise add this to the reducible nodes
						_nextReducible = octree.ReducibleNodes[level];
						octree.ReducibleNodes[level] = this;
						_children = new OctreeNode[8];
					}
				}

				/// <summary>
				/// Add a color into the tree
				/// </summary>
				/// <param name="pixel">The color</param>
				/// <param name="colorBits">The number of significant color bits</param>
				/// <param name="level">The level in the tree</param>
				/// <param name="octree">The tree to which this node belongs</param>
				public void AddColor(int pixel, int colorBits, int level, Octree octree)
				{
					// Update the color information if this is a leaf
					if (_leaf)
					{
						Increment(pixel);

						// Setup the previous node
						octree.TrackPrevious(this);
					}
					else
					{
						// Go to the next level down in the tree
						int shift = 7 - level;
						int b = pixel & 0xFF;
						int g = (pixel >> 8) & 0xFF;
						int r = (pixel >> 16) & 0xFF;
						int index = ((r & Mask[level]) >> (shift - 2)) |
									((g & Mask[level]) >> (shift - 1)) |
									((b & Mask[level]) >> (shift));

						OctreeNode child = _children[index];

						if (null == child)
						{
							// Create a new child node & store in the array
							child = new OctreeNode(level + 1, colorBits, octree);
							_children[index] = child;
						}

						// Add the color to the child node
						child.AddColor(pixel, colorBits, level + 1, octree);
					}

				}

				/// <summary>
				/// Get/Set the next reducible node
				/// </summary>
				public OctreeNode NextReducible => _nextReducible;

				/// <summary>
				/// Reduce this node by removing all of its children
				/// </summary>
				/// <returns>The number of leaves removed</returns>
				public int Reduce()
				{
					int children = 0;
					_red = 0;
					_green = 0;
					_blue = 0;

					// Loop through all children and add their information to this node
					for (int index = 0; index < 8; index++)
					{
						if (null != _children[index])
						{
							_red += _children[index]._red;
							_green += _children[index]._green;
							_blue += _children[index]._blue;
							_pixelCount += _children[index]._pixelCount;
							++children;
							_children[index] = null;
						}
					}

					// Now change this to a leaf node
					_leaf = true;

					// Return the number of nodes to decrement the leaf count by
					return (children - 1);
				}

				/// <summary>
				/// Traverse the tree, building up the color palette
				/// </summary>
				/// <param name="palette">The palette</param>
				/// <param name="paletteIndex">The current palette index</param>
				public void ConstructPalette(List<Color> palette, ref int paletteIndex)
				{
					if (_leaf)
					{
						// Consume the next palette index
						_paletteIndex = paletteIndex++;

						// And set the color of the palette entry
						int r = _red / _pixelCount;
						int g = _green / _pixelCount;
						int b = _blue / _pixelCount;

						palette.Add(Color.FromArgb(r, g, b));
					}
					else
					{
						// Loop through children looking for leaves
						for (int index = 0; index < 8; index++)
						{
							_children[index]?.ConstructPalette(palette, ref paletteIndex);
						}
					}
				}

				/// <summary>
				/// Return the palette index for the passed color
				/// </summary>
				public int GetPaletteIndex(int pixel, int level)
				{
					int paletteIndex = _paletteIndex;

					if (!_leaf)
					{
						int shift = 7 - level;
						int b = pixel & 0xFF;
						int g = (pixel >> 8) & 0xFF;
						int r = (pixel >> 16) & 0xFF;
						int index = ((r & Mask[level]) >> (shift - 2)) |
									((g & Mask[level]) >> (shift - 1)) |
									((b & Mask[level]) >> (shift));

						if (null != _children[index])
						{
							paletteIndex = _children[index].GetPaletteIndex(pixel, level + 1);
						}
						else
						{
							paletteIndex = -1;
						}
					}

					return paletteIndex;
				}

				/// <summary>
				/// Increment the pixel count and add to the color information
				/// </summary>
				public void Increment(int pixel)
				{
					++_pixelCount;
					int b = pixel & 0xFF;
					int g = (pixel >> 8) & 0xFF;
					int r = (pixel >> 16) & 0xFF;
					_red += r;
					_green += g;
					_blue += b;
				}

				/// <summary>
				/// Flag indicating that this is a leaf node
				/// </summary>
				private bool _leaf;

				/// <summary>
				/// Number of pixels in this node
				/// </summary>
				private int _pixelCount;

				/// <summary>
				/// Red component
				/// </summary>
				private int _red;

				/// <summary>
				/// Green Component
				/// </summary>
				private int _green;

				/// <summary>
				/// Blue component
				/// </summary>
				private int _blue;

				/// <summary>
				/// Pointers to any child nodes
				/// </summary>
				private readonly OctreeNode[] _children;

				/// <summary>
				/// Pointer to next reducible node
				/// </summary>
				private readonly OctreeNode _nextReducible;

				/// <summary>
				/// The index of this node in the palette
				/// </summary>
				private int _paletteIndex;
			}
		}
	}
}
