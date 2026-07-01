using DG.Tweening;
using UnityEngine;

public class TapeView : MonoBehaviour
{
    [SerializeField] private CellClickHandler _clickHandler;

    public int TapeId { get; private set; }

    private static readonly Color[] LayerColors =
    {
        new Color(0.85f, 0.20f, 0.20f),
        new Color(0.15f, 0.55f, 0.85f),
        new Color(0.15f, 0.75f, 0.35f),
        new Color(0.85f, 0.60f, 0.10f),
        new Color(0.55f, 0.15f, 0.80f),
        new Color(0.85f, 0.35f, 0.55f),
        new Color(0.15f, 0.75f, 0.75f),
        new Color(0.75f, 0.75f, 0.15f),
        new Color(0.40f, 0.40f, 0.40f),
    };

    private SpriteRenderer _sr;
    private Color _originalColor;
    private float _cellSize;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Setup(Tape tape, float cellSize, float tapeWidth, Vector2Int gridSize)
    {
        TapeId = tape.id;
        _cellSize = cellSize;

        Vector2 startPos = CellToWorld(tape.startCell, cellSize, gridSize);
        Vector2 endPos = CellToWorld(tape.endCell, cellSize, gridSize);
        Vector2 mid = (startPos + endPos) * 0.5f;
        float length = Vector2.Distance(startPos, endPos);
        float angle = Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x) * Mathf.Rad2Deg;

        transform.localPosition = new Vector3(mid.x, mid.y, 0);
        transform.localRotation = Quaternion.Euler(0, 0, angle);
        transform.localScale = new Vector3(length, tapeWidth, 1);

        _originalColor = GetLayerColor(tape.layer);
        if (_sr != null)
        {
            _sr.color = _originalColor;
            _sr.sortingOrder = tape.layer * 10 + 10;
        }

        if (_clickHandler != null) _clickHandler.tapeId = tape.id;
    }

    public void PlayRemoveAnimation()
    {
        transform.DOScale(0, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() => ObjectPoolManager.Instance.ReturnObject(gameObject));
    }

    public void PlayWrongAnimation()
    {
        if (_sr == null) return;

        DOTween.Kill(_sr);
        _sr.DOColor(Color.red, 0.1f).OnComplete(() =>
        {
            _sr.DOColor(_originalColor, 0.1f);
        });
    }

    private static Vector2 CellToWorld(Vector2Int cell, float cellSize, Vector2Int gridSize)
    {
        float offsetX = -gridSize.x * cellSize * 0.5f;
        float offsetY = -gridSize.y * cellSize * 0.5f;
        return new Vector2(
            offsetX + (cell.x + 0.5f) * cellSize,
            offsetY + (cell.y + 0.5f) * cellSize
        );
    }

    private static Color GetLayerColor(int layer)
    {
        if (layer >= 0 && layer < LayerColors.Length)
            return LayerColors[layer];

        float hue = (layer * 0.382f) % 1.0f;
        return Color.HSVToRGB(hue, 0.6f, 0.9f);
    }
}
