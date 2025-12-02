using UnityEngine;

public class CameraToggle : MonoBehaviour
{
    private Camera _cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.enabled = false;
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            _cam.enabled = !_cam.enabled;
        }
    }
}
