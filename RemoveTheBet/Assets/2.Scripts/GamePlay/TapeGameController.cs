using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TapeGameController : Singleton<TapeGameController>
{
    public TapeGameModel Model { get; private set; }

    [Header("Level Data")]
    [SerializeField] private TapeLevelData _levelData;

    [Header("Grid Settings")]
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private float _tapeWidth = 0.8f;

    [Header("Prefab")]
    [SerializeField] private GameObject _tapeViewPrefab;

    [Header("Containers")]
    [SerializeField] private Transform _tapeContainer;

    [Header("References")]
    [SerializeField] private TextMeshPro _tmpTapeCount;

    private Dictionary<int, TapeView> _tapeViews;

    protected override void Awake()
    {
        base.Awake();
        Model = new TapeGameModel();
        _tapeViews = new Dictionary<int, TapeView>();
    }

    private void Start()
    {
        if (_levelData != null)
            LoadLevel(_levelData);
    }

    public void LoadLevel(TapeLevelData data)
    {
        if (data == null)
        {
            Debug.LogError("[TapeGameController] LoadLevel: data is null!");
            return;
        }

        ClearLevel();
        Model.LoadLevel(data);

        foreach (Tape tape in data.tapes)
        {
            CreateTapeView(tape, data.gridSize);
        }

        EventDispatcher.Dispatch(EventID.TapeSetupComplete);
        EventDispatcher.Dispatch(EventID.TapeRefreshCount, Model.RemainingCount);

        UpdateTapeCountDisplay();
    }

    public void OnTapeTapped(int tapeId)
    {
        if (Model.LevelData == null)
        {
            Debug.LogWarning("[TapeGameController] No level loaded!");
            return;
        }

        if (Model.IsLevelComplete)
        {
            Debug.Log("[TapeGameController] Level already complete!");
            return;
        }

        Debug.LogError("[TapeGameController] Tape tapped!");
        if (Model.IsTapeRemovable(tapeId))
        {
            Model.TryRemoveTape(tapeId);

            if (_tapeViews.TryGetValue(tapeId, out TapeView tv))
            {
                _tapeViews.Remove(tapeId);
                tv.PlayRemoveAnimation();
            }

            UpdateTapeCountDisplay();
            EventDispatcher.Dispatch(EventID.TapeRemoved, tapeId);
            EventDispatcher.Dispatch(EventID.TapeRefreshCount, Model.RemainingCount);

            if (Model.IsLevelComplete)
            {
                Debug.Log("[TapeGameController] Level complete!");
            }
        }
        else
        {
            if (_tapeViews.TryGetValue(tapeId, out TapeView tv))
            {
                tv.PlayWrongAnimation();
            }
            EventDispatcher.Dispatch(EventID.WrongTap, tapeId);
        }
    }

    public void RestartLevel()
    {
        if (_levelData != null)
            LoadLevel(_levelData);
    }

    public void SetLevelData(TapeLevelData data)
    {
        _levelData = data;
    }

    private void CreateTapeView(Tape tape, Vector2Int gridSize)
    {
        if (_tapeContainer == null)
        {
            GameObject go = new GameObject("[Tapes]");
            go.transform.SetParent(transform, false);
            _tapeContainer = go.transform;
        }

        Vector3 pos = CellToWorld(tape.startCell, tape.endCell, gridSize);

        GameObject tapeGo = ObjectPoolManager.Instance.GetObject2D(
            _tapeViewPrefab, pos, _tapeContainer);

        TapeView tv = tapeGo.GetComponent<TapeView>();
        tv.Setup(tape, _cellSize, _tapeWidth, gridSize);

        _tapeViews[tape.id] = tv;
    }

    private void UpdateTapeCountDisplay()
    {
        if (_tmpTapeCount != null)
        {
            int count = Model.RemainingCount;
            _tmpTapeCount.text = $"{count} tape{(count != 1 ? "s" : "")}";
        }
    }

    private void ClearLevel()
    {
        foreach (var kvp in _tapeViews)
        {
            ObjectPoolManager.Instance.ReturnObject(kvp.Value.gameObject);
        }
        _tapeViews.Clear();
    }

    private Vector3 CellToWorld(Vector2Int startCell, Vector2Int endCell, Vector2Int gridSize)
    {
        float offsetX = -gridSize.x * _cellSize * 0.5f;
        float offsetY = -gridSize.y * _cellSize * 0.5f;

        Vector2 startPos = new Vector2(
            offsetX + (startCell.x + 0.5f) * _cellSize,
            offsetY + (startCell.y + 0.5f) * _cellSize);
        Vector2 endPos = new Vector2(
            offsetX + (endCell.x + 0.5f) * _cellSize,
            offsetY + (endCell.y + 0.5f) * _cellSize);

        return (startPos + endPos) * 0.5f;
    }
}
