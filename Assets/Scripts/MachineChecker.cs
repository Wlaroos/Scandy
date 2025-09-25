using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MachineChecker : MonoBehaviour
{
    // --- Serialized Fields ---
    [Header("Sprites")]
    [SerializeField] private Sprite _originalSprite;
    [SerializeField] private Sprite _highlightedSprite;

    [Header("Scan Settings")]
    [SerializeField] private float HoverDuration = 2f;
    [SerializeField] private float _nextScanDelay = 1f;

    [Header("UI")]
    [SerializeField] private GameObject _bar;

    // --- Private Fields ---
    private SpriteRenderer _sr;
    private BoxCollider2D _bc;

    private bool _candyHovering = false;
    private bool _isHighlighting = false;
    private float _hoverTimer = 0f;
    private GameObject _currentCandy = null;
    private Queue<GameObject> _candyQueue = new Queue<GameObject>();

    // --- Unity Methods ---
    private void Awake()
    {
        _sr = GetComponentInParent<SpriteRenderer>();
        _bc = GetComponent<BoxCollider2D>();

        if (_sr == null)
            Debug.LogError("MachineChecker: Missing SpriteRenderer component.");
        if (_bc == null)
            Debug.LogError("MachineChecker: Missing Collider component.");
    }

    private void Update()
    {
        if (_bar == null) return;

        if (_candyHovering && _currentCandy != null && !_isHighlighting)
        {
            UpdateScanBar();
            if (_hoverTimer >= HoverDuration)
                StartCoroutine(HighlightAndContinue());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Candy candy = other.GetComponent<Candy>();
        if (candy != null && !candy.Scanned)
        {
            _candyQueue.Enqueue(other.gameObject);
            if (!_candyHovering && !_isHighlighting)
                StartNextCandyScan();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Candy candy = other.GetComponent<Candy>();
        if (candy == null) return;

        RemoveCandyFromQueue(other.gameObject);

        if (other.gameObject == _currentCandy)
        {
            ResetScanState();
            if (_candyQueue.Count > 0 && !_isHighlighting)
                StartNextCandyScan();
        }
    }

    // --- Scan Logic ---
    private void UpdateScanBar()
    {
        _hoverTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_hoverTimer / HoverDuration);
        _bar.transform.localScale = new Vector3(progress, 0.12f, 0.1762f);
    }

    private IEnumerator HighlightAndContinue()
    {
        _isHighlighting = true;
        _candyHovering = false;

        _sr.sprite = _highlightedSprite;
        _currentCandy.GetComponent<Candy>().ScannedByMachine();

        // Dequeue the scanned candy
        if (_candyQueue.Count > 0 && _candyQueue.Peek() == _currentCandy)
            _candyQueue.Dequeue();
        _currentCandy = null;

        yield return new WaitForSeconds(_nextScanDelay);

        _sr.sprite = _originalSprite;
        _bar.transform.localScale = new Vector3(0f, 0.12f, 0.1762f);
        _hoverTimer = 0f;
        _isHighlighting = false;

        if (_candyQueue.Count > 0)
            StartNextCandyScan();
    }

    private void StartNextCandyScan()
    {
        if (_candyQueue.Count == 0) return;
        _currentCandy = _candyQueue.Peek();
        _candyHovering = true;
        _hoverTimer = 0f;
    }

    private void RemoveCandyFromQueue(GameObject candyObj)
    {
        if (!_candyQueue.Contains(candyObj)) return;

        var newQueue = new Queue<GameObject>();
        while (_candyQueue.Count > 0)
        {
            var c = _candyQueue.Dequeue();
            if (c != candyObj)
                newQueue.Enqueue(c);
        }
        _candyQueue = newQueue;
    }

    private void ResetScanState()
    {
        _candyHovering = false;
        _hoverTimer = 0f;
        _currentCandy = null;
        _bar.transform.localScale = new Vector3(0f, 0.12f, 0.1762f);
        if (_sr.sprite != _originalSprite)
            _sr.sprite = _originalSprite;
    }
}
