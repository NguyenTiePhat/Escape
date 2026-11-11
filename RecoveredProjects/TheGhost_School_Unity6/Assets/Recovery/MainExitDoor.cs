using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Door))]
public sealed class MainExitDoor : MonoBehaviour
{
    public Door partner;
    public EscapeWinController game;
    Door leaf;
    bool armed;
    public bool OpeningAuthorized => armed;
    void Awake() { leaf = GetComponent<Door>(); }
    public bool TryOpenWithKey(string keyId)
    {
        if (!leaf) leaf = GetComponent<Door>();
        if (!isActiveAndEnabled || !game || game.HasWon || keyId != "main_exit_key" ||
            leaf.requiredKeyId != keyId || (partner && partner.requiredKeyId != keyId)) return false;
        if (!leaf.UnlockDoor(keyId)) return false;
        if (partner && !partner.UnlockDoor(keyId)) return false;
        armed = true;
        if (!leaf.IsOpen) leaf.Open(1f);
        if (partner && !partner.IsOpen) partner.Open(1f);
        return true;
    }
    void Update()
    {
        // Unlocking alone or a blocked swing never grants victory.
        if (armed && game && !game.HasWon && leaf.IsFullyOpen && (!partner || partner.IsFullyOpen)) game.Win();
    }
}
