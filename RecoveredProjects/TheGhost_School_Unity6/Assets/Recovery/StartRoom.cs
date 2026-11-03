using UnityEngine;
[DefaultExecutionOrder(100)]
public sealed class StartRoom : MonoBehaviour {
 [Header("Nội dung giấy — chỉnh tại đây")]
 public string noteTitle="Gửi người tỉnh dậy trong căn phòng này";
 [TextArea(8,20)] public string noteText="Nếu bạn đọc được những dòng này, hãy giữ bình tĩnh.\n\nNhặt chiếc đèn pin trên bàn trước khi rời phòng. Nhấn L để bật hoặc tắt đèn.\n\nKhám phá các phòng học, tìm vật dụng và manh mối để mở đường thoát khỏi trường. Hãy quan sát kỹ những góc tối.\n\nWASD: di chuyển • Chuột: nhìn quanh • Shift: chạy\nE: tương tác với vật đang nhìn\n\nĐừng bỏ qua những mẩu giấy bạn tìm thấy.";
 public Camera view; public GameObject flashlightObject; public FirstPersonHands hands;
 public bool HasFlashlight => inventory && inventory.HasFlashlight; public bool HasReadNote {get;private set;}
 public bool Reading {get;private set;}
 public PlayerInventory inventory;
 PlayerInteractor interactor; int noteOpenedFrame; Vector2 scroll; string message; float messageUntil;
 void Start(){hands=view.GetComponent<FirstPersonHands>(); if(hands)hands.SetEquipped(false); inventory=GetComponent<PlayerInventory>(); if(!inventory)inventory=gameObject.AddComponent<PlayerInventory>(); inventory.Initialize(this); interactor=view.GetComponentInParent<PlayerInteractor>(); if(interactor)interactor.OnInteractWithoutTarget+=inventory.UseSelected;}
 void Update(){
 if(Reading && Time.frameCount!=noteOpenedFrame && (Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.E)))CloseNote();
 }
 void OnDestroy(){if(interactor && inventory)interactor.OnInteractWithoutTarget-=inventory.UseSelected;}
 public void TryPickup(StartRoomItem item){
 if(!inventory.TryAdd(item)){message="Thanh vật phẩm đã đầy (5/5)";messageUntil=Time.time+2;}
 }
 public void OpenNote(){noteOpenedFrame=Time.frameCount;Reading=true;HasReadNote=true;scroll=Vector2.zero;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 public void CloseNote(){Reading=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
 void OnGUI(){
 var label=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};
 if(Reading){float w=Mathf.Min(660,Screen.width-32),h=Mathf.Min(580,Screen.height-32);var rect=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);GUI.Box(rect,"");GUILayout.BeginArea(new Rect(rect.x+24,rect.y+20,w-48,h-40));GUILayout.Label(noteTitle,new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold});GUILayout.Space(16);scroll=GUILayout.BeginScrollView(scroll);GUILayout.Label(noteText,label);GUILayout.EndScrollView();if(GUILayout.Button("Đóng giấy [E / Esc]",GUILayout.Height(36)))CloseNote();GUILayout.EndArea();return;}
 GUI.Label(new Rect(Screen.width/2-5,Screen.height/2-12,20,24),"+");
 string objective=!HasFlashlight?"Nhặt đèn pin trên bàn":!HasReadNote?"Đọc tờ giấy trên bàn":"Khám phá trường học và tìm đường thoát";
 inventory.DrawBar();
 GUI.Label(new Rect(18,110,430,44),objective);
 if(interactor && interactor.Target==null && inventory.Selected)GUI.Box(new Rect(Screen.width/2-150,Screen.height*.72f,300,40),inventory.Selected.flashlight?"[E] Bật / tắt đèn pin":"[E] Đọc tờ giấy");
 if(Time.time<messageUntil)GUI.Box(new Rect(Screen.width/2-150,Screen.height*.64f,300,36),message);
 GUI.Label(new Rect(18,Screen.height-54,Screen.width-36,24),"WASD: đi  |  Shift: chạy  |  C: ngồi/đứng  |  Esc: thả chuột");
 GUI.Label(new Rect(18,Screen.height-30,Screen.width-36,24),"1–5 / cuộn chuột: chọn đồ  |  E: nhặt/dùng  |  F: cửa (Shift: mạnh)  |  G: thả  |  L: đèn pin  |  F2: micro");
 }
}
