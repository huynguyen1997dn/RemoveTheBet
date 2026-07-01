using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/// <summary>
/// TapeLevelEditorWindow — Editor Window thiết kế Level cho game Tape Puzzle (Tháo băng dính).
///
/// Tính năng chính:
///   - Tạo lưới Grid với kích thước tùy chỉnh.
///   - Kéo thả chuột để tạo Tape (băng dính) trên Grid.
///   - Tự động tính Layer dựa trên overlap (giao cắt) với Tape khác.
///   - Người dùng có thể chỉnh sửa Layer thủ công trong danh sách.
///   - Màu sắc Tape theo Layer (cùng Layer cùng màu).
///   - Lưu / Xóa Level.
/// </summary>
public class TapeLevelEditorWindow : EditorWindow
{
    // ===================== HẰNG SỐ =====================
    private const float GRID_PADDING    = 10f;
    private const float MIN_CELL_SIZE   = 20f;
    private const float MAX_CELL_SIZE   = 80f;
    private const float SETTINGS_HEIGHT = 30f;

    // ===================== CÀI ĐẶT GRID =====================
    private Vector2Int m_GridSize      = new Vector2Int(8, 8);
    private float      m_CellSize      = 50f;
    private bool       m_GridGenerated = false;

    // ===================== DỮ LIỆU TAPE =====================
    private TapeLevelData m_LevelData;
    private List<Tape>    m_Tapes;       // tham chiếu đến m_LevelData.tapes
    private int           m_NextId = 1;

    // ===================== LOAD ASSET =====================
    private TapeLevelData m_LoadAsset;

    // ===================== TRẠNG THÁI KÉO THẢ =====================
    private bool      m_IsDragging   = false;
    private Vector2Int m_DragStart;
    private Vector2Int m_DragCurrent;

    // ===================== REORDERABLE LIST =====================
    private ReorderableList m_TapeList;
    private Vector2 m_ListScrollPosition;

    // ===================== MÀU THEO LAYER =====================
    private static readonly Color[] s_LayerColors =
    {
        new Color(0.85f, 0.20f, 0.20f), // Layer 0: Đỏ
        new Color(0.15f, 0.55f, 0.85f), // Layer 1: Xanh dương
        new Color(0.15f, 0.75f, 0.35f), // Layer 2: Xanh lá
        new Color(0.85f, 0.60f, 0.10f), // Layer 3: Cam
        new Color(0.55f, 0.15f, 0.80f), // Layer 4: Tím
        new Color(0.85f, 0.35f, 0.55f), // Layer 5: Hồng
        new Color(0.15f, 0.75f, 0.75f), // Layer 6: Xanh lơ
        new Color(0.75f, 0.75f, 0.15f), // Layer 7: Vàng
        new Color(0.40f, 0.40f, 0.40f), // Layer 8: Xám
    };

    private static readonly Color s_PreviewColor       = new Color(0.30f, 0.60f, 1.00f, 0.40f);
    private static readonly Color s_PreviewBorderColor = new Color(0.20f, 0.40f, 0.80f, 0.65f);
    private static readonly Color s_GridLineColor      = new Color(0.45f, 0.45f, 0.45f);

