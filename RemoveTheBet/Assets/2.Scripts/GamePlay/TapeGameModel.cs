using System.Collections.Generic;
using UnityEngine;

public class TapeGameModel
{
    public TapeLevelData LevelData { get; private set; }
    public HashSet<int> RemovedTapeIds { get; private set; }

    private Dictionary<Vector2Int, List<Tape>> _cellToTapes;
    private Dictionary<int, List<Vector2Int>> _tapeToCells;
    private Vector2Int _gridSize;

    public void LoadLevel(TapeLevelData data)
    {
        LevelData = data;
        _gridSize = data.gridSize;
        RemovedTapeIds = new HashSet<int>();

        _cellToTapes = new Dictionary<Vector2Int, List<Tape>>();
        _tapeToCells = new Dictionary<int, List<Vector2Int>>();

        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                _cellToTapes[new Vector2Int(x, y)] = new List<Tape>();
            }
        }

        foreach (Tape tape in data.tapes)
        {
            List<Vector2Int> cells = GetLineCells(tape.startCell, tape.endCell);
            _tapeToCells[tape.id] = cells;

            foreach (Vector2Int cell in cells)
            {
                if (_cellToTapes.TryGetValue(cell, out List<Tape> tapesAtCell))
                {
                    tapesAtCell.Add(tape);
                }
            }
        }

        SortCellTapesByLayer();
    }

    private void SortCellTapesByLayer()
    {
        foreach (List<Tape> tapes in _cellToTapes.Values)
        {
            tapes.Sort((a, b) => b.layer.CompareTo(a.layer));
        }
    }

    public Tape GetTopTapeAtCell(Vector2Int cell)
    {
        if (!_cellToTapes.TryGetValue(cell, out List<Tape> tapes))
            return null;

        for (int i = 0; i < tapes.Count; i++)
        {
            if (!RemovedTapeIds.Contains(tapes[i].id))
                return tapes[i];
        }

        return null;
    }

    public bool IsTapeRemovable(int tapeId)
    {
        if (RemovedTapeIds.Contains(tapeId))
            return false;

        if (!_tapeToCells.TryGetValue(tapeId, out List<Vector2Int> cells))
            return false;

        foreach (Vector2Int cell in cells)
        {
            Tape topTape = GetTopTapeAtCell(cell);
            if (topTape == null || topTape.id != tapeId)
                return false;
        }

        return true;
    }

    public bool TryRemoveTape(int tapeId)
    {
        if (!IsTapeRemovable(tapeId))
            return false;

        RemovedTapeIds.Add(tapeId);
        return true;
    }

    public int RemainingCount
    {
        get
        {
            if (LevelData == null) return 0;
            return LevelData.tapes.Count - RemovedTapeIds.Count;
        }
    }

    public bool IsLevelComplete
    {
        get { return LevelData != null && RemainingCount == 0; }
    }

    public List<Tape> GetRemainingTapes()
    {
        List<Tape> remaining = new List<Tape>();
        if (LevelData == null) return remaining;

        foreach (Tape tape in LevelData.tapes)
        {
            if (!RemovedTapeIds.Contains(tape.id))
                remaining.Add(tape);
        }

        return remaining;
    }

    public Tape GetTapeById(int tapeId)
    {
        if (LevelData == null) return null;
        return LevelData.tapes.Find(t => t.id == tapeId);
    }

    public List<Vector2Int> GetLineCells(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        int x = start.x, y = start.y;
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            cells.Add(new Vector2Int(x, y));

            if (x == end.x && y == end.y) break;

            int e2 = 2 * err;
            if (e2 >= -dy) { err -= dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }

        return cells;
    }

    public List<Tape> GetTapesAtCell(Vector2Int cell)
    {
        if (_cellToTapes.TryGetValue(cell, out List<Tape> tapes))
        {
            List<Tape> result = new List<Tape>();
            foreach (Tape t in tapes)
            {
                if (!RemovedTapeIds.Contains(t.id))
                    result.Add(t);
            }
            return result;
        }
        return new List<Tape>();
    }
}
