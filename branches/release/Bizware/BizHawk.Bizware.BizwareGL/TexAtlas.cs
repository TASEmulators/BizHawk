using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

namespace BizHawk.Bizware.BizwareGL
{
	public class TexAtlas
	{
		public class RectItem
		{
			public RectItem(int width, int height, object item)
			{
				Width = width;
				Height = height;
				Item = item;
			}
			public int X, Y;
			public int Width, Height;
			public int TexIndex;
			public object Item;
		}


		class TryFitParam
		{
			public TryFitParam(int _w, int _h) { this.w = _w; this.h = _h; }
			public int w, h;
			public bool ok = true;
			public RectangleBinPack rbp = new RectangleBinPack();
			public List<RectangleBinPack.Node> nodes = new List<RectangleBinPack.Node>();
		}

		public class PackedAtlasResults
		{
			public class SingleAtlas
			{
				public Size Size;
				public List<RectItem> Items;
			}
			public List<SingleAtlas> Atlases = new List<SingleAtlas>();
		}

		public static int MaxSizeBits = 16;

		/// <summary>
		/// packs the supplied RectItems into an atlas. Modifies the RectItems with x/y values of location in new atlas.
		/// </summary>
		public static PackedAtlasResults PackAtlas(IEnumerable<RectItem> items)
		{
			PackedAtlasResults ret = new PackedAtlasResults();
			ret.Atlases.Add(new PackedAtlasResults.SingleAtlas());

			//initially, we'll try all the items; none remain
			List<RectItem> currentItems = new List<RectItem>(items);
			List<RectItem> remainItems = new List<RectItem>();

		RETRY:

			//this is where the texture size range is determined.
			//we run this every time we make an atlas, in case we want to variably control the maximum texture output size.
			//ALSO - we accumulate data in there, so we need to refresh it each time. ... lame.
			List<TryFitParam> todoSizes = new List<TryFitParam>();
			for (int i = 3; i <= MaxSizeBits; i++)
			{
				for (int j = 3; j <= MaxSizeBits; j++)
				{
					int w = 1 << i;
					int h = 1 << j;
					TryFitParam tfp = new TryFitParam(w, h);
					todoSizes.Add(tfp);
				}
			}

			//run the packing algorithm on each potential size
			Parallel.ForEach(todoSizes, (param) =>
			{
				var rbp = new RectangleBinPack();
				rbp.Init(16384, 16384);
				param.rbp.Init(param.w, param.h);

				foreach (var ri in currentItems)
				{
					RectangleBinPack.Node node = param.rbp.Insert(ri.Width, ri.Height);
					if (node == null)
					{
						param.ok = false;
					}
					else
					{
						node.ri = ri;
						param.nodes.Add(node);
					}
				}
			});

			//find the best fit among the potential sizes that worked
			long best = long.MaxValue;
			TryFitParam tfpFinal = null;
			foreach (TryFitParam tfp in todoSizes)
			{
				if (tfp.ok)
				{
					long area = (long)tfp.w * (long)tfp.h;
					long perimeter = (long)tfp.w + (long)tfp.h;
					if (area < best)
					{
						best = area;
						tfpFinal = tfp;
					}
					else if (area == best)
					{
						//try to minimize perimeter (to create squares, which are nicer to look at)
						if (tfpFinal == null)
						{ }
						else if (perimeter < tfpFinal.w + tfpFinal.h)
						{
							best = area;
							tfpFinal = tfp;
						}
					}
				}
			}

			//did we find any fit?
			if (best == long.MaxValue)
			{
				//nope - move an item to the remaining list and try again
				remainItems.Add(currentItems[currentItems.Count - 1]);
				currentItems.RemoveAt(currentItems.Count - 1);
				goto RETRY;
			}

			//we found a fit. setup this atlas in the result and drop the items into it
			var atlas = ret.Atlases[ret.Atlases.Count - 1];
			atlas.Size.Width = tfpFinal.w;
			atlas.Size.Height = tfpFinal.h;
			atlas.Items = new List<RectItem>(items);
			foreach (var item in currentItems)
			{
				object o = item.Item;
				var node = tfpFinal.nodes.Find((x) => x.ri == item);
				item.X = node.x;
				item.Y = node.y;
				item.TexIndex = ret.Atlases.Count - 1;
			}

			//if we have any items left, we've got to run this again
			if (remainItems.Count > 0)
			{
				//move all remaining items into the clear list
				currentItems.Clear();
				currentItems.AddRange(remainItems);
				remainItems.Clear();

				ret.Atlases.Add(new PackedAtlasResults.SingleAtlas());
				goto RETRY;
			}

			if (ret.Atlases.Count > 1)
				Console.WriteLine("Created animset with >1 texture ({0} textures)", ret.Atlases.Count);

			return ret;
		}

