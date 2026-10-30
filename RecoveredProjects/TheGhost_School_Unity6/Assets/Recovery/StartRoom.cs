using System.Linq;
using UnityEngine;
[DefaultExecutionOrder(100)]
public sealed class StartRoom : MonoBehaviour {
 [Header("Nội dung giấy — chỉnh tại đây")]
 public string noteTitle="Gửi người tỉnh dậy trong căn phòng này";
 [TextArea(8,20)] public string noteText="Nếu bạn đọc được những dòng này, hãy giữ bình tĩnh.\n\nNhặt chiếc đèn pin trên bàn trước khi rời phòng. Nhấn L để bật hoặc tắt đèn.\n\nKhám phá các phòng học, tìm vật dụng và manh mối để mở đường thoát khỏi trường. Hãy quan sát kỹ những góc tối.\n\nWASD: di chuyển • Chuột: nhìn quanh • Shift: chạy\nE: tương tác với vật đang nhìn\n\nĐừng bỏ qua những mẩu giấy bạn tìm thấy.";
 public Camera view; public GameObject flashlightObject; public FirstPersonHands hands;
 public bool HasFlashlight {get;private set;} public bool HasReadNote {get;private set;}
 public bool Reading {get;private set;}
 StartRoomItem target; Vector2 scroll;
 void Start(){hands=view.GetComponent<FirstPersonHands>(); if(hands)hands.SetEquipped(false);}
 void Update(){
 if(Reading){if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E))CloseNote();return;}
 target=null;
 if(Cursor.lockState!=CursorLockMode.Locked)return;
 var hits=Physics.RaycastAll(view.ViewportPointToRay(new Vector3(.5f,.5f,0)),2.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponentInParent<SchoolExplorer>()).OrderBy(h=>h.distance).ToArray(); if(hits.Length>0)target=hits[0].collider.GetComponentInParent<StartRoomItem>();
 if(target&&Input.GetKeyDown(KeyCode.E)){
 if(target.flashlight){HasFlashlight=true;flashlightObject.SetActive(false);hands.SetEquipped(true);target=null;}
 else {Reading=true;HasReadNote=true;scroll=Vector2.zero;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 }
 }
 public void CloseNote(){Reading=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 void OnGUI(){
 var label=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};
 if(Reading){float w=Mathf.Min(660,Screen.width-32),h=Mathf.Min(580,Screen.height-32);var rect=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);GUI.Box(rect,"");GUILayout.BeginArea(new Rect(rect.x+24,rect.y+20,w-48,h-40));GUILayout.Label(noteTitle,new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold});GUILayout.Space(16);scroll=GUILayout.BeginScrollView(scroll);GUILayout.Label(noteText,label);GUILayout.EndScrollView();if(GUILayout.Button("Đóng giấy [E / Esc]",GUILayout.Height(36)))CloseNote();GUILayout.EndArea();return;}
 GUI.Label(new Rect(Screen.width/2-5,Screen.height/2-12,20,24),"+");
 string objective=!HasFlashlight?"Nhặt đèn pin trên bàn":!HasReadNote?"Đọc tờ giấy trên bàn":"Khám phá trường học và tìm đường thoát";
 GUI.Box(new Rect(18,18,430,44),objective);
 if(target)GUI.Box(new Rect(Screen.width/2-150,Screen.height*.72f,300,40),target.flashlight?"[E] Nhặt đèn pin":"[E] Đọc tờ giấy");
 GUI.Label(new Rect(18,Screen.height-34,700,28),"WASD: đi  |  Shift: chạy  |  E: tương tác  |  L: đèn pin  |  Esc: thả chuột");
 }
}

