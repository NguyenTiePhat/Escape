using UnityEngine;
public sealed class SlidingDrawer : MonoBehaviour, IInteractable {
 public Vector3 localTravel;
 public float duration=.45f;
 public Transform cabinet;
 public bool IsOpen {get;private set;}
 Vector3 closed; bool ready;
 public string Prompt=>IsOpen?"Đẩy ngăn kéo vào":"Kéo ngăn tủ ra";
 void Awake(){Initialize();}
 public void Initialize(){if(ready)return;closed=transform.localPosition;ready=true;}
 public void Interact(){Initialize();IsOpen=!IsOpen;}
 void Update(){Tick(Time.deltaTime);}
 public void Tick(float dt){
 Initialize();if(dt<=0)return;
 var next=Vector3.MoveTowards(transform.localPosition,closed+(IsOpen?localTravel:Vector3.zero),localTravel.magnitude*dt/Mathf.Max(.1f,duration));
 Vector3 delta=transform.parent.TransformVector(next-transform.localPosition);
 var box=GetComponent<BoxCollider>();
 if(box&&delta.sqrMagnitude>0){
 foreach(var hit in Physics.BoxCastAll(box.bounds.center,box.bounds.extents*.98f,delta.normalized,Quaternion.identity,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
 if(hit.collider && !hit.transform.IsChildOf(cabinet?cabinet:transform)) return;
 }
 transform.localPosition=next;
 }
}
