using System.Numerics;

namespace DelaunayTriangulation3D;

public class DelaunayTriangulation3D {

	public Tetrahedron[] Tetrahedrons;

	/// <summary>
	/// Triangulates points into an array of tetrahedrons and saves it in <see cref="Tetrahedrons"/>
	/// </summary>
	/// <param name="points">Collection of points that have to be triangulated</param>
	/// <exception cref="TriangulationFailedException"></exception>
	public DelaunayTriangulation3D(IEnumerable<Vector3> points) {

		var min = new Vector3(float.MaxValue);
		var max = new Vector3(float.MinValue);

		foreach (Vector3 point in points) {
			if (point.X < min.X) min.X = point.X;
			if (point.Y < min.Y) min.Y = point.Y;
			if (point.Z < min.Z) min.Z = point.Z;

			if (point.X > max.X) max.X = point.X;
			if (point.Y > max.Y) max.Y = point.Y;
			if (point.Z > max.Z) max.Z = point.Z;
		}

		// Build initial tetrahedrons
		var initialPoints = new Vector3[] {
				 new(min.X - 1, min.Y - 1, min.Z - 1),
				 new(max.X + 1, min.Y - 1, min.Z - 1),
				 new(max.X + 1, min.Y - 1, max.Z + 1),
				 new(min.X - 1, min.Y - 1, max.Z + 1),
				 new(min.X - 1, max.Y + 1, max.Z + 1),
				 new(min.X - 1, max.Y + 1, min.Z - 1),
				 new(max.X + 1, max.Y + 1, min.Z - 1),
				 new(max.X + 1, max.Y + 1, max.Z + 1)
			};

		var initialTetrahedrons = new DelTetrahedron[] {
				new (initialPoints[0], initialPoints[1], initialPoints[2], initialPoints[7]),
				new (initialPoints[0], initialPoints[2], initialPoints[3], initialPoints[7]),
				new (initialPoints[0], initialPoints[3], initialPoints[4], initialPoints[7]),
				new (initialPoints[0], initialPoints[4], initialPoints[5], initialPoints[7]),
				new (initialPoints[0], initialPoints[5], initialPoints[6], initialPoints[7]),
				new (initialPoints[0], initialPoints[6], initialPoints[1], initialPoints[7]),
			};

		initialTetrahedrons[0].AssignNeighbors(null, null, initialTetrahedrons[1], initialTetrahedrons[5]);
		initialTetrahedrons[1].AssignNeighbors(null, null, initialTetrahedrons[2], initialTetrahedrons[0]);
		initialTetrahedrons[2].AssignNeighbors(null, null, initialTetrahedrons[3], initialTetrahedrons[1]);
		initialTetrahedrons[3].AssignNeighbors(null, null, initialTetrahedrons[4], initialTetrahedrons[2]);
		initialTetrahedrons[4].AssignNeighbors(null, null, initialTetrahedrons[5], initialTetrahedrons[3]);
		initialTetrahedrons[5].AssignNeighbors(null, null, initialTetrahedrons[0], initialTetrahedrons[4]);

		// Initialize data structures
		var tetrahedrons = new List<DelTetrahedron>(initialTetrahedrons);
		var tetrahedronsToFlip = new List<DelTetrahedron>();

		// Add points one by one
		foreach (Vector3 point in points) {

			// When input has 2 or more the same points adding 2nd one can be skipped
			bool skipPoint = false;

			// Adding step
			for (int i = 0; i < tetrahedrons.Count; i++) {

				ContainType ct = tetrahedrons[i].Contains(point);

				DelTetrahedron[] newTetrahedrons;
				DelTetrahedron[] toRemove;

				if (ct == ContainType.Outside || ct == ContainType.Error) {
					continue;

				} else if (ct == ContainType.OnPoint) {
					skipPoint = true;
					break;

				} else {
					newTetrahedrons = tetrahedrons[i].Split(point, out toRemove);

					if (newTetrahedrons.Length == 0) Console.WriteLine("POSSIBLE ERROR");

					tetrahedrons.RemoveAt(i);
					foreach (DelTetrahedron item in toRemove) {
						tetrahedrons.Remove(item);
					}

					tetrahedrons.AddRange(newTetrahedrons);
					tetrahedronsToFlip.AddRange(newTetrahedrons);

					break;
				}
			}

			if (skipPoint) continue;

			// Flipping step
			for (int i = 0; i < tetrahedronsToFlip.Count; i++) {

				// Sometimes flipping tetrahedrons make other tetrahedrons inside `tetrahedronsToFlip` obsolete
				if (tetrahedronsToFlip[i].Disposed) {
					tetrahedronsToFlip.RemoveAt(i);
					i--;
					continue;
				}

				List<DelTetrahedron> newTetrahedrons = tetrahedronsToFlip[i].FlipComplex();

				tetrahedrons.AddRange(newTetrahedrons);
				tetrahedronsToFlip.AddRange(newTetrahedrons);
			}

			// Cleaning after flipping
			tetrahedronsToFlip.Clear();

			for (int i = tetrahedrons.Count - 1; i >= 0; i--) {
				if (tetrahedrons[i].Disposed) tetrahedrons.RemoveAt(i);
			}

			Check(tetrahedrons);
		}


		// Remove initial tetrahedrons
		for (int i = tetrahedrons.Count - 1; i >= 0; i--) {
			DelTetrahedron tetrahedron = tetrahedrons[i];

			bool removed = false;
			foreach (Vector3 tPoint in tetrahedron.Points) {
				foreach (Vector3 iPoint in initialPoints) {
					if (tPoint == iPoint) {

						tetrahedrons.RemoveAt(i);
						removed = true;
						break;
					}
				}

				if (removed) break;
			}
		}

		// Check if triangulation failed
		try {
			Check(tetrahedrons);
		} catch (TriangulationFailedException e) {
			throw e;
		}

		// Save results
		Tetrahedrons = new Tetrahedron[tetrahedrons.Count];

		for (int i = 0; i < tetrahedrons.Count; i++) {
			Tetrahedrons[i] = tetrahedrons[i].AsStruct;
		}
	}

	/// <summary>
	/// Throws an exception if triangulation failed
	/// </summary>
	/// <exception cref="TriangulationFailedException"></exception>
	private static void Check(ICollection<DelTetrahedron> delTetrahedrons) {
		foreach (DelTetrahedron tetrahedron in delTetrahedrons) {
			foreach (DelTetrahedron? neighbor in tetrahedron.Neighbors) {

				if (neighbor == null || tetrahedron.Id > neighbor.Id || tetrahedron.Circumsphere.Radius == 0) continue;

				Vector3 point = neighbor.Points[(int)neighbor.GetCommonFacet(tetrahedron)];

				if (tetrahedron.Circumsphere.Contains(point)) {

					throw new TriangulationFailedException(tetrahedron.AsStruct, neighbor.AsStruct, (point - tetrahedron.Circumsphere.Centre).Length() - tetrahedron.Circumsphere.Radius);
				}
			}
		}
	}
}
