using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(GameLevelConfig))]
public class GameLevelConfigEditor : Editor
{
    private int brushValue = 0;
    private bool isDragging = false;
    private Vector2Int startPos;
    private Vector2Int currentPos;
    private int startBrushValue;

    private List<Vector2Int> dragPath = new List<Vector2Int>();

    private int lastWidth;
    private int lastHeight;

    public override void OnInspectorGUI()
    {
        GameLevelConfig config = (GameLevelConfig)target;

        serializedObject.Update();

        SerializedProperty widthProp = serializedObject.FindProperty("width");
        SerializedProperty heightProp = serializedObject.FindProperty("height");

        int newWidth = Mathf.Max(1, widthProp.intValue);
        int newHeight = Mathf.Max(1, heightProp.intValue);

        if (newWidth != lastWidth || newHeight != lastHeight)
        {
            config.ResizeGrid(newWidth, newHeight);
            widthProp.intValue = newWidth;
            heightProp.intValue = newHeight;
            lastWidth = newWidth;
            lastHeight = newHeight;
        }

        EditorGUILayout.PropertyField(widthProp);
        EditorGUILayout.PropertyField(heightProp);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("BrushValue");
        brushValue = EditorGUILayout.IntField("Brush Value", brushValue);
        if (GUILayout.Button("+1", GUILayout.Width(40)))
        {
            brushValue++;
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Auto Generate", GUILayout.Height(30)))
        {
            LevelGenerator.Generate(config);
            EditorUtility.SetDirty(config);
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear All", GUILayout.Height(30), GUILayout.Width(80)))
        {
            config.ClearSegments();
            int size = config.Width * config.Height;
            for (int i = 0; i < size; i++)
            {
                var coords = GetCoordsFromIndex(i, config.Width);
                config.SetValue(coords.x, coords.y, -1);
            }
            EditorUtility.SetDirty(config);
        }
        GUILayout.EndHorizontal();

        int placedCount = config.Segments != null ? config.Segments.Count : 0;
        EditorGUILayout.LabelField($"Snakes placed: {placedCount} | Grid: {config.Width}x{config.Height} = {config.Width * config.Height} cells", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space();

        DrawGrid(config);

        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }

    private void DrawGrid(GameLevelConfig config)
    {
        int width = config.Width;
        int height = config.Height;

        if (width <= 0 || height <= 0)
        {
            EditorGUILayout.LabelField("Invalid grid size");
            return;
        }

        int cellSize = 20;
        int cellPadding = 2;

        EditorGUILayout.LabelField($"Grid ({width}x{height}) - Click: set {brushValue} | Shift+Click: +1", EditorStyles.boldLabel);

        Event ev = Event.current;

        for (int y = 0; y < height; y++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(cellPadding);
            for (int x = 0; x < width; x++)
            {
                int actualCellSize = cellSize - cellPadding;
                Rect cellRect = GUILayoutUtility.GetRect(actualCellSize, actualCellSize, GUILayout.Width(actualCellSize), GUILayout.Height(actualCellSize));
                int value = config.GetValue(x, y);
                bool isSet = value != -1;

                bool isStart = isDragging && x == startPos.x && y == startPos.y;
                bool isEnd = isDragging && x == currentPos.x && y == currentPos.y;

                Color bgColor;
                string label;

                if (isSet)
                {
                    bgColor = GetColorForValue(value);
                    label = value.ToString();
                }
                else
                {
                    bgColor = new Color(0.15f, 0.15f, 0.15f);
                    label = "-";
                }

                GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.alignment = TextAnchor.MiddleCenter;
                boxStyle.fontSize = 14;
                boxStyle.fontStyle = FontStyle.Bold;
                boxStyle.normal.textColor = isSet ? Color.white : new Color(0.4f, 0.4f, 0.4f);

                Color originalColor = GUI.color;
                GUI.color = bgColor;

                GUI.Box(cellRect, label, boxStyle);

                int cellIndex = y * width + x;
                if (cellIndex >= 0)
                {
                    Rect idRect = new Rect(cellRect.x + 2, cellRect.y + 2, 20, 12);
                    GUIStyle idStyle = new GUIStyle(EditorStyles.label);
                    idStyle.fontSize = 8;
                    idStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
                    GUI.Label(idRect, cellIndex.ToString(), idStyle);
                }

                if (isSet)
                {
                    GUI.color = originalColor;
                    GUI.Box(cellRect, "", GUI.skin.box);
                    Rect borderRect = new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width - 4, cellRect.height - 4);
                    GUI.Box(borderRect, "", GUI.skin.box);
                }

                if (isStart)
                {
                    GUI.color = Color.green;
                    GUI.Box(cellRect, "S", GUI.skin.box);
                }

                if (isEnd)
                {
                    GUI.color = Color.red;
                    GUI.Box(cellRect, "E", GUI.skin.box);
                }

                GUI.color = originalColor;

                if (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag)
                {
                    if (ev.button == 0 && cellRect.Contains(ev.mousePosition))
                    {
                        if (ev.type == EventType.MouseDown)
                        {
                            isDragging = true;
                            startPos = new Vector2Int(x, y);
                            currentPos = new Vector2Int(x, y);
                            startBrushValue = brushValue;

                            dragPath.Clear();
                            dragPath.Add(new Vector2Int(x, y));

                            if (ev.shift)
                            {
                                brushValue++;
                            }

                            config.SetValue(x, y, brushValue);
                            EditorUtility.SetDirty(config);
                            ev.Use();
                        }
                        else if (isDragging)
                        {
                            currentPos = new Vector2Int(x, y);

                            Vector2Int newCell = new Vector2Int(x, y);
                            if (dragPath.Count == 0 || dragPath[dragPath.Count - 1] != newCell)
                                dragPath.Add(newCell);

                            if (ev.shift)
                            {
                                brushValue++;
                            }
                            config.SetValue(x, y, brushValue);
                            EditorUtility.SetDirty(config);
                            ev.Use();
                        }
                    }
                }
                if (x < width - 1)
                {
                    GUILayout.Space(cellPadding);
                }
            }
            GUILayout.EndHorizontal();
            if (y < height - 1)
            {
                GUILayout.Space(cellPadding);
            }
        }

        if (ev.type == EventType.MouseUp && ev.button == 0)
        {
            if (isDragging)
            {
                int[] precomputedBodyIndices;
                if (dragPath.Count <= 2)
                {
                    precomputedBodyIndices = new int[0];
                }
                else
                {
                    precomputedBodyIndices = new int[dragPath.Count - 2];
                    int w = config.Width;
                    for (int i = 1; i < dragPath.Count - 1; i++)
                    {
                        Vector2Int pos = dragPath[i];
                        precomputedBodyIndices[i - 1] = pos.y * w + pos.x;
                    }
                }

                config.AddSegment(startBrushValue, startPos.x, startPos.y, currentPos.x, currentPos.y, precomputedBodyIndices);
                EditorUtility.SetDirty(config);
            }
            isDragging = false;
            dragPath.Clear();
        }
    }

    private Color GetColorForValue(int value)
    {
        float hue = Mathf.Repeat((float)value / 10f, 1f);
        return Color.HSVToRGB(hue, 0.7f, 1f);
    }

    private (int x, int y) GetCoordsFromIndex(int index, int width)
    {
        int x = index % width;
        int y = index / width;
        return (x, y);
    }
}