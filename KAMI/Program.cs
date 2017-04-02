//#define VERBOSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

using DotImaging;

namespace KAMI
{
	public struct Node
	{
		public static readonly Node Empty =
			new Node { Class = -1, Links = null };

		public int Class { get; set; }
		public List<int> Links { get; set; }

		public Node(int clas, params int[] links)
		{
			Class = clas;
			Links = links.ToList();
		}

		public bool IsEmpty()
		{
			return Class == -1;
		}
	}

	public class Network
	{
		public Node[] Nodes { get; set; }
		public Network Parent { get; set; }
		public int Depth { get; set; }
		public int FromMoveNode { get; set; }
		public int FromMoveClass { get; set; }

		public string GetInvariant()
		{
			var builder = new StringBuilder();
			for (int i = 0; i < Nodes.Length; i++)
				if (!Nodes[i].IsEmpty())
					builder.Append($"N{i}({Nodes[i].Class})>{string.Join("+", Nodes[i].Links.Select(l => l.ToString()))} ");
			return builder.ToString();
		}

		public void PrintTree()
		{
			if (Parent != null)
				Parent.PrintTree();
			if (FromMoveNode != -1)
				Console.WriteLine($"MOVE -> Tap {FromMoveNode} with {MainClass.Colors[FromMoveClass]}");
#if VERBOSE
			Console.WriteLine($"     -> {GetInvariant()}");
#endif
		}

		public void FixLinks()
		{
			for (int i = 0; i < Nodes.Length; i++)
			{
				foreach (int j in Nodes[i].Links)
				{
					if (!Nodes[j].Links.Contains(i))
						Nodes[j].Links.Add(i);
				}
			}
		}

		public IEnumerable<Network> GetChildren()
		{
			// Loop through each possible move
			foreach (int clas in Classes)
			{
				for (int i = 0; i < Nodes.Length; i++)
				{
					// Pre-evaluate node with class
					if (Prevaluate(i, clas))
						// Apply move and return resulting network
						// will be pushed back on fringe by parent
						// algorithm
						yield return ApplyMove(i, clas);
				}
			}
		}

		public bool Prevaluate(int nodeid, int clas)
		{
			return (
				// No point in checking empty nodes
				!Nodes[nodeid].IsEmpty() &&
				// No point in converting nodes that already have target class
				!(Nodes[nodeid].Class == clas)
				// Has neighbouring nodes that can be converted
				// WARNING : Can speed up process for some puzzles but wont work at all on others!
				// && (Nodes[nodeid].Links.Count(nid => Nodes[nid].Class == clas) > 0)
			);
		}

		public Network ApplyMove(int nodeid, int clas)
		{
			Debug.Assert(nodeid != -1 && clas != -1);

			var sameadj = Nodes[nodeid]
				.Links
				.Where(nid => Nodes[nid].Class == clas);

			Network result;
			if (sameadj.Count() == 0)
			{
				// Copy all nodes
				var child = new Node[Nodes.Length];
				for (int i = 0; i < Nodes.Length; i++)
					child[i] = new Node
					{
						Class = Nodes[i].Class,
						Links = Nodes[i].Links != null ? new List<int>(Nodes[i].Links) : null
					};

				// Change class number
				child[nodeid].Class = clas;

				result = new Network
				{
					Nodes = child,
					Parent = this,
					Depth = Depth + 1,
					FromMoveNode = nodeid,
					FromMoveClass = clas
				};
			}
			else
			{
				// Copy non-same nodes
				var child = new Node[Nodes.Length];
				for (int i = 0; i < Nodes.Length; i++)
				{
					if (sameadj.Contains(i) && !(i == nodeid))
						child[i] = Node.Empty;
					else
						child[i] = new Node
						{
							Class = Nodes[i].Class,
							Links = Nodes[i].Links != null
			                    // Don't forget to prune all links to now non-existent nodes!
			                    ? new List<int>(Nodes[i].Links.Where(nid => !sameadj.Contains(nid)))
			                    : null
						};
				}

				// Merge same nodes into one
				child[nodeid].Links = sameadj
					.SelectMany(nid => Nodes[nid].Links)
					.Distinct()
					.ToList();
				child[nodeid].Class = clas;

				result = new Network
				{
					Nodes = child,
					Parent = this,
					Depth = Depth + 1,
					FromMoveNode = nodeid,
					FromMoveClass = clas
				};
			}

			Debug.Assert(!((result.NumNonEmptyNodes == 1) && (result.Nodes.First(n => !n.IsEmpty()).Links.Count() != 0)));
			return result;
		}

