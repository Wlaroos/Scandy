using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Candy : MonoBehaviour
{
    private SpriteRenderer _sr;
    private PolygonCollider2D _pc;
    private Color32 _randomColor;
    [SerializeField] private Sprite[] _candySprites;

    // Drag and Drop variables
    [SerializeField] private float _dragSpeed = 25f; // Speed of interpolation
    private Vector2 _mouseOffset;
    private Collider2D _collider;
    private SortingGroup _sg;

    [SerializeField] private GameObject _popEffectPrefab;

    private bool _scanned = false;
    public bool Scanned { get { return _scanned; } set { _scanned = value; } }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _pc = GetComponent<PolygonCollider2D>();
        _sg = GetComponent<SortingGroup>();

        if (_sr == null)
        {
            Debug.LogError("Candy: Missing SpriteRenderer component.");
        }

        if (_candySprites == null || _candySprites.Length == 0)
        {
            Debug.LogError("Candy: No candy sprites assigned.");
        }
        else
        {
            // Assign a random sprite from the array
            _sr.sprite = _candySprites[Random.Range(0, _candySprites.Length)];
        }

        if (_pc == null)
        {
            Debug.LogError("Candy: Missing PolygonCollider2D component.");
        }

        // Generate and assign a random color
        _randomColor = new Color32(
            (byte)Random.Range(1, 256),
            (byte)Random.Range(1, 256),
            (byte)Random.Range(1, 256),
            255);
        if (_sr != null)
        {
            _sr.color = _randomColor;
        }
    }

    void Start()
    {
        // Update the PolygonCollider2D to match the new sprite
        if (_pc != null && _sr != null && _sr.sprite != null)
        {
            _pc.pathCount = _sr.sprite.GetPhysicsShapeCount();
            for (int i = 0; i < _pc.pathCount; i++)
            {
                List<Vector2> path = new List<Vector2>();
                _sr.sprite.GetPhysicsShape(i, path);
                _pc.SetPath(i, path.ToArray());
            }
        }
    }
   
    private Vector2 GetMouseWorldPosition()
    {
        // Convert screen position to world position in 2D
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDown()
    {
        // Calculate the offset between the object's position and the mouse position
        _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
        _sg.sortingLayerName = "Dragging";
    }

    private void OnMouseDrag()
    {
        // Calculate the target position based on the mouse position and offset
        Vector2 targetPosition = GetMouseWorldPosition() + _mouseOffset;

        // Smoothly interpolate the object's position towards the target position
        transform.position = Vector2.Lerp(transform.position, targetPosition, _dragSpeed * Time.deltaTime);
    }

    private void OnMouseUp()
    {
        if(_collider == null)
        {
            _sg.sortingLayerName = "Default";
            return;
        }
        else if (_collider.CompareTag("Zone"))
        {
            if (_popEffectPrefab != null)
            {
                // Set particle color to match SpriteRenderer color
                ParticleSystem ps = _popEffectPrefab.GetComponent<ParticleSystem>();
                if (_sr != null && ps != null)
                {
                    var main = ps.main;
                    main.startColor = _sr.color;
                }

                Instantiate(_popEffectPrefab, transform.position, Quaternion.Euler(-90f, 0f, 0f));
            }

            Destroy(gameObject);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _collider = collision;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_collider == collision)
        {
            _collider = null;
        }
    }
}
