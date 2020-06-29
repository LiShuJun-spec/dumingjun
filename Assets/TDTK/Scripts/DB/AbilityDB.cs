using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {

	public class AbilityDB : MonoBehaviour {

		public List<Ability> abilityList=new List<Ability>();
		
		public static AbilityDB LoadDB(){
			GameObject obj=Resources.Load("DB_TDTK/AbilityDB", typeof(GameObject)) as GameObject;
			
			#if UNITY_EDITOR
				if(obj==null) obj=CreatePrefab();
			#endif
			
			return obj.GetComponent<AbilityDB>();
		}
		
		public static List<Ability> Load(){
			GameObject obj=Resources.Load("DB_TDTK/AbilityDB", typeof(GameObject)) as GameObject;
			
			#if UNITY_EDITOR
				if(obj==null) obj=CreatePrefab();
			#endif
			
			AbilityDB instance=obj.GetComponent<AbilityDB>();
			return instance.abilityList;
		}
		
		public static List<Ability> LoadClone(){
			GameObject obj=Resources.Load("DB_TDTK/AbilityDB", typeof(GameObject)) as GameObject;
			AbilityDB instance=obj.GetComponent<AbilityDB>();
			
			List<Ability> newList=new List<Ability>();
			
			if(instance!=null){
				for(int i=0; i<instance.abilityList.Count; i++){
					newList.Add(instance.abilityList[i].Clone());
				}
			}
			
			return newList;
		}
		
		#if UNITY_EDITOR
			private static GameObject CreatePrefab(){
				GameObject obj=new GameObject();
				obj.AddComponent<AbilityDB>();
				GameObject prefab=PrefabUtility.CreatePrefab("Assets/TDTK/Resources/DB_TDTK/AbilityDB.prefab", obj, ReplacePrefabOptions.ConnectToPrefab);
				DestroyImmediate(obj);
				AssetDatabase.Refresh ();
				return prefab;
			}
		#endif
		
	}

}
