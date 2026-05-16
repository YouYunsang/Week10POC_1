using UnityEngine;

namespace Jungle.Puzzle
{
    public class PuzzleTileView : MonoBehaviour
    {
        [SerializeField] private Vector2Int _position;
        [SerializeField] private PuzzleTileType _tileType;

        public Vector2Int Position => _position;
        public PuzzleTileType TileType => _tileType;

        public void Initialize(Vector2Int position, PuzzleTileType tileType)
        {
            _position = position;
            _tileType = tileType;

            gameObject.name = $"Tile_{tileType}_{position.x}_{position.y}";
        }
    }
}
