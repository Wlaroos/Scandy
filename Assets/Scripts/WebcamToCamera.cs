using UnityEngine;
using UnityEngine.UI;

// IMPORTANT: No changes needed in this script if using the RawImage approach.
// This script will drive the RawImage component which you will size and position
// to take up a sub-section of the screen.
public class WebcamToCamera : MonoBehaviour
{
    [Header("Webcam settings")]
    [SerializeField] private string deviceName = "";       // leave blank to auto-select first device
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;
    [SerializeField] private bool playOnEnable = true;

    [Header("UI")]
    [SerializeField] private bool useRawImage = true;      // set to true to display on a UI RawImage instead of replacing camera output
    [SerializeField] private RawImage rawImage;            // drag your RawImage here

    private WebCamTexture webcamTexture;

    void Awake()
    {

    }

    void OnEnable()
    {
        if (playOnEnable) StartWebcam();
    }

    void OnDisable()
    {
        StopWebcam();
    }

    public void StartWebcam()
    {
        if (webcamTexture != null && webcamTexture.isPlaying) return;

        var devices = WebCamTexture.devices;

        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning("WebcamToCamera: No webcam devices found.");
            return;
        }

        if (string.IsNullOrEmpty(deviceName))
            deviceName = devices[0].name;

        // Use the default if not specified
        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = devices[0].name;
        }


        webcamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
        webcamTexture.Play();

        // If using UI, assign texture and fix rotation/mirror
        if (useRawImage && rawImage != null)
        {
            rawImage.texture = webcamTexture;

            // Apply rotation reported by the webcam
            rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -webcamTexture.videoRotationAngle);

            // Mirror if needed (common for front cameras)
            float scaleX = webcamTexture.videoVerticallyMirrored ? -1f : 1f;
            rawImage.rectTransform.localScale = new Vector3(scaleX, 1f, 1f);
        }
    }

    public void StopWebcam()
    {
        if (webcamTexture == null) return;

        webcamTexture.Stop();

        if (useRawImage && rawImage != null && rawImage.texture == webcamTexture)
        {
            rawImage.texture = null;
        }

        Destroy(webcamTexture);
        webcamTexture = null;
    }

    // OnRenderImage is now harmless when useRawImage is true, so we keep it.
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!useRawImage)
        {
            if (webcamTexture != null && webcamTexture.isPlaying && webcamTexture.width > 16)
            {
                Graphics.Blit(webcamTexture, dest);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}