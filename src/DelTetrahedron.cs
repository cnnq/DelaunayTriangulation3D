using System.Numerics;

namespace DelaunayTriangulation3D;

/// <summary>
/// Mostly for internal use
/// </summary>
internal class DelTetrahedron {

	public Vector3[] Points = new Vector3[4];
	public Vector3 A => Points[0];
	public Vector3 B => Points[1];
	public Vector3 C => Points[2];
	public Vector3 D => Points[3];


	public DelTetrahedron?[] Neighbors = new DelTetrahedron[4];
	public DelTetrahedron? BCD => Neighbors[0];
	public DelTetrahedron? CDA => Neighbors[1];
	public DelTetrahedron? DAB => Neighbors[2];
	public DelTetrahedron? ABC => Neighbors[3];


	public Circumsphere Circumsphere;


	private FacetIndex FacetToCheck = FacetIndex.None;

	public bool Disposed { get; private set; } = false;

	private static int idMax = 0;
	public readonly int Id;


	public Tetrahedron AsStruct => new Tetrahedron(A, B, C, D);


	public DelTetrahedron(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
		Points[0] = a; Points[1] = b; Points[2] = c; Points[3] = d;
		Circumsphere = new Circumsphere(a, b, c, d);
		Id = idMax;
		idMax++;
	}

	#region Splitting


	/// <param name="toRemove">Additional (neighbouring) tetraherons to remove</param>
	internal DelTetrahedron[] Split(Vector3 point, out DelTetrahedron[] toRemove) {

		toRemove = Array.Empty<DelTetrahedron>();

		return Contains(point) switch {
			ContainType.Inside => SplitInside(point),
			ContainType.OnPlane => SplitOnPlane(point, out toRemove),
			ContainType.OnLine => SplitOnLine(point, out toRemove),
			_ => Array.Empty<DelTetrahedron>()
		};
	}

	private DelTetrahedron[] SplitInside(Vector3 point) {

		Console.WriteLine("> SplitInside");

		var ret = new DelTetrahedron[4];

		ret[0] = new DelTetrahedron(point, B, C, D);
		ret[1] = new DelTetrahedron(point, C, D, A);
		ret[2] = new DelTetrahedron(point, D, A, B);
		ret[3] = new DelTetrahedron(point, A, B, C);

		ret[0].AssignNeighbors(ret[3], BCD, ret[1], ret[2]);
		ret[1].AssignNeighbors(ret[0], CDA, ret[2], ret[3]);
		ret[2].AssignNeighbors(ret[1], DAB, ret[3], ret[0]);
		ret[3].AssignNeighbors(ret[2], ABC, ret[0], ret[1]);

		BCD?.ReplaceNeighbor(this, ret[0]);
		CDA?.ReplaceNeighbor(this, ret[1]);
		DAB?.ReplaceNeighbor(this, ret[2]);
		ABC?.ReplaceNeighbor(this, ret[3]);

		foreach (DelTetrahedron item in ret) {
			item.FacetToCheck = FacetIndex.BCD;
		}

		Disposed = true;

		return ret;
	}

