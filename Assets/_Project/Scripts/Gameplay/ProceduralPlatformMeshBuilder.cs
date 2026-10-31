using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAscent.Gameplay
{
    /// <summary>
    /// Generates platform meshes for side-view 2D gameplay (camera sees XY plane).
    /// Deformations are applied to the top silhouette so chips read clearly in 2D.
    /// </summary>
    public static class ProceduralPlatformMeshBuilder
    {
        // Тонкая глубина по Z — платформа как «пластина» для 2D камеры.
        const float ProfileDepth = 0.32f;

        /// <summary>
        /// Генерирует 2D-меш платформы с каменным профилем и сколами на верхней кромке.
        /// </summary>
        /// <param name="width">Ширина платформы в мировых единицах.</param>
        /// <param name="height">Высота платформы в мировых единицах.</param>
        /// <param name="roughness">Количество сколов на верхней поверхности (0 — гладкая).</param>
        /// <param name="seed">Зерно генератора для детерминированных сколов.</param>
        /// <returns>Готовый Mesh с вершинами, цветами и треугольниками.</returns>
        public static Mesh Build(float width, float height, int roughness, int seed = 0)
        {
            width = Mathf.Max(0.5f, width);
            height = Mathf.Max(0.05f, height);
            roughness = Mathf.Max(0, roughness);

            // Высокое разрешение по X — сколы видны в профиле сбоку.
            int resX = Mathf.Clamp(roughness * 6, 24, 180);

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float halfDepth = ProfileDepth * 0.5f;
            float endCapRadius = Mathf.Min(halfWidth * 0.12f, halfHeight * 0.85f);

            var rng = new System.Random(seed);
            var topY = new float[resX + 1];

            for (int i = 0; i <= resX; i++)
            {
                float t = i / (float)resX;
                float x = Mathf.Lerp(-halfWidth, halfWidth, t);
                topY[i] = BuildRoundedTopY(x, halfWidth, halfHeight, endCapRadius);
            }

            ApplyStoneChips2D(topY, resX, halfWidth, halfHeight, height, roughness, rng);
            SmoothProfile1D(topY, resX, iterations: 2);

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            int frontBottomStart = vertices.Count;
            AddProfileRow(vertices, colors, topY, resX, halfWidth, halfHeight, halfDepth, front: true, topRow: false);
            int frontTopStart = vertices.Count;
            AddProfileRow(vertices, colors, topY, resX, halfWidth, halfHeight, halfDepth, front: true, topRow: true);

            int backBottomStart = vertices.Count;
            AddProfileRow(vertices, colors, topY, resX, halfWidth, halfHeight, halfDepth, front: false, topRow: false);
            int backTopStart = vertices.Count;
            AddProfileRow(vertices, colors, topY, resX, halfWidth, halfHeight, halfDepth, front: false, topRow: true);

            BuildQuadStrip(triangles, frontBottomStart, frontTopStart, resX, doubleSided: false);
            BuildQuadStrip(triangles, backTopStart, backBottomStart, resX, doubleSided: false);

            BuildDepthStrip(triangles, frontTopStart, backTopStart, resX, topSurface: true);
            BuildDepthStrip(triangles, frontBottomStart, backBottomStart, resX, topSurface: false);

            CloseEndCap(triangles, frontBottomStart, frontTopStart, backBottomStart, backTopStart, resX, leftSide: true);
            CloseEndCap(triangles, frontBottomStart, frontTopStart, backBottomStart, backTopStart, resX, leftSide: false);

            var mesh = new Mesh { name = "ProceduralPlatform2D" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static float BuildRoundedTopY(float x, float halfWidth, float halfHeight, float endCapRadius)
        {
            float ax = Mathf.Abs(x);
            float inner = halfWidth - endCapRadius;
            if (ax <= inner)
                return halfHeight;

            float dx = ax - inner;
            if (dx >= endCapRadius)
                return -halfHeight;

            float t = dx / endCapRadius;
            return Mathf.Lerp(halfHeight, -halfHeight, t * t);
        }

        static void ApplyStoneChips2D(
            float[] topY,
            int resX,
            float halfWidth,
            float halfHeight,
            float height,
            int roughness,
            System.Random rng)
        {
            if (roughness <= 0)
                return;

            float minRadius = (halfWidth * 2f / resX) * 1.5f;
            float maxRadius = (halfWidth * 2f / resX) * 5f;

            for (int chip = 0; chip < roughness; chip++)
            {
                float centerX = Lerp(rng, -halfWidth * 0.88f, halfWidth * 0.88f);
                float chipRadius = Lerp(rng, minRadius, maxRadius);
                // Крупные сколы по Y — хорошо читаются в 2D профиле.
                float chipDepth = Lerp(rng, height * 0.18f, height * 0.55f);

                for (int i = 0; i <= resX; i++)
                {
                    float x = Mathf.Lerp(-halfWidth, halfWidth, i / (float)resX);
                    float dx = x - centerX;
                    if (Mathf.Abs(dx) >= chipRadius)
                        continue;

                    float falloff = 1f - Mathf.Abs(dx) / chipRadius;
                    falloff = falloff * falloff;
                    topY[i] -= chipDepth * falloff;

                    if (falloff > 0.65f && rng.NextDouble() > 0.45f)
                        topY[i] -= chipDepth * 0.12f * falloff;
                }
            }

            for (int i = 0; i <= resX; i++)
                topY[i] = Mathf.Clamp(topY[i], -halfHeight, halfHeight);
        }

        static void SmoothProfile1D(float[] topY, int resX, int iterations)
        {
            var buffer = new float[topY.Length];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i <= resX; i++)
                {
                    float sum = topY[i];
                    int count = 1;

                    if (i > 0) { sum += topY[i - 1]; count++; }
                    if (i < resX) { sum += topY[i + 1]; count++; }

                    buffer[i] = sum / count;
                }

                for (int i = 0; i <= resX; i++)
                    topY[i] = buffer[i];
            }
        }

        static void AddProfileRow(
            List<Vector3> vertices,
            List<Color> colors,
            float[] topY,
            int resX,
            float halfWidth,
            float halfHeight,
            float halfDepth,
            bool front,
            bool topRow)
        {
            float z = front ? halfDepth : -halfDepth;

            for (int i = 0; i <= resX; i++)
            {
                float x = Mathf.Lerp(-halfWidth, halfWidth, i / (float)resX);
                float y = topRow ? topY[i] : -halfHeight;
                vertices.Add(new Vector3(x, y, z));
                colors.Add(BuildVertexColor(topY[i], halfHeight, topRow));
            }
        }

        static Color BuildVertexColor(float topYValue, float halfHeight, bool topRow)
        {
            if (!topRow)
                return new Color(0.62f, 0.56f, 0.48f, 1f);

            float dent = Mathf.Clamp01((halfHeight - topYValue) / (halfHeight * 1.35f));
            float shade = Mathf.Lerp(1f, 0.38f, dent);
            return new Color(shade, shade * 0.94f, shade * 0.86f, 1f);
        }

        static void BuildQuadStrip(List<int> triangles, int bottomStart, int topStart, int resX, bool doubleSided)
        {
            for (int i = 0; i < resX; i++)
            {
                int b0 = bottomStart + i;
                int b1 = bottomStart + i + 1;
                int t0 = topStart + i;
                int t1 = topStart + i + 1;

                triangles.Add(b0);
                triangles.Add(t0);
                triangles.Add(b1);

                triangles.Add(b1);
                triangles.Add(t0);
                triangles.Add(t1);

                if (!doubleSided)
                    continue;

                triangles.Add(b0);
                triangles.Add(b1);
                triangles.Add(t0);

                triangles.Add(b1);
                triangles.Add(t1);
                triangles.Add(t0);
            }
        }

        static void BuildDepthStrip(List<int> triangles, int frontStart, int backStart, int resX, bool topSurface)
        {
            for (int i = 0; i < resX; i++)
            {
                int f0 = frontStart + i;
                int f1 = frontStart + i + 1;
                int b0 = backStart + i;
                int b1 = backStart + i + 1;

                if (topSurface)
                {
                    triangles.Add(f0);
                    triangles.Add(b0);
                    triangles.Add(f1);

                    triangles.Add(f1);
                    triangles.Add(b0);
                    triangles.Add(b1);
                }
                else
                {
                    triangles.Add(f0);
                    triangles.Add(f1);
                    triangles.Add(b0);

                    triangles.Add(f1);
                    triangles.Add(b1);
                    triangles.Add(b0);
                }
            }
        }

        static void CloseEndCap(
            List<int> triangles,
            int frontBottomStart,
            int frontTopStart,
            int backBottomStart,
            int backTopStart,
            int resX,
            bool leftSide)
        {
            int index = leftSide ? 0 : resX;
            int fb = frontBottomStart + index;
            int ft = frontTopStart + index;
            int bb = backBottomStart + index;
            int bt = backTopStart + index;

            if (leftSide)
            {
                triangles.Add(fb);
                triangles.Add(bb);
                triangles.Add(ft);

                triangles.Add(ft);
                triangles.Add(bb);
                triangles.Add(bt);
            }
            else
            {
                triangles.Add(fb);
                triangles.Add(ft);
                triangles.Add(bb);

                triangles.Add(ft);
                triangles.Add(bt);
                triangles.Add(bb);
            }
        }

        static float Lerp(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
