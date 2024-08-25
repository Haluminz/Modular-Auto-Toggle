using UnityEngine;
using nadena.dev.modular_avatar.core;

namespace ToggleTool.ViewModel
{
    public class ItemsHolder : AvatarTagComponent
    {
        public string rootObjectName;
        public GameObject[] items;
        public bool toggleSaved;
        public bool toggleReverse;
    }
}