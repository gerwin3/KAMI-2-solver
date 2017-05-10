using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace KAMI
{
	public class NetworkFactory
	{
		private class Blob
		{
			public Blob()
			{
				Id = -1;
				Points = new List<XY>();
				Neighbours = new List<int>();
			}

			public int Id { get; set; }
			public List<XY> Points { get; set; }
			public List<int> Neighbours { get; set; }
			public Color MyColor { get; set; }

			public IEnumerable<Color> GetColors(Color[,] field)
			{
				return Points
					.Select(p => field[p.X, p.Y]);
			}

			public Color GetAverageColor(Color[,] field)
			{
				var colors = GetColors(field);
				return Color.FromArgb(
					(int)colors.Select(x => (int)x.R).Average(),
					(int)colors.Select(x => (int)x.G).Average(),
					(int)colors.Select(x => (int)x.B).Average());
			}
		}

		private struct XY
		{
			public XY(int x, int y)
			{
				X = x;
				Y = y;
			}

			public int X { get; set; }
			public int Y { get; set; }
		}

		private readonly int ImageFieldHeight = MainClass.ImageFieldHeight;

		private readonly int FieldWidth = 10;
		private readonly int FieldHeight = 28;

		private readonly int ObjectColorThreshold = 35;

		private Bitmap Image { get; set; }
		private int Width => Image.Width;
		private int Height => Image.Height;

		private int HorizontalDelta => Width / FieldWidth;
		private int VerticalDelta => ImageFieldHeight / FieldHeight;

		private Color[,] ColorGrid { get; set; }
		private int ColorGridWidth => ColorGrid.GetLength(0);
		private int ColorGridHeight => ColorGrid.GetLength(1);

		private int[,] ObjectGrid { get; set; }
		private int ObjectGridWidth => ObjectGrid.GetLength(0);
		private int ObjectGridHeight => ObjectGrid.GetLength(1);

		private List<Blob> Blobs { get; set; }

		public Network CreateFromImage(Bitmap image)
		{
			Image = image;

			GenerateColorGrid();
			GenerateObjectGrid();
			FindBlobsAndNeighbours();
			AssignBlobColors();
			RemoveEmptyBlobs();
			DrawOutputImage();

			var nodes = Blobs
				.Select(b => new Node(
					b.MyColor.ToArgb(),
					b.Neighbours
				))
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

		private void GenerateColorGrid()
		{
			ColorGrid = new Color[FieldWidth, FieldHeight];

			for (int y = 0, cy = 0; cy < FieldHeight; y += VerticalDelta, cy++)
			{
				for (int x = HorizontalDelta / 2, cx = 0; cx < FieldWidth; x += HorizontalDelta, cx++)
				{
					ColorGrid[cx, cy] = Image.GetPixel(x, y);
#if DEBUG
					using (var g = Graphics.FromImage(Image))
						g.DrawEllipse(Pens.Black, new Rectangle(x, y, 1, 1));
						
#endif
				}
			}
		}

		private void GenerateObjectGrid()
		{
			ObjectGrid = new int[FieldWidth, FieldHeight];
			for (int y = 0; y < FieldHeight; y++)
				for (int x = 0; x < FieldWidth; x++)
					ObjectGrid[x, y] = -1;

			int c = 0;
			for (int y = 0; y < FieldHeight; y++)
			{
				for (int x = 0; x < FieldWidth; x++)
				{
					if (ObjectGrid[x, y] == -1)
					{
						Partition(x, y, c);
						c++;
					}
				}
			}
		}

		private List<XY> Neighbours(int x, int y)
		{
			return new List<XY>
				{
					new XY(x, y - 1),
					new XY(x, y + 1),
					(x % 2 == 0)
						? (y % 2 == 0)
							? new XY(x - 1, y)
							: new XY(x + 1, y)
						: (y % 2 == 0)
							? new XY(x + 1, y)
							: new XY(x - 1, y)
				}
				.Where(xy => xy.X >= 0 && xy.X < FieldWidth && xy.Y >= 0 && xy.Y < FieldHeight)
		 		.ToList();
		}

		private void Partition(int x, int y, int cc)
		{
			ObjectGrid[x, y] = cc;

			int r = ColorGrid[x, y].R,
				g = ColorGrid[x, y].G,
				b = ColorGrid[x, y].B;

			foreach (var cell in Neighbours(x, y))
			{
				int nx = cell.X,
					ny = cell.Y;

				if (ObjectGrid[nx, ny] != -1)
					continue;

				var pixel = ColorGrid[nx, ny];

				if (Math.Abs(r - pixel.R) < ObjectColorThreshold &&
					Math.Abs(g - pixel.G) < ObjectColorThreshold &&
					Math.Abs(b - pixel.B) < ObjectColorThreshold)
				{
					Partition(nx, ny, cc);
				}
			}
		}

		private void FindBlobsAndNeighbours()
		{
			var nblobs = ObjectGrid.Cast<int>().Distinct().Count();
			var blobs = new List<Blob>();
			for (int i = 0; i < nblobs; i++)
				blobs.Add(new Blob());

			for (int y = 0; y < FieldHeight; y++)
			{
				for (int x = 0; x < FieldWidth; x++)
				{
					int blobId = ObjectGrid[x, y];
					var blob = blobs[blobId];
					if (blob.Id == -1)
						blob.Id = blobId;
					blob.Points.Add(new XY(x, y));

					foreach (var neighbour in Neighbours(x, y))
					{
						int neighbourBlobId = ObjectGrid[neighbour.X, neighbour.Y];
						// Add neighbour if not yet found
						if (ObjectGrid[neighbour.X, neighbour.Y] != blobId &&
							!blob.Neighbours.Contains(neighbourBlobId))
						{
							blob.Neighbours.Add(neighbourBlobId);
						}
					}
				}
			}

			Blobs = blobs;
		}

		private void AssignBlobColors()
		{
			var classes = new List<Color>();
			foreach (var blob in Blobs)
			{
				var avgc = blob.GetAverageColor(ColorGrid);
				var pred = new Func<Color, bool>(x =>
				{
					var delta = Color.FromArgb(
							Math.Abs(x.R - avgc.R),
							Math.Abs(x.G - avgc.G),
							Math.Abs(x.B - avgc.B));
					return (delta.R < 30 && delta.G < 30 && delta.B < 30);
				});

				if (classes.Any(pred))
				{
					var colorclass = classes.First(pred);
					blob.MyColor = colorclass;
				}
				else
				{
					classes.Add(avgc);
					blob.MyColor = avgc;
				}
			}
		}

		private void RemoveEmptyBlobs()
		{
			var nilBlob = Blobs.FirstOrDefault(b =>
	 			(new byte[] { b.MyColor.R, b.MyColor.G, b.MyColor.B }).All(x => x > 210));

			Blobs.ForEach(b => b.Neighbours.RemoveAll(n => n == nilBlob.Id));
			Blobs.RemoveAt(nilBlob.Id);
		}

		private void DrawOutputImage()
		{
			var drawn = new List<int>();
			var graphics = Graphics.FromImage(Image);
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			for (int y = 0; y < FieldHeight; y++)
			{
				for (int x = 0; x < FieldWidth; x++)
				{
					if (!drawn.Contains(ObjectGrid[x, y]) &&
					    Blobs.Any(b => b.Id == ObjectGrid[x, y]))
					{
						graphics.FillEllipse(
							new SolidBrush(Color.FromArgb(100, 0, 0, 0)),
							new Rectangle(new Point(x * HorizontalDelta, y * VerticalDelta), new Size(60, 60)));
						graphics.DrawString(ObjectGrid[x, y].ToString(),
							 new Font("Tahoma", 20),
							 Brushes.White,
							 new PointF(x * HorizontalDelta + 10, y * VerticalDelta + 10));

						drawn.Add(ObjectGrid[x, y]);
					}
				}
			}

			Image.Save("output.bmp");
		}
	}
}