	private DelTetrahedron[] SplitOnPlane(Vector3 point, out DelTetrahedron[] toRemove) {

		Console.WriteLine(">> SplitOnPlane");

		FacetIndex facet = GetFacetContainingPoint(point);
		DelTetrahedron? neighbor = Neighbors[(int)facet];

		Vector3 p = Points[(int)facet];
		Vector3 a = Points[((int)facet + 1) % 4];
		Vector3 b = Points[((int)facet + 2) % 4];
		Vector3 c = Points[((int)facet + 3) % 4];

		// TODO What if null
		Vector3 q = neighbor.Points[(int)neighbor.GetCommonFacet(this)];

		// Create new tetrahedrons
		var ret = new DelTetrahedron[6];

		ret[0] = new DelTetrahedron(point, p, a, b);
		ret[1] = new DelTetrahedron(point, p, b, c);
		ret[2] = new DelTetrahedron(point, p, c, a);

		ret[3] = new DelTetrahedron(point, q, a, b);
		ret[4] = new DelTetrahedron(point, q, b, c);
		ret[5] = new DelTetrahedron(point, q, c, a);

		// Identify neighbors
		DelTetrahedron? abp = PickNeighbor(a, b, p);
		DelTetrahedron? bcp = PickNeighbor(b, c, p);
		DelTetrahedron? cap = PickNeighbor(c, a, p);
		
		DelTetrahedron? abq = neighbor.PickNeighbor(a, b, q);
		DelTetrahedron? bcq = neighbor.PickNeighbor(b, c, q);
		DelTetrahedron? caq = neighbor.PickNeighbor(c, a, q);

		// Assign neighbors
		ret[0].AssignNeighbors(ret[2], abp, ret[3], ret[1]);
		ret[1].AssignNeighbors(ret[0], bcp, ret[4], ret[2]);
		ret[2].AssignNeighbors(ret[1], cap, ret[5], ret[0]);

		ret[3].AssignNeighbors(ret[5], abq, ret[0], ret[4]);
		ret[4].AssignNeighbors(ret[3], bcq, ret[1], ret[5]);
		ret[5].AssignNeighbors(ret[4], caq, ret[2], ret[3]);

		abp?.ReplaceNeighbor(this, ret[0]);
		bcp?.ReplaceNeighbor(this, ret[1]);
		cap?.ReplaceNeighbor(this, ret[2]);

		abq?.ReplaceNeighbor(neighbor, ret[3]);
		bcq?.ReplaceNeighbor(neighbor, ret[4]);
		caq?.ReplaceNeighbor(neighbor, ret[5]);

		foreach (DelTetrahedron item in ret) {
			item.FacetToCheck = FacetIndex.BCD;
		}

		Disposed = true;
		toRemove = new DelTetrahedron[] { neighbor };

		return ret;
	}

