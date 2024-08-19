using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ToggleTool.Global;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ToggleTool.Utils
{
    public static class ToggleController
    {
        public static AnimatorController ConfigureAnimator(GameObject[] items, GameObject rootObject,
            string targetFolder, string groupName, string paramName, bool toggleSaved, bool toggleReverse, bool isUpdate, string hash)
        {
            // 디렉토리가 존재하지 않으면 생성
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            
            string animatorPath = targetFolder + "/" + FilePaths.ANIMATOR_FILE_NAME;

            AnimatorController toggleAnimator = null;

            if ((toggleAnimator = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath)) == null)
            {
                toggleAnimator = AnimatorController.CreateAnimatorControllerAtPath(animatorPath);
                toggleAnimator.RemoveLayer(0);
            }

            AnimatorStateMachine stateMachine = new AnimatorStateMachine
            {
                name = paramName,
                hideFlags = HideFlags.HideInHierarchy
            };

            if (toggleAnimator.layers.All(l => l.name != paramName))
            {
                AssetDatabase.AddObjectToAsset(stateMachine, toggleAnimator);
                toggleAnimator.AddLayer(new AnimatorControllerLayer
                {
                    name = paramName,
                    stateMachine = stateMachine,
                    defaultWeight = 1f
                });
            }

            if (toggleAnimator.parameters.All(p => p.name != paramName))
            {
                toggleAnimator.AddParameter(new AnimatorControllerParameter
                {
                    name = paramName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = toggleSaved
                });
            }

            AnimationClip onClip = RecordState(items, rootObject, targetFolder, groupName, true, isUpdate, hash);
            AnimationClip offClip = RecordState(items, rootObject, targetFolder, groupName, false, isUpdate, hash);

            AnimatorState onState = stateMachine.AddState(Messages.STATE_NAME_ON);
            onState.motion = onClip;
            AnimatorState offState = stateMachine.AddState(Messages.STATE_NAME_OFF);
            offState.motion = offClip;

            stateMachine.defaultState = offState;

            AnimatorConditionMode conditionModeForOn =
                toggleReverse ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If;
            AnimatorConditionMode conditionModeForOff =
                toggleReverse ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;

            AnimatorStateTransition transitionToOn = offState.AddTransition(onState);
            transitionToOn.hasExitTime = false;
            transitionToOn.exitTime = 0f;
            transitionToOn.duration = 0f;
            transitionToOn.AddCondition(conditionModeForOn, 0, paramName);

            AnimatorStateTransition transitionToOff = onState.AddTransition(offState);
            transitionToOff.hasExitTime = false;
            transitionToOff.exitTime = 0f;
            transitionToOff.duration = 0f;
            transitionToOff.AddCondition(conditionModeForOff, 0, paramName);

            // 변경 사항 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return toggleAnimator;
        }
        
        private static AnimationClip RecordState(GameObject[] items, GameObject rootObject, string folderPath,
            string groupName, bool activation, bool isUpdate, string hash)
        {
            string stateName = activation ? Messages.STATE_NAME_ON : Messages.STATE_NAME_OFF;
            string clipName;
            string fullPath;
            if (isUpdate)
            {
                clipName = $"{groupName}_" + hash + $"_{stateName}";
                fullPath = $"{folderPath}/{clipName}.anim";
            }
            else
            {
                clipName = $"{groupName}_" + Md5Hash(rootObject.name + "_" + groupName) + $"_{stateName}";
                fullPath = $"{folderPath}/Toggle_{clipName}.anim";
            }

            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fullPath);
            if (existingClip != null)
            {
                var overwrite = EditorUtility.DisplayDialog(
                    "Animation Clip Exists",
                    $"An animation clip already exists at '{fullPath}'. Do you want to overwrite it?",
                    "Overwrite",
                    "Cancel"
                );

                if (!overwrite)
                {
                    return existingClip;
                }
            }

            var clip = new AnimationClip { name = clipName };
            var curve = new AnimationCurve();
            curve.AddKey(0f, activation ? 1f : 0f);

            foreach (GameObject obj in items)
            {
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(
                        AnimationUtility.CalculateTransformPath(obj.transform, rootObject.transform),
                        typeof(GameObject), "m_IsActive"), curve);
            }

            AssetDatabase.CreateAsset(clip, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return clip;
        }

        public static string Md5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(input));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}