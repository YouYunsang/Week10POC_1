using UnityEngine;

namespace Jungle.Puzzle
{
    public class PuzzleBoardPreviewMarker : MonoBehaviour
    {
        [SerializeField] private Vector2Int _position; // 프리뷰 생성 당시의 보드 좌표.
        [SerializeField] private PuzzleTileType _tileType; // 프리뷰 생성 당시의 타일 타입.

        public Vector2Int Position => _position;
        public PuzzleTileType TileType => _tileType;

        public void Initialize(Vector2Int position, PuzzleTileType tileType)
        {
            _position = position; // 삭제/디버깅용 좌표 기록.
            _tileType = tileType; // 삭제/디버깅용 타입 기록.
        }

        public bool IsSameTile(Vector2Int position, PuzzleTileType tileType)
        {
            return _position == position && _tileType == tileType; // 위치와 타입이 모두 같은지 확인.
        }
    }
}