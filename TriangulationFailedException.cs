namespace DelaunayTriangulation3D {
	public class TriangulationFailedException : Exception {

        public Tetrahedron Tetrahedron1 { get; set; }

		public Tetrahedron Tetrahedron2 { get; set; }

		public float Error { get; set; }

		public TriangulationFailedException() { }

		public TriangulationFailedException(Tetrahedron tet1, Tetrahedron tet2, float error) {
			Tetrahedron1 = tet1;
			Tetrahedron2 = tet2;
			Error = error;
		}
	}
}
