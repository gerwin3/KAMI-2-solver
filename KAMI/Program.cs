using System;
using System.Drawing;
using System.IO;

namespace KAMI
{
	public class MainClass
	{
		public static int ImageFieldHeight = 1208;

		public static void Main(string[] args)
		{
			/* LOAD NETWORK */

			var image = new Bitmap("input.png");
			var root = new NetworkFactory().CreateFromImage(image);

			/* FIND SOLUTIONS */

			var solution = Algorithm.Bfs(root, 11);

			/* OUTPUT SOLUTION */

			Console.WriteLine("------");
			if (solution != null)
			{
				Console.WriteLine($"Depth: {solution.Depth}");
				solution.PrintTree();
				DrawSolution(new Bitmap("output.bmp"), solution);
			}
			else
				Console.WriteLine("No solution");
		}

		public static void DrawSolution(Bitmap image, Network solution)
		{
			using (var g = Graphics.FromImage(image))
			{
				g.FillRectangle(
					Brushes.White,
					new Rectangle(0, ImageFieldHeight, image.Width, image.Height - ImageFieldHeight));

				DrawSolution_DrawNode(g, solution);
			}

			image.Save("output.bmp");
		}

		public static int DrawSolution_DrawNode(Graphics g, Network network)
		{
			if (network.Parent == null)
				return 0;

			int i = DrawSolution_DrawNode(g, network.Parent);

			int x = i * 100,
				y = ImageFieldHeight;
			g.FillEllipse(
				new SolidBrush(Color.FromArgb(network.FromMoveClass)),
				new Rectangle(new Point(x, y), new Size(100, 100)));
			g.DrawString(
				network.FromMoveNode.ToString(),
				new Font("Tahoma", 40),
				Brushes.White,
				new PointF(x + 24, y + 16));

			return i + 1;
		}
	}
}
