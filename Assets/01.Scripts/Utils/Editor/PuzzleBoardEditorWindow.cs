#if UNITY_EDITOR

using Jungle.Puzzle;
using UnityEditor;
using UnityEngine;

namespace Jungle.PuzzleEditor
{
    public class PuzzleBoardEditorWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Puzzle Board Editor";
        private const float CELL_SIZE = 1f;
        private const float GRID_LINE_THICKNESS = 1f;
        private const int LARGE_BOARD_WARNING_AREA = 10000;

        private static readonly Color GridColor = new(1f, 1f, 1f, 0.25f);
        private static readonly Color FilledTileColor = new(0.25f, 0.65f, 1f, 0.45f);
        private static readonly Color FilledTileOutlineColor = new(0.25f, 0.75f, 1f, 1f);
        private static readonly Color BoardOutlineColor = new(1f, 1f, 1f, 0.8f);

        [SerializeField] private PuzzleBoardData _selectedBoardData;
        [SerializeField] private PuzzleTileType _selectedBrushType = PuzzleTileType.Normal;
        [SerializeField] private Vector2 _scrollPosition;

        private int _editingWidth = 1;
        private int _editingHeight = 1;
        private Vector2Int _lastPaintedPosition = new(int.MinValue, int.MinValue);

        [MenuItem("Tools/Jungle/Puzzle Board Editor")]
        private static void Open()
        {
            PuzzleBoardEditorWindow window = GetWindow<PuzzleBoardEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGui; // Scene View 입력과 미리보기 연결.
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui; // EditorWindow 종료 시 이벤트 해제.
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawBoardSelectionSection();
            EditorGUILayout.Space(10f);

            if (_selectedBoardData != null)
            {
                DrawBoardSizeSection();
                EditorGUILayout.Space(10f);

                DrawBrushSection();
                EditorGUILayout.Space(10f);

                DrawCommandSection();
                EditorGUILayout.Space(10f);

                DrawInfoSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBoardSelectionSection()
        {
            EditorGUILayout.LabelField("1. Board Data", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _selectedBoardData = (PuzzleBoardData)EditorGUILayout.ObjectField(
                "Selected Board",
                _selectedBoardData,
                typeof(PuzzleBoardData),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                SyncEditingSizeFromData(); // 선택 데이터 변경 시 크기 입력값 동기화.
                SceneView.RepaintAll();
            }

            if (_selectedBoardData == null)
            {
                EditorGUILayout.HelpBox(
                    "PuzzleBoardData 에셋을 선택하세요. 없으면 Project 창에서 Create > Jungle > Puzzle > Puzzle Board Data로 생성하세요.",
                    MessageType.Info
                );
            }
        }

        private void DrawBoardSizeSection()
        {
            EditorGUILayout.LabelField("2. Board Size", EditorStyles.boldLabel);

            _editingWidth = EditorGUILayout.IntField("Width", _editingWidth);
            _editingHeight = EditorGUILayout.IntField("Height", _editingHeight);

            _editingWidth = Mathf.Max(1, _editingWidth); // 최소 1 보장.
            _editingHeight = Mathf.Max(1, _editingHeight); // 최소 1 보장.

            int boardArea = _editingWidth * _editingHeight;

            if (boardArea > LARGE_BOARD_WARNING_AREA)
            {
                EditorGUILayout.HelpBox(
                    "보드 면적이 큽니다. 편집은 가능하지만 Scene View 표시 성능이 떨어질 수 있습니다.",
                    MessageType.Warning
                );
            }

            if (GUILayout.Button("Apply Size"))
            {
                ApplyBoardSize();
            }
        }

        private void DrawBrushSection()
        {
            EditorGUILayout.LabelField("3. Brush", EditorStyles.boldLabel);

            _selectedBrushType = (PuzzleTileType)EditorGUILayout.EnumPopup(
                "Brush Type",
                _selectedBrushType
            );

            EditorGUILayout.HelpBox(
                "Scene View에서 좌클릭 드래그: 칠하기 / 우클릭 드래그: 삭제",
                MessageType.None
            );
        }

        private void DrawCommandSection()
        {
            EditorGUILayout.LabelField("4. Commands", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Frame Scene View"))
                {
                    FrameSceneView();
                }

                if (GUILayout.Button("Clear Board"))
                {
                    ClearBoard();
                }
            }
        }

        private void DrawInfoSection()
        {
            EditorGUILayout.LabelField("5. Info", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Width", _selectedBoardData.Width.ToString());
            EditorGUILayout.LabelField("Height", _selectedBoardData.Height.ToString());
            EditorGUILayout.LabelField("Tile Count", _selectedBoardData.Tiles.Count.ToString());
            EditorGUILayout.LabelField("Origin", "Left Bottom (0, 0)");
        }

        private void OnSceneGui(SceneView sceneView)
        {
            if (_selectedBoardData == null)
            {
                return;
            }

            Event currentEvent = Event.current;

            DrawScenePreview();
            HandleSceneInput(currentEvent);

            if (currentEvent.type == EventType.MouseUp)
            {
                _lastPaintedPosition = new Vector2Int(int.MinValue, int.MinValue); // 드래그 종료 시 중복 방지 좌표 초기화.
            }
        }

        private void DrawScenePreview()
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            DrawBoardGrid();
            DrawFilledTiles();
            DrawBoardOutline();
        }

        private void DrawBoardGrid()
        {
            Handles.color = GridColor;

            for (int x = 0; x <= _selectedBoardData.Width; x++)
            {
                Vector3 start = new(x * CELL_SIZE, 0f, 0f);
                Vector3 end = new(x * CELL_SIZE, _selectedBoardData.Height * CELL_SIZE, 0f);

                Handles.DrawLine(start, end, GRID_LINE_THICKNESS); // 세로 그리드 라인.
            }

            for (int y = 0; y <= _selectedBoardData.Height; y++)
            {
                Vector3 start = new(0f, y * CELL_SIZE, 0f);
                Vector3 end = new(_selectedBoardData.Width * CELL_SIZE, y * CELL_SIZE, 0f);

                Handles.DrawLine(start, end, GRID_LINE_THICKNESS); // 가로 그리드 라인.
            }
        }

        private void DrawFilledTiles()
        {
            for (int i = 0; i < _selectedBoardData.Tiles.Count; i++)
            {
                PuzzleTileData tileData = _selectedBoardData.Tiles[i];
                Vector2Int position = tileData.Position;

                Vector3 center = new(
                    position.x * CELL_SIZE + CELL_SIZE * 0.5f,
                    position.y * CELL_SIZE + CELL_SIZE * 0.5f,
                    0f
                );

                Vector3 size = new(CELL_SIZE, CELL_SIZE, 0f);

                Handles.DrawSolidRectangleWithOutline(
                    GetCellCorners(center, size),
                    FilledTileColor,
                    FilledTileOutlineColor
                ); // 존재하는 타일을 반투명 사각형으로 표시.
            }
        }

        private void DrawBoardOutline()
        {
            Handles.color = BoardOutlineColor;

            Vector3 bottomLeft = Vector3.zero;
            Vector3 bottomRight = new(_selectedBoardData.Width * CELL_SIZE, 0f, 0f);
            Vector3 topRight = new(_selectedBoardData.Width * CELL_SIZE, _selectedBoardData.Height * CELL_SIZE, 0f);
            Vector3 topLeft = new(0f, _selectedBoardData.Height * CELL_SIZE, 0f);

            Handles.DrawAAPolyLine(3f, bottomLeft, bottomRight, topRight, topLeft, bottomLeft); // 보드 외곽선.
        }

        private void HandleSceneInput(Event currentEvent)
        {
            if (currentEvent.alt)
            {
                return; // Alt 누른 상태에서는 Scene View 카메라 조작 우선.
            }

            if (currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag)
            {
                return;
            }

            if (currentEvent.button != 0 && currentEvent.button != 1)
            {
                return;
            }

            Vector2Int gridPosition = GetGridPositionFromMouse(currentEvent.mousePosition);

            if (_selectedBoardData.IsOutOfRange(gridPosition))
            {
                return;
            }

            if (_lastPaintedPosition == gridPosition)
            {
                currentEvent.Use();
                return; // 같은 칸에 대한 반복 Undo 기록 방지.
            }

            if (currentEvent.button == 0)
            {
                PaintTile(gridPosition);
            }
            else if (currentEvent.button == 1)
            {
                EraseTile(gridPosition);
            }

            _lastPaintedPosition = gridPosition;
            currentEvent.Use();
        }

        private Vector2Int GetGridPositionFromMouse(Vector2 mousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Vector3 worldPosition = ray.origin;

            int x = Mathf.FloorToInt(worldPosition.x / CELL_SIZE);
            int y = Mathf.FloorToInt(worldPosition.y / CELL_SIZE);

            return new Vector2Int(x, y); // 왼쪽 아래 기준 좌표로 변환.
        }

        private void PaintTile(Vector2Int position)
        {
            Undo.RecordObject(_selectedBoardData, "Paint Puzzle Tile");

            _selectedBoardData.SetTile(position, _selectedBrushType); // 선택 브러시 타입으로 칠하기.

            MarkBoardDirty();
        }

        private void EraseTile(Vector2Int position)
        {
            Undo.RecordObject(_selectedBoardData, "Erase Puzzle Tile");

            _selectedBoardData.RemoveTile(position); // 해당 칸 삭제.

            MarkBoardDirty();
        }

        private void ApplyBoardSize()
        {
            Undo.RecordObject(_selectedBoardData, "Resize Puzzle Board");

            _selectedBoardData.SetSize(_editingWidth, _editingHeight); // 보드 크기 적용.

            MarkBoardDirty();
        }

        private void ClearBoard()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Board",
                "모든 타일을 삭제할까요?",
                "Clear",
                "Cancel"
            );

            if (!confirmed)
            {
                return;
            }

            Undo.RecordObject(_selectedBoardData, "Clear Puzzle Board");

            _selectedBoardData.Clear(); // 전체 타일 삭제.

            MarkBoardDirty();
        }

        private void SyncEditingSizeFromData()
        {
            if (_selectedBoardData == null)
            {
                _editingWidth = 1;
                _editingHeight = 1;
                return;
            }

            _editingWidth = _selectedBoardData.Width;
            _editingHeight = _selectedBoardData.Height;
        }

        private void MarkBoardDirty()
        {
            EditorUtility.SetDirty(_selectedBoardData); // 에셋 변경 사항 저장 대상으로 표시.
            Repaint();
            SceneView.RepaintAll();
        }

        private void FrameSceneView()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                return;
            }

            Bounds bounds = new(
                new Vector3(
                    _selectedBoardData.Width * CELL_SIZE * 0.5f,
                    _selectedBoardData.Height * CELL_SIZE * 0.5f,
                    0f
                ),
                new Vector3(
                    _selectedBoardData.Width * CELL_SIZE,
                    _selectedBoardData.Height * CELL_SIZE,
                    CELL_SIZE
                )
            );

            SceneView.lastActiveSceneView.Frame(bounds, false); // 보드 전체가 보이도록 Scene View 이동.
        }

        private Vector3[] GetCellCorners(Vector3 center, Vector3 size)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            return new[]
            {
                new Vector3(center.x - halfWidth, center.y - halfHeight, 0f),
                new Vector3(center.x - halfWidth, center.y + halfHeight, 0f),
                new Vector3(center.x + halfWidth, center.y + halfHeight, 0f),
                new Vector3(center.x + halfWidth, center.y - halfHeight, 0f)
            };
        }
    }
}

#endif