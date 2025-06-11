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

    [Header("Scale Variation")]
    [Tooltip("Minimum uniform scale for splats.")]
    public float minScale = 0.2f;
    [Tooltip("Maximum uniform scale for splats.")]
    public float maxScale = 1.5f;
    public enum VariationMode { AxisX, AxisZ, Radial }
    [Tooltip("How to vary scale across the grid.")]
    public VariationMode scaleVariationMode = VariationMode.Radial;

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

        // Optionally seed the random for reproducibility
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
                if (scaleVariationMode == VariationMode.Radial || opacityVariationMode == VariationMode.Radial)
                {
                    Vector3 worldCenter = originOffset + new Vector3(totalWidth * 0.5f, 0f, totalHeight * 0.5f);
                    float dist = Vector3.Distance(position, worldCenter);
                    radialNorm = (maxRadius > 0f) ? Mathf.Clamp01(dist / maxRadius) : 0f;
                }

                // Determine scale factor based on selected mode
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
                float uniformScale = Mathf.Lerp(minScale, maxScale, scaleT);

                // Determine opacity based on selected mode
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

                // Determine color
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
                    scale = new Vector3(uniformScale, uniformScale, uniformScale),
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
}
