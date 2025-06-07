using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct SplatData
{
    public Vector3 position;
    public Vector3 scale;
    public Vector4 rotation; // (x,y,z,w) quaternion
    public Vector3 color;
    public float opacity;
}
public struct SplatSHData
{
    public Vector3 col, sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8, sh9, sh10, sh11, sh12, sh13, sh14, sh15;
};

public class GaussianSplatAsset
{

    public List<SplatData> splats = new List<SplatData>();
    public List<SplatSHData> sh = new List<SplatSHData>();


}
