using UnityEngine;
using UnityEditor;

using System.Collections;

using TDTK;

namespace TDTK {

	public class MenuExtension : EditorWindow {
		
		[MenuItem ("Tools/TDTK/New Scene - Fixed Path", false, -100)]
		private static void NewScene(){
			CreateEmptyScene();
			
			GameObject obj=(GameObject)Instantiate(Resources.Load("ScenePrefab/TDTK_FixedPath", typeof(GameObject)));
			obj.name="TDTK_FixedPath";
			
			SpawnManager spawnManager=(SpawnManager)FindObjectOfType(typeof(SpawnManager));
			if(spawnManager.waveList[0].subWaveList[0].unit==null)
				spawnManager.waveList[0].subWaveList[0].unit=CreepDB.GetFirstPrefab().gameObject;
		}
		
		[MenuItem ("Tools/TDTK/New Scene - Open Path", false, -100)]
		static void New2 () {
			CreateEmptyScene();
			
			GameObject obj=(GameObject)Instantiate(Resources.Load("ScenePrefab/TDTK_OpenPath", typeof(GameObject)));
			obj.name="TDTK_OpenPath";
			
			SpawnManager spawnManager=(SpawnManager)FindObjectOfType(typeof(SpawnManager));
			if(spawnManager.waveList[0].subWaveList[0].unit==null)
				spawnManager.waveList[0].subWaveList[0].unit=CreepDB.GetFirstPrefab().gameObject;
		}

		static void CreateEmptyScene(){
			EditorApplication.NewScene();
			GameObject camObj=Camera.main.gameObject; 	DestroyImmediate(camObj);
			Light light=GameObject.FindObjectOfType<Light>();
			if(light!=null) DestroyImmediate(light.gameObject);
			
			RenderSettings.skybox=null;
		}
		
		
		
		
		
		
		
		[MenuItem ("Tools/TDTK/CreepEditor", false, 10)]
		static void OpenCreepEditor () {
			UnitCreepEditorWindow.Init();
		}
		
		[MenuItem ("Tools/TDTK/TowerEditor", false, 10)]
		static void OpenTowerEditor () {
			UnitTowerEditorWindow.Init();
		}
		
		[MenuItem ("Tools/TDTK/SpawnEditor", false, 10)]
		static void OpenSpawnEditor () {
			SpawnEditorWindow.Init();
		}
		
		[MenuItem ("Tools/TDTK/FPSWeaponEditor", false, 10)]
		public static void OpenAbilityEditor () {
			FPSWeaponEditorWindow.Init();
		}
		
		[MenuItem ("Tools/TDTK/AbilityEditor", false, 10)]
		public static void OpenFPSWeaponEditor () {
			AbilityEditorWindow.Init();
		}
		
		[MenuItem ("Tools/TDTK/PerkEditor", false, 10)]
		public static void OpenPerkEditor () {
			PerkEditorWindow.Init();
		}
		
		[MenuItem ("Tools/TDTK/ResourceDBEditor", false, 10)]
		public static void OpenResourceEditor () {
			ResourceDBEditor.Init();
		}
		
		[MenuItem ("Tools/TDTK/DamageArmorTable", false, 10)]
		public static void OpenDamageTable () {
			DamageArmorDBEditor.Init();
		}
		
		[MenuItem ("Tools/TDTK/Contact and Support Info", false, 100)]
		static void OpenForumLink () {
			SupportContactWindow.Init();
		}
		
		
	}


}