	private DelTetrahedron[] SplitOnLine(Vector3 point, out DelTetrahedron[] toRemove) {

		Console.WriteLine(">>> SplitOnLine");

		var neighbors = new List<DelTetrahedron>() { this };

		DelTetrahedron currentTetrahedron = this;

		// TODO This code is ugly

		bool continueSearching = true;
		bool closedCircle = false;

		while (continueSearching) {
			continueSearching = false;

			for (int i = 0; i < currentTetrahedron.Neighbors.Length; i++) {

				DelTetrahedron? currentNeighbor = currentTetrahedron.Neighbors[i];

				if ((neighbors.Count < 2 || currentNeighbor != neighbors[^2])) {

					// End
					if (currentNeighbor == this) {
						closedCircle = true;
						break;
					}

					if (currentNeighbor != null && currentNeighbor.Contains(point) == ContainType.OnLine) {

						neighbors.Add(currentNeighbor);
						currentTetrahedron = currentNeighbor;
						continueSearching = true;
						break;
					}
				}

			}
		}

		if (!closedCircle) {

			neighbors.Reverse();

			currentTetrahedron = neighbors[^1];
			continueSearching = true;

			while (continueSearching) {
				continueSearching = false;

				for (int i = 0; i < currentTetrahedron.Neighbors.Length; i++) {

					DelTetrahedron? currentNeighbor = currentTetrahedron.Neighbors[i];

					// Ommit if because is not closed circle

					if (currentNeighbor != null &&
						(neighbors.Count < 2 || currentNeighbor != neighbors[^2]) &&
						currentNeighbor.Contains(point) == ContainType.OnLine) {

						neighbors.Add(currentNeighbor);
						currentTetrahedron = currentNeighbor;
						continueSearching = true;
						break;
					}
				}
			}

		}


		Vector3 p, q;
		(p, q) = GetLineContainingPoint(point);

		var ret = new List<DelTetrahedron>();


		for (int i = 0; i < neighbors.Count; i++) {

			DelTetrahedron current = neighbors[i];

			FacetIndex facetRight = current.GetCommonFacet(i != neighbors.Count - 1 ? neighbors[i + 1] : neighbors[0]);
			FacetIndex facetLeft = current.GetCommonFacet(i != 0 ? neighbors[i - 1] : neighbors[^1]);

			Vector3 a = current.Points[(int)facetRight]; // left point
			Vector3 b = current.Points[(int)facetLeft];  // right point

			var tetrahedonUp = new DelTetrahedron(point, a, b, p);
			var tetrahedonDown = new DelTetrahedron(point, a, b, q);

			// Identify neighbors
			DelTetrahedron abp = current.PickNeighbor(a, b, p);
			DelTetrahedron abq = current.PickNeighbor(a, b, q);

			// Assign some neighbors
			tetrahedonUp.AssignNeighbors(tetrahedonDown, abp, null, null);
			tetrahedonDown.AssignNeighbors(tetrahedonUp, abq, null, null);

			abp?.ReplaceNeighbor(current, tetrahedonUp);
			abq?.ReplaceNeighbor(current, tetrahedonDown);

			// ... and other
			if (ret.Count >= 2) {
				tetrahedonUp.Neighbors[(int)FacetIndex.DAB] = ret[^2];
				tetrahedonDown.Neighbors[(int)FacetIndex.DAB] = ret[^1];

				ret[^2].Neighbors[(int)FacetIndex.CDA] = tetrahedonUp;
				ret[^1].Neighbors[(int)FacetIndex.CDA] = tetrahedonDown;
			}

			tetrahedonUp.FacetToCheck = FacetIndex.All;
			tetrahedonDown.FacetToCheck = FacetIndex.All;

			ret.Add(tetrahedonUp);
			ret.Add(tetrahedonDown);

			current.Disposed = true;
		}

		// ... and the rest
		ret[0].Neighbors[(int)FacetIndex.DAB] = ret[^2];
		ret[1].Neighbors[(int)FacetIndex.DAB] = ret[^1];

		ret[^2].Neighbors[(int)FacetIndex.CDA] = ret[0];
		ret[^1].Neighbors[(int)FacetIndex.CDA] = ret[1];

		toRemove = neighbors.ToArray();

		return ret.ToArray();
	}

	#endregion

	#region Flipping

	internal List<DelTetrahedron> FlipComplex() {

		List<DelTetrahedron> ret;

		switch (FacetToCheck) {
			case FacetIndex.None:
				return new List<DelTetrahedron>();

			case FacetIndex.ABC:
			case FacetIndex.BCD:
			case FacetIndex.CDA:
			case FacetIndex.DAB:
				ret = Flip();
				if (ret.Count != 0) return ret;
				break;

			case FacetIndex.ABCorBCD:
				FacetToCheck = FacetIndex.ABC;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.BCD;
				ret = Flip();
				if (ret.Count != 0) return ret;
				break;

			case FacetIndex.ABCorCDA:
				FacetToCheck = FacetIndex.ABC;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.CDA;
				ret = Flip();
				if (ret.Count != 0) return ret;
				break;

			case FacetIndex.ABCorDAB:
				FacetToCheck = FacetIndex.ABC;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.DAB;
				ret = Flip();
				if (ret.Count != 0) return ret;
				break;

			case FacetIndex.NotABC:
				FacetToCheck = FacetIndex.BCD;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.CDA;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.DAB;
				ret = Flip();
				if (ret.Count != 0) return ret;
				break;

			case FacetIndex.All:
				FacetToCheck = FacetIndex.BCD;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.CDA;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.DAB;
				ret = Flip();
				if (ret.Count != 0) return ret;

				FacetToCheck = FacetIndex.ABC;
				ret = Flip();
				if (ret.Count != 0) return ret;
				break;

			default:
				throw new Exception("FlipComplex - unvalid FacetIndex");
		}

		return new List<DelTetrahedron>();
	}

