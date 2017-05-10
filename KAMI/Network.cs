using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace KAMI
{
	public class Node
	{
		public static readonly Node Empty = new Node(-1, (List<int>)null);

		public int Class { get; set; }
		public List<int> Links { get; set; }

		public Node(int clas, params int[] links)
			: this(clas, links.ToList()) { }

		public Node(int clas, List<int> links)
		{
			Class = clas;
			Links = links;
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
				Console.WriteLine($"MOVE -> Tap {FromMoveNode} with {FromMoveClass}");
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
					child[i] = new Node(
						Nodes[i].Class,
						Nodes[i].Links != null ? new List<int>(Nodes[i].Links) : null);

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
						child[i] = new Node(
							Nodes[i].Class,
							Nodes[i].Links != null
								// Don't forget to prune all links to now non-existent nodes!
								? new List<int>(Nodes[i].Links.Where(nid => !sameadj.Contains(nid)))
								: null);
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
}
