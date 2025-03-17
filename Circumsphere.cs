using System.Numerics;

namespace DelaunayTriangulation3D;

public class Circumsphere {

	public Vector3 Centre { get; private set; }
	public float Radius { get; private set; }

	// For certain cases changing this to bigger value can help
	public const float EPSILON = 9E-07f;

	/// <summary>
	/// Create a sphere such that all four points lie on the surface of that sphere
	/// </summary>
	public Circumsphere(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {

		var P = new Matrix4x4(a.X, a.Y, a.Z, 1,
								b.X, b.Y, b.Z, 1,
								c.X, c.Y, c.Z, 1,
								d.X, d.Y, d.Z, 1);

		Matrix4x4 mat = P;

		float m11, m12, m13, m14, m15;

		// Find minor 1, 1.
		m11 = mat.GetDeterminant();

		// Find minor 1, 2.
		mat.M11 = a.LengthSquared();
		mat.M21 = b.LengthSquared();
		mat.M31 = c.LengthSquared();
		mat.M41 = d.LengthSquared();
		m12 = mat.GetDeterminant();

		// Find minor 1, 3.
		mat.M12 = P.M11;
		mat.M22 = P.M21;
		mat.M32 = P.M31;
		mat.M42 = P.M41;
		m13 = mat.GetDeterminant();

		// Find minor 1, 4.
		mat.M13 = P.M12;
		mat.M23 = P.M22;
		mat.M33 = P.M32;
		mat.M43 = P.M42;
		m14 = mat.GetDeterminant();

		// Find minor 1, 5.
		mat.M14 = P.M13;
		mat.M24 = P.M23;
		mat.M34 = P.M33;
		mat.M44 = P.M43;
		m15 = mat.GetDeterminant();

		if (m11 == 0) {
			Centre = new Vector3(0);
			Radius = float.PositiveInfinity;

		} else {
			Centre = new Vector3(0.5f * m12, -0.5f * m13, 0.5f * m14) / m11;
			Radius = MathF.Sqrt(Centre.LengthSquared() - m15 / m11);
		}
	}

	/// <summary>
	/// Checks if point is inside circumsphere (but not on border)
	/// </summary>
	public bool Contains(Vector3 point) {
		// When point is nearly exactly on edge of circumsphere
		// numerical instability can cause algorithm to flip the same tetrahedrons back and forth
		// That's why I check if is less than `EPSILON`
		return (point - Centre).Length() < Radius - EPSILON;
	}

	public override string ToString() {
		return "Centre: " + Centre.ToString() + " Radius: " + Radius;
	}
}

