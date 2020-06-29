using UnityEngine;
using System.Collections;

public class SelfDeactivator : MonoBehaviour {

	public bool useObjectPool=true;
	public float duration=1;
	
	
	void OnEnable(){
		StartCoroutine(DeactivateRoutine());
	}
	
	IEnumerator DeactivateRoutine(){
		yield return new WaitForSeconds(duration);
		if(useObjectPool) ObjectPoolManager.Unspawn(gameObject);
		else Destroy(gameObject);
	}
	
}
