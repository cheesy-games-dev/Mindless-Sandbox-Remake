using UnityEngine;

[CreateAssetMenu(fileName = "EquipableItemScriptableObject", menuName = "Mindless Sandbox/EquipableItemScriptableObject")]
public class EquipableItemScriptableObject : ScriptableObject
{
    public string itemName = "Pistol";
    public string itemDesc = "Great sidearm, Perfect for situations where you can't reload another firearm!";
    public Color itemColor = Color.red;
    public Sprite itemSprite;
    public EquipableItemScript itemObject;
}
