namespace DelaunayTriangulation3D;

/// <summary>
/// Point can be outside, inside or on tetrahedron's plane, line, or vertex.
/// </summary>
internal enum ContainType {
	Outside = 0,
	Inside = 1,
	OnPlane = 0b11,
	OnLine = 0b111,
	OnPoint = 0b1111,
	Error = 0b11111
}