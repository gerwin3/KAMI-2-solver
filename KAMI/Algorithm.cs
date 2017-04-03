using System;
using System.Collections.Generic;

namespace KAMI
{
	public class Algorithm
	{
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
