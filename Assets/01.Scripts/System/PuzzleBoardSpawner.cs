using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Jungle.Puzzle
{
    [ExecuteAlways]
    public class PuzzleBoardSpawner : MonoBehaviour
    {
        private const float CELL_CENTER_OFFSET = 0.5f; // 타일을 셀 중앙에 배치하기 위한 보정값.
        private const string PREVIEW_ROOT_NAME = "PreviewRoot"; // 자동 생성할 프리뷰 루트 이름.

        [SerializeField] private PuzzleBoardData _boardData; // 생성할 퍼즐판 데이터.
        [SerializeField] private PuzzleTilePrefabTable _prefabTable; // 타일 타입별 프리팹 테이블.
        [SerializeField] private Transform _tileRoot; // 생성된 타일들을 담을 부모 Transform.
        [SerializeField] private bool _spawnOnStart = true; // Start 시 자동 생성 여부.
        [SerializeField] private bool _autoRefreshPreview = true; // 보드 데이터 변경 시 에디터 프리뷰 자동 갱신 여부.

        private Transform _cachedTransform;

        public PuzzleBoardData BoardData => _boardData;
        public PuzzleTilePrefabTable PrefabTable => _prefabTable;
        public Transform TileRoot => _tileRoot != null ? _tileRoot : transform;
        public bool AutoRefreshPreview => _autoRefreshPreview;

        private Transform CachedTransform => _cachedTransform != null ? _cachedTransform : transform;

#if UNITY_EDITOR
        private PuzzleBoardData _subscribedBoardData;
        private bool _hasPendingPreviewRefresh;
#endif

        private void Awake()
        {
            _cachedTransform = transform; // 자기 Transform 캐싱.
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureRuntimeTileRoot();

            if (_spawnOnStart)
            {
                SpawnBoard();
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            SubscribeToBoardChanges(); // Edit Mode에서도 BoardData 변경 감지.
        }

        private void OnDisable()
        {
            UnsubscribeFromBoardChanges(); // 이벤트 구독 해제.
            EditorApplication.delayCall -= RefreshEditorPreviewDelayed;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            SubscribeToBoardChanges();
            RequestEditorPreviewRefresh(); // Inspector에서 값이 바뀐 경우 갱신 예약.
        }
#endif

        public void SetBoardData(PuzzleBoardData boardData)
        {
#if UNITY_EDITOR
            UnsubscribeFromBoardChanges();
#endif

            _boardData = boardData; // 외부에서 보드 데이터 교체 가능.

#if UNITY_EDITOR
            SubscribeToBoardChanges();
            RequestEditorPreviewRefresh();
#endif
        }

        public void SpawnBoard()
        {
            if (!CanSpawn())
            {
                return;
            }

            EnsureRuntimeTileRoot();
            ClearSpawnedTiles();

            for (int i = 0; i < _boardData.Tiles.Count; i++)
            {
                SpawnTile(_boardData.Tiles[i]); // 보드 데이터에 존재하는 타일만 생성.
            }
        }

        public void ClearSpawnedTiles()
        {
            Transform root = TileRoot;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject childObject = root.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(childObject); // Play Mode에서는 일반 Destroy 사용.
                }
                else
                {
                    DestroyImmediate(childObject); // Edit Mode에서는 즉시 삭제.
                }
            }
        }

        private void SpawnTile(PuzzleTileData tileData)
        {
            GameObject prefab = _prefabTable.GetPrefab(tileData.TileType);

            if (prefab == null)
            {
                Debug.LogWarning($"타일 타입 {tileData.TileType}에 사용할 프리팹이 없습니다.", this);
                return;
            }

            Vector3 spawnPosition = GetWorldPosition(tileData.Position);

            GameObject tileObject = Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity,
                TileRoot
            ); // 타일 프리팹 생성.

            InitializeTileObject(tileObject, tileData);
        }

        private void InitializeTileObject(GameObject tileObject, PuzzleTileData tileData)
        {
            if (tileObject.TryGetComponent(out PuzzleTileView tileView))
            {
                tileView.Initialize(tileData.Position, tileData.TileType); // 생성된 타일에 데이터 주입.
            }
        }

        private Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            float cellSize = _prefabTable.CellSize;

            float x = (gridPosition.x + CELL_CENTER_OFFSET) * cellSize;
            float y = (gridPosition.y + CELL_CENTER_OFFSET) * cellSize;

            return CachedTransform.position + new Vector3(x, y, 0f); // 왼쪽 아래 기준 좌표를 월드 좌표로 변환.
        }

        private bool CanSpawn()
        {
            if (_boardData == null)
            {
                Debug.LogWarning("PuzzleBoardData가 할당되지 않았습니다.", this);
                return false;
            }

            if (_prefabTable == null)
            {
                Debug.LogWarning("PuzzleTilePrefabTable이 할당되지 않았습니다.", this);
                return false;
            }

            return true;
        }

        private void EnsureRuntimeTileRoot()
        {
            if (_tileRoot != null)
            {
                return;
            }

            Transform existingRoot = CachedTransform.Find(PREVIEW_ROOT_NAME);

            if (existingRoot != null)
            {
                _tileRoot = existingRoot; // 이미 PreviewRoot가 있으면 재사용.
                return;
            }

            GameObject rootObject = new(PREVIEW_ROOT_NAME);
            rootObject.transform.SetParent(CachedTransform);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            _tileRoot = rootObject.transform; // 런타임용 루트 자동 생성.
        }

