using UnityEngine;
using System.IO;

public class CaptureCameraView : MonoBehaviour
{
    // The camera to capture
    public Camera targetCamera;

    // The game object whose transform you want to set
    public GameObject targetObject;

    // Desired camera transform settings
    public Vector3 cameraPosition = new Vector3(0f, 5f, -10f);
    public Vector3 cameraRotationEuler = new Vector3(20f, 0f, 0f);

    // Desired target object transform settings
    public Vector3 objectPosition = new Vector3(0f, 0f, 0f);
    public Vector3 objectRotationEuler = new Vector3(0f, 45f, 0f);

    // Capture resolution settings
    public int captureWidth = 1920;
    public int captureHeight = 1080;

    public void Start()
    {
        CaptureImage();
    }

    // Update the transforms of the camera and the target object
    public void UpdateTransforms()
    {
        if (targetCamera != null)
        {
            targetCamera.transform.position = cameraPosition;
            targetCamera.transform.eulerAngles = cameraRotationEuler;
        }

        if (targetObject != null)
        {
            targetObject.transform.position = objectPosition;
            targetObject.transform.eulerAngles = objectRotationEuler;
        }
    }

    // Capture the camera's view and save it as a PNG file
    public void CaptureImage()
    {
        // First update the transforms as specified
        UpdateTransforms();

        // Create a RenderTexture with the desired resolution
        RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);
        targetCamera.targetTexture = rt;

        // Create a Texture2D to hold the image data
        Texture2D screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

        // Render the camera's view into the RenderTexture
        targetCamera.Render();

        // Read the pixels from the RenderTexture into the Texture2D
        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        screenshot.Apply();

        // Clean up by resetting the active RenderTexture and releasing the temporary one
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // Encode the texture into PNG format
        byte[] bytes = screenshot.EncodeToPNG();

        // Save the PNG file to the persistent data path (works on all platforms)
        string filePath = Path.Combine(Application.persistentDataPath, "CameraCapture.png");
        File.WriteAllBytes(filePath, bytes);

        Debug.Log("Screenshot saved to: " + filePath);
    }
}