		// Is it even possible for network to be solved
		// with given target?
		public bool CanReachTarget(int target)
		{
			// Impossible to reach NDC = 1 w/ T=x
			// in some conditions
			return (NumDistinctClasses + Depth) <= (target + 1);
		}

		// Solved when only one non-empty node is left in
		// network
		public bool Solved => Nodes.Count(n => !n.IsEmpty()) == 1;

		// List of distinct classes left in network
		public IEnumerable<int> Classes => Nodes
			.Where(n => !n.IsEmpty())
			.Select(n => n.Class)
			.Distinct();

		// Number of nodes left over in network
		public int NumNonEmptyNodes => Nodes.Count(n => !n.IsEmpty());

		// Number of distinct classes left over in network
		public int NumDistinctClasses => Classes.Count();
	}

	public class MainClass
	{
		// These colors per code are present in puzzle
		public static readonly string[] Colors =
		{
			"Black",	// 0
			"Red",  	// 1
			"Green",   	// 2
			"White"     // 3
		};

		public static void LoadNetworkFromBitmap_Conquor(Bgr<byte>[,] grd, int x, int y, int[,] objs, int cc)
		{
			const int threshold = 20;

			int r = grd[x, y].R,
				g = grd[x, y].G,
				b = grd[x, y].B;

			var neighbours = new List<Tuple<int, int>>
					{
						Tuple.Create(x, y - 1),
						Tuple.Create(x, y + 1),
						(x % 2 == 0)
							? Tuple.Create(x - 1, y)
							: Tuple.Create(x + 1, y)
					};

			foreach (var cell in neighbours)
			{
				int nx = cell.Item1,
					ny = cell.Item2;

				if (nx < 0 || nx >= grd.Width())
					continue;
				if (ny < 0 || ny >= grd.Height())
					continue;

				var pixel = grd[nx, ny];

				if (Math.Abs(r - pixel.R) < threshold &&
					Math.Abs(g - pixel.G) < threshold &&
					Math.Abs(b - pixel.B) < threshold)
				{
					objs[nx, ny] = cc;
					LoadNetworkFromBitmap_Conquor(grd, nx, ny, objs, cc);
				}
			}
		}

		public static Network LoadNetworkFromBitmap(Bgr<byte>[,] image)
		{
			const int fieldHeight = 1208;
			const int fieldVerticalNum = 28;
			const int fieldHorizontalNum = 10;

			int horizontalDelta = image.Width() / fieldHorizontalNum;
			int verticalDelta = fieldHeight / fieldVerticalNum;

			var grid = new Bgr<byte>[fieldHorizontalNum, fieldVerticalNum];

			for (int y = 0, cy = 0; y < fieldHeight; y += verticalDelta, cy++)
				for (int x = horizontalDelta / 2, cx = 0; x < image.Width(); x += horizontalDelta, cx++)
					grid[cx, cy] = image[x, y];

			var objects = new int[fieldHorizontalNum, fieldVerticalNum];
			for (int y = 0; y < fieldVerticalNum; y++)
				for (int x = 0; x < fieldHorizontalNum; x++)
					objects[x, y] = -1;

			int c = 0;
			for (int y = 0; y < fieldVerticalNum; y++)
			{
				for (int x = 0; x < fieldHorizontalNum; x++)
				{
					if (objects[x, y] == -1)
					{
						LoadNetworkFromBitmap_Conquor(grid, x, y, objects, c);
						c++;
					}
				}
			}

		}

