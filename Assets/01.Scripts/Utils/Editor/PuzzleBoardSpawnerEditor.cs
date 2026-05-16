#if UNITY_EDITOR

using Jungle.Puzzle;
using UnityEditor;
using UnityEngine;

namespace Jungle.PuzzleEditor
{
    [CustomEditor(typeof(PuzzleBoardSpawner))]
    public class PuzzleBoardSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // 기존 SerializeField Inspector 표시.

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

            PuzzleBoardSpawner spawner = (PuzzleBoardSpawner)target;

            DrawValidationMessages(spawner);
            DrawPreviewButtons(spawner);
        }

        private void DrawValidationMessages(PuzzleBoardSpawner spawner)
        {
            if (spawner.BoardData == null)
            {
                EditorGUILayout.HelpBox(
                    "Board Data가 비어 있습니다. PuzzleBoardData 에셋을 할당하세요.",
                    MessageType.Warning
                );
            }

            if (spawner.PrefabTable == null)
            {
                EditorGUILayout.HelpBox(
                    "Prefab Table이 비어 있습니다. PuzzleTilePrefabTable 에셋을 할당하세요.",
                    MessageType.Warning
                );
            }
        }

        private void DrawPreviewButtons(PuzzleBoardSpawner spawner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(spawner.BoardData == null || spawner.PrefabTable == null))
                {
                    if (GUILayout.Button("Spawn Board Preview"))
                    {
                        SpawnPreview(spawner);
                    }
                }

                if (GUILayout.Button("Clear Preview"))
                {
                    ClearPreview(spawner);
                }
            }

            EditorGUILayout.HelpBox(
                "Play Mode에 들어가지 않고 현재 PuzzleBoardData를 Scene에 생성합니다. 생성/삭제는 Undo를 지원합니다.",
                MessageType.Info
            );
        }

        private void SpawnPreview(PuzzleBoardSpawner spawner)
        {
            Undo.RecordObject(spawner, "Spawn Puzzle Board Preview");

            spawner.SpawnBoardEditorPreview(); // 에디터 전용 프리뷰 생성.

            EditorUtility.SetDirty(spawner);
        }

        private void ClearPreview(PuzzleBoardSpawner spawner)
        {
            Undo.RecordObject(spawner, "Clear Puzzle Board Preview");

            spawner.ClearSpawnedTilesEditorPreview(); // 에디터 전용 프리뷰 삭제.

            EditorUtility.SetDirty(spawner);
        }
    }
}

#endif