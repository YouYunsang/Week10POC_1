using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jungle.Puzzle
{
    [CreateAssetMenu(
        fileName = "PuzzleBoardData",
        menuName = "Jungle/Puzzle/Puzzle Board Data"
    )]
    public class PuzzleBoardData : ScriptableObject
    {
        private const int MIN_BOARD_SIZE = 1; // 최소 보드 크기.

        [SerializeField] private int _width = MIN_BOARD_SIZE;
        [SerializeField] private int _height = MIN_BOARD_SIZE;
        [SerializeField] private List<PuzzleTileData> _tiles = new();

        public event Action<PuzzleBoardData> Changed;

        public int Width => _width;
        public int Height => _height;
        public IReadOnlyList<PuzzleTileData> Tiles => _tiles;

        public void SetSize(int width, int height)
        {
            int newWidth = Mathf.Max(MIN_BOARD_SIZE, width); // 최소 1 보장.
            int newHeight = Mathf.Max(MIN_BOARD_SIZE, height); // 최소 1 보장.

            bool sizeChanged = _width != newWidth || _height != newHeight;

            _width = newWidth;
            _height = newHeight;

            bool removedOutOfRangeTiles = RemoveOutOfRangeTiles();

            if (sizeChanged || removedOutOfRangeTiles)
            {
                NotifyChanged(); // 크기 변경 또는 타일 제거 발생 시 알림.
            }
        }

        public bool HasTile(Vector2Int position)
        {
            return FindTile(position) != null; // 해당 좌표에 타일이 있는지 확인.
        }

        public PuzzleTileType GetTileType(Vector2Int position)
        {
            PuzzleTileData tileData = FindTile(position);

            if (tileData == null)
            {
                return default;
            }

            return tileData.TileType;
        }

        public void SetTile(Vector2Int position, PuzzleTileType tileType)
        {
            if (IsOutOfRange(position))
            {
                return; // 보드 범위 밖 좌표는 무시.
            }

            PuzzleTileData tileData = FindTile(position);

            if (tileData == null)
            {
                _tiles.Add(new PuzzleTileData(position, tileType)); // 새 타일 추가.
                NotifyChanged();
                return;
            }

            if (tileData.TileType == tileType)
            {
                return; // 같은 타입으로 다시 칠하는 경우 변경 없음.
            }

            tileData.SetTileType(tileType); // 기존 타일 타입 갱신.
            NotifyChanged();
        }

        public void RemoveTile(Vector2Int position)
        {
            PuzzleTileData tileData = FindTile(position);

            if (tileData == null)
            {
                return;
            }

            _tiles.Remove(tileData); // 해당 좌표의 타일 제거.
            NotifyChanged();
        }

        public void Clear()
        {
            if (_tiles.Count == 0)
            {
                return;
            }

            _tiles.Clear(); // 모든 타일 제거.
            NotifyChanged();
        }

        public bool IsOutOfRange(Vector2Int position)
        {
            return position.x < 0
                   || position.y < 0
                   || position.x >= _width
                   || position.y >= _height;
        }

        private PuzzleTileData FindTile(Vector2Int position)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i].Position == position)
                {
                    return _tiles[i]; // 같은 좌표의 타일 반환.
                }
            }

            return null;
        }

        private bool RemoveOutOfRangeTiles()
        {
            bool removed = false;

            for (int i = _tiles.Count - 1; i >= 0; i--)
            {
                if (IsOutOfRange(_tiles[i].Position))
                {
                    _tiles.RemoveAt(i); // 보드 밖으로 밀려난 타일 제거.
                    removed = true;
                }
            }

            return removed;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this); // 이 데이터를 참조 중인 에디터/스포너에 변경 알림.
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            int newWidth = Mathf.Max(MIN_BOARD_SIZE, _width);
            int newHeight = Mathf.Max(MIN_BOARD_SIZE, _height);

            bool changed = _width != newWidth || _height != newHeight;

            _width = newWidth;
            _height = newHeight;

            bool removedOutOfRangeTiles = RemoveOutOfRangeTiles();

            if (changed || removedOutOfRangeTiles)
            {
                NotifyChanged(); // Inspector에서 직접 수정한 경우도 알림.
            }
        }
#endif
    }
}