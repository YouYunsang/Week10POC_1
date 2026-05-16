using UnityEngine;
using System;

namespace Jungle.Puzzle
{
    [Serializable]
    public class PuzzleTilePrefabEntry
    {
        [SerializeField] private PuzzleTileType _tileType;
        [SerializeField] private GameObject _prefab;

        public PuzzleTileType TileType => _tileType;
        public GameObject Prefab => _prefab;
    }
}