	private List<DelTetrahedron> Flip() {

		// Neighbor 1
		DelTetrahedron? neighbor1 = Neighbors[(int)FacetToCheck];

		if (neighbor1 == null) {
			FacetToCheck = FacetIndex.None;
			return new List<DelTetrahedron>();
		}

		// Neighbor 2
		DelTetrahedron? neighbor2 = GetCommonNeighbor(this, neighbor1);

		Vector3 a, b, c, p = default, q = default;

		if (neighbor2 != null) {
			// 3 -> 2

			a = Points[(int)FacetToCheck]; // Equivalent to FindNeighbor(neighbor1)
			b = Points[(int)GetCommonFacet(neighbor2)];
			c = neighbor1.Points[(int)neighbor1.GetCommonFacet(this)];

			// Check if everything is ok
			if (!Circumsphere.Contains(c) && !neighbor1.Circumsphere.Contains(a) && !neighbor2.Circumsphere.Contains(b)) {
				FacetToCheck = FacetIndex.None;
				return new List<DelTetrahedron>();
			}

			// Get other points
			for (int i = 0; i < Points.Length; i++) {
				if (Points[i] != a && Points[i] != b) {
					p = Points[i];
					break;
				}
			}

			for (int i = Points.Length - 1; i >= 0; i--) {
				if (Points[i] != a && Points[i] != b) {
					q = Points[i];
					break;
				}
			}


			if (IsImpossibleToFlip()) {
				FacetToCheck = FacetIndex.None;
				return new List<DelTetrahedron>();
			}

			// Create new tetrahedrons
			var newTets = new DelTetrahedron[2];

			newTets[0] = new DelTetrahedron(a, b, c, q);
			newTets[1] = new DelTetrahedron(a, b, c, p);

			// Identify neighbors
			DelTetrahedron? abq = PickNeighbor(a, b, q);
			DelTetrahedron? bcq = neighbor1.PickNeighbor(b, c, q);
			DelTetrahedron? caq = neighbor2.PickNeighbor(c, a, q);

			DelTetrahedron? abp = PickNeighbor(a, b, p);
			DelTetrahedron? bcp = neighbor1.PickNeighbor(b, c, p);
			DelTetrahedron? cap = neighbor2.PickNeighbor(c, a, p);

			// Assign neighbors
			newTets[0].AssignNeighbors(newTets[1], bcq, caq, abq);
			newTets[1].AssignNeighbors(newTets[0], bcp, cap, abp);

			abq?.ReplaceNeighbor(this, newTets[0]);
			bcq?.ReplaceNeighbor(neighbor1, newTets[0]);
			caq?.ReplaceNeighbor(neighbor2, newTets[0]);

			abp?.ReplaceNeighbor(this, newTets[1]);
			bcp?.ReplaceNeighbor(neighbor1, newTets[1]);
			cap?.ReplaceNeighbor(neighbor2, newTets[1]);


			newTets[0].FacetToCheck = FacetIndex.NotABC;
			newTets[1].FacetToCheck = FacetIndex.NotABC;

			var ret = new List<DelTetrahedron>();
			ret.AddRange(newTets);

			Disposed = true;
			neighbor1.Disposed = true;
			neighbor2.Disposed = true;

			return ret;

		} else {
			// 2 -> 3

			q = neighbor1.Points[(int)neighbor1.GetCommonFacet(this)];

			// Check if everything is ok
			if (!Circumsphere.Contains(q)) {
				FacetToCheck = FacetIndex.None;
				return new List<DelTetrahedron>();
			}

			p = Points[(int)FacetToCheck];
			a = Points[((int)FacetToCheck + 1) % 4];
			b = Points[((int)FacetToCheck + 2) % 4];
			c = Points[((int)FacetToCheck + 3) % 4];

			if (IsImpossibleToFlip()) {
				FacetToCheck = FacetIndex.None;
				return new List<DelTetrahedron>();
			}

			// Create new tetrahedrons
			var newTets = new DelTetrahedron[3];

			newTets[0] = new DelTetrahedron(p, a, b, q);
			newTets[1] = new DelTetrahedron(p, b, c, q);
			newTets[2] = new DelTetrahedron(p, c, a, q);

			// Identify neighbors
			DelTetrahedron? abp = PickNeighbor(a, b, p); // Neighbors[((int)FacetToCheck + 3) % 4];
			DelTetrahedron? bcp = PickNeighbor(b, c, p); // Neighbors[((int)FacetToCheck + 1) % 4];
			DelTetrahedron? cap = PickNeighbor(c, a, p); // Neighbors[((int)FacetToCheck + 2) % 4];

			DelTetrahedron? abq = neighbor1.PickNeighbor(a, b, q);
			DelTetrahedron? bcq = neighbor1.PickNeighbor(b, c, q);
			DelTetrahedron? caq = neighbor1.PickNeighbor(c, a, q);

			// Assign neighbors
			newTets[0].AssignNeighbors(abp, abq, newTets[1], newTets[2]);
			newTets[1].AssignNeighbors(bcp, bcq, newTets[2], newTets[0]);
			newTets[2].AssignNeighbors(cap, caq, newTets[0], newTets[1]);

			abp?.ReplaceNeighbor(this, newTets[0]);
			bcp?.ReplaceNeighbor(this, newTets[1]);
			cap?.ReplaceNeighbor(this, newTets[2]);

			abq?.ReplaceNeighbor(neighbor1, newTets[0]);
			bcq?.ReplaceNeighbor(neighbor1, newTets[1]);
			caq?.ReplaceNeighbor(neighbor1, newTets[2]);


			newTets[0].FacetToCheck = FacetIndex.ABCorBCD;
			newTets[1].FacetToCheck = FacetIndex.ABCorBCD;
			newTets[2].FacetToCheck = FacetIndex.ABCorBCD;


			var ret = new List<DelTetrahedron>();
			ret.AddRange(newTets);

			Disposed = true;
			neighbor1.Disposed = true;

			return ret;
		}

		bool IsImpossibleToFlip() {
			int v1 = Math.Sign(SignedVolume6(q, a, b, c));
			int v2 = Math.Sign(SignedVolume6(p, a, b, c));

			if (v1 == v2) return true;

			int vA = Math.Sign(SignedVolume6(q, p, b, c));
			int vB = Math.Sign(SignedVolume6(q, p, c, a));
			int vC = Math.Sign(SignedVolume6(q, p, a, b));

			return !(vA == vB || vA == 0 || vB == 0) || !(vB == vC || vB == 0 || vC == 0) || !(vC == vA || vC == 0 || vA == 0);
		}
	}

