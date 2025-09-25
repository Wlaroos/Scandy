using UnityEngine;

public class CandySpawner : MonoBehaviour
{

    [SerializeField] private GameObject _candyPrefab;

    private BoxCollider2D _bc;

    private void Awake()
    {
        _bc = GetComponent<BoxCollider2D>();

        if (_bc == null)
        {
            Debug.LogError("CandySpawner: Missing Collider component.");
        }
    }

    public void SpawnCandy()
    {
        Vector2 spawnPosition = new Vector2(Random.Range(_bc.bounds.min.x, _bc.bounds.max.x), Random.Range(_bc.bounds.min.y, _bc.bounds.max.y));

        if (_candyPrefab != null)
        {
            GameObject candy = Instantiate(_candyPrefab, spawnPosition, Quaternion.identity);

            candy.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
        else
        {
            Debug.LogError("CandySpawner: Candy prefab is not assigned.");
        }
    }

}
