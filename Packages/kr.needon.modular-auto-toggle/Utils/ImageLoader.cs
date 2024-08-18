using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ToggleTool.Global;

namespace ToggleTool.Utils
{
    public class ImageLoader
    {
        public static readonly Dictionary<string, ImageLoader> instance = new Dictionary<string, ImageLoader>()
        {
            { "ToggleON", new ImageLoader(FilePaths.IMAGE_PATH_TOGGLE_ON) },
            { "ToggleOFF", new ImageLoader(FilePaths.IMAGE_PATH_TOGGLE_OFF) }
        };
        
        public Texture2D iconTexture;  // 로드된 텍스처를 저장할 필드

        public ImageLoader(string path)
        {
            LoadImageFromPath(path);
        }

        private void LoadImageFromPath(string path)
        {
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                iconTexture = new Texture2D(2, 2);
                iconTexture.LoadImage(fileData); // 텍스처 데이터 로드
            }
            else
            {
                Debug.LogError($"지정된 경로에 파일이 존재하지 않습니다: {path}");
            }
        }
    }
}