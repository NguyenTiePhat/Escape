using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CampusWalk : MonoBehaviour {
    public Camera eyes;
    CharacterController body;
    Light torch;
    float pitch, velocity;
    bool bright = true;
    void Start() {
        body = GetComponent<CharacterController>();
        torch = eyes.GetComponent<Light>();
    }
    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (Input.GetKeyDown(KeyCode.F)) torch.enabled = !torch.enabled;
        if (Input.GetKeyDown(KeyCode.L)) {
            bright = !bright;
            RenderSettings.ambientIntensity = bright ? 1f : .25f;
            RenderSettings.fogDensity = bright ? .006f : .018f;
        }
        if (Input.GetKeyDown(KeyCode.R) || transform.position.y < -8) {
            body.enabled = false; transform.position = new Vector3(0, .15f, -25); body.enabled = true;
            velocity = 0;
        }
        if (Cursor.lockState != CursorLockMode.Locked) return;
        transform.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2, -85, 85);
        eyes.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        Vector3 move = Vector3.ClampMagnitude(transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical"), 1);
        if (body.isGrounded && velocity < 0) velocity = -2;
        velocity += Physics.gravity.y * Time.deltaTime;
        body.Move((move * (Input.GetKey(KeyCode.LeftShift) ? 5 : 3) + Vector3.up * velocity) * Time.deltaTime);
    }
    void OnGUI() {
        GUI.Box(new Rect(18,18,470,100), "IUH / ABANDONED CAMPUS — ENVIRONMENT PREVIEW");
        GUI.Label(new Rect(30,44,450,25), "Click: look | WASD: walk | Shift: run | Esc: release mouse");
        GUI.Label(new Rect(30,68,450,25), "F: flashlight | L: lighting | R: return to courtyard");
        GUI.Label(new Rect(30,90,450,25), transform.position.y > 3 ? "LEVEL 02 / Upper floor" : "LEVEL 01 / Ground floor & courtyard");
    }
}
