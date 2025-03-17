using System.Numerics;

namespace DelaunayTriangulation3D;

public struct Tetrahedron {

	public Vector3[] Points = new Vector3[4];

	public readonly Vector3 A => Points[0];
	public readonly Vector3 B => Points[1];
	public readonly Vector3 C => Points[2];
	public readonly Vector3 D => Points[3];

	public readonly float Volume => MathF.Abs(1f / 6 * Vector3.Dot(Vector3.Cross(B - A, C - A), D - A));


	public Tetrahedron(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
		Points[0] = a; Points[1] = b; Points[2] = c; Points[3] = d;
	}

	/// <summary>
	/// Checks if point is inside tetrahedron
	/// </summary>
	public bool Contains(Vector3 point) {

		return SameSide(A, B, C, D, point) &&
				SameSide(B, C, D, A, point) &&
				SameSide(C, D, A, B, point) &&
				SameSide(D, A, B, C, point);

		static bool SameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p) {
			var normal = Vector3.Cross(v2 - v1, v3 - v1);
			float dotV4 = Vector3.Dot(normal, v4 - v1);
			float dotP = Vector3.Dot(normal, p - v1);

			return dotP == 0 || MathF.Sign(dotV4) == MathF.Sign(dotP);
		}
	}

	public override string ToString() {
		return $"[{A}, {B}, {C}, {D}]";
	}
}