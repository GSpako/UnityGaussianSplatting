using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomPlyCreator : MonoBehaviour
{
    public string locationToCreate = "Assets/plyFiles";
    public bool create;

    // Start is called before the first frame update
    void Start()
    {
        if (!create)
            return;

        // Ensure the output directory exists.
        if (!Directory.Exists(locationToCreate))
        {
            Directory.CreateDirectory(locationToCreate);
        }

        // Create a new GaussianSplatAsset and add five splats.
        GaussianSplatAsset asset = new GaussianSplatAsset();
        asset.splats = new List<SplatData>();

        // Define a common y and z position for all splats.
        float posY = 0f;
        float posZ = 0f;

        // Create 5 splats along the x-axis, 10 units apart.
        /*
        for (int i = 0; i < 0; i++)
        {
            SplatData splat = new SplatData
            {
                // Each splat's x position is 10 * i; y and z remain constant.
                position = new Vector3(i * 10f, posY, posZ),

                // For demonstration, assign each splat a distinct color.
                // You could adjust these values as needed.
                color = new Vector3(Mathf.Clamp01(1f - i * 0.2f), Mathf.Clamp01(1f -i * 0.1f) , 1f),

                // Set opacity, scale, and rotation to constant values.
                opacity = 1f,
                scale = new Vector3(1f - 0.3f * i, 1f - 0.15f * i, 1f - 0.05f * i),
                rotation = new Vector4(0f, 0f, 0f, 1f)
            };
            asset.splats.Add(splat);
        }

        */

        SplatData splat = new SplatData
        {
            position = new Vector3(0,0,0),
            color = new Vector3(1, 0, 0),
            opacity = 1f,
            scale = new Vector3(.2f, .2f, .2f),
            rotation = new Vector4(0f, 0f, 0f, 1f)
        };
        asset.splats.Add(splat);

        splat = new SplatData
        {
            position = new Vector3(0, 5, 0),
            color = new Vector3(0, 1, 0),
            opacity = 1f,
            scale = new Vector3(1f, 1f, 1f),
            rotation = new Vector4(0f, 0f, 0f, 1f)
        };
        asset.splats.Add(splat);

        splat = new SplatData
        {
            position = new Vector3(0, 0, 5),
            color = new Vector3(1, 0, 0),
            opacity = 1f,
            scale = new Vector3(1f, 1f, 1f),
            rotation = new Vector4(0f, 0f, 0f, 1f)
        };
        asset.splats.Add(splat);

        // Define the output file path.
        string filePath = Path.Combine(locationToCreate, "testSplats.ply");
         
        // Write the asset to a PLY file using the PlyWriter.
        PlyWriter.WriteGaussianSplatAsset(asset, filePath);
        Debug.Log("PLY file created at: " + filePath);
    }
}
