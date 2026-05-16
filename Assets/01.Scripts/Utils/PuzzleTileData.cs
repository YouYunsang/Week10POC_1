using UnityEngine;
using System;

namespace Jungle.Puzzle
{
    [Serializable]
    public class PuzzleTileData
    {
        [SerializeField] private Vector2Int _position;
        [SerializeField] private PuzzleTileType _tileType;

        public Vector2Int Position => _position;
        public PuzzleTileType TileType => _tileType;

        public PuzzleTileData(Vector2Int position, PuzzleTileType tileType)
        {
            _position = position;
            _tileType = tileType;
        }

        public void SetTileType(PuzzleTileType tileType)
        {
            _tileType = tileType;
        }
    }
}
