#if UNITY_EDITOR
using System;
using System.IO;
using nadena.dev.modular_avatar.core;
using ToggleTool.Global;
using UnityEngine;
using Version = ToggleTool.Global.Version;

namespace ToggleTool.Runtime
{
    //v1.0.71
    [DisallowMultipleComponent]
    [AddComponentMenu("Hirami/Toggle/ToggleConfig")]
    public class ToggleConfig : AvatarTagComponent
    {
            
        public Texture2D _icon;
            
        [Serializable]
        public struct SetToggleConfig
        {
            public string version;
            public bool toggleSaved;
            public bool toggleReverse;
            public string toggleMenuName;
        }

        public SetToggleConfig toggleConfig;
            
        private void Reset()
        {
            // 기본값 설정
            LoadConfigFromFile();
        }

        public void ApplyConfig(SetToggleConfig config)
        {
            toggleConfig = config;
            // 필요한 경우 추가 로직 작성
        }

        public void LoadConfigFromFile()
        {
            if (File.Exists(FilePaths.JSON_FILE_PATH))
            {
                string json = File.ReadAllText(FilePaths.JSON_FILE_PATH);
                SetToggleConfig config = JsonUtility.FromJson<SetToggleConfig>(json);
                ApplyConfig(config);
                Debug.Log("Settings loaded from JSON file.");
            }
            else
            {
                // 파일이 없는 경우 기본값 설정
                toggleConfig.version = Version.LATEST_VERSION;
                toggleConfig.toggleSaved = true;
                toggleConfig.toggleReverse = false;
                toggleConfig.toggleMenuName = Components.DEFAULT_COMPONENT_NAME;
                Debug.LogWarning("Settings file not found. Default settings applied.");
            }
        }
    }
}
#endif