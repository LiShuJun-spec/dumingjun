using UnityEngine;
using System.Collections;

using TDTK;

public class BGStar : MonoBehaviour {
	
	// Use this for initialization
	void Start () {
		OnRefreshMainCamera();
	}
	
	void OnEnable(){
		FPSControl.onFPSCameraE += OnRefreshMainCamera;
	}
	void OnDisable(){
		FPSControl.onFPSCameraE -= OnRefreshMainCamera;
	}
	
	void OnRefreshMainCamera(){
		Camera mainCam=Camera.main;
		transform.parent=mainCam.transform;
		transform.localPosition=Vector3.zero;
	}
	
}
