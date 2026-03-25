using UnityEngine;
using UnityEngine.Serialization;

public class SymbolBehaviour : MonoBehaviour
{
    [SerializeField] private SymbolData symbolData;

    public SymbolData SymbolData => symbolData;

    private SpriteRenderer _spriteRenderer;
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = symbolData.symbolSprite;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
