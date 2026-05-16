using System.Collections.Generic;
using UnityEngine;

namespace Jungle.Puzzle
{
    [CreateAssetMenu(
        fileName = "PuzzleTilePrefabTable",
        menuName = "Jungle/Puzzle/Puzzle Tile Prefab Table"
    )]
    public class PuzzleTilePrefabTable : ScriptableObject
    {
        private const float MIN_CELL_SIZE = 0.01f; // 셀 크기 최소값.

        [SerializeField] private float _cellSize = 1f; // 한 칸의 월드 크기.
        [SerializeField] private GameObject _fallbackPrefab; // 타입 매핑 실패 시 사용할 기본 프리팹.
        [SerializeField] private List<PuzzleTilePrefabEntry> _entries = new();

        public float CellSize => Mathf.Max(MIN_CELL_SIZE, _cellSize);
        public GameObject FallbackPrefab => _fallbackPrefab;

        public GameObject GetPrefab(PuzzleTileType tileType)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TileType == tileType)
                {
                    return _entries[i].Prefab; // 타입에 맞는 프리팹 반환.
                }
            }

            return _fallbackPrefab; // 매핑이 없으면 기본 프리팹 반환.
        }
    }
}