		//original file: RectangleBinPack.cpp
		//author: Jukka Jylänki
		class RectangleBinPack
		{
			/** A node of a binary tree. Each node represents a rectangular area of the texture
				we surface. Internal nodes store rectangles of used data, whereas leaf nodes track 
				rectangles of free space. All the rectangles stored in the tree are disjoint. */
			public class Node
			{
				// Left and right child. We don't really distinguish which is which, so these could
				// as well be child1 and child2.
				public Node left;
				public Node right;

				// The top-left coordinate of the rectangle.
				public int x;
				public int y;

				// The dimension of the rectangle.
				public int width;
				public int height;

				public RectItem ri;
			};

			/// Starts a new packing process to a bin of the given dimension.
			public void Init(int width, int height)
			{
				binWidth = width;
				binHeight = height;
				root = new Node();
				root.left = root.right = null;
				root.x = root.y = 0;
				root.width = width;
				root.height = height;
			}


			/// Inserts a new rectangle of the given size into the bin.
			/** Running time is linear to the number of rectangles that have been already packed.
				@return A pointer to the node that stores the newly added rectangle, or 0 
					if it didn't fit. */
			public Node Insert(int width, int height)
			{
				return Insert(root, width, height);
			}

			/// Computes the ratio of used surface area.
			float Occupancy()
			{
				int totalSurfaceArea = binWidth * binHeight;
				int usedSurfaceArea = UsedSurfaceArea(root);

				return (float)usedSurfaceArea / totalSurfaceArea;
			}

			private Node root;

			// The total size of the bin we started with.
			private int binWidth;
			private int binHeight;

			/// @return The surface area used by the subtree rooted at node.
			private int UsedSurfaceArea(Node node)
			{
				if (node.left != null || node.right != null)
				{
					int usedSurfaceArea = node.width * node.height;
					if (node.left != null)
						usedSurfaceArea += UsedSurfaceArea(node.left);
					if (node.right != null)
						usedSurfaceArea += UsedSurfaceArea(node.right);

					return usedSurfaceArea;
				}

				// This is a leaf node, it doesn't constitute to the total surface area.
				return 0;
			}


			/// Inserts a new rectangle in the subtree rooted at the given node.
			private Node Insert(Node node, int width, int height)
			{

				// If this node is an internal node, try both leaves for possible space.
				// (The rectangle in an internal node stores used space, the leaves store free space)
				if (node.left != null || node.right != null)
				{
					if (node.left != null)
					{
						Node newNode = Insert(node.left, width, height);
						if (newNode != null)
							return newNode;
					}
					if (node.right != null)
					{
						Node newNode = Insert(node.right, width, height);
						if (newNode != null)
							return newNode;
					}
					return null; // Didn't fit into either subtree!
				}

				// This node is a leaf, but can we fit the new rectangle here?
				if (width > node.width || height > node.height)
					return null; // Too bad, no space.

				// The new cell will fit, split the remaining space along the shorter axis,
				// that is probably more optimal.
				int w = node.width - width;
				int h = node.height - height;
				node.left = new Node();
				node.right = new Node();
				if (w <= h) // Split the remaining space in horizontal direction.
				{
					node.left.x = node.x + width;
					node.left.y = node.y;
					node.left.width = w;
					node.left.height = height;

					node.right.x = node.x;
					node.right.y = node.y + height;
					node.right.width = node.width;
					node.right.height = h;
				}
				else // Split the remaining space in vertical direction.
				{
					node.left.x = node.x;
					node.left.y = node.y + height;
					node.left.width = width;
					node.left.height = h;

					node.right.x = node.x + width;
					node.right.y = node.y;
					node.right.width = w;
					node.right.height = node.height;
				}
				// Note that as a result of the above, it can happen that node.left or node.right
				// is now a degenerate (zero area) rectangle. No need to do anything about it,
				// like remove the nodes as "unnecessary" since they need to exist as children of
				// this node (this node can't be a leaf anymore).

				// This node is now a non-leaf, so shrink its area - it now denotes
				// *occupied* space instead of free space. Its children spawn the resulting
				// area of free space.
				node.width = width;
				node.height = height;
				return node;
			}
		};

	}


}