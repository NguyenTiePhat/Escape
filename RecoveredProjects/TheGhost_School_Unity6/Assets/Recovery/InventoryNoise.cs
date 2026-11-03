using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Inventory))]
public sealed class InventoryNoise : MonoBehaviour
{
    [Min(0)] public float releaseSpeed = .8f;
    Inventory inventory;
    void OnEnable() { inventory = GetComponent<Inventory>(); inventory.OnItemDropped += OnDropped; }
    void OnDisable() { if (inventory) inventory.OnItemDropped -= OnDropped; }
    void OnDropped(Vector3 position, float weight)
    {
        NoiseManager.EmitNoise(position, NoiseOnImpact.CalculateLoudness(weight, releaseSpeed), NoiseSource.Drop, inventory.gameObject);
    }
}
