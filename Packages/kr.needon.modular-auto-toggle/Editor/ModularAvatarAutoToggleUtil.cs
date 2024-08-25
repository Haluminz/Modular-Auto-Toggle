#if UNITY_EDITOR
using System.IO;
using System.Linq;
using nadena.dev.modular_avatar.core;
using ToggleTool.Runtime;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using ToggleTool.Global;
using ToggleTool.Utils;
using ToggleTool.Models;
using ToggleTool.ViewModel;

//v1.0.71
namespace ToggleTool.Editor
{
    public abstract class AutoToggleCreator
    {
        private static bool toggleSaved = true;
        private static bool toggleReverse = false;
        private static string toggleMenuName;
        private static string componentName = null;

        // componentName 변수를 EditorPrefs에 저장하는 함수
        private static void SaveComponentNameToEditorPrefs(string name)
        {
            EditorPrefs.SetString("AutoToggleCreator_componentName", name);
        }

        // componentName 변수를 EditorPrefs에서 불러오는 함수
        private static void LoadComponentNameFromEditorPrefs()
        {
            if (EditorPrefs.HasKey("AutoToggleCreator_componentName"))
            {
                componentName = EditorPrefs.GetString("AutoToggleCreator_componentName");
            }
        }

        [MenuItem("GameObject/Create Toggle Items", false, 0)]
        private static void CreateToggleItems()
        {
            // 클래스 초기화 시 componentName을 불러옴
            LoadComponentNameFromEditorPrefs();

            GameObject[] selectedObjects = Selection.gameObjects;
            GameObject rootObject = null;
            rootObject = selectedObjects[0].transform.root.gameObject;
            string targetFolder = FilePaths.TARGET_FOLDER_PATH + "/" + rootObject.name;
            Debug.Log("targetFolderPath :: " + targetFolder);

            // targetFolder가 없으면 componentName을 null로 설정하여 다시 입력을 받도록 함
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                componentName = null;
            }

            // componentName이 null이거나 비어있으면 입력 창을 띄움
            if (string.IsNullOrEmpty(componentName))
            {
                componentName = ComponentNameWindow.OpenComponentNameDialog();
                if (string.IsNullOrEmpty(componentName))
                {
                    Debug.LogWarning("No name entered for the toggle item. Operation cancelled.");
                    return; // 사용자가 이름을 입력하지 않으면 중단
                }

                // componentName을 설정할 때마다 저장
                SaveComponentNameToEditorPrefs(componentName);
            }

            // 사용자 입력값을 ReadToggleMenuNameSetting에 설정
            SetToggleMenuNameSetting(componentName);

            if (selectedObjects.Length <= 0)
            {
                Debug.LogError(
                    "The selected GameObjects must be part of an avatar with a VRC Avatar Descriptor.\n선택된 오브젝트들은 VRC 아바타 디스크립터를 가진 아바타의 일부여야 합니다.");
                EditorUtility.DisplayDialog(Messages.DIALOG_TITLE_ERROR,
                    "The selected GameObjects must be part of an avatar with a VRC Avatar Descriptor.\n선택된 오브젝트들은 VRC 아바타 디스크립터를 가진 아바타의 일부여야 합니다.",
                    Messages.DIALOG_BUTTON_OK);
                return;
            }

            if (!rootObject)
            {
                Debug.LogError("The selected GameObject has no parent.\n선택한 오브젝트에 부모 오브젝트가 없습니다.");
                EditorUtility.DisplayDialog(Messages.DIALOG_TITLE_ERROR, "The selected GameObject has no parent.", Messages.DIALOG_BUTTON_OK);
                return;
            }

            ReadSetting();

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                string[] folders = targetFolder.Split('/');
                string parentFolder = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    string tmpFolder = parentFolder + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(tmpFolder))
                    {
                        AssetDatabase.CreateFolder(parentFolder, folders[i]);
                        Debug.Log(tmpFolder + " folder has been created.");
                    }

