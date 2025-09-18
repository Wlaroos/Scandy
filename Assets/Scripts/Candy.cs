using System.Collections.Generic;
using UnityEngine;

public class Candy : MonoBehaviour
{
    private SpriteRenderer _sr;
    private PolygonCollider2D _pc;
    private Color32 _randomColor;
    [SerializeField] private Sprite[] _candySprites;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _pc = GetComponent<PolygonCollider2D>();

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
            (byte)Random.Range(100, 256),
            (byte)Random.Range(100, 256),
            (byte)Random.Range(100, 256),
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
}
