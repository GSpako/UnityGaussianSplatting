using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomPlyCreator : MonoBehaviour
{
    [Header("Output Settings")]
    [Tooltip("Folder (relative to project) where PLY files will be created.")]
    public string locationToCreate = "Assets/plyFiles";
    [Tooltip("Toggle to actually generate the PLY at Start.")]
    public bool create = true;
    [Tooltip("Filename for the generated PLY (will be overwritten if exists).")]
    public string outputFileName = "planeOfSplats.ply";

    [Header("Grid Settings")]
    [Tooltip("Number of splats along X-axis.")]
    public int gridWidth = 20;
    [Tooltip("Number of splats along Z-axis.")]
    public int gridHeight = 20;
    [Tooltip("Spacing between splats on the grid.")]
    public float spacing = 1f;

    public enum VariationMode { AxisX, AxisZ, Radial }

    [Header("Uniform Scale Settings (legacy)")]
    [Tooltip("Minimum uniform scale for splats.")]
    public float minScale = 0.2f;
    [Tooltip("Maximum uniform scale for splats.")]
    public float maxScale = 1.5f;
    [Tooltip("How to vary base uniform scale across the grid.")]
    public VariationMode scaleVariationMode = VariationMode.Radial;

    [Header("Anisotropic Scale Settings")]
    [Tooltip("Enable non-uniform (anisotropic) scaling per splat. If false, uses uniform-scale behavior.")]
    public bool allowNonUniformScale = false;
    [Tooltip("If true, each axis scale is fully randomized independently within its range.")]
    public bool randomizeAxesIndependently = false;
    [Tooltip("Range of scales for X axis when using anisotropic scaling.")]
    public Vector2 scaleRangeX = new Vector2(0.2f, 1.5f);
    [Tooltip("Range of scales for Y axis when using anisotropic scaling.")]
    public Vector2 scaleRangeY = new Vector2(0.2f, 1.5f);
    [Tooltip("Range of scales for Z axis when using anisotropic scaling.")]
    public Vector2 scaleRangeZ = new Vector2(0.2f, 1.5f);
    [Tooltip("How to vary base scale for X axis across the grid (if not fully random).")]
    public VariationMode scaleVariationModeX = VariationMode.AxisX;
    [Tooltip("How to vary base scale for Y axis across the grid (if not fully random).")]
    public VariationMode scaleVariationModeY = VariationMode.Radial;
    [Tooltip("How to vary base scale for Z axis across the grid (if not fully random).")]
    public VariationMode scaleVariationModeZ = VariationMode.AxisZ;

    [Header("Scale Jitter Settings")]
    [Tooltip("Enable random jitter on top of base scale (uniform or per-axis).")]
    public bool enableRandomJitter = true;
    [Tooltip("Maximum fractional jitter. e.g. 0.3 means scale is multiplied by [0.7 .. 1.3]. Applied per-axis if anisotropic.")]
    [Range(0f, 1f)]
    public float maxJitterFraction = 0.3f;

    [Header("Opacity Variation")]
    [Tooltip("Minimum opacity for splats.")]
    [Range(0f, 1f)]
    public float minOpacity = 0.1f;
    [Tooltip("Maximum opacity for splats.")]
    [Range(0f, 1f)]
    public float maxOpacity = 1f;
    [Tooltip("How to vary opacity across the grid.")]
    public VariationMode opacityVariationMode = VariationMode.AxisZ;

    [Header("Color Variation")]
    [Tooltip("If true, color is varied by position (HSV along axes). If false, random hue is used per splat.")]
    public bool colorByPosition = true;
    [Tooltip("Saturation range if randomizing saturation.")]
    [Range(0f, 1f)]
    public float minSaturation = 0.5f;
    [Range(0f, 1f)]
    public float maxSaturation = 1f;
    [Tooltip("Value (brightness) range if randomizing value.")]
    [Range(0f, 1f)]
    public float minValue = 0.5f;
    [Range(0f, 1f)]
    public float maxValue = 1f;

    [Header("Random Seed (optional)")]
    [Tooltip("If >= 0, seeds UnityEngine.Random for reproducible variations. If < 0, uses random seed.")]
    public int randomSeed = -1;

    void Start()
    {
        if (!create)
            return;

        // Seed random for reproducibility
        if (randomSeed >= 0)
        {
            Random.InitState(randomSeed);
        }

        // Ensure the output directory exists.
        if (!Directory.Exists(locationToCreate))
        {
            Directory.CreateDirectory(locationToCreate);
        }

        // Create a new GaussianSplatAsset and add splats in a plane.
        GaussianSplatAsset asset = new GaussianSplatAsset();
        asset.splats = new List<SplatData>();

        // Compute offsets to center the grid on origin
        float totalWidth = (gridWidth - 1) * spacing;
        float totalHeight = (gridHeight - 1) * spacing;
        Vector3 originOffset = new Vector3(-totalWidth * 0.5f, 0f, -totalHeight * 0.5f);

        // Precompute max radial distance if using Radial mode
        float maxRadius = Mathf.Sqrt(Mathf.Pow(totalWidth * 0.5f, 2) + Mathf.Pow(totalHeight * 0.5f, 2));

        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                // Compute world position for this splat
                Vector3 position = new Vector3(i * spacing, 0f, j * spacing) + originOffset;

                // Normalize positions to [0,1] across grid axes
                float normX = (gridWidth > 1) ? (float)i / (gridWidth - 1) : 0f;
                float normZ = (gridHeight > 1) ? (float)j / (gridHeight - 1) : 0f;

                // Compute radial normalized distance from center [0,1]
                float radialNorm = 0f;
                {
                    Vector3 worldCenter = originOffset + new Vector3(totalWidth * 0.5f, 0f, totalHeight * 0.5f);
                    float dist = Vector3.Distance(position, worldCenter);
                    radialNorm = (maxRadius > 0f) ? Mathf.Clamp01(dist / maxRadius) : 0f;
                }

                // --- Determine scale ---
                Vector3 finalScale = Vector3.one;

                if (allowNonUniformScale)
                {
                    if (randomizeAxesIndependently)
                    {
                        // Fully random each axis within its own range:
                        float sx = Random.Range(scaleRangeX.x, scaleRangeX.y);
                        float sy = Random.Range(scaleRangeY.x, scaleRangeY.y);
                        float sz = Random.Range(scaleRangeZ.x, scaleRangeZ.y);
                        finalScale = new Vector3(sx, sy, sz);
                    }
                    else
                    {
                        // Gradient-based per-axis:
                        float tX = ComputeT(scaleVariationModeX, normX, normZ, radialNorm);
                        float tY = ComputeT(scaleVariationModeY, normX, normZ, radialNorm);
                        float tZ = ComputeT(scaleVariationModeZ, normX, normZ, radialNorm);

                        float baseX = Mathf.Lerp(scaleRangeX.x, scaleRangeX.y, tX);
                        float baseY = Mathf.Lerp(scaleRangeY.x, scaleRangeY.y, tY);
                        float baseZ = Mathf.Lerp(scaleRangeZ.x, scaleRangeZ.y, tZ);

                        // Apply jitter per-axis if enabled
                        if (enableRandomJitter)
                        {
                            float jx = Random.Range(1f - maxJitterFraction, 1f + maxJitterFraction);
                            float jy = Random.Range(1f - maxJitterFraction, 1f + maxJitterFraction);
                            float jz = Random.Range(1f - maxJitterFraction, 1f + maxJitterFraction);
                            baseX *= jx;
                            baseY *= jy;
                            baseZ *= jz;
                            // Optionally clamp back into ranges so extremes don't exceed:
                            baseX = Mathf.Clamp(baseX, scaleRangeX.x, scaleRangeX.y);
                            baseY = Mathf.Clamp(baseY, scaleRangeY.x, scaleRangeY.y);
                            baseZ = Mathf.Clamp(baseZ, scaleRangeZ.x, scaleRangeZ.y);
                        }

                        finalScale = new Vector3(baseX, baseY, baseZ);
                    }
                }
                else
                {
                    // Uniform-scale path (legacy)
                    // Determine base scale factor T based on selected mode
                    float scaleT = 0f;
                    switch (scaleVariationMode)
                    {
                        case VariationMode.AxisX:
                            scaleT = normX;
                            break;
                        case VariationMode.AxisZ:
                            scaleT = normZ;
                            break;
                        case VariationMode.Radial:
                            scaleT = radialNorm;
                            break;
                    }
                    float baseUniform = Mathf.Lerp(minScale, maxScale, scaleT);

                    float finalUniform = baseUniform;
                    // Jitter if enabled
                    if (enableRandomJitter)
                    {
                        float jf = Random.Range(1f - maxJitterFraction, 1f + maxJitterFraction);
                        finalUniform *= jf;
                        finalUniform = Mathf.Clamp(finalUniform, minScale, maxScale);
                    }
                    finalScale = new Vector3(finalUniform, finalUniform, finalUniform);
                }

                // --- Determine opacity ---
                float opacityT = 0f;
                switch (opacityVariationMode)
                {
                    case VariationMode.AxisX:
                        opacityT = normX;
                        break;
                    case VariationMode.AxisZ:
                        opacityT = normZ;
                        break;
                    case VariationMode.Radial:
                        opacityT = radialNorm;
                        break;
                }
                float opacity = Mathf.Lerp(minOpacity, maxOpacity, opacityT);

                // --- Determine color ---
                Vector3 colorVec;
                if (colorByPosition)
                {
                    // Hue varies by X axis, saturation by Z axis, value fixed at 1
                    float hue = normX;
                    float saturation = normZ;
                    float value = 1f;
                    Color col = Color.HSVToRGB(hue, saturation, value);
                    colorVec = new Vector3(col.r, col.g, col.b);
                }
                else
                {
                    // Random hue, random saturation/value within ranges
                    float hue = Random.value;
                    float saturation = Random.Range(minSaturation, maxSaturation);
                    float value = Random.Range(minValue, maxValue);
                    Color col = Color.HSVToRGB(hue, saturation, value);
                    colorVec = new Vector3(col.r, col.g, col.b);
                }

                // Create splat data
                SplatData splat = new SplatData
                {
                    position = position,
                    color = colorVec,
                    opacity = opacity,
                    scale = finalScale,
                    rotation = new Vector4(0f, 0f, 0f, 1f)
                };
                asset.splats.Add(splat);
            }
        }

        // Define the output file path.
        string filePath = Path.Combine(locationToCreate, outputFileName);

        // Write the asset to a PLY file using the PlyWriter.
        PlyWriter.WriteGaussianSplatAsset(asset, filePath);
        Debug.Log("PLY file created at: " + filePath + " with " + asset.splats.Count + " splats.");
    }

    /// <summary>
    /// Helper to compute interpolation t in [0,1] given a VariationMode.
    /// normX, normZ, radialNorm are precomputed per cell.
    /// </summary>
    float ComputeT(VariationMode mode, float normX, float normZ, float radialNorm)
    {
        switch (mode)
        {
            case VariationMode.AxisX:
                return normX;
            case VariationMode.AxisZ:
                return normZ;
            case VariationMode.Radial:
                return radialNorm;
            default:
                return 0f;
        }
    }
}
