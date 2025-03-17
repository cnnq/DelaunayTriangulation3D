using System.Numerics;

namespace DelaunayTriangulation3D {
	internal class Program {
		static void Main() {

			// Example of use

			var points = new List<Vector3>();

			var random = new Random();
			int seed = random.Next();
			random = new Random(seed);

			Console.WriteLine("Seed: " + seed);

			/*for (int i = 0; i < 256; i++) {
				points.Add(new Vector3((float)random.Next(16) / 16f, (float)random.Next(16) / 16f, (float)random.Next(16) / 16f));
			}*/

			for (int i = 0; i < 256; i++) {
				points.Add(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()));
			}

			var delaunay = new DelaunayTriangulation3D(points);

		}
	}
}