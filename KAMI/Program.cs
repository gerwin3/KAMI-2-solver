using System;
using System.Drawing;
using System.IO;

namespace KAMI
{
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

		public static void Main(string[] args)
		{
			/*
			var rootNodes = new Node[] {
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
			*/

			/* LOAD NETWORK */

			var image = new Bitmap("input.png");
			var root = NetworkFactory.CreateFromImage(image);

			/* FIND SOLUTIONS */

			var solution = Algorithm.Bfs(root, 7);

			/* PRINT SOLUTION */

			Console.WriteLine("------");
			if (solution != null)
			{
				Console.WriteLine($"Depth: {solution.Depth}");
				solution.PrintTree();
			}
			else
				Console.WriteLine("No solution");
		}
	}
}