    // =============================================================
    //  MỞ CỬA SỔ EDITOR WINDOW
    // =============================================================
    [MenuItem("Tools/Tape Level Editor")]
    public static void OpenWindow()
    {
        TapeLevelEditorWindow window = GetWindow<TapeLevelEditorWindow>("Tape Level Editor");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    // =============================================================
    //  KHỞI TẠO
    // =============================================================
    private void OnEnable()
    {
        InitializeData();
        SetupReorderableList();
    }

    /// <summary> Khởi tạo ScriptableObject tạm và danh sách Tape. </summary>
    private void InitializeData()
    {
        m_LevelData = ScriptableObject.CreateInstance<TapeLevelData>();
        m_LevelData.gridSize = m_GridSize;
        m_Tapes = m_LevelData.tapes;
        m_NextId = 1;
        m_IsDragging = false;
    }

    /// <summary> Thiết lập ReorderableList hiển thị danh sách Tape. </summary>
    private void SetupReorderableList()
    {
        m_TapeList = new ReorderableList(m_Tapes, typeof(Tape), true, true, false, true);

        // -- Header: ID | Start | End | Layer --
        m_TapeList.drawHeaderCallback = (Rect rect) =>
        {
            float w = rect.width;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, w * 0.12f, rect.height), "ID");
            EditorGUI.LabelField(new Rect(rect.x + w * 0.14f, rect.y, w * 0.30f, rect.height), "Start → End");
            EditorGUI.LabelField(new Rect(rect.x + w * 0.46f, rect.y, w * 0.12f, rect.height), "Layer");

            // Tổng số tape
            GUI.Label(new Rect(rect.x + w * 0.65f, rect.y, w * 0.35f, rect.height),
                      $"Tổng: {m_Tapes.Count} tape(s)", EditorStyles.miniLabel);
        };

