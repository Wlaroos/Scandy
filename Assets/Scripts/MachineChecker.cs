using UnityEngine;

public class MachineChecker : MonoBehaviour
{
    private SpriteRenderer _sr;
    private BoxCollider2D _bc;

    [SerializeField] private Sprite _originalSprite;
    [SerializeField] private Sprite _highlightedSprite;
    [SerializeField] private float HoverDuration = 2f;
    [SerializeField] private GameObject _bar;

    private bool _candyHovering = false;
    private float _hoverTimer = 0f;
    private GameObject _currentCandy = null;

    private void Awake()
    {
        _sr = GetComponentInParent<SpriteRenderer>();
        _bc = GetComponent<BoxCollider2D>();

        if (_sr == null)
        {
            Debug.LogError("MachineChecker: Missing SpriteRenderer component.");
        }

        if (_bc == null)
        {
            Debug.LogError("MachineChecker: Missing Collider component.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Candy>() != null)
        {
            _candyHovering = true;
            _hoverTimer = 0f;
            _currentCandy = other.gameObject;
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Candy>() != null && other.gameObject == _currentCandy)
        {
            _candyHovering = false;
            _hoverTimer = 0f;
            _currentCandy = null;
            if (_bar != null)
            {
                _bar.transform.localScale = new Vector3(0f, 1f, 1f);
            }
        }
    }

    private void Update()
    {
        if (_bar == null) return;

        if (_candyHovering)
        {
            _hoverTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_hoverTimer / HoverDuration);
            _bar.transform.localScale = new Vector3(progress, 0.12f, 0.1762f);

            if (_hoverTimer >= HoverDuration && _sr.sprite != _highlightedSprite)
            {
                _sr.sprite = _highlightedSprite;
            }
        }
        else
        {
            _bar.transform.localScale = new Vector3(0f, 0.12f, 0.1762f);
            if (_sr.sprite != _originalSprite)
            {
                _sr.sprite = _originalSprite;
            }
        }
    }

    
}
