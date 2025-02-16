#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MindlessSandbox.SDK
{

    [ExecuteAlways]
    [ExecuteInEditMode]
    public class CustomItemSDK : EditorWindow
    {

        [MenuItem("Window/Mindless Sandbox SDK/Custom Item Editor")]
        public static void OpenGameplayObjectCalculator()
        {
            GetWindow<CustomItemSDK>("Custom Item Editor");
        }

        public string itemName = "Pistol";
        public string itemDesc = "Great sidearm, Perfect for situations where you can't reload another firearm!";
        public Color itemColor = Color.red;
        public Sprite itemSprite;
        private EquipableItemScriptableObject itemScriptableObject;
        private EquipableItemScript itemObject;

        public void CreateItem()
        {
            if (!itemScriptableObject.itemObject)
            {
                string path = $"Assets/Resources/{itemName} Item Prefab.prefab";

                itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<EquipableItemScript>();
                itemObject.name = itemName;

                itemScriptableObject.itemObject = PrefabUtility.SaveAsPrefabAssetAndConnect(itemObject.gameObject, path, InteractionMode.AutomatedAction).GetComponent<EquipableItemScript>();
            }
            else
            {
                itemObject = Instantiate(itemScriptableObject.itemObject, null);
                PrefabUtility.ConnectGameObjectToPrefab(itemObject.gameObject, itemScriptableObject.itemObject.gameObject);
            }
            itemObject.name = itemName;
        }

        public void CreateItemScriptable()
        {
            // Create an instance
            itemScriptableObject = ScriptableObject.CreateInstance<EquipableItemScriptableObject>();
            itemScriptableObject.itemName = this.itemName;
            itemScriptableObject.itemDesc = this.itemDesc;
            itemScriptableObject.itemColor = this.itemColor;
            itemScriptableObject.itemSprite = this.itemSprite;
            itemScriptableObject.itemObject = this.itemObject;

            // Define a valid asset path
            string path = $"Assets/Resources/{itemScriptableObject.itemName} Item Asset.asset";

            // Save the instance as an asset
            AssetDatabase.CreateAsset(itemScriptableObject, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("ScriptableObject created at: " + path);
        }


        private string itemColorHEX = "#ff0000";

        private void OnGUI()
        {
            GUIStyle HeaderStyle = new GUIStyle();
            HeaderStyle.alignment = TextAnchor.MiddleCenter;
            HeaderStyle.fontStyle = FontStyle.Bold;
            HeaderStyle.fontSize = 20;
            HeaderStyle.richText = true;

            GUIStyle WeightStyle = new GUIStyle();
            WeightStyle.alignment = TextAnchor.MiddleCenter;
            WeightStyle.fontStyle = FontStyle.Normal;
            WeightStyle.fontSize = 14;
            WeightStyle.richText = true;

            GUIStyle NoteStyle = WeightStyle;
            WeightStyle.fontSize = 10;

            GUILayout.Label($"<color=white> Used to create custom items", NoteStyle);

            GUILayout.Label("<color=white>Custom Item Editor", HeaderStyle);

            GUIStyle WarnStyle = WeightStyle;
            WarnStyle.fontStyle = FontStyle.Bold;
            WarnStyle.fontSize = 10;

            if (itemScriptableObject)
            {
                itemName = itemScriptableObject.itemName;
                itemDesc = itemScriptableObject.itemDesc;
                itemColor = itemScriptableObject.itemColor;
                itemSprite = itemScriptableObject.itemSprite;
            }
            else
            {
                GUILayout.Label("<color=white>Item Properties", NoteStyle);

                GUILayout.Label("<color=white>Item Name:", NoteStyle);
                itemName = GUILayout.TextField(itemName);

                GUILayout.Label("<color=white>Item Description:", NoteStyle);
                itemDesc = GUILayout.TextField(itemDesc);

                ColorUtility.TryParseHtmlString(itemColorHEX, out itemColor);
                GUILayout.Label($"<color={itemColorHEX}>Item Color (HEX):", NoteStyle);
                itemColorHEX = GUILayout.TextField(itemColorHEX);

                GUILayout.Label("<color=white>Item Sprite:", NoteStyle);
                this.itemSprite = (Sprite)EditorGUILayout.ObjectField(this.itemSprite, typeof(Sprite), false);
            }

            GUILayout.Label("<color=white>Item Scriptable Object:", NoteStyle);
            this.itemScriptableObject = (EquipableItemScriptableObject)EditorGUILayout.ObjectField(this.itemScriptableObject, typeof(EquipableItemScriptableObject), false);


            if (itemScriptableObject)
            {
                if (GUILayout.Button("Create Item")) CreateItem();
            }
            else
            {
                if (GUILayout.Button("Create Item Scriptable Object")) CreateItemScriptable();
                GUILayout.Label($"<color=white> Drag/Create a scriptable object to create an item", NoteStyle);
            }
        }
    }
}

#endif