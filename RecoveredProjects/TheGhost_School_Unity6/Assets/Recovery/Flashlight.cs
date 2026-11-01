using UnityEngine;

public sealed class Flashlight : MonoBehaviour
{
    [Min(1)] public float capacity = 100;
    [SerializeField] float battery = 100;
    public float Battery => battery;
    [Min(0)] public float drainPerSecond = 1;
    void Update()
    {
        var hands = GetComponent<FirstPersonHands>();
        var beam = transform.Find("First person arms/Flashlight beam");
        if (hands && hands.FlashlightOn && beam && beam.gameObject.activeInHierarchy)
        {
            ConsumeBattery(drainPerSecond * Time.deltaTime);
            if (battery <= 0) hands.ToggleFlashlight();
        }
    }
    public bool AddBattery(float amount)
    {
        if (amount <= 0 || battery >= capacity) return false;
        battery = Mathf.Min(capacity, battery + amount);
        return true;
    }
    public void ConsumeBattery(float amount) { battery = Mathf.Max(0, battery - Mathf.Max(0, amount)); }
}