#if UNITY_EDITOR
        public void SpawnBoardEditorPreview()
        {
            if (Application.isPlaying)
            {
                SpawnBoard(); // Play Mode에서는 런타임 생성 로직 사용.
                return;
            }

            if (!CanSpawn())
            {
                return;
            }

            EnsureEditorPreviewRoot();
            RefreshPreviewIncremental();

            EditorUtility.SetDirty(this);
        }

        public void ClearSpawnedTilesEditorPreview()
        {
            EnsureEditorPreviewRoot();
            ClearPreviewTilesOnly();

            EditorUtility.SetDirty(this);
        }

        private void SubscribeToBoardChanges()
        {
            if (_subscribedBoardData == _boardData)
            {
                return;
            }

            UnsubscribeFromBoardChanges();

            _subscribedBoardData = _boardData;

            if (_subscribedBoardData != null)
            {
                _subscribedBoardData.Changed += OnBoardDataChanged; // 현재 BoardData 변경 이벤트 구독.
            }
        }

        private void UnsubscribeFromBoardChanges()
        {
            if (_subscribedBoardData == null)
            {
                return;
            }

            _subscribedBoardData.Changed -= OnBoardDataChanged; // 이전 BoardData 이벤트 구독 해제.
            _subscribedBoardData = null;
        }

        private void OnBoardDataChanged(PuzzleBoardData changedBoardData)
        {
            if (!_autoRefreshPreview)
            {
                return;
            }

            if (changedBoardData != _boardData)
            {
                return;
            }

            RequestEditorPreviewRefresh(); // 보드 변경 시 프리뷰 갱신 예약.
        }

        private void RequestEditorPreviewRefresh()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!_autoRefreshPreview)
            {
                return;
            }

            if (_boardData == null || _prefabTable == null)
            {
                return;
            }

            if (_hasPendingPreviewRefresh)
            {
                return;
            }

            _hasPendingPreviewRefresh = true;
            EditorApplication.delayCall += RefreshEditorPreviewDelayed; // 여러 변경을 한 번의 갱신으로 묶음.
        }

        private void RefreshEditorPreviewDelayed()
        {
            EditorApplication.delayCall -= RefreshEditorPreviewDelayed;
            _hasPendingPreviewRefresh = false;

            if (this == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                return;
            }

            if (!_autoRefreshPreview)
            {
                return;
            }

            SpawnBoardEditorPreview(); // 예약된 프리뷰 자동 갱신 실행.
        }

        private void EnsureEditorPreviewRoot()
        {
            if (_tileRoot != null)
            {
                return;
            }

            Transform existingRoot = CachedTransform.Find(PREVIEW_ROOT_NAME);

            if (existingRoot != null)
            {
                Undo.RecordObject(this, "Assign Puzzle Preview Root");
                _tileRoot = existingRoot; // 이미 있는 PreviewRoot 재사용.
                EditorUtility.SetDirty(this);
                return;
            }

            GameObject rootObject = new(PREVIEW_ROOT_NAME);

            Undo.RegisterCreatedObjectUndo(rootObject, "Create Puzzle Preview Root");

            rootObject.transform.SetParent(CachedTransform);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            Undo.RecordObject(this, "Assign Puzzle Preview Root");
            _tileRoot = rootObject.transform; // Inspector의 Tile Root에 자동 할당.

            EditorUtility.SetDirty(this);
        }

        private void RefreshPreviewIncremental()
        {
            Dictionary<Vector2Int, PuzzleBoardPreviewMarker> previewMarkers = BuildPreviewMarkerMap();
            HashSet<Vector2Int> requiredPositions = new();

            for (int i = 0; i < _boardData.Tiles.Count; i++)
            {
                PuzzleTileData tileData = _boardData.Tiles[i];
                requiredPositions.Add(tileData.Position);

                if (!previewMarkers.TryGetValue(tileData.Position, out PuzzleBoardPreviewMarker marker))
                {
                    SpawnTileEditorPreview(tileData); // 없는 타일만 새로 생성.
                    continue;
                }

                if (!marker.IsSameTile(tileData.Position, tileData.TileType))
                {
                    ReplacePreviewTile(marker, tileData); // 타입이 달라진 타일만 교체.
                    continue;
                }

                SyncPreviewTileTransform(marker.transform, tileData.Position); // 위치만 보정.
            }

            RemoveUnusedPreviewTiles(previewMarkers, requiredPositions); // 데이터에서 사라진 타일 삭제.
        }

        private Dictionary<Vector2Int, PuzzleBoardPreviewMarker> BuildPreviewMarkerMap()
        {
            Dictionary<Vector2Int, PuzzleBoardPreviewMarker> markerMap = new();
            PuzzleBoardPreviewMarker[] markers = TileRoot.GetComponentsInChildren<PuzzleBoardPreviewMarker>(true);

            for (int i = 0; i < markers.Length; i++)
            {
                PuzzleBoardPreviewMarker marker = markers[i];

                if (marker == null)
                {
                    continue;
                }

                if (markerMap.ContainsKey(marker.Position))
                {
                    Undo.DestroyObjectImmediate(marker.gameObject); // 같은 좌표의 중복 프리뷰는 삭제.
                    continue;
                }

                markerMap.Add(marker.Position, marker);
            }

            return markerMap;
        }

        private void RemoveUnusedPreviewTiles(
            Dictionary<Vector2Int, PuzzleBoardPreviewMarker> previewMarkers,
            HashSet<Vector2Int> requiredPositions
        )
        {
            foreach (KeyValuePair<Vector2Int, PuzzleBoardPreviewMarker> pair in previewMarkers)
            {
                if (requiredPositions.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(pair.Value.gameObject); // 더 이상 데이터에 없는 프리뷰 삭제.
            }
        }

        private void ReplacePreviewTile(PuzzleBoardPreviewMarker marker, PuzzleTileData tileData)
        {
            if (marker != null)
            {
                Undo.DestroyObjectImmediate(marker.gameObject); // 타입이 바뀐 기존 프리뷰 삭제.
            }

            SpawnTileEditorPreview(tileData); // 새 타입 프리팹으로 다시 생성.
        }

        private void SpawnTileEditorPreview(PuzzleTileData tileData)
        {
            GameObject prefab = _prefabTable.GetPrefab(tileData.TileType);

            if (prefab == null)
            {
                Debug.LogWarning($"타일 타입 {tileData.TileType}에 사용할 프리팹이 없습니다.", this);
                return;
            }

            Vector3 spawnPosition = GetWorldPosition(tileData.Position);
            GameObject tileObject = PrefabUtility.InstantiatePrefab(prefab, TileRoot) as GameObject;

            if (tileObject == null)
            {
                Debug.LogWarning("타일 프리팹 생성에 실패했습니다.", this);
                return;
            }

            Undo.RegisterCreatedObjectUndo(tileObject, "Spawn Puzzle Tile Preview");

            tileObject.transform.position = spawnPosition; // 데이터 좌표를 월드 위치로 반영.
            tileObject.transform.rotation = Quaternion.identity; // 기본 회전값으로 생성.
            tileObject.transform.localScale = prefab.transform.localScale; // 프리팹 스케일 유지.

            InitializeTileObject(tileObject, tileData);
            AddPreviewMarker(tileObject, tileData);

            EditorUtility.SetDirty(tileObject);
        }

        private void SyncPreviewTileTransform(Transform tileTransform, Vector2Int gridPosition)
        {
            if (tileTransform == null)
            {
                return;
            }

            tileTransform.position = GetWorldPosition(gridPosition); // Spawner 위치나 CellSize 변경에 따른 위치 보정.
        }

        private void AddPreviewMarker(GameObject tileObject, PuzzleTileData tileData)
        {
            PuzzleBoardPreviewMarker marker = tileObject.GetComponent<PuzzleBoardPreviewMarker>();

            if (marker == null)
            {
                marker = Undo.AddComponent<PuzzleBoardPreviewMarker>(tileObject); // 프리뷰 식별용 Marker 추가.
            }

            marker.Initialize(tileData.Position, tileData.TileType);
            EditorUtility.SetDirty(marker);
        }

        private void ClearPreviewTilesOnly()
        {
            Transform root = TileRoot;
            PuzzleBoardPreviewMarker[] previewMarkers = root.GetComponentsInChildren<PuzzleBoardPreviewMarker>(true);

            for (int i = previewMarkers.Length - 1; i >= 0; i--)
            {
                if (previewMarkers[i] == null)
                {
                    continue;
                }

                GameObject previewObject = previewMarkers[i].gameObject;

                Undo.DestroyObjectImmediate(previewObject); // Marker가 붙은 프리뷰 오브젝트만 삭제.
            }
        }
#endif
    }
}