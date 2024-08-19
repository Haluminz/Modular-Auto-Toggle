using System.Collections.Generic;
using nadena.dev.modular_avatar.core;
using UnityEngine;
using ToggleTool.ViewModel;
using ToggleTool.Global;
using ToggleTool.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ToggleTool.Runtime
{
    public static class UpdateToggle
    {
        public static void UpdateSetting(GameObject parentObject)
        {
            if (!parentObject)
            {
                Debug.LogError("Parent object is null.");
                return;
            }

            // 하위 객체를 저장할 리스트 선언
            List<GameObject> childObjects = new List<GameObject>();

            // parentObject의 모든 하위 객체를 리스트에 저장
            foreach (Transform child in parentObject.transform)
            {
                childObjects.Add(child.gameObject);
            }

            // parentObject의 최상위 오브젝트를 찾음
            GameObject rootObject = parentObject.transform.root.gameObject;

            // targetFolder 경로 설정
            string targetFolder = FilePaths.TARGET_FOLDER_PATH + "/" + rootObject.name;

            // childObjects 리스트에 저장된 하위 객체들을 순회하면서 작업 수행
            foreach (GameObject childObject in childObjects)
            {
                // ItemsHolder 컴포넌트를 가져와서 작업 수행
                var itemsHolder = childObject.GetComponent<ItemsHolder>();
                if (itemsHolder)
                {
                    string rootObjectName = itemsHolder.rootObjectName;
                    // Root Object의 이름이 일치하는지 체크. 일치할경우 아바타 이름 변경되지 하지 않은상태에서 Update를 누름
                    if (rootObjectName == rootObject.name)
                    {
                        Debug.LogError($"Failed to update toggle setting. The root object name '{rootObjectName}' is identical to the previous setting name!");
#if UNITY_EDITOR
                        EditorUtility.DisplayDialog(Messages.DIALOG_TITLE_ERROR,
                            "Cannot execute update. Avatar name must be changed first.\n아바타의 이름이 변경되지 않은상태에서는 업데이트를 실행할수 없습니다.",
                            Messages.DIALOG_BUTTON_OK);
#endif
                        return;
                    }
                    GameObject[] storedItems = itemsHolder.items;
                    bool toggleSaved = itemsHolder.toggleSaved;
                    bool toggleReverse = itemsHolder.toggleReverse;
                    if (storedItems != null && storedItems.Length > 0)
                    {
                        Debug.Log($"Found ItemsHolder on {childObject.name} with {storedItems.Length} items.");

                        // paramName을 가져옴
                        string paramName = GetFirstAvatarParameterName(childObject);

                        // groupName을 하위 객체의 이름으로 설정
                        string groupName = childObject.name;
                        
                        // Hash값을 가져오기위해 Parameter Component를 Get
                        var avatarParameters = childObject.GetComponent<ModularAvatarParameters>();
                        var hash = string.Empty;
                        if (avatarParameters)
                        {
                            if (avatarParameters.parameters.Count > 0)
                            {
                                hash = avatarParameters.parameters[0].nameOrPrefix;
                            }
                        }
                        
                        // ConfigureAnimator 호출
                        ToggleController.ConfigureAnimator(storedItems, rootObject, targetFolder, groupName, paramName, toggleSaved, toggleReverse, true, hash);
                        
                        // 바뀐 rootObjectName을 ItemHolder에 저장
                        itemsHolder.rootObjectName = rootObject.name;
                    }
                    else
                    {
                        Debug.LogWarning($"ItemsHolder found on {childObject.name}, but items array is null or empty.");
                    }
                }
                else
                {
                    Debug.LogWarning($"No ItemsHolder found on {childObject.name}.");
                }
                
                // 블랜더 쉐이프를 적용
                var toggleItem = childObject.GetComponent<ToggleItem>();
                if (toggleItem)
                {
                    toggleItem.applyBlendShape();
                }
            }
        }
        
        public static string GetFirstAvatarParameterName(GameObject obj)
        {
            var avatarParameters = obj.GetComponent<ModularAvatarParameters>();

            if (!avatarParameters)
            {
                Debug.LogWarning("ModularAvatarParameters component not found on the object.");
                return null;
            }

            if (avatarParameters.parameters.Count > 0)
            {
                string firstParamName = avatarParameters.parameters[0].nameOrPrefix;
                Debug.Log($"First parameter found: {firstParamName}");
                return firstParamName;
            }
            else
            {
                Debug.LogWarning("No parameters found in ModularAvatarParameters.");
                return null;
            }
        }
    }
}