	#endregion

	internal void AssignNeighbors(DelTetrahedron? abc, DelTetrahedron? bcd, DelTetrahedron? cda, DelTetrahedron? dab) {
		Neighbors[0] = bcd; Neighbors[1] = cda; Neighbors[2] = dab; Neighbors[3] = abc;
	}

	/// <summary>
	/// Replace 'a' with 'b'
	/// </summary>
	private void ReplaceNeighbor(DelTetrahedron a, DelTetrahedron b) {
		Neighbors[(int)GetCommonFacet(a)] = b;
	}

	/// <summary>
	/// Pick neighboring tetrahedron that contains these 3 points
	/// </summary>
	private DelTetrahedron? PickNeighbor(Vector3 a, Vector3 b, Vector3 c) {
		for (int i = 0; i < Points.Length; i++)
			if (Points[i] != a && Points[i] != b && Points[i] != c)
				return Neighbors[i];

		return null;
	}

	internal FacetIndex GetCommonFacet(DelTetrahedron? t) {
		if (t == Neighbors[0]) return FacetIndex.BCD;
		if (t == Neighbors[1]) return FacetIndex.CDA;
		if (t == Neighbors[2]) return FacetIndex.DAB;
		if (t == Neighbors[3]) return FacetIndex.ABC;

		return FacetIndex.None;
	}

