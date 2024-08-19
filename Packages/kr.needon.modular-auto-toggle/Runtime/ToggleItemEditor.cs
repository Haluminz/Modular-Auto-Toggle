#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using ToggleTool.Global;
using ToggleTool.Utils;

namespace ToggleTool.Runtime
{
    [CustomEditor(typeof(ToggleItem))]
    [InitializeOnLoad]
    public class ToggleItemEditor : UnityEditor.Editor
    {
        static ToggleItemEditor()
        {
            // 유니티 에디터 로드 시 자동으로 호출되는 정적 생성자
            EditorApplication.update += UpdateIcons;
        }

        private static void UpdateIcons()
        {
            // 모든 ToggleItem 인스턴스에 대해 아이콘 설정
            var toggleItems = Resources.FindObjectsOfTypeAll<ToggleItem>();
            foreach (var toggleItem in toggleItems)
            {
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(FilePaths.PACKAGE_RESOURCES_PATH + FilePaths.IMAGE_NAME_TOGGLE_ON);;
                if (icon != null)
                {
                    EditorGUIUtility.SetIconForObject(toggleItem, icon);
                }
            }

            // 설정 완료 후 이벤트 해제
            EditorApplication.update -= UpdateIcons;
        }
        
        private Texture2D _icon;
        private bool _applyToOnAnimation = true; // On 애니메이션에 적용할지 여부
        private bool _applyToOffAnimation = true; // Off 애니메이션에 적용할지 여부

        private void OnEnable()
        {
            UpdateIcons();
            ToggleItem toggleItem = (ToggleItem)target;
            _applyToOnAnimation = toggleItem._applyToOnAnimation;
            _applyToOffAnimation = toggleItem._applyToOffAnimation;
        }

        private void OnDisable()
        {
            ToggleItem toggleItem = (ToggleItem)target;
            toggleItem._applyToOnAnimation = _applyToOnAnimation;
            toggleItem._applyToOffAnimation = _applyToOffAnimation;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var blendShapesToChange = serializedObject.FindProperty("_blendShapesToChange");

            EditorGUILayout.LabelField("Blend Shapes");

            EditorGUI.indentLevel++;

            int listSize = blendShapesToChange.arraySize;
            EditorGUILayout.LabelField("Size", listSize.ToString());

            EditorGUILayout.Space();

            for (int i = 0; i < blendShapesToChange.arraySize; i++)
            {
                if (i > 0) EditorGUILayout.Space(); // Add space between elements

                EditorGUILayout.BeginVertical(GUI.skin.box); // Start box

                var element = blendShapesToChange.GetArrayElementAtIndex(i);
                var skinnedMesh = element.FindPropertyRelative("SkinnedMesh");
                var blendShapeName = element.FindPropertyRelative("name");
                var blendShapeValue = element.FindPropertyRelative("value");

                EditorGUILayout.PropertyField(skinnedMesh, new GUIContent("Skinned Mesh"));

                if (skinnedMesh.objectReferenceValue is SkinnedMeshRenderer renderer)
                {
                    List<string> blendShapeNames = new List<string> { "Please select" };
                    blendShapeNames.AddRange(Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                        .Select(index => renderer.sharedMesh.GetBlendShapeName(index)));

                    int currentIndex = blendShapeNames.IndexOf(blendShapeName.stringValue);
                    if (currentIndex == -1) currentIndex = 0; // 기본값으로 "Please select" 설정

                    currentIndex = EditorGUILayout.Popup("Name", currentIndex, blendShapeNames.ToArray());

                    blendShapeName.stringValue = blendShapeNames[currentIndex];

                    if (blendShapeName.stringValue != "Please select")
                    {
                        blendShapeValue.intValue = EditorGUILayout.IntSlider("Value", blendShapeValue.intValue, 0, 100);
                    }
                    else
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.IntSlider("Value", 0, 0, 100);
                        EditorGUI.EndDisabledGroup();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Name", "Please select a Skinned Mesh");
                    blendShapeName.stringValue = "Please select";
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntSlider("Value", 0, 0, 100);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndVertical(); // End box
            }

            EditorGUILayout.Space();

            // Add and remove buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Push buttons to the right
            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(20)))
            {
                blendShapesToChange.InsertArrayElementAtIndex(blendShapesToChange.arraySize);
                var newElement = blendShapesToChange.GetArrayElementAtIndex(blendShapesToChange.arraySize - 1);
                newElement.FindPropertyRelative("SkinnedMesh").objectReferenceValue = null;
                newElement.FindPropertyRelative("name").stringValue = "Please select";
                newElement.FindPropertyRelative("value").intValue = 0;
            }

            if (GUILayout.Button("-", GUILayout.Width(30), GUILayout.Height(20)))
            {
                if (blendShapesToChange.arraySize > 0)
                {
                    blendShapesToChange.DeleteArrayElementAtIndex(blendShapesToChange.arraySize - 1);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            // Apply 버튼
            EditorGUILayout.BeginHorizontal();
            // Cast target to ToggleItem
            ToggleItem toggleItem = (ToggleItem)target;
            _applyToOnAnimation = EditorGUILayout.Toggle("Apply to On Animation", toggleItem._applyToOnAnimation);
            _applyToOffAnimation = EditorGUILayout.Toggle("Apply to Off Animation", toggleItem._applyToOffAnimation);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply"))
            {
                toggleItem.applyBlendShape();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif