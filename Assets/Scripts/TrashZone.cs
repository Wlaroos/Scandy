using UnityEngine;

public class TrashZone : MonoBehaviour
{
    private Collider2D _cd;
    private SpriteRenderer _sr;

    private Color32 _normalColor;
    [SerializeField] private Color32 _highlightColor;


    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        if (_sr == null)
        {
            Debug.LogError("TrashZone: Missing SpriteRenderer component.");
        }
        else
        {
            _normalColor = _sr.color;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _cd = collision;

        if (collision.CompareTag("Candy") && _sr != null && collision.GetComponent<Candy>().Scanned == true)
        {
            _sr.color = _highlightColor;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.CompareTag("Candy") && _sr != null  && collision.GetComponent<Candy>().Scanned == true)
        {
            _sr.color = _normalColor;
        }

        if (_cd == collision)
        {
            _cd = null;
        }
    }
}
