using UnityEngine;

public enum ItemType { Key, Battery, Medkit, PuzzleItem }

[CreateAssetMenu(menuName = "The Ghost/Item")]
public sealed class ItemData : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemType type;
    public bool stackable;
    [Min(1)] public int maxStack = 1;
    [Min(.01f)] public float weight = .2f;
    [Min(0)] public float batteryAmount = 25;
    public PickupItem pickupPrefab;
    public int StackLimit => stackable ? Mathf.Max(1, maxStack) : 1;
}
