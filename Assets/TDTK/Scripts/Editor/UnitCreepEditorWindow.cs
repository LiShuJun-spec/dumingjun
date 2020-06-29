using UnityEngine;
using UnityEditor;

using System;

using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {

	public class UnitCreepEditorWindow : UnitEditorWindow {
		
		private static UnitCreepEditorWindow window;
		
		
		public static void Init() {
			 // Get existing open window or if none, make a new one:
			window = (UnitCreepEditorWindow)EditorWindow.GetWindow(typeof (UnitCreepEditorWindow));
			//~ window.minSize=new Vector2(375, 449);
			//~ window.maxSize=new Vector2(375, 800);
			
			EditorDBManager.Init();
			
			InitLabel();
			UpdateObjectHierarchyList();
		}
		
		private static string[] creepTypeLabel;
		private static string[] creepTypeTooltip;
		
		private static string[] animationTypeLabel;
		private static string[] animationTypeTooltip;
		
		private static void InitLabel(){
			int enumLength = Enum.GetValues(typeof(_CreepType)).Length;
			creepTypeLabel=new string[enumLength];
			creepTypeTooltip=new string[enumLength];
			for(int i=0; i<enumLength; i++){
				creepTypeLabel[i]=((_CreepType)i).ToString();
				if((_CreepType)i==_CreepType.Default) 		creepTypeTooltip[i]="Typical TD creep, just moving from start to finish";
				if((_CreepType)i==_CreepType.Offense) 	creepTypeTooltip[i]="Offensive creep, creep will attack tower";
				if((_CreepType)i==_CreepType.Support) 	creepTypeTooltip[i]="Support creep, creep will buff other creep";
			}
			
			enumLength = Enum.GetValues(typeof(UnitCreepAnimation._AniType)).Length;
			animationTypeLabel=new string[enumLength];
			animationTypeTooltip=new string[enumLength];
			for(int i=0; i<enumLength; i++){
				animationTypeLabel[i]=((UnitCreepAnimation._AniType)i).ToString();
				if((UnitCreepAnimation._AniType)i==UnitCreepAnimation._AniType.None) 		animationTypeTooltip[i]="Disable animation";
				if((UnitCreepAnimation._AniType)i==UnitCreepAnimation._AniType.Mecanim) 	animationTypeTooltip[i]="Use mecanim animation";
				if((UnitCreepAnimation._AniType)i==UnitCreepAnimation._AniType.Legacy) 	animationTypeTooltip[i]="Use legacy animation";
			}
		}
		
		
		
		
		
		private static List<GameObject> objHList=new List<GameObject>();
		private static string[] objHLabelList=new string[0];
		private static void UpdateObjectHierarchyList(){
			List<UnitCreep> creepList=EditorDBManager.GetCreepList();
			if(creepList.Count==0 || selectID>=creepList.Count) return;
			EditorUtilities.GetObjectHierarchyList(creepList[selectID].gameObject, SetObjListCallback);
		}
		public static void SetObjListCallback(List<GameObject> objList, string[] labelList){
			objHList=objList;
			objHLabelList=labelList;
		}
		
		void SelectCreep(int ID){
			selectID=ID;
			UpdateObjectHierarchyList();
			GUI.FocusControl ("");
			
			if(selectID*35<scrollPos1.y) scrollPos1.y=selectID*35;
			if(listVisibleRect.height-35<selectID*35) scrollPos1.y=(selectID+1)*35-listVisibleRect.height+5;
		}
		
		
		
		
		
		private Vector2 scrollPos1;
		private Vector2 scrollPos2;
		
		
		
		void OnGUI () {
			if(window==null) Init();
			
			List<UnitCreep> creepList=EditorDBManager.GetCreepList();
			
			if(GUI.Button(new Rect(window.position.width-120, 5, 100, 25), "Save")) EditorDBManager.SetDirtyCreep();
			
			EditorGUI.LabelField(new Rect(5, 7, 150, 17), "Add new creep:");
			UnitCreep newCreep=null;
			newCreep=(UnitCreep)EditorGUI.ObjectField(new Rect(100, 7, 140, 17), newCreep, typeof(UnitCreep), false);
			if(newCreep!=null){
				int newSelectID=EditorDBManager.AddNewCreep(newCreep);
				if(newSelectID!=-1) SelectCreep(newSelectID);
			}
			
			
			float startX=5;
			float startY=50;	float cachedY=50;
			
			if(minimiseList){
				if(GUI.Button(new Rect(startX, startY-20, 30, 18), ">>")) minimiseList=false;
			}
			else{
				if(GUI.Button(new Rect(startX, startY-20, 30, 18), "<<")) minimiseList=true;
			}
			Vector2 v2=DrawUnitList(startX, startY, creepList); 	startX=v2.x+25;
			
			if(creepList.Count==0) return;
			
			cont=new GUIContent("Creep Prefab:", "The prefab object of the creep\nClick this to highlight it in the ProjectTab");
			EditorGUI.LabelField(new Rect(startX, startY, width, height), cont);
			EditorGUI.ObjectField(new Rect(startX+90, startY, 185, height), creepList[selectID].gameObject, typeof(GameObject), false);
			startY+=spaceY+10;
			
			
			Rect visibleRect=new Rect(startX, startY, window.position.width-startX-10, window.position.height-startY-5);
			Rect contentRect=new Rect(startX, startY, contentWidth, contentHeight);
			
			//~ GUI.color=new Color(.8f, .8f, .8f, 1f);
			//~ GUI.Box(visibleRect, "");
			//~ GUI.color=Color.white;
			
			scrollPos2 = GUI.BeginScrollView(visibleRect, scrollPos2, contentRect);
				
				UnitCreep creep=creepList[selectID];
				v3=DrawUnitConfigurator(startX, startY, creepList, creep.type==_CreepType.Offense);
				contentWidth=v3.z;
				contentHeight=v3.y;
				
				startX+=spaceX+width+45;
				startY+=75;
				
				if(creep.type!=_CreepType.Default){
					v3=DrawStat(creep.stats[0], startX, startY, statContentHeight, creepList[selectID]);
					statContentHeight=v3.z;
					if(contentHeight<v3.y) contentHeight=v3.y;
					if(contentWidth<530) contentWidth=530;
				}
				
				contentHeight-=cachedY;
				
			GUI.EndScrollView();
			
			
			if(GUI.changed) EditorDBManager.SetDirtyCreep();
			
		}
		
		private float statContentHeight=0;

		
		
		
		private Rect listVisibleRect;
		private Rect listContentRect;
		
		private int deleteID=-1;
		private bool minimiseList=false;
		Vector2 DrawUnitList(float startX, float startY, List<UnitCreep> creepList){
			
			float width=260;
			if(minimiseList) width=60;
			
			
			if(!minimiseList){
				if(GUI.Button(new Rect(startX+180, startY-20, 40, 18), "up")){
					if(selectID>0){
						UnitCreep creep=creepList[selectID];
						creepList[selectID]=creepList[selectID-1];
						creepList[selectID-1]=creep;
						selectID-=1;
						
						if(selectID*35<scrollPos1.y) scrollPos1.y=selectID*35;
					}
				}
				if(GUI.Button(new Rect(startX+222, startY-20, 40, 18), "down")){
					if(selectID<creepList.Count-1){
						UnitCreep creep=creepList[selectID];
						creepList[selectID]=creepList[selectID+1];
						creepList[selectID+1]=creep;
						selectID+=1;
						
						if(listVisibleRect.height-35<selectID*35) scrollPos1.y=(selectID+1)*35-listVisibleRect.height+5;
					}
				}
			}
			
			
			listVisibleRect=new Rect(startX, startY, width+15, window.position.height-startY-5);
			listContentRect=new Rect(startX, startY, width, creepList.Count*40);
			
			GUI.color=new Color(.8f, .8f, .8f, 1f);
			GUI.Box(listVisibleRect, "");
			GUI.color=Color.white;
			
			scrollPos1 = GUI.BeginScrollView(listVisibleRect, scrollPos1, listContentRect);
			
			
				startY+=5;	startX+=5;
			
				for(int i=0; i<creepList.Count; i++){
					
					EditorUtilities.DrawSprite(new Rect(startX, startY+(i*35), 30, 30), creepList[i].iconSprite);
					
					if(minimiseList){
						if(selectID==i) GUI.color = new Color(0, 1f, 1f, 1f);
						if(GUI.Button(new Rect(startX+35, startY+(i*35), 30, 30), "")) SelectCreep(i);
						GUI.color = Color.white;
						
						continue;
					}
					
					
					
					if(selectID==i) GUI.color = new Color(0, 1f, 1f, 1f);
					if(GUI.Button(new Rect(startX+35, startY+(i*35), 150, 30), creepList[i].unitName)) SelectCreep(i);
					GUI.color = Color.white;
					
					if(deleteID==i){
						
						if(GUI.Button(new Rect(startX+190, startY+(i*35), 60, 15), "cancel")) deleteID=-1;
						
						GUI.color = Color.red;
						if(GUI.Button(new Rect(startX+190, startY+(i*35)+15, 60, 15), "confirm")){
							if(selectID>=deleteID) SelectCreep(Mathf.Max(0, selectID-1));
							EditorDBManager.RemoveCreep(deleteID);
							deleteID=-1;
						}
						GUI.color = Color.white;
					}
					else{
						if(GUI.Button(new Rect(startX+190, startY+(i*35), 60, 15), "remove")) deleteID=i;
					}
				}
			
			GUI.EndScrollView();
			
			return new Vector2(startX+width, startY);
		}
		
		
		private static int selectID=0;
		private float contentHeight=0;
		private float contentWidth=0;
		private Vector3 v3;
		
		private bool rscGainFoldout=false;
		
		Vector3 DrawUnitConfigurator(float startX, float startY, List<UnitCreep> creepList, bool offense=false){
			UnitCreep creep=creepList[selectID];
			
			float maxWidth=0;
			//float cachedY=startY;
			float cachedX=startX;
			startX+=65;	//startY+=20;
			
			int type=(int)creep.type;
			cont=new GUIContent("Creep Type:", "Type of the creep. Different type of creep has different capabilities");
			EditorGUI.LabelField(new Rect(startX, startY, width, height), cont);
			contL=new GUIContent[creepTypeLabel.Length];
			for(int i=0; i<contL.Length; i++) contL[i]=new GUIContent(creepTypeLabel[i], creepTypeTooltip[i]);
			type = EditorGUI.Popup(new Rect(startX+80, startY, width-40, 15), new GUIContent(""), type, contL);
			creep.type=(_CreepType)type;
			startX=cachedX;
			
			v3=DrawIconAndName(creep, startX, startY);	startY=v3.y;	maxWidth=v3.z;
			
			
			cont=new GUIContent("Dodge Chance:", "Take value from 0-1. 0 being 0% and 1 being 100%. Final value are subject to target's hitChance. Assume two attackers with 1 hitChance and .8 hitChance and the dodgeChance set to .2, the chances to dodge attack from each attacker are 20% and 40% respectively.");
			EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			creep.stats[0].dodge=EditorGUI.FloatField(new Rect(startX+spaceX, startY, 50, height), creep.stats[0].dodge);
			
			v3=DrawUnitDefensiveSetting(creep, startX, startY, objHList, objHLabelList);	startY=v3.y;	if(maxWidth<v3.z) maxWidth=v3.z;
			
			startY+=20;
			
			
			cont=new GUIContent("Flying:", "Check to mark the creep as flying unit.");
			EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			creep.flying=EditorGUI.Toggle(new Rect(startX+spaceX, startY, 40, height), creep.flying);
			
			cont=new GUIContent("Move Speed:", "Moving speed of the creep");
			EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			creep.moveSpeed=EditorGUI.FloatField(new Rect(startX+spaceX, startY, 40, height), creep.moveSpeed);
			
			cont=new GUIContent("Life Cost:", "The amont of life taken from player when this creep reach it's destination");
			EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			creep.lifeCost=EditorGUI.IntField(new Rect(startX+spaceX, startY, 40, height), creep.lifeCost);
			
			//cont=new GUIContent("Score Value:", "Score gained when destroy this creep");
			//EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			//creep.scoreValue=EditorGUI.IntField(new Rect(startX+spaceX, startY, 40, height), creep.scoreValue);
			
			cont=new GUIContent("Life Gain:", "Life awarded to the player when player successfully destroy this creep");
			EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			creep.lifeValue=EditorGUI.IntField(new Rect(startX+spaceX, startY, 40, height), creep.lifeValue);
			
			cont=new GUIContent("Energy Gain:", "Energy awarded to the player when player successfully destroy this creep");
			EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			creep.valueEnergyGain=EditorGUI.IntField(new Rect(startX+spaceX, startY, 40, height), creep.valueEnergyGain);
			
			
			cont=new GUIContent("Resource Gain Upon Destroyed:", "The amont of life taken from player when this creep reach it's destination");
			//EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
			rscGainFoldout=EditorGUI.Foldout(new Rect(startX, startY+=spaceY, width, height), rscGainFoldout, cont);
			if(rscGainFoldout){
				List<Rsc> rscList=EditorDBManager.GetRscList();
				for(int i=0; i<rscList.Count; i++){
					EditorUtilities.DrawSprite(new Rect(startX+25, startY+=spaceY-2, 20, 20), rscList[i].icon);	startY+=2;
					EditorGUI.LabelField(new Rect(startX, startY, width, height), "    -       min/max");//+rscList[i].name);
					creep.valueRscMin[i]=EditorGUI.IntField(new Rect(startX+spaceX, startY, 40, height), creep.valueRscMin[i]);
					creep.valueRscMax[i]=EditorGUI.IntField(new Rect(startX+spaceX+40, startY, 40, height), creep.valueRscMax[i]);
				}
				startY+=5;
			}
			
			
			string[] creepNameList=EditorDBManager.GetCreepNameList();
			cont=new GUIContent("SpawnUponDestroyed:", "Creep prefab to be spawn when an instance of this unit is destroyed. Note that the HP of the spawned unit is inherit from the destroyed unit. Use HP-multiplier to specifiy how much of the HP should be carried forward");
			GUI.Label(new Rect(startX, startY+=spaceY, width, height), cont);
			int ID=-1;
			for(int i=0; i<creepList.Count; i++){ if(creepList[i].gameObject==creep.spawnUponDestroyed) ID=i+1; }
			ID = EditorGUI.Popup(new Rect(startX+spaceX+30, startY, 120, height), ID, creepNameList);
			if(ID>0 && creepList[ID-1]!=creep) creep.spawnUponDestroyed=creepList[ID-1].gameObject;
			else if(ID==0) creep.spawnUponDestroyed=null;
			
			if(creep.spawnUponDestroyed!=null){
				cont=new GUIContent(" - Num to Spawn:", "The amount of creep to spawn when this unit is destroyed");
				EditorGUI.LabelField(new Rect(startX+20, startY+=spaceY, width, height), cont);
				creep.spawnUponDestroyedCount=EditorGUI.IntField(new Rect(startX+spaceX+30, startY, 40, height), creep.spawnUponDestroyedCount);
				
				cont=new GUIContent(" - HP Multiplier:", "The percentage of HP to pass to the next unit. 0.5 being 50% of parent unit's fullHP, 1 being 100% of parent unit's fullHP");
				EditorGUI.LabelField(new Rect(startX+20, startY+=spaceY, width, height), cont);
				creep.spawnUnitHPMultiplier=EditorGUI.FloatField(new Rect(startX+spaceX+30, startY, 40, height), creep.spawnUnitHPMultiplier);
			}
			
			startY+=20;
			
			
			if(creep.type==_CreepType.Offense){
				cont=new GUIContent("Stop To Attack:", "Check to have the creep stop moving when there's target to attack");
				EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
				creep.stopToAttack=EditorGUI.Toggle(new Rect(startX+spaceX, startY, 40, height), creep.stopToAttack);
				startY+=spaceY;
			}
			v3=DrawUnitOffensiveSetting(creep, startX, startY, objHList, objHLabelList);		startY=v3.y+20;	if(maxWidth<v3.z) maxWidth=v3.z;
			
			
			if(creep.type==_CreepType.Offense) startY+=30;
			
			UnitCreepAnimation ani=creep.gameObject.GetComponent<UnitCreepAnimation>();
			if(ani==null){
				if(GUI.Button(new Rect(startX, startY, width+50, height+2), "Add animation component"))
					ani=creep.gameObject.AddComponent<UnitCreepAnimation>();
			}
			
			if(ani!=null){
				if(GUI.Button(new Rect(startX, startY, width+50, height+2), "Remove animation component")){
					DestroyImmediate(ani, true);
					//creep.gameObject.GetComponent<UnitCreepAnimation>();
					return new Vector3(startX, startY, maxWidth);
				}
				
				startY+=5;
				
				type=(int)ani.type;
				cont=new GUIContent("Type:", "Type of the animation to use");
				contL=new GUIContent[animationTypeLabel.Length];
				for(int i=0; i<contL.Length; i++) contL[i]=new GUIContent(animationTypeLabel[i], animationTypeTooltip[i]);
				EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
				type = EditorGUI.Popup(new Rect(startX+spaceX, startY, width, height), type, contL);
				ani.type=(UnitCreepAnimation._AniType)type;
				
				if(ani.type==UnitCreepAnimation._AniType.Legacy){
					int objID=GetObjectIDFromHList(ani.aniRootObj!=null ? ani.aniRootObj.transform : null, objHList);
					cont=new GUIContent("AnimationRootObj:", "The gameObject that contains the Animation component that runs the animation on the unit");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					objID = EditorGUI.Popup(new Rect(startX+spaceX, startY, width, height), objID, objHLabelList);
					ani.aniRootObj = (objHList[objID]==null) ? null : objHList[objID];
					
					
					cont=new GUIContent("Move Speed Multiplier:", "The multiplier used to match the move animation speed to the unit's move speed");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					ani.moveSpeedMultiplier=EditorGUI.FloatField(new Rect(startX+spaceX+30, startY, 40, height), ani.moveSpeedMultiplier);
					
					
				}
				else if(ani.type==UnitCreepAnimation._AniType.Mecanim){
					int objID=GetObjectIDFromHList(ani.aniRootObj!=null ? ani.aniRootObj.transform : null, objHList);
					cont=new GUIContent("Animator RootObj:", "The gameObject that contains the Animator component that control the animation on the unit");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					objID = EditorGUI.Popup(new Rect(startX+spaceX, startY, width, height), objID, objHLabelList);
					ani.aniRootObj = (objHList[objID]==null) ? null : objHList[objID];
				}
				
				if(ani.type!=UnitCreepAnimation._AniType.None){
					cont=new GUIContent("Spawn Clip:", "The animation clip to be played when the creep is destroyed");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					ani.clipSpawn=(AnimationClip)EditorGUI.ObjectField(new Rect(startX+spaceX, startY, width, height), ani.clipSpawn, typeof(AnimationClip), false);
					
					cont=new GUIContent("Move Clip:", "The animation clip to be played when the creep is moving");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					ani.clipMove=(AnimationClip)EditorGUI.ObjectField(new Rect(startX+spaceX, startY, width, height), ani.clipMove, typeof(AnimationClip), false);
					
					cont=new GUIContent("Dead Clip:", "The animation clip to be played when the creep is destroyed");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					ani.clipDead=(AnimationClip)EditorGUI.ObjectField(new Rect(startX+spaceX, startY, width, height), ani.clipDead, typeof(AnimationClip), false);
					
					cont=new GUIContent("Destination Clip:", "The animation clip to be played when the creep reach its destination");
					EditorGUI.LabelField(new Rect(startX, startY+=spaceY, width, height), cont);
					ani.clipDestination=(AnimationClip)EditorGUI.ObjectField(new Rect(startX+spaceX, startY, width, height), ani.clipDestination, typeof(AnimationClip), false);
				}
			}
			
			
			
			return new Vector3(startX, startY, maxWidth);
		}
		
		
	}

}