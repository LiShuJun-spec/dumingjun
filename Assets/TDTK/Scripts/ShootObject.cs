using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {
	
	public enum _ShootObjectType{Projectile, Missile, Beam, Effect, FPSProjectile, FPSBeam, FPSEffect}
	
	public class ShootObject : MonoBehaviour {
		
		public _ShootObjectType type;
		
		public float speed=5;
		public float beamDuration=.5f;
		
		private Transform shootPoint;
		
		private bool hit=false;
		
		public bool autoSearchLineRenderer=true;
		public List<LineRenderer> lineList=new List<LineRenderer>();
		
		private List<TrailRenderer> trailList=new List<TrailRenderer>();
		
		
		public GameObject shootEffect;
		public GameObject hitEffect;
		
		
		private AttackInstance attInstance;
		
		private GameObject thisObj;
		private Transform thisT;
		
		void Awake(){
			thisObj=gameObject;
			thisT=transform;
			
			thisObj.layer=LayerManager.LayerShootObject();
			
			if(autoSearchLineRenderer){
				LineRenderer[] lines = thisObj.GetComponentsInChildren<LineRenderer>(true);
				for(int i=0; i<lines.Length; i++) lineList.Add(lines[i]);
			}
			
			TrailRenderer[] trails = thisObj.GetComponentsInChildren<TrailRenderer>(true);
			for(int i=0; i<trails.Length; i++) trailList.Add(trails[i]);
			
			if(type==_ShootObjectType.FPSProjectile){
				SphereCollider sphereCol=GetComponent<SphereCollider>();
				if(sphereCol==null){
					sphereCol=thisObj.AddComponent<SphereCollider>();
					sphereCol.radius=0.15f;
				}
				hitRadius=sphereCol.radius;
			}
			
			if(shootEffect!=null) ObjectPoolManager.New(shootEffect);
			if(hitEffect!=null) ObjectPoolManager.New(hitEffect);
		}
		
		void Start () {
		
		}
		
		void OnEnable(){
			for(int i=0; i<trailList.Count; i++) StartCoroutine(ClearTrail(trailList[i]));
		}
		void OnDisable(){
			
		}
		
		
		
		
		
		
		
		public void Shoot(AttackInstance attInst=null, Transform sp=null){
			if(attInst.tgtUnit==null || attInst.tgtUnit.GetTargetT()==null){
				ObjectPoolManager.Unspawn(thisObj);
				return;
			}
			
			attInstance=attInst;
			target=attInstance.tgtUnit;
			targetPos=target.GetTargetT().position;
			hitThreshold=Mathf.Max(0.1f, target.hitThreshold);
			
			shootPoint=sp;
			if(shootPoint!=null) thisT.rotation=shootPoint.rotation;
			
			if(shootEffect!=null) ObjectPoolManager.Spawn(shootEffect, thisT.position, thisT.rotation);
			
			hit=false;
			
			if(type==_ShootObjectType.Projectile) StartCoroutine(ProjectileRoutine());
			if(type==_ShootObjectType.Beam) StartCoroutine(BeamRoutine());
			if(type==_ShootObjectType.Missile) StartCoroutine(MissileRoutine());
			if(type==_ShootObjectType.Effect) StartCoroutine(EffectRoutine());
		}
		
		
		
		public void ShootFPS(AttackInstance attInst=null, Transform sp=null){
			shootPoint=sp;
			if(shootPoint!=null) thisT.rotation=shootPoint.rotation;
			
			if(shootEffect!=null) ObjectPoolManager.Spawn(shootEffect, thisT.position, thisT.rotation);
			
			hit=false;
			attInstance=attInst;
			if(type==_ShootObjectType.FPSProjectile) StartCoroutine(FPSProjectileRoutine());
			if(type==_ShootObjectType.FPSBeam) StartCoroutine(FPSBeamRoutine(sp));
			if(type==_ShootObjectType.FPSEffect) StartCoroutine(FPSEffectRoutine());
		}
		
		
		public float hitRadius=.1f;
		IEnumerator FPSEffectRoutine(){
			yield return new WaitForSeconds(0.05f);
			
			RaycastHit raycastHit;
			Vector3 dir=thisT.TransformDirection(new Vector3(0, 0, 1));
			if(Physics.SphereCast(thisT.position, hitRadius/2, dir, out raycastHit)){
				Unit unit=raycastHit.transform.GetComponent<Unit>();
				FPSHit(unit, raycastHit.point);
				
				if(hitEffect!=null) ObjectPoolManager.Spawn(hitEffect, raycastHit.point, Quaternion.identity);
			}
			
			yield return new WaitForSeconds(0.1f);
			ObjectPoolManager.Unspawn(thisObj);
		}
		
		
		IEnumerator FPSBeamRoutine(Transform sp){
			thisT.parent=sp;
			float duration=0;
			while(duration<beamDuration){
				RaycastHit raycastHit;
				Vector3 dir=thisT.TransformDirection(new Vector3(0, 0, 1));
				bool hitCollider=Physics.SphereCast(thisT.position, hitRadius, dir, out raycastHit);
				if(hitCollider){
					if(!hit){
						hit=true;
						Unit unit=raycastHit.transform.GetComponent<Unit>();
						FPSHit(unit, raycastHit.point);
						
						if(hitEffect!=null) ObjectPoolManager.Spawn(hitEffect, raycastHit.point, Quaternion.identity);
					}
				}
				
				float lineDist=raycastHit.distance==0 ? 9999 : raycastHit.distance;
				for(int i=0; i<lineList.Count; i++) lineList[i].SetPosition(1, new Vector3(0, 0, lineDist));
				
				duration+=Time.fixedDeltaTime;
				yield return new WaitForSeconds(Time.fixedDeltaTime);
			}
			
			thisT.parent=null;
			ObjectPoolManager.Unspawn(thisObj);
		}
		
		
		IEnumerator FPSProjectileRoutine(){
			float timeShot=Time.time;
			while(true){
				RaycastHit raycastHit;
				Vector3 dir=thisT.TransformDirection(new Vector3(0, 0, 1));
				float travelDist=speed*Time.fixedDeltaTime;
				bool hitCollider=Physics.SphereCast(thisT.position, hitRadius, dir, out raycastHit, travelDist);
				if(hitCollider) travelDist=raycastHit.distance+hitRadius;
				
				thisT.Translate(Vector3.forward*travelDist);
				if(Time.time-timeShot>5) break;
				yield return new WaitForSeconds(Time.fixedDeltaTime);
			}
			
			ObjectPoolManager.Unspawn(thisObj);
			yield return null;
		}
		void OnTriggerEnter(Collider collider){
			if(!hit && type!=_ShootObjectType.FPSProjectile) return;
			
			hit=true;
			
			if(hitEffect!=null) ObjectPoolManager.Spawn(hitEffect, thisT.position, Quaternion.identity);
			
			attInstance.impactPoint=thisT.position;
			
			Unit unit=collider.gameObject.GetComponent<Unit>();
			FPSHit(unit, thisT.position);
			
			if(hitEffect!=null) ObjectPoolManager.Spawn(hitEffect, thisT.position, thisT.rotation);
			
			ObjectPoolManager.Unspawn(thisObj);
		}
		
		
		void FPSHit(Unit hitUnit, Vector3 hitPoint){
			if(attInstance.srcWeapon.GetAOERange()>0){
				LayerMask mask1=1<<LayerManager.LayerCreep();
				LayerMask mask2=1<<LayerManager.LayerCreepF();
				LayerMask mask = mask1 | mask2;
				
				Collider[] cols=Physics.OverlapSphere(hitPoint, attInstance.srcWeapon.GetAOERange(), mask);
				if(cols.Length>0){
					List<Unit> tgtList=new List<Unit>();
					for(int i=0; i<cols.Length; i++){
						Unit unit=cols[i].gameObject.GetComponent<Unit>();
						if(!unit.dead) tgtList.Add(unit);
					}
					if(tgtList.Count>0){
						for(int i=0; i<tgtList.Count; i++){
							AttackInstance attInst=new AttackInstance();
							attInst.srcWeapon=attInstance.srcWeapon;
							attInst.tgtUnit=tgtList[i];
							tgtList[i].ApplyEffect(attInst);
						}
					}
				}
			}
			else{
				if(hitUnit!=null && hitUnit.IsCreep()){
					attInstance.tgtUnit=hitUnit;
					hitUnit.ApplyEffect(attInstance);
				}
			}
		}
		
		
		
		
		
		
		
		
		
		private Unit target;
		private Vector3 targetPos;
		public float maxShootAngle=30f;
		public float maxShootRange=0.5f;
		private float hitThreshold=0.15f;		
		
		
		public float GetMaxShootRange(){
			if(type==_ShootObjectType.Projectile || type==_ShootObjectType.Missile) return maxShootRange;
			return 1;
		}
		public float GetMaxShootAngle(){
			if(type==_ShootObjectType.Projectile || type==_ShootObjectType.Missile) return maxShootAngle;
			return 0;
		}
		
		
		IEnumerator EffectRoutine(){
			yield return new WaitForSeconds(0.125f);
			Hit();
		}
		
		IEnumerator BeamRoutine(){
			float timeShot=Time.time;
			
			while(!hit){
				if(target!=null) targetPos=target.GetTargetT().position;
				
				float dist=Vector3.Distance(shootPoint.position, targetPos);
				Ray ray=new Ray(shootPoint.position, (targetPos-shootPoint.position));
				Vector3 targetPosition=ray.GetPoint(dist-hitThreshold);
				
				for(int i=0; i<lineList.Count; i++){
					lineList[i].SetPosition(0, shootPoint.position);
					lineList[i].SetPosition(1, targetPosition);
				}
				
				if(Time.time-timeShot>beamDuration){
					Hit();
					break;
				}
				
				yield return null;
			}
		}
		
		
		IEnumerator ProjectileRoutine(){
			if(shootEffect!=null) Instantiate(shootEffect, thisT.position, thisT.rotation);
			
			float timeShot=Time.time;
			
			//make sure the shootObject is facing the target and adjust the projectile angle
			thisT.LookAt(targetPos);
			float angle=Mathf.Min(1, Vector3.Distance(thisT.position, targetPos)/maxShootRange)*maxShootAngle;
			//clamp the angle magnitude to be less than 45 or less the dist ratio will be off
			thisT.rotation=thisT.rotation*Quaternion.Euler(-angle, 0, 0);
			
			Vector3 startPos=thisT.position;
			float iniRotX=thisT.rotation.eulerAngles.x;
			
			float y=Mathf.Min(targetPos.y, startPos.y);
			float totalDist=Vector3.Distance(startPos, targetPos);
			
			//while the shootObject havent hit the target
			while(!hit){
				if(target!=null) targetPos=target.GetTargetT().position;
				
				//calculating distance to targetPos
				Vector3 curPos=thisT.position;
				curPos.y=y;
				float currentDist=Vector3.Distance(curPos, targetPos);
				float curDist=Vector3.Distance(thisT.position, targetPos);
				//if the target is close enough, trigger a hit
				if(curDist<hitThreshold && !hit){
					Hit();
					break;
				}
				
				if(Time.time-timeShot<3.5f){
					//calculate ratio of distance covered to total distance
					float invR=1-Mathf.Min(1, currentDist/totalDist);
					
					//use the distance information to set the rotation, 
					//as the projectile approach target, it will aim straight at the target
					Vector3 wantedDir=targetPos-thisT.position;
					if(wantedDir!=Vector3.zero){
						Quaternion wantedRotation=Quaternion.LookRotation(wantedDir);
						float rotX=Mathf.LerpAngle(iniRotX, wantedRotation.eulerAngles.x, invR);
						
						//make y-rotation always face target
						thisT.rotation=Quaternion.Euler(rotX, wantedRotation.eulerAngles.y, wantedRotation.eulerAngles.z);
					}
				}
				else{
					//this shoot time exceed 3.5sec, abort the trajectory and just head to the target
					thisT.LookAt(targetPos);
				}
					
				//move forward
				thisT.Translate(Vector3.forward*Mathf.Min(speed*Time.deltaTime, curDist));
				
				yield return new WaitForSeconds(Time.fixedDeltaTime);
			}
		}
		
		
		public float shootAngleY=20;
		private float missileSpeedModifier=1;
		IEnumerator MissileSpeedRoutine(){
			missileSpeedModifier=.05f;
			float duration=0;
			while(duration<1){
				missileSpeedModifier=Mathf.Sin(Mathf.Sin(duration*Mathf.PI/2)*Mathf.PI/2);
				duration+=Time.deltaTime*1f;
				yield return null;
			}
			missileSpeedModifier=1;
		}
		IEnumerator MissileRoutine() {
			StartCoroutine(MissileSpeedRoutine());
			
			float angleX=Random.Range(maxShootAngle/2, maxShootAngle);
			float angleY=Random.Range(shootAngleY/2, maxShootAngle);
			if(Random.Range(0f, 1f)>0.5f) angleY*=-1;
			thisT.LookAt(targetPos);
			thisT.rotation=thisT.rotation;
			Quaternion wantedRotation=thisT.rotation*Quaternion.Euler(-angleX, angleY, 0);
			float rand=Random.Range(4f, 10f);
			
			float totalDist=Vector3.Distance(thisT.position, targetPos);
			
			float estimateTime=totalDist/speed;
			float shootTime=Time.time;
			
			Vector3 startPos=thisT.position;
			
			while(!hit){
				if(target!=null) targetPos=target.GetTargetT().position;
				float currentDist=Vector3.Distance(thisT.position, targetPos);
				
				float delta=totalDist-Vector3.Distance(startPos, targetPos);
				float eTime=estimateTime-delta/speed;
				
				if(Time.time-shootTime>eTime){
					Vector3 wantedDir=targetPos-thisT.position;
					if(wantedDir!=Vector3.zero){
						wantedRotation=Quaternion.LookRotation(wantedDir);
						float val1=(Time.time-shootTime)-(eTime);
						thisT.rotation=Quaternion.Slerp(thisT.rotation, wantedRotation, val1/(eTime*currentDist));
					}
				}
				else thisT.rotation=Quaternion.Slerp(thisT.rotation, wantedRotation, Time.fixedDeltaTime*rand);
				
				if(currentDist<hitThreshold){
					Hit();
					break;
				}
				
				thisT.Translate(Vector3.forward*Mathf.Min(speed*Time.fixedDeltaTime*missileSpeedModifier, currentDist));
				yield return new WaitForSeconds(Time.fixedDeltaTime);
			}
		}
		
		
		
		
		
		void Hit(){
			hit=true;
			
			if(hitEffect!=null) ObjectPoolManager.Spawn(hitEffect, targetPos, Quaternion.identity);
			
			//thisT.position=targetPos;
			attInstance.impactPoint=targetPos;
			
			if(attInstance.srcUnit.GetAOERadius()>0){
				LayerMask mask=attInstance.srcUnit.GetTargetMask();
				
				Collider[] cols=Physics.OverlapSphere(targetPos, attInstance.srcUnit.GetAOERadius(), mask);
				if(cols.Length>0){
					List<Unit> tgtList=new List<Unit>();
					for(int i=0; i<cols.Length; i++){
						Unit unit=cols[i].gameObject.GetComponent<Unit>();
						if(!unit.dead) tgtList.Add(unit);
					}
					if(tgtList.Count>0){
						for(int i=0; i<tgtList.Count; i++){
							if(tgtList[i]==target) target.ApplyEffect(attInstance);
							else{
								AttackInstance attInst=new AttackInstance();
								attInst.srcUnit=attInstance.srcUnit;
								attInst.tgtUnit=tgtList[i];
								attInst.Process();
								tgtList[i].ApplyEffect(attInst);
							}
						}
					}
				}
			}
			else{
				if(target!=null) target.ApplyEffect(attInstance);
			}
			
			ObjectPoolManager.Unspawn(thisObj);
			//Destroy(thisObj);
		}
		
		
		
		
		
		
		
		
		
		IEnumerator ClearTrail(TrailRenderer trail){
			if(trail==null) yield break;
			float trailDuration=trail.time;
			trail.time=-1;
			yield return null;
			trail.time=trailDuration;
		}
		
		
		
	}

}