        // -- Vẽ từng dòng Tape --
        m_TapeList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index >= m_Tapes.Count) return;
            Tape tape = m_Tapes[index];

            rect.y += 2;
            float h = EditorGUIUtility.singleLineHeight;
            float w = rect.width;

            // Cột ID (chỉ đọc)
            Rect idRect = new Rect(rect.x, rect.y, w * 0.12f, h);
            EditorGUI.LabelField(idRect, $"#{tape.id}");

            // Cột Start → End (chỉ đọc)
            Rect posRect = new Rect(rect.x + w * 0.14f, rect.y, w * 0.30f, h);
            EditorGUI.LabelField(posRect, $"({tape.startCell.x},{tape.startCell.y}) → ({tape.endCell.x},{tape.endCell.y})");

            // Cột Layer (có thể chỉnh sửa)
            Rect layerRect = new Rect(rect.x + w * 0.46f, rect.y, w * 0.12f, h);

            // Tô màu nền theo layer để trực quan
            Color original = GUI.color;
            Color layerBg = GetLayerColor(tape.layer);
            layerBg.a = 0.3f;
            EditorGUI.DrawRect(layerRect, layerBg);

            int newLayer = EditorGUI.IntField(layerRect, GUIContent.none, tape.layer);
            if (newLayer != tape.layer)
            {
                tape.layer = Mathf.Max(0, newLayer);
            }
        };

        m_TapeList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;

        // Khi danh sách thay đổi → cập nhật lại giao diện
        m_TapeList.onChangedCallback = (list) => Repaint();
    }

    // =============================================================
    //  ONGUI — VẼ GIAO DIỆN CHÍNH MỖI FRAME
    // =============================================================
    private void OnGUI()
    {
        DrawSettingsSection();

        if (m_GridGenerated)
        {
            DrawGridSection();
        }
        else
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(
                "Vui lòng nhập kích thước Grid và nhấn 'Generate Grid' để bắt đầu.",
                MessageType.Info);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        DrawBottomSection();

        // Gọi Repaint liên tục khi đang kéo để Preview mượt mà
        if (m_IsDragging)
            Repaint();
    }

    // =============================================================
    //  PHẦN 1: CÀI ĐẶT GRID (THANH TOOLBAR PHÍA TRÊN)
    // =============================================================
    private void DrawSettingsSection()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            GUILayout.Label("Grid Settings", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Space(8);

            GUILayout.Label("W:", GUILayout.Width(16));
            m_GridSize.x = Mathf.Max(2, EditorGUILayout.IntField(m_GridSize.x, GUILayout.Width(36)));

            GUILayout.Label("H:", GUILayout.Width(16));
            m_GridSize.y = Mathf.Max(2, EditorGUILayout.IntField(m_GridSize.y, GUILayout.Width(36)));

            GUILayout.Space(8);

            // Load Level
            m_LoadAsset = (TapeLevelData)EditorGUILayout.ObjectField(
                GUIContent.none, m_LoadAsset, typeof(TapeLevelData), false, GUILayout.Width(140));

            GUI.enabled = m_LoadAsset != null;
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(40)))
                LoadFromAsset();
            GUI.enabled = true;

            GUILayout.Space(8);

            if (GUILayout.Button("Generate Grid", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                GenerateGrid();
            }

            GUILayout.Space(12);

            // Nút Save / Clear nằm trên cùng toolbar với Generate
            GUI.enabled = m_GridGenerated && m_Tapes.Count > 0;
            if (GUILayout.Button("Save Level", EditorStyles.toolbarButton, GUILayout.Width(75)))
                SaveLevel();

            GUI.enabled = m_GridGenerated;
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
                ClearAll();
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary> Tạo Grid mới, tự động tính cell size vừa với cửa sổ. </summary>
    private void GenerateGrid()
    {
        m_GridGenerated = true;
        m_LevelData.gridSize = m_GridSize;

        // Tính cell size dựa trên không gian có sẵn
        RecalculateCellSize();

        // Reset dữ liệu Tape cũ
        m_Tapes.Clear();
        m_NextId = 1;
        m_IsDragging = false;
        Repaint();
    }

    private void RecalculateCellSize()
    {
        float availW = position.width  - 20f;
        float availH = position.height - SETTINGS_HEIGHT - EstimateBottomHeight() - 10f;

        float fitW = availW / m_GridSize.x;
        float fitH = availH / m_GridSize.y;
        m_CellSize = Mathf.Clamp(Mathf.Min(fitW, fitH), MIN_CELL_SIZE, MAX_CELL_SIZE);
    }

    // =============================================================
    //  PHẦN 2: KHU VỰC GRID — VẼ LƯỚI, TAPE, PREVIEW + XỬ LÝ CHUỘT
    //
    //  Tất cả vẽ bằng GUI (tọa độ tuyệt đối trong cửa sổ).
    //  GridRect được tính toán để nằm giữa khu vực Settings và Bottom.
    // =============================================================
    private void DrawGridSection()
    {
        RecalculateCellSize();

        float gridPixelW = m_GridSize.x * m_CellSize;
        float gridPixelH = m_GridSize.y * m_CellSize;

        // Căn giữa Grid trong vùng có sẵn
        float offsetX = (position.width  - gridPixelW) * 0.5f;
        float offsetY = SETTINGS_HEIGHT + 4f;

        Rect gridRect = new Rect(offsetX, offsetY, gridPixelW, gridPixelH);

        DrawGrid(gridRect);
        DrawAllTapes(gridRect);

        if (m_IsDragging)
            DrawDragPreview(gridRect);

        // Xử lý sự kiện chuột trên Grid
        HandleGridMouseEvents(gridRect);

        // Dành không gian GUILayout để BottomSection không đè lên Grid
        GUILayout.Space(gridPixelH + 12f);
    }

    // =============================================================
    //  VẼ LƯỚI Ô VUÔNG
    //  Xen kẽ màu (bàn cờ), vẽ tọa độ ô nhỏ ở góc.
    // =============================================================
    private void DrawGrid(Rect gridRect)
    {
        if (Event.current.type != EventType.Repaint) return;

        Color cellA = new Color(0.92f, 0.92f, 0.92f);
        Color cellB = new Color(0.84f, 0.84f, 0.84f);

        for (int row = 0; row < m_GridSize.y; row++)
        {
            for (int col = 0; col < m_GridSize.x; col++)
            {
                int displayRow = m_GridSize.y - 1 - row;
                Rect cellRect = new Rect(
                    gridRect.x + col * m_CellSize,
                    gridRect.y + displayRow * m_CellSize,
                    m_CellSize, m_CellSize);

                // Nền xen kẽ
                Color bg = (row + col) % 2 == 0 ? cellA : cellB;
                EditorGUI.DrawRect(cellRect, bg);

                // Viền
                Handles.color = s_GridLineColor;
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y),
                                 new Vector3(cellRect.x + cellRect.width, cellRect.y));
                Handles.DrawLine(new Vector3(cellRect.x, cellRect.y),
                                 new Vector3(cellRect.x, cellRect.y + cellRect.height));

                // Tọa độ ô (chỉ khi đủ lớn)
                if (m_CellSize >= 32f)
                {
                    GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize   = Mathf.RoundToInt(m_CellSize * 0.15f),
                        alignment  = TextAnchor.LowerRight
                    };
                    style.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                    Rect coordRect = new Rect(cellRect.x - 2, cellRect.y - 2,
                                               cellRect.width, cellRect.height);
                    GUI.Label(coordRect, $"{col},{row}", style);
                }
            }
        }

        // Viền ngoài cùng (phải + dưới)
        Handles.DrawLine(new Vector3(gridRect.x + gridRect.width, gridRect.y),
                         new Vector3(gridRect.x + gridRect.width, gridRect.y + gridRect.height));
        Handles.DrawLine(new Vector3(gridRect.x, gridRect.y + gridRect.height),
                         new Vector3(gridRect.x + gridRect.width, gridRect.y + gridRect.height));
    }

    // =============================================================
    //  VẼ TẤT CẢ TAPE
    //  Mỗi Tape tô màu theo layer, vẽ số layer ở ô giữa.
    // =============================================================
    private void DrawAllTapes(Rect gridRect)
    {
        if (Event.current.type != EventType.Repaint) return;

        foreach (Tape tape in m_Tapes)
        {
            DrawSingleTape(gridRect, tape);
        }
    }

    private void DrawSingleTape(Rect gridRect, Tape tape)
    {
        Vector2Int start = tape.startCell;
        Vector2Int end   = tape.endCell;

        // Tâm ô bắt đầu và kết thúc (tọa độ pixel) — flip Y
        float startY = gridRect.y + (m_GridSize.y - 1 - start.y + 0.5f) * m_CellSize;
        float endY   = gridRect.y + (m_GridSize.y - 1 - end.y   + 0.5f) * m_CellSize;

        Vector3 startCenter = new Vector3(
            gridRect.x + (start.x + 0.5f) * m_CellSize, startY, 0);
        Vector3 endCenter = new Vector3(
            gridRect.x + (end.x + 0.5f) * m_CellSize, endY, 0);

        // Hướng dọc theo tape và vector vuông góc (để tạo bề rộng)
        Vector3 dir  = (endCenter - startCenter).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0) * m_CellSize * 0.4f;

        // 4 góc của dải băng liền mạch (solid quad)
        Vector3[] corners = new Vector3[]
        {
            startCenter + perp,
            startCenter - perp,
            endCenter   - perp,
            endCenter   + perp
        };

        Color tapeColor = GetLayerColor(tape.layer);
        Handles.DrawSolidRectangleWithOutline(corners, tapeColor, Color.white);

        // Vẽ số Layer ở giữa dải băng
        if (m_CellSize >= 28f)
        {
            Vector3 mid = (startCenter + endCenter) * 0.5f;
            Rect midRect = new Rect(
                mid.x - m_CellSize * 0.5f,
                mid.y - m_CellSize * 0.5f,
                m_CellSize, m_CellSize);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = Mathf.RoundToInt(m_CellSize * 0.38f),
                alignment = TextAnchor.MiddleCenter
            };

            // Đổ bóng (đen mờ)
            labelStyle.normal.textColor = new Color(0, 0, 0, 0.45f);
            EditorGUI.LabelField(new Rect(midRect.x + 1, midRect.y + 1, midRect.width, midRect.height),
                                 tape.layer.ToString(), labelStyle);

            // Số trắng
            labelStyle.normal.textColor = Color.white;
            EditorGUI.LabelField(midRect, tape.layer.ToString(), labelStyle);
        }
    }

    // =============================================================
    //  VẼ DRAG PREVIEW
    //  Khi kéo chuột, highlight các ô Tape sắp được tạo.
    // =============================================================
    // =============================================================
    //  VẼ DRAG PREVIEW — DẢI BĂNG LIỀN MẠCH
    //  Khi kéo chuột, vẽ một dải băng xanh từ startCell đến
    //  vị trí chuột hiện tại để preview.
    // =============================================================
    private void DrawDragPreview(Rect gridRect)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (m_DragStart == m_DragCurrent) return;

        Vector2Int start = m_DragStart;
        Vector2Int end   = m_DragCurrent;

        float startY = gridRect.y + (m_GridSize.y - 1 - start.y + 0.5f) * m_CellSize;
        float endY   = gridRect.y + (m_GridSize.y - 1 - end.y   + 0.5f) * m_CellSize;

        Vector3 startCenter = new Vector3(
            gridRect.x + (start.x + 0.5f) * m_CellSize, startY, 0);
        Vector3 endCenter = new Vector3(
            gridRect.x + (end.x + 0.5f) * m_CellSize, endY, 0);

        Vector3 dir  = (endCenter - startCenter).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0) * m_CellSize * 0.4f;

        Vector3[] corners = new Vector3[]
        {
            startCenter + perp,
            startCenter - perp,
            endCenter   - perp,
            endCenter   + perp
        };

        Handles.DrawSolidRectangleWithOutline(corners, s_PreviewColor, s_PreviewBorderColor);
    }

    // =============================================================
    //  XỬ LÝ SỰ KIỆN CHUỘT TRÊN GRID
    //
    //  MouseDown : Click chuột trái → ghi nhận startCell
    //  MouseDrag : Kéo chuột → cập nhật ô hiện tại, Repaint() preview
    //  MouseUp   : Thả chuột → tạo Tape mới nếu start ≠ end
    //
    //  MouseUp được xử lý NGAY CẢ KHI CHUỘT NGOÀI GRID
    //  (trường hợp người dùng kéo ra ngoài rồi thả), nhưng chỉ tạo
    //  Tape nếu thả chuột ở trong Grid.
    //
    //  gridRect là Rect của Grid trong tọa độ cửa sổ.
    //  Event.current.mousePosition là tọa độ chuột trong cửa sổ.
    // =============================================================
    private void HandleGridMouseEvents(Rect gridRect)
    {
        Event e = Event.current;
        if (e.button != 0) return;

        Vector2Int? cell = MousePosToCell(e.mousePosition, gridRect);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (cell.HasValue)
                {
                    m_DragStart   = cell.Value;
                    m_DragCurrent = cell.Value;
                    m_IsDragging  = true;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (m_IsDragging && cell.HasValue)
                {
                    m_DragCurrent = cell.Value;
                    // Repaint() để vẽ Preview mượt mà
                    Repaint();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                // Xử lý MouseUp kể cả khi chuột ngoài Grid
                if (m_IsDragging)
                {
                    // Chỉ tạo Tape nếu thả chuột trong Grid và có kéo
                    if (cell.HasValue && m_DragStart != cell.Value)
                    {
                        m_DragCurrent = cell.Value;
                        CreateNewTape(m_DragStart, m_DragCurrent);
                    }

                    m_IsDragging = false;
                    Repaint();
                    e.Use();
                }
                break;
        }
    }

    // =============================================================
    //  CHUYỂN TỌA ĐỘ CHUỘT (PIXEL) → Ô GRID (COL, ROW)
    //
    //  mousePos: tọa độ chuột trong cửa sổ (Event.mousePosition)
    //  gridRect: Rect của Grid trong tọa độ cửa sổ
    //  Trả về null nếu chuột nằm ngoài Grid.
    // =============================================================
    private Vector2Int? MousePosToCell(Vector2 mousePos, Rect gridRect)
    {
        float localX = mousePos.x - gridRect.x;
        float localY = mousePos.y - gridRect.y;

        if (localX < 0 || localY < 0) return null;

        int col = (int)(localX / m_CellSize);
        int row = m_GridSize.y - 1 - (int)(localY / m_CellSize);

        if (col < m_GridSize.x && row >= 0 && row < m_GridSize.y)
            return new Vector2Int(col, row);

        return null;
    }

    // =============================================================
    //  THUẬT TOÁN BRESENHAM — SINH CÁC Ô TRÊN ĐƯỜNG THẲNG
    //
    //  Dùng để xác định danh sách ô (col, row) mà đường thẳng
    //  từ startCell đến endCell đi qua trên lưới Grid.
    //
    //  Input : start, end — tọa độ ô bắt đầu / kết thúc
    //  Output: List<Vector2Int> — các ô thuộc đường thẳng
    // =============================================================
    private List<Vector2Int> GetLineCells(Vector2Int start, Vector2Int end)
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
            if (e2 <= dx)  { err += dx; y += sy; }
        }

        return cells;
    }

    // =============================================================
    //  TẠO TAPE MỚI — TÍNH LAYER TỰ ĐỘNG DỰA TRÊN OVERLAP
    //
    //  Quy tắc tính Layer tự động:
    //    1. Sinh danh sách ô của Tape mới (Bresenham).
    //    2. Với mỗi Tape cũ, kiểm tra giao cắt (cùng ô).
    //    3. Nếu có giao cắt → ghi nhận layer của Tape đó.
    //    4. Layer mới = max(layer bị đè) + 1; nếu không đè ai → layer = 0.
    //
    //  Người dùng có thể chỉnh sửa Layer thủ công sau đó qua danh sách.
    // =============================================================
    private void CreateNewTape(Vector2Int start, Vector2Int end)
    {
        // Clamp trong phạm vi Grid
        start.x = Mathf.Clamp(start.x, 0, m_GridSize.x - 1);
        start.y = Mathf.Clamp(start.y, 0, m_GridSize.y - 1);
        end.x   = Mathf.Clamp(end.x,   0, m_GridSize.x - 1);
        end.y   = Mathf.Clamp(end.y,   0, m_GridSize.y - 1);

        List<Vector2Int> newCells = GetLineCells(start, end);
        if (newCells.Count < 2) return; // Cần ít nhất 2 ô

        int autoLayer = CalculateLayer(newCells);

        Tape tape = new Tape(m_NextId++, start, end, autoLayer);
        m_Tapes.Add(tape);

        Debug.Log($"[TapeEditor] Đã tạo Tape #{tape.id}: " +
                  $"({start.x},{start.y}) → ({end.x},{end.y}), Layer={autoLayer}");
    }

    /// <summary>
    /// Tính layer tự động dựa trên overlap với các Tape hiện có.
    ///
    /// Ý tưởng:
    ///   1. Duyệt từng Tape cũ, sinh danh sách ô của nó (Bresenham).
    ///   2. Dùng HashSet để kiểm tra giao cắt ô nhanh.
    ///   3. Nếu không chung ô, kiểm tra giao cắt hình học (segment intersection)
    ///      giữa 2 tape: nếu đường thẳng AB và CD cắt nhau → overlap.
    ///   4. Tìm maxLayer của các Tape bị đè → layer mới = maxLayer + 1.
    /// </summary>
    private int CalculateLayer(List<Vector2Int> newCells)
    {
        if (m_Tapes.Count == 0) return 0;

        Vector2Int newStart = newCells[0];
        Vector2Int newEnd   = newCells[newCells.Count - 1];
        HashSet<Vector2Int> newSet = new HashSet<Vector2Int>(newCells);
        int maxOverlapLayer = -1;

        foreach (Tape tape in m_Tapes)
        {
            bool hasOverlap = false;

            // Bước 1: kiểm tra bằng Bresenham (chung ô)
            List<Vector2Int> existCells = GetLineCells(tape.startCell, tape.endCell);
            foreach (Vector2Int c in existCells)
            {
                if (newSet.Contains(c))
                {
                    hasOverlap = true;
                    break;
                }
            }

            // Bước 2: nếu không chung ô, kiểm tra giao cắt hình học (segment intersection)
            if (!hasOverlap)
            {
                hasOverlap = LinesIntersect(
                    tape.startCell, tape.endCell,
                    newStart, newEnd
                );
            }

            if (hasOverlap)
            {
                if (tape.layer > maxOverlapLayer)
                    maxOverlapLayer = tape.layer;
            }
        }

        return maxOverlapLayer >= 0 ? maxOverlapLayer + 1 : 0;
    }

    /// <summary>
    /// Kiểm tra 2 đoạn thẳng AB và CD có giao cắt nhau hay không
    /// bằng phương pháp cross product (tích có hướng).
    ///
    /// Công thức:
    ///   cross(A, B, C) = (B - A) × (C - A)
    ///   Nếu (cross(A,B,C) * cross(A,B,D) < 0) và (cross(C,D,A) * cross(C,D,B) < 0)
    ///   → hai đoạn thẳng cắt nhau.
    ///
    /// Trường hợp collinear (cùng đường thẳng): kiểm tra overlap bằng projection.
    /// Tọa độ dùng cell center (x + 0.5, y + 0.5) để chính xác với mọi góc.
    /// </summary>
    private bool LinesIntersect(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
    {
        Vector2 A = new Vector2(a.x + 0.5f, a.y + 0.5f);
        Vector2 B = new Vector2(b.x + 0.5f, b.y + 0.5f);
        Vector2 C = new Vector2(c.x + 0.5f, c.y + 0.5f);
        Vector2 D = new Vector2(d.x + 0.5f, d.y + 0.5f);

        float crossC = Cross(A, B, C);
        float crossD = Cross(A, B, D);
        float crossA = Cross(C, D, A);
        float crossB = Cross(C, D, B);

        // Proper intersection (X shape)
        if (crossC * crossD < 0 && crossA * crossB < 0)
            return true;

        // Collinear: kiểm tra đoạn thẳng overlap
        if (Mathf.Approximately(crossC, 0) && Mathf.Approximately(crossD, 0) &&
            Mathf.Approximately(crossA, 0) && Mathf.Approximately(crossB, 0))
        {
            return CollinearOverlap(A, B, C, D);
        }

        return false;
    }

    private float Cross(Vector2 A, Vector2 B, Vector2 C)
    {
        return (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x);
    }

    /// <summary> Kiểm tra 2 đoạn thẳng cùng phương có giao nhau không. </summary>
    private bool CollinearOverlap(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
    {
        float dotAB = Vector2.Dot(B - A, B - A);
        if (dotAB == 0) return false;

        float tC = Vector2.Dot(C - A, B - A) / dotAB;
        float tD = Vector2.Dot(D - A, B - A) / dotAB;

        float tMin = Mathf.Min(tC, tD);
        float tMax = Mathf.Max(tC, tD);

        return tMin <= 1f && tMax >= 0f;
    }

    // =============================================================
    //  LẤY MÀU THEO LAYER
    //  Mỗi Layer có màu cố định, dễ phân biệt thứ tự chồng lớp.
    //  Với Layer > 8, dùng HSV pha tự động.
    // =============================================================
    private Color GetLayerColor(int layer)
    {
        if (layer >= 0 && layer < s_LayerColors.Length)
        {
            Color c = s_LayerColors[layer];
            c.a = 0.55f;
            return c;
        }

        // Phía ngoài dải màu: pha màu theo tỷ lệ vàng (golden ratio)
        float hue = (layer * 0.382f) % 1.0f;
        Color auto = Color.HSVToRGB(hue, 0.6f, 0.9f);
        auto.a = 0.55f;
        return auto;
    }

    // =============================================================
    //  PHẦN 3: KHU VỰC PHÍA DƯỚI
    //  Danh sách Tape (ReorderableList) — Save/Clear đã chuyển
    //  lên toolbar Settings.
    // =============================================================
    private void DrawBottomSection()
    {
        if (!m_GridGenerated) return;

        GUILayout.Space(4);
        EditorGUILayout.BeginVertical();

        // Header
        EditorGUILayout.LabelField("Danh sách Tape", EditorStyles.boldLabel);

        // ReorderableList với scroll khi danh sách dài
        if (m_TapeList != null && m_Tapes != null)
        {
            float maxListHeight = Mathf.Min(
                position.height * 0.35f,
                m_Tapes.Count * (m_TapeList.elementHeight + 2f) + 30f
            );

            m_ListScrollPosition = EditorGUILayout.BeginScrollView(
                m_ListScrollPosition,
                GUILayout.MaxHeight(maxListHeight),
                GUILayout.ExpandHeight(false)
            );
            m_TapeList.DoLayoutList();
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(4);
        EditorGUILayout.EndVertical();
    }

    /// <summary> Ước lượng chiều cao bottom section để tính cell size. </summary>
    private float EstimateBottomHeight()
    {
        float headerH = 20f;
        float listH = 20f + Mathf.Max(1, m_Tapes != null ? m_Tapes.Count : 0) * 27f;
        float padding = 16f;
        return Mathf.Min(headerH + listH + padding, 350f);
    }

    // =============================================================
    //  LƯU LEVEL — TẠO SCRIPTABLEOBJECT .asset
    //  Lưu một bản sao dữ liệu hiện tại ra file asset trong Project.
    // =============================================================
    private void SaveLevel()
    {
        if (m_Tapes.Count == 0)
        {
            EditorUtility.DisplayDialog("Lưu Level",
                "Không có Tape nào để lưu. Hãy tạo ít nhất một Tape.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level", "NewLevel", "asset",
            "Chọn nơi lưu Level Data"
        );

        if (string.IsNullOrEmpty(path)) return;

        // Tạo bản sao dữ liệu để lưu (không ảnh hưởng dữ liệu đang chỉnh sửa)
        TapeLevelData saveData = ScriptableObject.CreateInstance<TapeLevelData>();
        saveData.gridSize = m_GridSize;
        saveData.tapes = new List<Tape>();
        foreach (Tape t in m_Tapes)
        {
            saveData.tapes.Add(new Tape(t.id, t.startCell, t.endCell, t.layer));
        }

        AssetDatabase.CreateAsset(saveData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Lưu Level",
            $"Đã lưu Level thành công tại:\n{path}", "OK");

        Debug.Log($"[TapeEditor] Đã lưu Level tại: {path}");
    }

    // =============================================================
    //  XÓA TẤT CẢ
    //  Xóa toàn bộ Tape, reset trạng thái về ban đầu.
    // =============================================================
    private void ClearAll()
    {
        if (m_Tapes.Count > 0)
        {
            bool confirm = EditorUtility.DisplayDialog("Xóa tất cả",
                "Bạn có chắc muốn xóa tất cả dữ liệu? Hành động này không thể hoàn tác.",
                "Xóa", "Hủy");
            if (!confirm) return;
        }

        m_Tapes.Clear();
        m_NextId = 1;
        m_IsDragging = false;
        Repaint();

        Debug.Log("[TapeEditor] Đã xóa toàn bộ dữ liệu.");
    }

    // =============================================================
    //  LOAD LEVEL — NẠP DỮ LIỆU TỪ FILE .asset VÀO EDITOR
    // =============================================================
    private void LoadFromAsset()
    {
        if (m_LoadAsset == null) return;

        m_GridSize = m_LoadAsset.gridSize;
        m_GridGenerated = true;

        m_LevelData.gridSize = m_GridSize;
        m_Tapes.Clear();

        int maxId = 0;
        foreach (Tape t in m_LoadAsset.tapes)
        {
            m_Tapes.Add(new Tape(t.id, t.startCell, t.endCell, t.layer));
            if (t.id > maxId) maxId = t.id;
        }
        m_NextId = maxId + 1;
        m_IsDragging = false;

        RecalculateCellSize();
        Repaint();

        Debug.Log($"[TapeEditor] Đã load Level: {AssetDatabase.GetAssetPath(m_LoadAsset)}");
    }
}
