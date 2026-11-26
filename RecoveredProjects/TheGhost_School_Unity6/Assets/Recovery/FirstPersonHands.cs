using UnityEngine;
public class FirstPersonHands : MonoBehaviour {
 public GameObject customFlashlightPrefab;
 public Material customFlashlightMaterial;
 public GameObject customNotePrefab;
 public Material customNoteMaterial;
 public GameObject customHandsPrefab;
 public Material customHandsMaterial;
 Transform rig,rightHandHolder,leftHandHolder; Light beam; float phase; bool equipped=true; public void SetEquipped(bool value){equipped=value; if(rig)rig.gameObject.SetActive(value);}
 bool holdingNote;
 public bool FlashlightOn => beam && beam.enabled;
 public void ToggleFlashlight(){if(equipped&&!holdingNote&&beam){var battery=GetComponent<Flashlight>();if(beam.enabled || !battery || battery.Battery>0)beam.enabled=!beam.enabled;}}
 public void SetHeldItem(bool hasItem,bool note){
 holdingNote=note;SetEquipped(hasItem);if(!rig)return;
 foreach(Transform child in rig){
 if(child.name.StartsWith("Flashlight"))child.gameObject.SetActive(!note);
 if(child.name=="Held note")child.gameObject.SetActive(note);
 }
 PositionHands(note);
 }
 Material skin,cloth,metal,lens;
 public void Build(){
  if(transform.Find("First person arms"))return;
  skin=Mat("Skin",new Color(.48f,.29f,.19f));cloth=Mat("Dark jacket",new Color(.07f,.09f,.11f));metal=Mat("Flashlight metal",new Color(.045f,.05f,.055f));lens=Mat("Lens",new Color(.7f,.85f,.8f));
  rig=new GameObject("First person arms").transform;rig.SetParent(transform,false);
  if(!customHandsPrefab){
   Arm("Right",new Vector3(.33f,-.45f,.16f),new Vector3(.24f,-.20f,.48f),true);
   Arm("Left",new Vector3(-.34f,-.48f,.17f),new Vector3(-.20f,-.30f,.50f),false);
  }
  var lamp=Part("Flashlight grip",PrimitiveType.Cylinder,new Vector3(.24f,-.17f,.52f),new Vector3(.065f,.13f,.065f),metal);lamp.localRotation=Quaternion.Euler(90,0,0);
  var head=Part("Flashlight head",PrimitiveType.Cylinder,new Vector3(.24f,-.17f,.67f),new Vector3(.10f,.055f,.10f),metal);head.localRotation=Quaternion.Euler(90,0,0);
  var glass=Part("Flashlight lens",PrimitiveType.Cylinder,new Vector3(.24f,-.17f,.727f),new Vector3(.082f,.003f,.082f),lens);glass.localRotation=Quaternion.Euler(90,0,0);
  var existing=GetComponent<Light>();if(existing)existing.enabled=false;
  var beamObject=new GameObject("Flashlight beam");beamObject.transform.SetParent(rig,false);beamObject.transform.localPosition=new Vector3(.24f,-.17f,.74f);beam=beamObject.AddComponent<Light>();beam.type=LightType.Spot;beam.range=28;beam.spotAngle=58;beam.intensity=2;beam.color=new Color(.9f,.96f,1);beam.transform.localRotation=Quaternion.identity;
 }
 void Start(){Build();rig=transform.Find("First person arms");beam=rig.GetComponentInChildren<Light>();rig.gameObject.SetActive(equipped);
 if(customHandsPrefab){
  rightHandHolder=CreateHand("Right real hand",false);
  leftHandHolder=CreateHand("Left real hand",true);
  foreach(Transform child in rig)if(child!=rightHandHolder && child!=leftHandHolder && (child.name.StartsWith("Right ")||child.name.StartsWith("Left ")))
   foreach(var r in child.GetComponentsInChildren<Renderer>(true))r.enabled=false;
  PositionHands(false);
 }
 if(customFlashlightPrefab && !rig.Find("Flashlight custom")){
  var custom=Instantiate(customFlashlightPrefab,rig);custom.name="Flashlight custom";
  foreach(var c in custom.GetComponentsInChildren<Collider>(true))Destroy(c);
  var rs=custom.GetComponentsInChildren<Renderer>(true);
  if(rs.Length>0){if(customFlashlightMaterial)foreach(var r in rs)r.sharedMaterial=customFlashlightMaterial;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var scale=Mathf.Min(.13f/Mathf.Max(b.size.x,.0001f),.13f/Mathf.Max(b.size.y,.0001f),.26f/Mathf.Max(b.size.z,.0001f));
   custom.transform.localScale=custom.transform.localScale*scale;
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   custom.transform.position+=rig.TransformPoint(new Vector3(.24f,-.17f,.61f))-b.center;
  }
  foreach(Transform child in rig)if(child.name=="Flashlight grip"||child.name=="Flashlight head"||child.name=="Flashlight lens"){
   var renderer=child.GetComponent<Renderer>();if(renderer)renderer.enabled=false;
  }
 }
 if(!rig.Find("Held note")){
  if(customNotePrefab){
   var paper=Instantiate(customNotePrefab,rig);paper.name="Held note";
   foreach(var c in paper.GetComponentsInChildren<Collider>(true))Destroy(c);
   var rs=paper.GetComponentsInChildren<Renderer>(true);
   if(rs.Length>0){
    foreach(var r in rs)if(customNoteMaterial)r.sharedMaterial=customNoteMaterial;
    var t=paper.transform;t.localPosition=Vector3.zero;t.localRotation=Quaternion.Euler(-75,0,0);
    var baseScale=t.localScale;
    var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    float scale=Mathf.Min(.30f/Mathf.Max(b.size.x,.0001f),.40f/Mathf.Max(b.size.y,.0001f));
    t.localScale=baseScale*scale;
    b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    t.position+=rig.TransformPoint(new Vector3(-.06f,-.23f,.56f))-b.center;
   }
   paper.SetActive(false);
  }else{
   var paper=Part("Held note",PrimitiveType.Cube,new Vector3(.05f,-.25f,.53f),new Vector3(.27f,.32f,.008f),Mat("Held paper",new Color(.85f,.80f,.65f)));
   paper.localRotation=Quaternion.Euler(18,-8,0);paper.gameObject.SetActive(false);
  }
 }
 }
 Transform CreateHand(string name,bool left){
  var holder=new GameObject(name).transform;holder.SetParent(rig,false);
  var hand=Instantiate(customHandsPrefab,holder);hand.name="Anatomical hand mesh";
  foreach(var c in hand.GetComponentsInChildren<Collider>(true))Destroy(c);
  var rs=hand.GetComponentsInChildren<Renderer>(true);
  if(rs.Length>0){
   foreach(var r in rs)if(customHandsMaterial)r.sharedMaterial=customHandsMaterial;
   var t=hand.transform;t.localPosition=Vector3.zero;t.localRotation=Quaternion.Euler(0,0,180);
   var baseScale=t.localScale;
   var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   float scale=Mathf.Min(.14f/Mathf.Max(b.size.x,.0001f),.18f/Mathf.Max(b.size.y,.0001f));
   t.localScale=baseScale*scale;
   if(left)t.localScale=new Vector3(-t.localScale.x,t.localScale.y,t.localScale.z);
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   t.position+=holder.position-b.center;
  }
  return holder;
 }
 void PositionHands(bool note){
  if(!rightHandHolder||!leftHandHolder)return;
  rightHandHolder.localPosition=note?new Vector3(.16f,-.38f,.47f):new Vector3(.25f,-.26f,.59f);
  leftHandHolder.localPosition=note?new Vector3(-.27f,-.38f,.47f):new Vector3(-.30f,-.50f,.32f);
 }
 Material Mat(string n,Color c){var m=new Material(Shader.Find("Standard"));m.name=n;m.color=c;m.SetFloat("_Glossiness",.25f);return m;}
 Transform Part(string n,PrimitiveType type,Vector3 pos,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(rig,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;var col=g.GetComponent<Collider>();col.enabled=false;if(Application.isPlaying)Destroy(col);else DestroyImmediate(col);return g.transform;}
 void Arm(string side,Vector3 elbow,Vector3 wrist,bool grip){
  var mid=(elbow+wrist)*.5f;var sleeve=Part(side+" sleeve",PrimitiveType.Capsule,mid,new Vector3(.115f,Vector3.Distance(elbow,wrist)*.5f,.115f),cloth);sleeve.localRotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);
  Part(side+" palm",PrimitiveType.Sphere,wrist,new Vector3(.09f,.065f,.115f),skin);
  for(int i=0;i<4;i++){var pos=wrist+new Vector3((i-1.5f)*.019f,-.012f,.055f);var finger=Part(side+" finger "+i,PrimitiveType.Capsule,pos,new Vector3(.018f,.035f,.018f),skin);finger.localRotation=Quaternion.Euler(grip?35:70,0,0);}
  Part(side+" thumb",PrimitiveType.Capsule,wrist+new Vector3(grip?-.043f:.043f,.005f,.015f),new Vector3(.023f,.035f,.023f),skin).localRotation=Quaternion.Euler(45,0,35);
 }
 void LateUpdate(){if(!rig||!equipped||Cursor.lockState!=CursorLockMode.Locked)return;if(Input.GetKeyDown(KeyCode.L))ToggleFlashlight();
  bool moving=Cursor.lockState==CursorLockMode.Locked&&(Mathf.Abs(Input.GetAxisRaw("Vertical"))+Mathf.Abs(Input.GetAxisRaw("Horizontal"))>.1f);
  phase+=Time.deltaTime*(moving?((Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))?8:12):1.8f);
  float amp=moving?.012f:.002f;var target=new Vector3(Mathf.Sin(phase)*amp,Mathf.Cos(phase*2)*amp,0);
  rig.localPosition=Vector3.Lerp(rig.localPosition,target,Time.deltaTime*12);
  rig.localRotation=Quaternion.Slerp(rig.localRotation,Quaternion.Euler(-Input.GetAxis("Mouse Y")*.6f,Input.GetAxis("Mouse X")*.6f,Mathf.Sin(phase)*amp*35),Time.deltaTime*10);
 }
}