		public static void Main(string[] args)
		{
			var n0 = new Node(0);
			var n1 = new Node(0);
			var n2 = new Node(1, 0);
			var n3 = new Node(1, 0);
			var n4 = new Node(1);
			var n5 = new Node(1);
			var n6 = new Node(1, 1);
			var n7 = new Node(1, 1);
			var n8 = new Node(2, 3);
			var n9 = new Node(2, 2);
			var n10 = new Node(2, 4);
			var n11 = new Node(2, 5);
			var n12 = new Node(2, 4);
			var n13 = new Node(2, 5);
			var n14 = new Node(2, 6, 1);
			var n15 = new Node(2, 7, 1);
			var n16 = new Node(3, 8, 10);
			var n17 = new Node(3, 9, 11);
			var n18 = new Node(3, 1, 12, 14);
			var n19 = new Node(3, 1, 13, 15);
			var n20 = new Node(3, 4, 6, 12, 14);
			var n21 = new Node(3, 5, 7, 13, 15);
			var n22 = new Node(3, 1, 4, 6);
			var n23 = new Node(3, 1, 5, 7);

			var rootNodes = new Node[] {
				n0, n1, n2, n3, n4, n5, n6, n7, n8, n9, n10,
				n11, n12, n13, n14, n15, n16, n17, n18, n19,
				n20, n21, n22, n23
			};

			var root = new Network
			{
				Nodes = rootNodes,
				Parent = null,
				Depth = 0,
				FromMoveNode = -1,
				FromMoveClass = -1
			};
			// Double links nodes that weren't already
			root.FixLinks();

			// Find solution
			var solution = Bfs(root, 7);

			// Print solution if available
			Console.WriteLine("------");
			if (solution != null)
			{
				Console.WriteLine($"Depth: {solution.Depth}");
				solution.PrintTree();
			}
			else
				Console.WriteLine("No solution");
		}

		class NetworkHeuristic : IComparer<Network>
		{
			// Heuristic
			//  - First: num non-empty nodes in network
			//  - Second: num non-empty distinct classes left over
			int IComparer<Network>.Compare(Network a, Network b)
			{
				if (a.NumNonEmptyNodes > b.NumNonEmptyNodes)
					return 1;
				else if (a.NumNonEmptyNodes < b.NumNonEmptyNodes)
					return -1;
				else
				{
					if (a.NumDistinctClasses > b.NumDistinctClasses)
						return 1;
					else if (a.NumDistinctClasses < b.NumDistinctClasses)
						return -1;
					else
						return 0;
				}
			}
		}

		public static Network Bfs(Network start, int target)
		{
			Network solution = null;
			var fringe = new C5.IntervalHeap<Network>(
				// Use this heuristic for picking the next
				// item to process
				new NetworkHeuristic(),
				C5.MemoryType.Normal
			);

			var discovered = new HashSet<string>();

			fringe.Add(start);
			while (solution == null && fringe.Count > 0)
			{
				// Find network with lowest heuristic value
				var network = fringe.FindMin();
				fringe.DeleteMin();
				if (network == null)
					continue;

#if VERBOSE
				Console.WriteLine(network.GetInvariant());
				if (fringe.Count % 100 == 0)
				{
					Console.WriteLine($"Progress Report:\n    FringeSize: {fringe.Count}\n    Discovered: {discovered.Count}");
				}
#endif

				// Solution is found when network is solved
				// (e.g. has one node) and meets target depth
				if (network.Solved)
					if (network.Depth <= target)
						solution = network;

				// Seen this, store invariant
				discovered.Add(network.GetInvariant());

				foreach (var child in network.GetChildren())
				{
					var childinv = child.GetInvariant();
					// Dont process if
					//  - seen already
					//  - cannot meet target because of constraints
					if (!discovered.Contains(childinv) &&
					    child.CanReachTarget(target))
					{
						fringe.Add(child);
					}
				}
			}

			return solution;
		}
	}
}
