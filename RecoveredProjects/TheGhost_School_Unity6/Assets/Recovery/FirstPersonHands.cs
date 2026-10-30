using UnityEngine;
public class FirstPersonHands : MonoBehaviour {
 Transform rig; Light beam; float phase; bool equipped=true; public void SetEquipped(bool value){equipped=value; if(rig)rig.gameObject.SetActive(value);}
 Material skin,cloth,metal,lens;
 public void Build(){
  if(transform.Find("First person arms"))return;
  skin=Mat("Skin",new Color(.48f,.29f,.19f));cloth=Mat("Dark jacket",new Color(.07f,.09f,.11f));metal=Mat("Flashlight metal",new Color(.045f,.05f,.055f));lens=Mat("Lens",new Color(.7f,.85f,.8f));
  rig=new GameObject("First person arms").transform;rig.SetParent(transform,false);
  Arm("Right",new Vector3(.33f,-.45f,.16f),new Vector3(.24f,-.20f,.48f),true);
  Arm("Left",new Vector3(-.34f,-.48f,.17f),new Vector3(-.20f,-.30f,.50f),false);
  var lamp=Part("Flashlight grip",PrimitiveType.Cylinder,new Vector3(.24f,-.17f,.52f),new Vector3(.065f,.13f,.065f),metal);lamp.localRotation=Quaternion.Euler(90,0,0);
  var head=Part("Flashlight head",PrimitiveType.Cylinder,new Vector3(.24f,-.17f,.67f),new Vector3(.10f,.055f,.10f),metal);head.localRotation=Quaternion.Euler(90,0,0);
  var glass=Part("Flashlight lens",PrimitiveType.Cylinder,new Vector3(.24f,-.17f,.727f),new Vector3(.082f,.003f,.082f),lens);glass.localRotation=Quaternion.Euler(90,0,0);
  var existing=GetComponent<Light>();if(existing)existing.enabled=false;
  var beamObject=new GameObject("Flashlight beam");beamObject.transform.SetParent(rig,false);beamObject.transform.localPosition=new Vector3(.24f,-.17f,.74f);beam=beamObject.AddComponent<Light>();beam.type=LightType.Spot;beam.range=28;beam.spotAngle=58;beam.intensity=2;beam.color=new Color(.9f,.96f,1);beam.transform.localRotation=Quaternion.identity;
 }
 void Start(){Build();rig=transform.Find("First person arms");beam=rig.GetComponentInChildren<Light>();rig.gameObject.SetActive(equipped);}
 Material Mat(string n,Color c){var m=new Material(Shader.Find("Standard"));m.name=n;m.color=c;m.SetFloat("_Glossiness",.25f);return m;}
 Transform Part(string n,PrimitiveType type,Vector3 pos,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(rig,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;var col=g.GetComponent<Collider>();col.enabled=false;if(Application.isPlaying)Destroy(col);else DestroyImmediate(col);return g.transform;}
 void Arm(string side,Vector3 elbow,Vector3 wrist,bool grip){
  var mid=(elbow+wrist)*.5f;var sleeve=Part(side+" sleeve",PrimitiveType.Capsule,mid,new Vector3(.115f,Vector3.Distance(elbow,wrist)*.5f,.115f),cloth);sleeve.localRotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);
  Part(side+" palm",PrimitiveType.Sphere,wrist,new Vector3(.09f,.065f,.115f),skin);
  for(int i=0;i<4;i++){var pos=wrist+new Vector3((i-1.5f)*.019f,-.012f,.055f);var finger=Part(side+" finger "+i,PrimitiveType.Capsule,pos,new Vector3(.018f,.035f,.018f),skin);finger.localRotation=Quaternion.Euler(grip?35:70,0,0);}
  Part(side+" thumb",PrimitiveType.Capsule,wrist+new Vector3(grip?-.043f:.043f,.005f,.015f),new Vector3(.023f,.035f,.023f),skin).localRotation=Quaternion.Euler(45,0,35);
 }
 void LateUpdate(){if(!rig||!equipped||Cursor.lockState!=CursorLockMode.Locked)return;if(Input.GetKeyDown(KeyCode.L)&&beam)beam.enabled=!beam.enabled;
  bool moving=Cursor.lockState==CursorLockMode.Locked&&(Mathf.Abs(Input.GetAxisRaw("Vertical"))+Mathf.Abs(Input.GetAxisRaw("Horizontal"))>.1f);
  phase+=Time.deltaTime*(moving?(Input.GetKey(KeyCode.LeftShift)?12:8):1.8f);
  float amp=moving?.012f:.002f;var target=new Vector3(Mathf.Sin(phase)*amp,Mathf.Cos(phase*2)*amp,0);
  rig.localPosition=Vector3.Lerp(rig.localPosition,target,Time.deltaTime*12);
  rig.localRotation=Quaternion.Slerp(rig.localRotation,Quaternion.Euler(-Input.GetAxis("Mouse Y")*.6f,Input.GetAxis("Mouse X")*.6f,Mathf.Sin(phase)*amp*35),Time.deltaTime*10);
 }
}


