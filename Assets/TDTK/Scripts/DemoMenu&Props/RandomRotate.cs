using UnityEngine;
using System.Collections;

public enum _Axis{X, Y, Z}

public class RandomRotate : MonoBehaviour {

	public _Axis rotateAxis;
	
	public float min=-30;
	public float max=30;
	
	private Quaternion targetRot;
	private float rotateSpeed;
	
	// Use this for initialization
	void Start () {
		StartCoroutine(RotateRoutine());
	}
	
	IEnumerator RotateRoutine(){
		yield return new WaitForSeconds(Random.Range(1f, 5f));
		while(true){
			rotateSpeed=Random.Range(3, 6);
			float val=Random.Range(min, max);
			if(rotateAxis==_Axis.X) targetRot=Quaternion.Euler(val, 0, 0);
			else if(rotateAxis==_Axis.Y) targetRot=Quaternion.Euler(0, val, 0);
			else if(rotateAxis==_Axis.Z) targetRot=Quaternion.Euler(0, 0, val);
			yield return new WaitForSeconds(Random.Range(3f, 5f));
		}
	}
	
	// Update is called once per frame
	void Update () {
		transform.localRotation=Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime*rotateSpeed);
	}
}
