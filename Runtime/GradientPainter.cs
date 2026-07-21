using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyGradient.UIToolkit
{
    /// <summary>
    /// Builds the vertex-colored mesh of a GradientStyle for a given rectangle, and submits
    /// it to a VisualElement's MeshGenerationContext.
    ///
    /// Not a VisualElement — it's a static service called from
    /// GradientElement.generateVisualContent. It's the "procedural" counterpart of a
    /// CustomPainter: UI Toolkit does not expose a "CustomPainter" base class to inherit
    /// from, the standard entry point for custom drawing is MeshGenerationContext (see
    /// GradientElement).
    ///
    /// Linear mode: the mesh is a regular grid fully contained within <c>rect</c> (see
    /// PaintGrid). Each vertex gets a color computed from its projection onto the gradient's
    /// axis, normalized to t (0-1) via Mathf.InverseLerp, then evaluated by
    /// GradientStyle.Evaluate(t). The GPU then interpolates vertex colors inside each
    /// triangle — no geometry ever extends beyond rect, so no clipping is needed.
    ///
    /// Radial mode: still based on concentric rings (not yet refactored). Eventually it
    /// could reuse PaintGrid by only changing the t calculation (distance to center instead
    /// of projection onto an axis) — see PaintGrid's signature, designed with that in mind.
    /// </summary>
    public static class GradientPainter
    {
        /// <summary>
        /// Number of grid cells per axis. The mesh has (GridResolution + 1) ×
        /// (GridResolution + 1) vertices, i.e. 33 × 33 with the default value.
        /// </summary>
        private const int GridResolution = 32;
        const int verticesPerRow = GridResolution + 1;

        private const int RadialAngularSegments = 32;  // angular subdivisions (full turn)
        private const int RadialRadialSegments = 16;   // radial subdivisions (center → edge)

        /// <summary>Draws <paramref name="style"/> inside <paramref name="rect"/> (element's local space).</summary>
        public static void Paint(MeshGenerationContext mgc, Rect rect, GradientStyle style)
        {
            if (style == null || rect.width <= 0f || rect.height <= 0f) return;
            if (style.mode == GradientMode.Radial)
                PaintRadial(mgc, rect, style);
            else
                PaintLinear(mgc, rect, style);
        }

        // ─── Linear ──────────────────────────────────────────────────────

        /// <summary>
        /// Linear gradient: a regular grid of vertices covering exactly rect. Each vertex's
        /// color depends on its projection onto the axis defined by style.angle, normalized
        /// between the min/max projections of the rect's 4 corners.
        /// </summary>
        /*private static void PaintLinear(MeshGenerationContext mgc, Rect rect, GradientStyle style)
        {
            var mesh = mgc.Allocate(4, 6);

            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ),
                tint = Color.red
            });

            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ),
                tint = Color.green
            });

            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ),
                tint = Color.blue
            });

            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ),
                tint = Color.yellow
            });

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
        }*/
        private static void PaintLinear(MeshGenerationContext mgc, Rect rect, GradientStyle style)
        {
            float rad = style.angle * Mathf.Deg2Rad;
            Vector2 axis = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            GetCornerProjectionRange(rect, axis, out float minProjection, out float maxProjection);

            PaintGrid(mgc, rect, style, position =>
            {
                float projection = Vector2.Dot(position, axis);
                return Mathf.InverseLerp(minProjection, maxProjection, projection);
            });
        }

        /// <summary>
        /// Projects the 4 corners of rect onto the unit axis <paramref name="axis"/> and
        /// returns the extent (min/max) of those projections — used to normalize the
        /// projection of any vertex into t (0-1) via Mathf.InverseLerp.
        /// </summary>
        private static void GetCornerProjectionRange(Rect rect, Vector2 axis, out float min, out float max)
        {
            Vector2 c0 = new Vector2(rect.xMin, rect.yMin);
            Vector2 c1 = new Vector2(rect.xMax, rect.yMin);
            Vector2 c2 = new Vector2(rect.xMin, rect.yMax);
            Vector2 c3 = new Vector2(rect.xMax, rect.yMax);

            float p0 = Vector2.Dot(c0, axis), p1 = Vector2.Dot(c1, axis);
            float p2 = Vector2.Dot(c2, axis), p3 = Vector2.Dot(c3, axis);

            min = Mathf.Min(Mathf.Min(p0, p1), Mathf.Min(p2, p3));
            max = Mathf.Max(Mathf.Max(p0, p1), Mathf.Max(p2, p3));
        }

        // ─── Grid (reusable — see Radial in a future pass) ──

        /// <summary>
        /// Generates a regular grid of (GridResolution + 1) × (GridResolution + 1) vertices,
        /// all positioned inside rect via Mathf.Lerp, and two triangles per cell. Each
        /// vertex's color is determined by <paramref name="evaluateT"/>, which receives the
        /// vertex position (element's local space) and returns its t (0-1) for
        /// GradientStyle.Evaluate — this is the only point that would need to change for a
        /// future grid-based mode (Radial: t = distance to center instead of projection onto
        /// an axis).
        /// </summary>
        /// <summary>
        /// Generates a regular grid of (GridResolution + 1) × (GridResolution + 1)
        /// vertices directly into the MeshWriteData (no List allocation).
        /// All vertices stay inside the Rect.
        /// </summary>
        private static void PaintGrid(
            MeshGenerationContext mgc,
            Rect rect,
            GradientStyle style,
            Func<Vector2, float> evaluateT)
        {
            const int verticesPerRow = GridResolution + 1;

            int vertexCount = verticesPerRow * verticesPerRow;
            int indexCount = GridResolution * GridResolution * 6;
            
            Debug.Log(rect);
            
            var mesh = mgc.Allocate(vertexCount, indexCount);

            //
            // Vertices
            //
            for (int y = 0; y < verticesPerRow; y++)
            {

                float v = y / (float)GridResolution;
                float posY = Mathf.Lerp(rect.yMin, rect.yMax, v);

                for (int x = 0; x < verticesPerRow; x++)
                {
                    float u = x / (float)GridResolution;
                    float posX = Mathf.Lerp(rect.xMin, rect.xMax, u);

                    Vector2 position = new(posX, posY);

                    mesh.SetNextVertex(new Vertex
                    {
                        position = new Vector3(posX, posY, Vertex.nearZ),
                        tint = style.Evaluate(evaluateT(position))
                    });
                }
            }

            //
            // Indices
            //
            for (int y = 0; y < GridResolution; y++)
            {
                for (int x = 0; x < GridResolution; x++)
                {
                    ushort topLeft = (ushort)(y * verticesPerRow + x);
                    ushort topRight = (ushort)(topLeft + 1);
                    ushort bottomLeft = (ushort)(topLeft + verticesPerRow);
                    ushort bottomRight = (ushort)(bottomLeft + 1);

                    mesh.SetNextIndex(topLeft);
                    mesh.SetNextIndex(topRight);
                    mesh.SetNextIndex(bottomRight);

                    mesh.SetNextIndex(topLeft);
                    mesh.SetNextIndex(bottomRight);
                    mesh.SetNextIndex(bottomLeft);
                }
            }
        }
        // ─── Radial ──────────────────────────────────────────────────────

        private static void PaintRadial(MeshGenerationContext mgc, Rect rect, GradientStyle style)
        {
            Vector2 center = rect.center;
            // The gradient reaches its last stop at the farthest corner, so no leftover of
            // the intermediate color is ever left visible in the corners.
            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;

            int angularSegments = RadialAngularSegments;
            int radialSegments = RadialRadialSegments;

            var verts = new List<Vertex>(1 + angularSegments * radialSegments);
            var indices = new List<ushort>(angularSegments * radialSegments * 6);

            verts.Add(MakeVertex(center, style.Evaluate(0f))); // center vertex, index 0

            for (int r = 1; r <= radialSegments; r++)
            {
                float t = r / (float)radialSegments;
                float radius = maxRadius * t;
                Color32 col = style.Evaluate(t);

                for (int a = 0; a < angularSegments; a++)
                {
                    float theta = (a / (float)angularSegments) * Mathf.PI * 2f;
                    Vector2 p = center + new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
                    verts.Add(MakeVertex(p, col));
                }
            }

            // Fan from the center to the first ring.
            for (int a = 0; a < angularSegments; a++)
            {
                int next = (a + 1) % angularSegments;
                indices.Add(0);
                indices.Add((ushort)(1 + a));
                indices.Add((ushort)(1 + next));
            }

            // Bands between successive rings.
            for (int r = 0; r < radialSegments - 1; r++)
            {
                int ring = 1 + r * angularSegments;
                int nextRing = 1 + (r + 1) * angularSegments;
                for (int a = 0; a < angularSegments; a++)
                {
                    int next = (a + 1) % angularSegments;
                    ushort i0 = (ushort)(ring + a), i1 = (ushort)(ring + next);
                    ushort i2 = (ushort)(nextRing + a), i3 = (ushort)(nextRing + next);

                    indices.Add(i0); indices.Add(i1); indices.Add(i2);
                    indices.Add(i1); indices.Add(i3); indices.Add(i2);
                }
            }

            Submit(mgc, verts, indices);
        }

        // ─── Common ─────────────────────────────────────────────────────

        private static Vertex MakeVertex(Vector2 position, Color32 tint)
        {
            return new Vertex
            {
                position = new Vector3(position.x, position.y, Vertex.nearZ),
                tint = tint,
                uv = Vector2.zero
            };
        }

        private static void Submit(MeshGenerationContext mgc, List<Vertex> verts, List<ushort> indices)
        {
            if (verts.Count == 0 || indices.Count == 0) return;

            var mwd = mgc.Allocate(verts.Count, indices.Count);
            for (int i = 0; i < verts.Count; i++) mwd.SetNextVertex(verts[i]);
            for (int i = 0; i < indices.Count; i++) mwd.SetNextIndex(indices[i]);
        }
    }
}
