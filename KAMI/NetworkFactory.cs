using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace KAMI
{
	public static class NetworkFactory
	{
		public static Network CreateFromImage(Bitmap image)
		{
			const int fieldHeight = 1208;
			const int fieldVerticalNum = 28;
			const int fieldHorizontalNum = 10;

			int horizontalDelta = image.Width / fieldHorizontalNum;
			int verticalDelta = fieldHeight / fieldVerticalNum;

			var grid = new Color[fieldHorizontalNum, fieldVerticalNum];

			for (int y = 0, cy = 0; cy < fieldVerticalNum; y += verticalDelta, cy++)
				for (int x = horizontalDelta / 2, cx = 0; cx < fieldHorizontalNum; x += horizontalDelta, cx++)
					grid[cx, cy] = image.GetPixel(x, y);

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
						Partition(grid, x, y, objects, c);
						c++;
					}
				}
			}

			var blobs = objects.Cast<int>().Distinct();
			var blobNeighbours = new List<List<Tuple<int, int>>>();
			for (int i = 0; i < blobs.Count(); i++)
				blobNeighbours.Add(new List<Tuple<int, int>>());
			for (int y = 0; y < fieldVerticalNum; y++)
				for (int x = 0; x < fieldHorizontalNum; x++)
					blobNeighbours[objects[x, y]].AddRange(Neighbours(x, y, fieldHorizontalNum, fieldVerticalNum));
			var blobLinks = blobNeighbours
				.Select(ns => ns
					.Select(n => objects[n.Item1, n.Item2])
					.Distinct()
			        .ToList()
				)
				.ToList();

			var classes = new List<Color>();
			var blobColors = new List<List<Color>>();
			for (int i = 0; i < blobs.Count(); i++)
				blobColors.Add(new List<Color>());
			for (int y = 0; y < fieldVerticalNum; y++)
				for (int x = 0; x < fieldHorizontalNum; x++)
					blobColors[objects[x, y]].Add(grid[x, y]);
			var blobColor = new Color[blobs.Count()];
			int blobIndex = 0;
			foreach (var blobC in blobColors)
			{
				Color avgc = Color.FromArgb(
					(int) blobC.Select(x => (int) x.R).Average(),
					(int) blobC.Select(x => (int) x.G).Average(),
					(int) blobC.Select(x => (int) x.B).Average());

				blobColor[blobIndex] = avgc;

				if (classes.Count(x => {
						var delta = Color.FromArgb(
							Math.Abs(x.R - avgc.R),
							Math.Abs(x.G - avgc.G),
							Math.Abs(x.B - avgc.B));
						return (delta.R < 30 && delta.G < 30 && delta.B < 30);
					}) <= 0)
				{
					classes.Add(avgc);
				}

				blobIndex++;
			}

			var nodes = blobs
				.Select(b =>
				{
					int clas = 0;
					int tdeltaMin = int.MaxValue;
					for (int i = 0; i < classes.Count(); i++)
					{
						var delta = Color.FromArgb(
							Math.Abs(classes[i].R - blobColor[b].R),
							Math.Abs(classes[i].G - blobColor[b].G),
							Math.Abs(classes[i].B - blobColor[b].B));
						var tdelta = delta.R + delta.G + delta.B;
						if (tdelta < tdeltaMin)
						{
							tdeltaMin = tdelta;
							clas = i;
						}
					}
					return new Node(clas, blobLinks[b]);
				})
				.ToArray();

			var network = new Network
			{
				Nodes = nodes,
				Parent = null,
				Depth = 0,
				FromMoveNode = -1,
				FromMoveClass = -1
			};
			network.FixLinks();

			return network;
		}

		private static List<Tuple<int, int>> Neighbours(int x, int y, int w, int h)
		{
			return new List<Tuple<int, int>>
			{
				Tuple.Create(x, y - 1),
				Tuple.Create(x, y + 1),
				(y % 2 == 0)
					? Tuple.Create(x - 1, y)
					: Tuple.Create(x + 1, y)
			}
			.Where(xy => xy.Item1 >= 0 && xy.Item1 < w && xy.Item2 >= 0 && xy.Item2 < h)
		 	.ToList();
		}

		private static void Partition(Color[,] grd, int x, int y, int[,] objs, int cc)
		{
			int w = grd.GetLength(0),
			    h = grd.GetLength(1);

			const int threshold = 30;
			objs[x, y] = cc;

			int r = grd[x, y].R,
				g = grd[x, y].G,
				b = grd[x, y].B;

			foreach (var cell in Neighbours(x, y, w, h))
			{
				int nx = cell.Item1,
					ny = cell.Item2;

				if (nx < 0 || nx >= w)
					continue;
				if (ny < 0 || ny >= h)
					continue;
				if (objs[nx, ny] != -1)
					continue;

				var pixel = grd[nx, ny];

				if (Math.Abs(r - pixel.R) < threshold &&
					Math.Abs(g - pixel.G) < threshold &&
					Math.Abs(b - pixel.B) < threshold)
				{
					Partition(grd, nx, ny, objs, cc);
				}
			}
		}
	}
}