	internal ContainType Contains(Vector3 p) {

		ContainType c1 = SameSide(A, B, C, D, p); // ABC
		ContainType c2 = SameSide(B, C, D, A, p); // BCD // <=
		ContainType c3 = SameSide(C, D, A, B, p); // CDA
		ContainType c4 = SameSide(D, A, B, C, p); // DAB

		if (c1 == ContainType.Outside || c2 == ContainType.Outside || c3 == ContainType.Outside || c4 == ContainType.Outside) return ContainType.Outside;

		int onPlaneCount = 0;
		if (c1 == ContainType.OnPlane) onPlaneCount++;
		if (c2 == ContainType.OnPlane) onPlaneCount++;
		if (c3 == ContainType.OnPlane) onPlaneCount++;
		if (c4 == ContainType.OnPlane) onPlaneCount++;

		return onPlaneCount switch {
			0 => ContainType.Inside,
			1 => ContainType.OnPlane,
			2 => ContainType.OnLine,
			3 => ContainType.OnPoint,
			_ => ContainType.Error
		};


		static ContainType SameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p) {
			var normal = Vector3.Cross(v2 - v1, v3 - v1);
			double dotV4 = Vector3.Dot(normal, v4 - v1);
			double dotP = Vector3.Dot(normal, p - v1);

			if (dotP == 0) return ContainType.OnPlane;
			else if (Math.Sign(dotV4) == Math.Sign(dotP)) return ContainType.Inside;
			else return ContainType.Outside;
		}
	}


	#region Helper functions

	private FacetIndex GetFacetContainingPoint(Vector3 p) {

		ContainType c1 = SameSide(A, B, C, D, p); // ABC
		ContainType c2 = SameSide(B, C, D, A, p); // BCD
		ContainType c3 = SameSide(C, D, A, B, p); // CDA
		ContainType c4 = SameSide(D, A, B, C, p); // DAB

		if (c1 == ContainType.OnPlane) return FacetIndex.ABC;
		else if (c2 == ContainType.OnPlane) return FacetIndex.BCD;
		else if (c3 == ContainType.OnPlane) return FacetIndex.CDA;
		else if (c4 == ContainType.OnPlane) return FacetIndex.DAB;
		else return FacetIndex.None;


		static ContainType SameSide(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 p) {
			var normal = Vector3.Cross(v2 - v1, v3 - v1);
			double dotV4 = Vector3.Dot(normal, v4 - v1);
			double dotP = Vector3.Dot(normal, p - v1);

			if (dotP == 0) return ContainType.OnPlane;
			else if (Math.Sign(dotV4) == Math.Sign(dotP)) return ContainType.Inside;
			else return ContainType.Outside;
		}
	}

	private (Vector3, Vector3) GetLineContainingPoint(Vector3 p) {

		for (int i = 0; i < Points.Length; i++) {
			for (int j = i + 1; j < Points.Length; j++) {

				if (Vector3.Cross(p - Points[i], Points[j] - Points[i]).LengthSquared() == 0) {
					return (Points[i], Points[j]);
				}
			}
		}

		throw new Exception();
	}

	/// <summary>
	/// Check if both tetrahedrons 'a' and 'b' are neighboring other tetrahedron 'c' and return it.
	/// </summary>
	private static DelTetrahedron? GetCommonNeighbor(DelTetrahedron a, DelTetrahedron b) {
		for (int i = 0; i < a.Neighbors.Length; i++) {
			if (b.GetCommonFacet(a.Neighbors[i]) != FacetIndex.None) {
				return a.Neighbors[i];
			}
		}

		return null;
	}

	private static double SignedVolume6(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
		return Vector3.Dot(Vector3.Cross(b - a, c - a), d - a);
	}

	#endregion

	public override string ToString() {
		return Id.ToString();
	}

	internal enum FacetIndex {
		BCD = 0, CDA = 1, DAB = 2, ABC = 3,
		
		None = 4,

		ABCorBCD, ABCorCDA, ABCorDAB,
		// UNUSED:
		// BCDorCDA, BCDorDAB,
		// CDAorDAB,

		NotABC,
		// UNUSED:
		// NotBCD, NotCDA, NotDAB

		All // TODO There might be better solution than checking all faces

	}
}
