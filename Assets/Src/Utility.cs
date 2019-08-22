using System;
using UnityEngine;

namespace Source {
    public static class Utility {
        /// <summary>
        /// Generates a grid mesh.
        /// </summary>
        /// <param name="width">The desired width of the grid (X-axis).</param>
        /// <param name="height">The desired height of the grid (Y or Z).</param>
        /// <param name="horizontal">If true, the grid generates in the XZ plane.</param>
        /// <returns>The generated grid mesh.</returns>
        public static Mesh GenerateGrid(int width, int height, bool horizontal) {
            // Mesh data arrays.
            var vertices = new Vector3[(width + 1) * (height + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[width * height * 6];
            var uvs = new Vector2[vertices.Length];

            // Generate vertex data.
            for (int i = 0, h = 0; h <= height; h++) {
                for (int w = 0; w <= width; w++, i++) {
                    // Vertices and normals;
                    if (horizontal) {
                        // Horizontal, use Z coordinate.
                        vertices[i] = new Vector3(w, 0f, h);
                        normals[i] = Vector3.up;
                    }
                    else {
                        // Vertical, use Y coordinate.
                        vertices[i] = new Vector3(w, h, 0f);
                        normals[i] = Vector3.back;
                    }

                    // UV coordinates.
                    uvs[i] = new Vector2((float) w / width, (float) h / height);
                }
            }

            // Generate triangle indices.
            for (var i = 0; i < triangles.Length; i += 6) {
                // Vertex index.
                var vi = (i / 6);

                // Shift the index forward by the current row index.
                vi += vi / (height);

                // First triangle.
                triangles[i] = vi;
                triangles[i + 1] = vi + width + 1;
                triangles[i + 2] = vi + 1;

                // Second triangle.
                triangles[i + 3] = triangles[i + 1];
                triangles[i + 4] = triangles[i + 1] + 1;
                triangles[i + 5] = triangles[i + 2];
            }

            // Create and return the mesh
            var mesh = new Mesh {
                vertices = vertices,
                normals = normals,
                triangles = triangles,
                uv = uvs,
            };

            return mesh;
        }

        /// <summary>
        /// Generates a 1D texture (res, 1) from a given gradient.
        /// For best performance, resolution should be a power of two.
        /// </summary>
        /// <param name="gradient">The gradient to sample.</param>
        /// <param name="resolution">The desired number of samples.</param>
        /// <returns>The generated gradient texture.</returns>
        public static Texture2D GenerateGradientTexture(Gradient gradient, int resolution) {
            // Validate resolution parameter.
            if (resolution < 1) {
                throw new ArgumentException("Resolution must be greater than zero!");
            }

            // Color array and texture.
            var colors = new Color[resolution];
            var texture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);

            // Sample the gradient based upon the desired resolution.
            for (var i = 0; i < resolution; i++) {
                colors[i] = gradient.Evaluate((float) i / resolution);
            }

            // Set texture data from colors and apply changes.
            texture.SetPixels(colors, 0);
            texture.Apply();

            // Return the texture.
            return texture;
        }
    }
}