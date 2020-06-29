using UnityEngine;
using System.Collections;
//using System.Collections.Generic;

public class Utility : MonoBehaviour {

	public static Vector3 GetWorldScale(Transform transform){
		Vector3 worldScale = transform.localScale;
		Transform parent = transform.parent;
		
		while (parent != null){
			worldScale = Vector3.Scale(worldScale,parent.localScale);
			parent = parent.parent;
		}
		
		return worldScale;
	}
	
	
	
	
	public static void DestroyColliderRecursively(Transform root){
		foreach(Transform child in root) {
			if(child.GetComponent<Collider>()!=null) {
				Destroy(child.GetComponent<Collider>());
			}
			DestroyColliderRecursively(child);
		}
	}
	
	public static void DisableColliderRecursively(Transform root){
		foreach(Transform child in root) {
			if(child.gameObject.GetComponent<Collider>()!=null)  child.gameObject.GetComponent<Collider>().enabled=false;
			DisableColliderRecursively(child);
		}
	}
	
	
	
	public static void SetMatRecursively(Transform root, string materialName){
		foreach(Transform child in root) {
			if(child.GetComponent<Renderer>()!=null){
				foreach(Material mat in child.GetComponent<Renderer>().materials)
					mat.shader=Shader.Find(materialName);
			}
			SetMatRecursively(child, materialName);
		}
	}
	
	public static void SetMatColorRecursively(Transform root, string colorName, Color color){
		foreach(Transform child in root) {
			if(child.GetComponent<Renderer>()!=null){
				foreach(Material mat in child.GetComponent<Renderer>().materials)  
					mat.SetColor(colorName, color);
			}
			SetMatColorRecursively(child, colorName, color);
		}
	}

	
}

