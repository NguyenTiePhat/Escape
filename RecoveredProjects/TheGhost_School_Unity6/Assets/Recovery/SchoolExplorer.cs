using UnityEngine;

public sealed class SchoolExplorer : MonoBehaviour
{
    public Camera view;
    public Vector3 home;
    CharacterController body;
    float pitch, fallSpeed;
    bool flying;
    Light torch; StartRoom startRoom;
    void Start() { startRoom=FindFirstObjectByType<StartRoom>();body=GetComponent<CharacterController>(); pitch=view.transform.localEulerAngles.x;torch=view.GetComponent<Light>(); if(!view.GetComponent<FirstPersonHands>()) view.gameObject.AddComponent<FirstPersonHands>(); }
    void Update()
    {
        if(startRoom && startRoom.Reading)return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        if (Input.GetMouseButtonDown(0)) { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
        if (!startRoom && Input.GetKeyDown(KeyCode.F)) { flying=!flying; body.enabled=!flying; fallSpeed=0; }
        
        if (Input.GetKeyDown(KeyCode.R) || transform.position.y < home.y-80)
        { body.enabled=false; transform.position=home; body.enabled=!flying; fallSpeed=0; }
        if (Cursor.lockState != CursorLockMode.Locked) return;
        transform.Rotate(0,Input.GetAxis("Mouse X")*2,0);
        pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,-85,85);
        view.transform.localEulerAngles=new Vector3(pitch,0,0);
        float speed=Input.GetKey(KeyCode.LeftShift)?7:3;
        Vector3 move=(flying?view.transform.forward:transform.forward)*Input.GetAxisRaw("Vertical")+transform.right*Input.GetAxisRaw("Horizontal");
        if(flying)
        {
            if(Input.GetKey(KeyCode.E)) move+=Vector3.up;
            if(Input.GetKey(KeyCode.Q)) move-=Vector3.up;
            transform.position+=Vector3.ClampMagnitude(move,1)*speed*Time.deltaTime;
        }
        else
        {
            if(body.isGrounded && fallSpeed<0) fallSpeed=-2;
            fallSpeed+=Physics.gravity.y*Time.deltaTime;
            body.Move((Vector3.ClampMagnitude(move,1)*speed+Vector3.up*fallSpeed)*Time.deltaTime);
        }
    }
    void OnGUI()
    {
        if(startRoom)return; GUI.Box(new Rect(16,16,580,82),"THE GHOST / RECOVERED SCHOOL — MAP INSPECTION");
        GUI.Label(new Rect(28,43,550,22),"Click: look | WASD: move | Shift: fast | Esc: release mouse");
        GUI.Label(new Rect(28,65,550,22),"F: walk/fly | Q/E: fly down/up | L: flashlight | R: reset — "+(flying?"FLY":"WALK"));
    }
}



