using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class PlyWriter
{
    /// <summary>
    /// Writes the given GaussianSplatAsset to a binary little-endian PLY file.
    /// The file will contain 62 floats per vertex.
    /// </summary>
    public static void WriteGaussianSplatAsset(GaussianSplatAsset asset, string filePath)
    {
        if (asset == null || asset.splats.Count == 0)
        {
            Debug.LogError("Asset is null or empty.");
            return;
        }

        int vertexCount = asset.splats.Count;
        const int fileFloatCount = 62; // Expected number of floats per vertex

        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            // Write PLY header
            WriteHeader(fs, vertexCount);

            // Write binary data for each splat
            for (int i = 0; i < vertexCount; i++)
            {
                SplatData splat = asset.splats[i];

                float[] values = new float[fileFloatCount];

                // Position (x, y, z)
                values[0] = splat.position.x;
                values[1] = splat.position.y;
                values[2] = splat.position.z;

                // Normal (unused, set to 0)
                values[3] = 0f;
                values[4] = 1f;
                values[5] = 0f;

                // Color (f_dc values)
                values[6] = splat.color.x;
                values[7] = splat.color.y;
                values[8] = splat.color.z;

                // Spherical Harmonics coefficients (SH1 to SH15) - **Set all to 0**
                for (int j = 9; j < 54; j++)
                {
                    values[j] = 0f;
                }

                // Opacity
                values[54] = splat.opacity;

                // Scale (x, y, z)
                values[55] = splat.scale.x;
                values[56] = splat.scale.y;
                values[57] = splat.scale.z;

                // Rotation (Quaternion x, y, z, w)
                values[58] = splat.rotation.x;
                values[59] = splat.rotation.y;
                values[60] = splat.rotation.z;
                values[61] = splat.rotation.w;

                // Write 62 floats as binary little-endian
                foreach (float f in values)
                {
                    byte[] bytes = BitConverter.GetBytes(f);
                    if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        Debug.Log($"PLY file successfully written: {filePath} with {vertexCount} splats.");
    }

    /// <summary>
    /// Writes the PLY header for a binary little-endian file.
    /// </summary>
    static void WriteHeader(FileStream fs, int vertexCount)
    {
        StringBuilder header = new StringBuilder();
        header.AppendLine("ply");
        header.AppendLine("format binary_little_endian 1.0");
        header.AppendLine($"element vertex {vertexCount}");

        // Position
        header.AppendLine("property float x");
        header.AppendLine("property float y");
        header.AppendLine("property float z");

        // Normal (unused, placeholder)
        header.AppendLine("property float nx");
        header.AppendLine("property float ny");
        header.AppendLine("property float nz");

        // Color (f_dc)
        header.AppendLine("property float f_dc_0");
        header.AppendLine("property float f_dc_1");
        header.AppendLine("property float f_dc_2");

        // SH coefficients (f_rest values: 45 total) - **Defined but will be set to 0**
        for (int i = 0; i < 45; i++)
        {
            header.AppendLine($"property float f_rest_{i}");
        }

        // Opacity
        header.AppendLine("property float opacity");

        // Scale
        header.AppendLine("property float scale_0");
        header.AppendLine("property float scale_1");
        header.AppendLine("property float scale_2");

        // Rotation (Quaternion)
        header.AppendLine("property float rot_0");
        header.AppendLine("property float rot_1");
        header.AppendLine("property float rot_2");
        header.AppendLine("property float rot_3");

        header.AppendLine("end_header");

        // Write the header
        byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
        fs.Write(headerBytes, 0, headerBytes.Length);
    }
}
