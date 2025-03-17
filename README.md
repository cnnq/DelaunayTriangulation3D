# DelaunayTriangulation3D

This repository contains simple implementation of Delaunay Triangulation algorithm
implemented for three-dimensional space.

## How to use it?

Generate mesh of tetrahedrons using `DelaunayTriangulation3D`. To access data use `DelaunayTriangulation3D.Tetrahedrons`

Example:

```
var random = new Random();
var points = new List<Vector3>();

for (int i = 0; i < 256; i++) {
    points.Add(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()));
}

var delaunay = new DelaunayTriangulation3D(points);

foreach (var tetrahedron in delaunay.Tetrahedrons) {
    Console.WriteLine(tetrahedron);
}
```

## Troubleshooting

Sometimes creating `DelaunayTriangulation3D` throws in `TriangulationFailedException` for certain data.
This can happen because Delaunay Triangulation algorithm is inherently numerically unstable (at least my implementation).
If this occurs to you, you can fix it by changing `Circumsphere.EPSILON` to bigger value.

If it didn't help that possibly mean there is a bug.
