using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {

	public enum _CreepType{Default, Offense, Support}
	
	public class UnitCreep : Unit {
		
		public delegate void DestinationHandler(UnitCreep unit);
		public static event DestinationHandler onDestinationE;
		
		public _CreepType type=_CreepType.Default;
		public bool flying=false;
		
		public GameObject spawnUponDestroyed;
		public int spawnUponDestroyedCount=0;
		public float spawnUnitHPMultiplier=0.5f;
		
		public int waveID=0;
		public int lifeCost=1;
		public int scoreValue=1;
		
		public int lifeValue=0;
		public List<int> valueRscMin=new List<int>();
		public List<int> valueRscMax=new List<int>();
		public int valueEnergyGain=0;
		
		public bool stopToAttack=false;
		
		private Vector3 pathDynamicOffset;
		public Vector3 GetPathDynamicOffset(){ return pathDynamicOffset; }
		
		public override void Awake() {
			SetSubClass(this);
			
			base.Awake();
			
			if(thisObj.GetComponent<Collider>()==null){
				thisObj.AddComponent<SphereCollider>();
			}
		}
		
		public override void Start() {
			base.Start();
		}
		
		//parent unit is for unit which is spawned from destroyed unit
		public void Init(PathTD p, int ID, int wID, UnitCreep parentUnit=null){
			Init();
			
			path=p;
			instanceID=ID;
			waveID=wID;
			
			float dynamicX=Random.Range(-path.dynamicOffset, path.dynamicOffset);
			float dynamicZ=Random.Range(-path.dynamicOffset, path.dynamicOffset);
			pathDynamicOffset=new Vector3(dynamicX, 0, dynamicZ);
			thisT.position+=pathDynamicOffset;
			
			if(parentUnit==null){
				waypointID=1;
				subWaypointID=0;
				subPath=path.GetWPSectionPath(waypointID);
			}
			else{
				//inherit stats and path from parent unit
				waypointID=parentUnit.waypointID;
				subWaypointID=parentUnit.subWaypointID;
				subPath=parentUnit.subPath;
				
				fullHP=parentUnit.fullHP*parentUnit.spawnUnitHPMultiplier;
				fullShield=parentUnit.fullShield*parentUnit.spawnUnitHPMultiplier;
				HP=fullHP; shield=fullShield;
			}
			
			distFromDestination=CalculateDistFromDestination();
			
			if(type==_CreepType.Offense){
				StartCoroutine(ScanForTargetRoutine());
				StartCoroutine(TurretRoutine());
			}
			if(type==_CreepType.Support){
				StartCoroutine(SupportRoutine());
			}
		}
		
		
		
		
		public override void Update() {
			base.Update();
		}
		
		public override void OnEnable(){
			SubPath.onPathChangedE += OnSubPathChanged;
		}
		public override void OnDisable(){
			SubPath.onPathChangedE -= OnSubPathChanged;
		}
		
		
		
		
		
		public float rotateSpd=10;
		public float moveSpeed=3;
		
		public PathTD path;
		public List<Vector3> subPath=new List<Vector3>();
		public int waypointID=1;
		public int subWaypointID=0;
		
		void OnSubPathChanged(SubPath platformSubPath){
			if(platformSubPath.parentPath==path && platformSubPath.wpIDPlatform==waypointID){
				ResetSubPath(platformSubPath);
			}
		}
		private static Transform dummyT;
		void ResetSubPath(SubPath platformSubPath){
			if(dummyT==null) dummyT=new GameObject().transform;
			
			Quaternion rot=Quaternion.LookRotation(subPath[subWaypointID]-thisT.position);
			dummyT.rotation=rot;
			dummyT.position=thisT.position;
			
			Vector3 pos=dummyT.TransformPoint(0, 0, BuildManager.GetGridSize()/2);
			NodeTD startN=PathFinder.GetNearestNode(pos, platformSubPath.parentPlatform.GetNodeGraph());
			PathFinder.GetPath(startN, platformSubPath.endN, platformSubPath.parentPlatform.GetNodeGraph(), this.SetSubPath);
		}
		void SetSubPath(List<Vector3> pathList){
			subPath=pathList;
			subWaypointID=0;
			distFromDestination=CalculateDistFromDestination();
		}
		
		public override void FixedUpdate(){
			base.FixedUpdate();
			
			if(!stunned && !dead){
				if(MoveToPoint(subPath[subWaypointID])){
					subWaypointID+=1;
					if(subWaypointID>=subPath.Count){
						subWaypointID=0;
						waypointID+=1;
						if(waypointID>=path.GetPathWPCount()){
							ReachDestination();
						}
						else{
							subPath=path.GetWPSectionPath(waypointID);
						}
					}
				}
			}
		}
		
		//function call to rotate and move toward a pecific point, return true when the point is reached
		public bool MoveToPoint(Vector3 point){
			if(type==_CreepType.Offense && stopToAttack && target!=null) return false;
			
			//this is for dynamic waypoint, each unit creep have it's own offset pos
			//point+=dynamicOffset;
			point+=pathDynamicOffset;//+flightHeightOffset;
			
			float dist=Vector3.Distance(point, thisT.position);
			
			//if the unit have reached the point specified
			//~ if(dist<0.15f) return true;
			if(dist<0.005f) return true;
			
			//rotate towards destination
			if(moveSpeed>0){
				Quaternion wantedRot=Quaternion.LookRotation(point-thisT.position);
				thisT.rotation=Quaternion.Slerp(thisT.rotation, wantedRot, rotateSpd*Time.deltaTime);
			}
			
			//move, with speed take distance into accrount so the unit wont over shoot
			Vector3 dir=(point-thisT.position).normalized;
			thisT.Translate(dir*Mathf.Min(dist, moveSpeed * slowMultiplier * Time.fixedDeltaTime), Space.World);
			
			distFromDestination-=(moveSpeed * slowMultiplier * Time.fixedDeltaTime);
			
			return false;
		}
		
		
		void ReachDestination(){
			if(path.loop){
				if(onDestinationE!=null) onDestinationE(this);
				subWaypointID=0;
				waypointID=path.GetLoopPoint();
				subPath=path.GetWPSectionPath(waypointID);
				return;
			}
			
			dead=true;
			
			if(onDestinationE!=null) onDestinationE(this);
			
			float delay=0;
			if(aniInstance!=null){ delay=aniInstance.PlayDestination(); }
			
			StartCoroutine(_ReachDestination(delay));
		}
		IEnumerator _ReachDestination(float duration){
			yield return new WaitForSeconds(duration);
			ObjectPoolManager.Unspawn(thisObj);
		}
		
		
		public float CreepDestroyed(){
			List<int> rscGain=new List<int>();
			for(int i=0; i<valueRscMin.Count; i++){
				rscGain.Add(Random.Range(valueRscMin[i], valueRscMax[i]));
			}
			ResourceManager.GainResource(rscGain, PerkManager.GetRscCreepKilled());
			
			AbilityManager.GainEnergy(valueEnergyGain+(int)PerkManager.GetEnergyWaveClearedModifier());
			
			if(spawnUponDestroyed!=null && spawnUponDestroyedCount>0){
				for(int i=0; i<spawnUponDestroyedCount; i++){
					Vector3 posOffset=new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
					GameObject obj=ObjectPoolManager.Spawn(spawnUponDestroyed, thisT.position+posOffset, thisT.rotation);
					UnitCreep unit=obj.GetComponent<UnitCreep>();
					
					unit.waveID=waveID;
					int ID=SpawnManager.AddDestroyedSpawn(unit);
					unit.Init(path, ID, waveID, this);
				}
			}
			
			if(aniInstance!=null){ return aniInstance.PlayDead(); }
			
			return 0;
		}
		
		
		private UnitCreepAnimation aniInstance;
		public void SetAnimationComponent(UnitCreepAnimation ani){ aniInstance=ani; }
		public void Hit(){ if(aniInstance!=null) aniInstance.PlayHit(); }
		
		
		public float GetMoveSpeed(){ return moveSpeed * slowMultiplier; }
		
		public float distFromDestination=0;
		public float _GetDistFromDestination(){ return distFromDestination; }
		public float CalculateDistFromDestination(){
			float dist=Vector3.Distance(thisT.position, subPath[subWaypointID]);
			for(int i=subWaypointID+1; i<subPath.Count; i++){
				dist+=Vector3.Distance(subPath[i-1], subPath[i]);
			}
			dist+=path.GetPathDistance(waypointID+1);
			
			return dist;
		}
		
		
		
		/*
		//for unity-navmesh based navigation, not in used (reason: requird navmesh carving - pro only feature)
		private NavMeshAgent agent;
		private Vector3 tgtPos;
		public void InitNavMesh(PathTD p, int ID, int wID){
			Init();
			
			path=p;
			instanceID=ID;
			waveID=wID;
			
			tgtPos=path.wpList[path.wpList.Count-1].position;
			agent = thisObj.GetComponent<NavMeshAgent>();
			agent.SetDestination(tgtPos);
			
			StartCoroutine(CheckDestination());
		}
		IEnumerator CheckDestination(){
			while(!stunned && !dead){
				tgtPos.y=thisT.pos.y;
				if(Vector3.Distance(tgtPos, thisT.position)<0.5f) ReachDestination();
				yield return null;
			}
		}
		*/
		
		
		/*
		//check if the creep will turn at the next waypoint, return true if yes, false if otherwise
		bool TurningOnNextWaypoint(){
			Vector3 nextPoint=Vector3.zero;
			
			//if we are not at the end of current subpath
			if(subWaypointID<subPath.Count-1){
				//get next point inc current subpath
				nextPoint=subPath[subWaypointID+1];
			}
			else{
				if(waypointID<path.GetPathWPCount()-1){
					//get the first point of next subpath
					List<Vector3> nextSubPath=	path.GetWPSectionPath(waypointID+1);
					nextPoint=nextSubPath[0];
				}
				else return false;	//end of path, no more turning
			}
			
			//current moving dir
			Vector3 dir1=(subPath[subWaypointID]-thisT.position).normalized;
			//next waypoint moving dir
			Vector3 dir2=(nextSubPath[0]-subPath[subWaypointID]).normalized;
			
			if(dir1==dir2) return false;
			
			return true;
		}
		*/
	}
	
}
