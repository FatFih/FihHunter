using UnityEngine;

public class CollectibleCount : MonoBehaviour
{
    TMPro.TextMeshProUGUI text;
    int count;

    void Awake()
    {
        text = GetComponent<TMPro.TextMeshProUGUI>();
    }

    void Start() => UpdateCount();
    void OnEnable() => Collectible.OnCollected += OnCollectibleCollected;
    void OnDisable() => Collectible.OnCollected -= OnCollectibleCollected;

    void OnCollectibleCollected()
    { 
        count++;
        UpdateCount();
    }

    void UpdateCount()
    {
        if (text == null) return;
        text.text = $"{count} / {Collectible.total}";
    }
}
