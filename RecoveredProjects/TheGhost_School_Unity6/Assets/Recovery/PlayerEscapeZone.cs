using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class PlayerEscapeZone : MonoBehaviour
{
    public MainExitDoor exit;
    BoxCollider volume;
    void Awake() { volume = GetComponent<BoxCollider>(); }
    void Update()
    {
        var game = GameStateManager.Instance;
        if (!game || game.State != GameState.Playing || !exit || !exit.IsExitReady || !volume.enabled) return;
        foreach (var player in game.Players)
        {
            if (!player || !player.CanParticipate) continue;
            var body = player.GetComponent<CharacterController>();
            Vector3 point = body ? body.bounds.center : player.transform.position;
            Vector3 local = transform.InverseTransformPoint(point) - volume.center; Vector3 half = volume.size * .5f;
            if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z) player.Escape();
        }
    }
}
