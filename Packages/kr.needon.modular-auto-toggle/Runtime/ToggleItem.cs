#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nadena.dev.modular_avatar.core;
using ToggleTool.Global;
using UnityEditor;
using UnityEngine;

namespace ToggleTool.Runtime
{
    //v1.0.71
    [DisallowMultipleComponent]
    public class ToggleItem : AvatarTagComponent
    {
        public Texture2D _icon;
        
        [Serializable]
        public struct SetBlendShape
        {
            public SkinnedMeshRenderer SkinnedMesh;
            public string name;
            public int value;
        }

        [SerializeField] private List<SetBlendShape> _blendShapesToChange = new List<SetBlendShape>();
        public IEnumerable<SetBlendShape> BlendShapesToChange => _blendShapesToChange.Where(e => e.SkinnedMesh != null);
        
        [SerializeField]
        public bool _applyToOnAnimation = true; // On 애니메이션에 적용할지 여부
        [SerializeField]
        public bool _applyToOffAnimation = true; // Off 애니메이션에 적용할지 여부
        
        public void applyBlendShape()
        {
            // Cast target to ToggleItem
            ToggleItem toggleItem = this;

            // Get the other components
            var menuItem = toggleItem.GetComponent<ModularAvatarMenuItem>();

            if (menuItem)
            {
                Debug.Log($"MAMenuItem found: {menuItem.name}");

                // Access the Control property
                var control = menuItem.Control;

                // Get the parameter name
                string parameterName = control.parameter?.name ?? "None";
                string rootName = menuItem.transform.root.name;

                Debug.Log($"Parameter name: {parameterName}");
                string fullPath = FindFileByGuid(parameterName, FilePaths.TARGET_FOLDER_PATH + "/" + rootName).Replace("_off.anim", "");

                string onToggleAnimePath = fullPath + "_on.anim";
                string offToggleAnimePath = fullPath + "_off.anim";

                // Check if the files exist
                bool onToggleExists = File.Exists(onToggleAnimePath);
                bool offToggleExists = File.Exists(offToggleAnimePath);

                if (onToggleExists && offToggleExists)
                {
                    AnimationClip onClip = _applyToOnAnimation
                        ? AssetDatabase.LoadAssetAtPath<AnimationClip>(onToggleAnimePath)
                        : null;
                    AnimationClip offClip = _applyToOffAnimation
                        ? AssetDatabase.LoadAssetAtPath<AnimationClip>(offToggleAnimePath)
                        : null;

                    // Clear all existing blend shape animations
                    if (_applyToOnAnimation)
                    {
                        clearAllBlendShapeAnimations(onClip, toggleItem);
                    }

                    if (_applyToOffAnimation)
                    {
                        clearAllBlendShapeAnimations(offClip, toggleItem);
                    }

                    foreach (var blendShapeChange in toggleItem.BlendShapesToChange)
                    {
                        // Get the SkinnedMeshRenderer
                        var skinnedMeshRenderer = blendShapeChange.SkinnedMesh;
                        if (!skinnedMeshRenderer) continue;

                        // Get the blend shape index
                        int blendShapeIndex =
                            skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeChange.name);
                        if (blendShapeIndex < 0) continue;

                        // Apply blend shape changes to onClip and offClip
                        var transform = skinnedMeshRenderer.transform;
                        var blendShapePath = AnimationUtility.CalculateTransformPath(transform, transform.root);

                        if (onClip)
                        {
                            AnimationCurve onCurve = AnimationCurve.Linear(0, blendShapeChange.value, 0,
                                blendShapeChange.value);
                            onClip.SetCurve(blendShapePath, typeof(SkinnedMeshRenderer),
                                $"blendShape.{blendShapeChange.name}", onCurve);
                        }

                        if (offClip)
                        {
                            AnimationCurve offCurve = AnimationCurve.Linear(0, blendShapeChange.value, 0,
                                blendShapeChange.value);
                            offClip.SetCurve(blendShapePath, typeof(SkinnedMeshRenderer),
                                $"blendShape.{blendShapeChange.name}", offCurve);
                        }

                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
        
        
        private void clearAllBlendShapeAnimations(AnimationClip clip, ToggleItem toggleItem)
        {
            if (!clip) return;

            // Remove all blend shape animations from the clip
            var editorCurveBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in editorCurveBindings)
            {
                if (!binding.propertyName.Equals("m_IsActive"))
                {
                    Debug.Log("path :: " + binding.path);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                }
            }

            AssetDatabase.SaveAssets();
        }
        
        private string FindFileByGuid(string guid, string searchFolder)
        {
            var allFiles = Directory.GetFiles(searchFolder, "*", SearchOption.AllDirectories);
            var fileWithGuid = allFiles.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).Contains(guid));
            return fileWithGuid;
        }

    }
}
#endif