                    parentFolder = tmpFolder;
                }
            }

            Debug.Log("ToggleName :: " + rootObject.name);

            EditorApplication.delayCall = null;
            EditorApplication.delayCall += () =>
            {
                // GameObject 생성
                CreateToggleObject(selectedObjects, rootObject, targetFolder);

                // 로딩 바 종료
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Toggle Items Creation",
                    "All toggle items have been created successfully.\n\n모든 토글 아이템이 성공적으로 생성되었습니다.",
                    Messages.DIALOG_BUTTON_OK);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };
        }

        private static void SetToggleMenuNameSetting(string name)
        {
            ToggleConfigModel settings;
            if (File.Exists(FilePaths.JSON_FILE_PATH))
            {
                string json = File.ReadAllText(FilePaths.JSON_FILE_PATH);
                settings = JsonUtility.FromJson<ToggleConfigModel>(json);
            }
            else
            {
                settings = new ToggleConfigModel();
            }

            settings.toggleMenuName = name;
            string updatedJson = JsonUtility.ToJson(settings, true);
            File.WriteAllText(FilePaths.JSON_FILE_PATH, updatedJson);
            AssetDatabase.Refresh();

            // Save settings to EditorPrefs
            SaveSettingsToEditorPrefs(settings);
        }

        private static void CreateToggleObject(GameObject[] items, GameObject rootObject, string targetFolder)
        {
            var toggleTransform = rootObject.transform.Find(toggleMenuName);
            var toggleGameObject = !toggleTransform ? null : toggleTransform.gameObject;
            string groupName = string.Join("_", items.Select(obj => obj.name));
            string paramName = ToggleController.Md5Hash(rootObject.name + "_" + groupName);
            int currentStep = 0, totalSteps = 6;

            UpdateProgressBar(currentStep++, totalSteps, "Initializing...");
            if (!toggleGameObject || toggleGameObject.GetComponentsInChildren<ToggleConfig>().Length <= 0)
            {
                toggleGameObject = new GameObject(toggleMenuName);
                toggleTransform = toggleGameObject.transform;
            }

            toggleTransform.SetParent(rootObject.transform, false);

            UpdateProgressBar(currentStep++, totalSteps, "Creating Toggle Object...");
            GameObject newObj = new GameObject("Toggle_" + groupName);
            newObj.transform.SetParent(toggleTransform, false);

            // ItemsHolder 컴포넌트 추가 및 items 배열 설정
            var itemsHolder = newObj.AddComponent<ItemsHolder>();
            itemsHolder.rootObjectName = rootObject.name;
            itemsHolder.items = items;
            itemsHolder.toggleSaved = toggleSaved;
            itemsHolder.toggleReverse = toggleReverse;

            UpdateProgressBar(currentStep++, totalSteps, "Configure Parameter...");
            ConfigureAvatarParameters(newObj, paramName);

            UpdateProgressBar(currentStep++, totalSteps, "Configure Menu...");
            ConfigureMenuItem(newObj, paramName);

            newObj.AddComponent<ToggleItem>();

            var mergeAnimator = toggleGameObject.GetComponent<ModularAvatarMergeAnimator>();

            UpdateProgressBar(currentStep++, totalSteps, "Configure MA Settings...");

            if (!mergeAnimator)
            {
                toggleGameObject.AddComponent<ModularAvatarMenuInstaller>();
                ConfigureParentMenuItem(toggleGameObject);
                mergeAnimator = toggleGameObject.AddComponent<ModularAvatarMergeAnimator>();
            }

            UpdateProgressBar(currentStep++, totalSteps, "Configure Animator...");
            mergeAnimator.animator = ToggleController.ConfigureAnimator(items, rootObject, targetFolder, groupName, paramName, toggleSaved, toggleReverse, false, string.Empty);
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            mergeAnimator.matchAvatarWriteDefaults = true;
            mergeAnimator.deleteAttachedAnimator = true;

            UpdateProgressBar(currentStep++, totalSteps, "Complete!");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Modular Avatar
        private static void ConfigureAvatarParameters(GameObject obj, string paramName)
        {
            var avatarParameters = obj.AddComponent<ModularAvatarParameters>();

            if (avatarParameters.parameters.Any(p => p.nameOrPrefix == paramName)) return;

            avatarParameters.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = paramName,
                syncType = ParameterSyncType.Bool,
                defaultValue = 1,
                saved = toggleSaved
            });
        }
        
        private static void ConfigureMenuItem(GameObject obj, string paramName)
        {
            var identifier = obj.AddComponent<MenuItemIdentifier>();
            identifier.IdentifierName = Components.TOGGLE_TOOL_IDENTIFIER;  // 토글툴 컴포넌트를 식별할 수 있는 정보 설정
            var menuItem = obj.AddComponent<ModularAvatarMenuItem>();
            menuItem.Control = menuItem.Control ?? new VRCExpressionsMenu.Control();

            menuItem.Control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
            menuItem.Control.parameter = new VRCExpressionsMenu.Control.Parameter { name = paramName }; //모듈러 파라미터 이름 설정
            menuItem.Control.icon = ImageLoader.instance["ToggleON"].iconTexture; // 메뉴 아이콘 설정
        }
        
        private static void ConfigureParentMenuItem(GameObject obj)
        {
            var identifier = obj.AddComponent<MenuItemIdentifier>();
            identifier.IdentifierName = Components.TOGGLE_TOOL_IDENTIFIER;  // 토글툴 컴포넌트를 식별할 수 있는 정보 설정

            obj.AddComponent<ToggleConfig>();
            obj.AddComponent<DeleteToggle>();
            var menuItem = obj.AddComponent<ModularAvatarMenuItem>();
            menuItem.Control = menuItem.Control ?? new VRCExpressionsMenu.Control();

            menuItem.Control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
            menuItem.MenuSource = SubmenuSource.Children;
            menuItem.Control.icon = ImageLoader.instance["ToggleON"].iconTexture; // 메뉴 아이콘 설정
        }
        
        // Settings
        private static void ReadSetting()
        {
            ToggleConfigModel settings = File.Exists(FilePaths.JSON_FILE_PATH)
                ? JsonUtility.FromJson<ToggleConfigModel>(File.ReadAllText(FilePaths.JSON_FILE_PATH))
                : new ToggleConfigModel();
            toggleSaved = settings.toggleSaved;
            toggleReverse = settings.toggleReverse;
            toggleMenuName = settings.toggleMenuName ?? Components.DEFAULT_COMPONENT_NAME;
            AssetDatabase.Refresh();

            // Load settings from EditorPrefs
            LoadSettingsFromEditorPrefs();
        }

        private static void UpdateProgressBar(int currentStep, int totalSteps, string message)
        {
            float progress = (float)currentStep / totalSteps;
            EditorUtility.DisplayProgressBar("Creating Toggle Items", message, progress);
        }

        private static void SaveSettingsToEditorPrefs(ToggleConfigModel settings)
        {
            EditorPrefs.SetBool("AutoToggleCreator_toggleSaved", settings.toggleSaved);
            EditorPrefs.SetBool("AutoToggleCreator_toggleReverse", settings.toggleReverse);
            EditorPrefs.SetString("AutoToggleCreator_toggleMenuName", settings.toggleMenuName);
        }

        private static void LoadSettingsFromEditorPrefs()
        {
            if (EditorPrefs.HasKey("AutoToggleCreator_toggleSaved"))
            {
                toggleSaved = EditorPrefs.GetBool("AutoToggleCreator_toggleSaved");
            }

            if (EditorPrefs.HasKey("AutoToggleCreator_toggleReverse"))
            {
                toggleReverse = EditorPrefs.GetBool("AutoToggleCreator_toggleReverse");
            }

            if (EditorPrefs.HasKey("AutoToggleCreator_toggleMenuName"))
            {
                toggleMenuName = EditorPrefs.GetString("AutoToggleCreator_toggleMenuName");
            }
        }
    }
    public class MenuItemIdentifier : AvatarTagComponent
    {
        public string IdentifierName;
    }
}
#endif