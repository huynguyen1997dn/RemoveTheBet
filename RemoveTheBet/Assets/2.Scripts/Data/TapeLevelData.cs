using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ dữ liệu của một Level trong game Tape Puzzle.
/// Bao gồm kích thước Grid và danh sách các Tape (băng dính) đã được đặt.
/// </summary>
[CreateAssetMenu(fileName = "NewTapeLevel", menuName = "Tape Puzzle/Level Data", order = 1)]
public class TapeLevelData : ScriptableObject
{
    [Header("Grid Settings")]
    public Vector2Int gridSize = new Vector2Int(8, 8);

    [Header("Tapes")]
    public List<Tape> tapes = new List<Tape>();
}

/// <summary>
/// Một Tape (băng dính) đại diện cho một đường thẳng từ startCell đến endCell.
/// layer thể hiện thứ tự chồng lớp: layer càng cao càng ở trên.
/// </summary>
[System.Serializable]
public class Tape
{
    public int id;
    public Vector2Int startCell;
    public Vector2Int endCell;
    public int layer;

    public Tape(int id, Vector2Int startCell, Vector2Int endCell, int layer)
    {
        this.id = id;
        this.startCell = startCell;
        this.endCell = endCell;
        this.layer = layer;
    }
}
