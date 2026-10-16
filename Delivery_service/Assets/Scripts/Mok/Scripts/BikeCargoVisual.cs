using System;
using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok {
public sealed class BikeCargoVisual : MonoBehaviour {
    [SerializeField] Transform lid;
    [SerializeField] Transform contents;
    BikePizzaStorage storage;
    GameObject moving;
    GameObject handDisplay;
    public bool IsBusy {get;private set;}
    public Transform Lid => lid;
    public void Configure(Transform hinge,Transform inside){lid=hinge;contents=inside;}
    void Awake(){storage=GetComponent<BikePizzaStorage>();}
    void Start(){Refresh();}
    public void Animate(ToppingInventory inventory,bool storing,int count,Func<bool> commit){
        if(IsBusy||!isActiveAndEnabled||lid==null||contents==null)return;
        IsBusy=true;StartCoroutine(Transfer(inventory,storing,count,commit));
    }
    IEnumerator Turn(float from,float to){for(float t=0;t<.3f;t+=Time.deltaTime){lid.localRotation=Quaternion.Euler(Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,t/.3f)),0,0);yield return null;}lid.localRotation=Quaternion.Euler(to,0,0);}
    GameObject Stack(Transform parent,int count){
        var root=new GameObject("Cargo Pizza Stack");root.transform.SetParent(parent,false);
        var prefab=Resources.Load<GameObject>("MokPizzaArt/Box");
        for(int i=0;i<Mathf.Min(5,count);i++){var box=Instantiate(prefab,root.transform,false);box.transform.localScale=Vector3.one*.45f;box.transform.localPosition=Vector3.up*(.055f*i);foreach(var c in box.GetComponentsInChildren<Collider>())Destroy(c);}
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
        return root;
    }
    IEnumerator Transfer(ToppingInventory inventory,bool storing,int count,Func<bool> commit){
        yield return Turn(0,105);
        var preview=inventory.GetComponent<CarriedItemVisual>();
        handDisplay=preview!=null?preview.Display:null;
        Vector3 hand=handDisplay!=null?handDisplay.transform.position:inventory.HoldPoint.position;
        if(storing&&handDisplay!=null)handDisplay.SetActive(false);
        if(!storing)contents.gameObject.SetActive(false);
        moving=Stack(transform,count);
        Vector3 inside=new Vector3(0,.04f,0),outside=transform.InverseTransformPoint(hand);
        // Lift above the rim before moving sideways, then lower into the compartment.
        Vector3 above=new Vector3(0,.72f,0);
        Vector3 approach=new Vector3(outside.x,Mathf.Max(.72f,outside.y),outside.z);
        for(float t=0;t<.85f;t+=Time.deltaTime){
            float f=Mathf.Clamp01(t/.85f);if(!storing)f=1-f;
            if(f<.25f)moving.transform.localPosition=Vector3.Lerp(outside,approach,Mathf.SmoothStep(0,1,f/.25f));
            else if(f<.65f)moving.transform.localPosition=Vector3.Lerp(approach,above,Mathf.SmoothStep(0,1,(f-.25f)/.4f));
            else moving.transform.localPosition=Vector3.Lerp(above,inside,Mathf.SmoothStep(0,1,(f-.65f)/.35f));
            yield return null;
        }
        commit();
        moving.SetActive(false);Destroy(moving);moving=null;
        if(handDisplay!=null)handDisplay.SetActive(true);handDisplay=null;
        contents.gameObject.SetActive(true);Refresh();
        yield return Turn(105,0);IsBusy=false;
    }
    public void Refresh(){
        if(contents==null)return;if(storage==null)storage=GetComponent<BikePizzaStorage>();
        foreach(Transform t in contents){t.gameObject.SetActive(false);Destroy(t.gameObject);}
        if(storage!=null&&storage.StoredCount>0)Stack(contents,storage.StoredCount);
    }
    void OnDisable(){
        StopAllCoroutines();if(moving!=null){moving.SetActive(false);Destroy(moving);}moving=null;
        if(handDisplay!=null)handDisplay.SetActive(true);handDisplay=null;
        if(lid!=null)lid.localRotation=Quaternion.identity;if(contents!=null)contents.gameObject.SetActive(true);
        IsBusy=false;
    }
}}
