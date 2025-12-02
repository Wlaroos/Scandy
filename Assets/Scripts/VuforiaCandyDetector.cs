using UnityEngine;
using Vuforia;

public class VuforiaCandyDetector : MonoBehaviour
{
    // Reference to the main TransitionManager
    [Tooltip("Drag the GameObject with TransitionManager here.")]
    [SerializeField] private TransitionManager transitionManager;

    private ObserverBehaviour mObserverBehaviour;
    private string targetName;

    void Start()
    {
        mObserverBehaviour = GetComponent<ObserverBehaviour>();
        if (mObserverBehaviour)
        {
            // Subscribe to the new Observer callback
            mObserverBehaviour.OnTargetStatusChanged += OnObserverStatusChanged;

            // Use the Observer's name if available, otherwise fall back to GameObject name
            targetName = !string.IsNullOrEmpty(mObserverBehaviour.TargetName) ? mObserverBehaviour.TargetName : gameObject.name;
        }

        if (transitionManager == null)
        {
            // Try to find the manager if it wasn't assigned (e.g., if it's a singleton)
            transitionManager = FindAnyObjectByType<TransitionManager>();
            if (transitionManager == null)
            {
                Debug.LogError("VuforiaCandyDetector: TransitionManager not assigned or found in scene!");
            }
        }
    }

    void OnDestroy()
    {
        if (mObserverBehaviour != null)
            mObserverBehaviour.OnTargetStatusChanged -= OnObserverStatusChanged;
    }

    // New observer callback signature
    private void OnObserverStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        // Determine whether the target is considered "tracked"
        var status = targetStatus.Status;
        bool isTracked = status == Status.TRACKED || status == Status.EXTENDED_TRACKED;

        if (isTracked)
        {
            OnTrackingFound();
        }
        else
        {
            OnTrackingLost();
        }
    }

    private void OnTrackingFound()
    {
        Debug.Log($"Vuforia: Target Found - {targetName}");
        
        // Notify the TransitionManager to spawn the associated candy bar
        if (transitionManager != null)
        {
            transitionManager.HandleImageTracked(targetName);
        }
    }

    private void OnTrackingLost()
    {
        Debug.Log($"Vuforia: Target Lost - {targetName}");
        
        // Notify the TransitionManager that the target is lost (optional)
        if (transitionManager != null)
        {
            transitionManager.HandleImageLost(targetName);
        }
    }
}