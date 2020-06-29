using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {

	public class Unit : MonoBehaviour {
		
		//public delegate void NewUnitHandler(Unit unit);
		//public static event NewUnitHandler onNewUnitE;	//no longer in use for now?
		
		public delegate void DamagedHandler(Unit unit);
		public static event DamagedHandler onDamagedE;	//call when unit HP/shield value is changed, for displaying unit overlay
		
		public delegate void DestroyedHandler(Unit unit);
		public static event DestroyedHandler onDestroyedE;
		
		public int prefabID=-1;
		public int instanceID=-1;
		
		public string unitName="unit";
		public Sprite iconSprite;
		//public Texture icon;
		
		public string desp="";
		
		public enum _UnitSubClass{Creep, Tower};
		public _UnitSubClass subClass=_UnitSubClass.Creep;
		public UnitCreep unitC;
		public UnitTower unitT;
		//Call by inherited class UnitCreep, caching inherited UnitCreep instance to this instance
		public void SetSubClass(UnitCreep unit){ 
			unitC=unit; 
			subClass=_UnitSubClass.Creep;
			if(!unitC.flying) gameObject.layer=LayerManager.LayerCreep();
			else gameObject.layer=LayerManager.LayerCreepF();
		}
		//Call by inherited class UnitTower, caching inherited UnitTower instance to this instance
		public void SetSubClass(UnitTower unit){ 
			unitT=unit; 
			subClass=_UnitSubClass.Tower;
			gameObject.layer=LayerManager.LayerTower();
		}
		public bool IsTower(){ return subClass==_UnitSubClass.Tower ? true : false; }
		public bool IsCreep(){ return subClass==_UnitSubClass.Creep ? true : false; }
		public UnitTower GetUnitTower(){ return unitT; }
		public UnitCreep GetUnitCreep(){ return unitC; }
		
		
		public float defaultHP=10;
		public float fullHP=10;
		public float HP=10;
		public float HPRegenRate=0;
		public float HPStaggerDuration=10;
		private float currentHPStagger=0;
		
		public float defaultShield=0;
		public float fullShield=0;
		public float shield=0;
		public float shieldRegenRate=1;
		public float shieldStaggerDuration=1;
		private float currentShieldStagger=0;
		
		
		public int damageType=0;
		public int armorType=0;
		
		//public bool immuneToDmg=false;
		public bool immuneToCrit=false;
		public bool immuneToSlow=false;
		public bool immuneToStun=false;
		
		
		//public int level=1;
		
		public int currentActiveStat=0;
		public List<UnitStat> stats=new List<UnitStat>();
		
		
		public bool dead=false;
		public bool stunned=false;
		private float stunDuration=0;
		
		public float slowMultiplier=1;
		public List<Slow> slowEffectList=new List<Slow>();
		
		public List<Buff> buffEffect=new List<Buff>();
		
		
		[HideInInspector] public Transform localShootObjectT;
		[HideInInspector] public ShootObject localShootObject;		//get from stats and store locally, just in case the upgraded stats doesnt have any shootObject assigned
		public List<Transform> shootPoints=new List<Transform>();
		public float delayBetweenShootPoint=0;
		public Transform targetPoint;
		public float hitThreshold=0.25f;		//hit distance from the targetPoint for the shootObj
		
		public GameObject thisObj;
		public Transform thisT;
		
		public virtual void Awake(){
			thisObj=gameObject;
			thisT=transform;
			
			if(shootPoints.Count==0) shootPoints.Add(thisT);
			
			ResetBuff();
			
			for(int i=0; i<stats.Count; i++){
				if(stats[i].shootObject!=null){
					stats[i].shootObjectT=stats[i].shootObject.transform;
					if(localShootObject==null) localShootObject=stats[i].shootObject;
				}
				
				if(stats[i].shootObjectT!=null){
					if(localShootObjectT==null) localShootObjectT=stats[i].shootObjectT;
				}
			}
		}
		
		public void Init(){
			dead=false;
			stunned=false;
			
			fullHP=GetFullHP();
			HP=fullHP;
			fullShield=GetFullShield();
			shield=fullShield;
			
			currentHPStagger=0;
			currentShieldStagger=0;
			
			ResetBuff();
			ResetSlow();
		}
		

		
		public virtual void Start() {
		
		}
		
		public virtual void OnEnable() {
		
		}
		public virtual void OnDisable() {
		
		}
		
		public virtual void Update() {
			
		}
		public virtual void FixedUpdate() {
			if(regenHPBuff!=0){
				HP+=regenHPBuff*Time.fixedDeltaTime;
				HP=Mathf.Clamp(HP, 0, fullHP);
			}
			
			if(HPRegenRate>0 && currentHPStagger<=0){
				HP+=GetHPRegenRate()*Time.fixedDeltaTime;
				HP=Mathf.Clamp(HP, 0, fullHP);
			}
			if(fullShield>0 && shieldRegenRate>0 && currentShieldStagger<=0){
				shield+=GetShieldRegenRate()*Time.fixedDeltaTime;
				shield=Mathf.Clamp(shield, 0, fullShield);
			}
			
			currentHPStagger-=Time.fixedDeltaTime;
			currentShieldStagger-=Time.fixedDeltaTime;
			
			
			if(target!=null && !IsInConstruction() && !stunned){
				if(turretObject!=null){
					if(rotateTurretAimInXAxis && barrelObject!=null){
						Vector3 targetPos=target.GetTargetT().position;
						Vector3 dummyPos=targetPos;
						dummyPos.y=turretObject.position.y;
						
						Quaternion wantedRot=Quaternion.LookRotation(dummyPos-turretObject.position);
						turretObject.rotation=Quaternion.Slerp(turretObject.rotation, wantedRot, turretRotateSpeed*Time.deltaTime);
						
						float angle=Quaternion.LookRotation(targetPos-barrelObject.position).eulerAngles.x;
						float distFactor=Mathf.Min(1, Vector3.Distance(turretObject.position, targetPos)/GetSOMaxRange());
						float offset=distFactor*GetSOMaxAngle();
						wantedRot=turretObject.rotation*Quaternion.Euler(angle-offset, 0, 0);
						
						barrelObject.rotation=Quaternion.Slerp(barrelObject.rotation, wantedRot, turretRotateSpeed*Time.deltaTime);
						
						if(Quaternion.Angle(barrelObject.rotation, wantedRot)<aimTolerance) targetInLOS=true;
						else targetInLOS=false;
					}
					else{
						Vector3 targetPos=target.GetTargetT().position;
						if(!rotateTurretAimInXAxis) targetPos.y=turretObject.position.y;
						
						Quaternion wantedRot=Quaternion.LookRotation(targetPos-turretObject.position);
						if(rotateTurretAimInXAxis){
							float distFactor=Mathf.Min(1, Vector3.Distance(turretObject.position, targetPos)/GetSOMaxRange());
							float offset=distFactor*GetSOMaxAngle();
							wantedRot*=Quaternion.Euler(-offset, 0, 0);
						}
						turretObject.rotation=Quaternion.Slerp(turretObject.rotation, wantedRot, turretRotateSpeed*Time.deltaTime);
						
						if(Quaternion.Angle(turretObject.rotation, wantedRot)<aimTolerance) targetInLOS=true;
						else targetInLOS=false;
					}
				}
				else targetInLOS=true;
			}
			
			//rotate turret back to origin
			if(IsCreep() && target==null && turretObject!=null && !stunned){
				turretObject.localRotation=Quaternion.Slerp(turretObject.localRotation, Quaternion.identity, turretRotateSpeed*Time.deltaTime*0.25f);
			}
			
		}
		
		
		
		
		public Transform turretObject;
		public Transform barrelObject;
		public bool rotateTurretAimInXAxis=true;
		float GetSOMaxRange(){ 
			if(stats[currentActiveStat].shootObject==null) return localShootObject.GetMaxShootRange();
			return stats[currentActiveStat].shootObject.GetMaxShootRange();
		}
			
		float GetSOMaxAngle(){ 
			if(stats[currentActiveStat].shootObject==null) return localShootObject.GetMaxShootAngle();
			return stats[currentActiveStat].shootObject.GetMaxShootAngle();
		}
		
		private float turretRotateSpeed=12;
		private float aimTolerance=5;
		private bool targetInLOS=false;
		
		
		public Unit target;
		
		public enum _TargetPriority{Nearest, Weakest, Toughest, First, Random};
		public _TargetPriority targetPriority=_TargetPriority.Random;
		public void SwitchToNextTargetPriority(){
			int nextPrior=(int)targetPriority+1;
			if(nextPrior>=5) nextPrior=0;
			targetPriority=(_TargetPriority)nextPrior;
		}
		
		public void ChangeScanAngle(int angle){
			if(turretObject!=null && target==null)
				turretObject.localRotation=Quaternion.identity*Quaternion.Euler(0f, dirScanAngle, 0f);
			dirScanAngle=angle;
			dirScanRot=thisT.rotation*Quaternion.Euler(0f, dirScanAngle, 0f);
			if(IsTower()) GameControl.TowerScanAngleChanged(unitT);
		}
		
		
		public bool directionalTargeting=false;
		public float dirScanAngle=0;
		public float dirScanFOV=30;
		private Vector3 dirScanV;
		private Quaternion dirScanRot;
		protected LayerMask maskTarget=0;
		public LayerMask GetTargetMask(){ return maskTarget; }
		
		public Transform scanDirT;
		
		public IEnumerator ScanForTargetRoutine(){
			if(subClass==_UnitSubClass.Tower){
				if(unitT.targetMode==_TargetMode.Hybrid){
					LayerMask mask1=1<<LayerManager.LayerCreep();
					LayerMask mask2=1<<LayerManager.LayerCreepF();
					
					maskTarget=mask1 | mask2;
				}
				else if(unitT.targetMode==_TargetMode.Air){
					maskTarget=1<<LayerManager.LayerCreepF();
				}
				else if(unitT.targetMode==_TargetMode.Ground){
					maskTarget=1<<LayerManager.LayerCreep();
				}
			}
			else if(subClass==_UnitSubClass.Creep){
				maskTarget=1<<LayerManager.LayerTower();
			}
			
			//initiate scan direction
			//if(directionalTargeting){
			//	if(IsCreep()) dirScanRot=thisT.rotation;
			//	else dirScanRot=thisT.rotation*Quaternion.Euler(0f, dirScanAngle, 0f);
			//}
			
			while(true){
				ScanForTarget();
				yield return new WaitForSeconds(0.1f);
				if(GameControl.ResetTargetAfterShoot()){
					while(turretOnCooldown) yield return null;
				}
			}
			
			//yield return null;
		}
		
		void ScanForTarget(){
			if(dead || IsInConstruction() || stunned) return;
				
			//creeps changes direction so the scan direction for creep needs to be update 
			if(directionalTargeting){
				if(IsCreep()) dirScanRot=thisT.rotation;
				else dirScanRot=thisT.rotation*Quaternion.Euler(0f, dirScanAngle, 0f);
			}
			
			if(directionalTargeting && scanDirT!=null) scanDirT.rotation=dirScanRot;
			
			if(target==null){
				Collider[] cols=Physics.OverlapSphere(thisT.position, GetRange(), maskTarget);
				
				if(cols.Length>0){
					float minRange=GetRangeMin();
					
					List<Unit> tgtList=new List<Unit>();
					for(int i=0; i<cols.Length; i++){
						if(minRange>0 && Vector3.Distance(cols[i].transform.position, thisT.position)<minRange){
							continue;
						}
						
						Unit unit=cols[i].gameObject.GetComponent<Unit>();
						if(!unit.dead) tgtList.Add(unit);
					}
					
					if(directionalTargeting){
						List<Unit> filtered=new List<Unit>();
						for(int i=0; i<tgtList.Count; i++){
							Quaternion currentRot=Quaternion.LookRotation(tgtList[i].thisT.position-thisT.position);
							if(Quaternion.Angle(dirScanRot, currentRot)<=dirScanFOV*0.5f) filtered.Add(tgtList[i]);
						}
						tgtList=filtered;
					}
					
					if(tgtList.Count>0){
						if(targetPriority==_TargetPriority.Random) target=tgtList[Random.Range(0, tgtList.Count-1)];
						else if(targetPriority==_TargetPriority.Nearest){
							float nearest=Mathf.Infinity;
							for(int i=0; i<tgtList.Count; i++){
								float dist=Vector3.Distance(thisT.position, tgtList[i].thisT.position);
								if(dist<nearest){
									nearest=dist;
									target=tgtList[i];
								}
							}
						}
						else if(targetPriority==_TargetPriority.Weakest){
							float lowest=Mathf.Infinity;
							for(int i=0; i<tgtList.Count; i++){
								if(tgtList[i].HP<lowest){
									lowest=tgtList[i].HP;
									target=tgtList[i];
								}
							}
						}
						else if(targetPriority==_TargetPriority.Toughest){
							float highest=0;
							for(int i=0; i<tgtList.Count; i++){
								if(tgtList[i].HP>highest){
									highest=tgtList[i].HP;
									target=tgtList[i];
								}
							}
						}
						else if(targetPriority==_TargetPriority.First){
							target=tgtList[Random.Range(0, tgtList.Count-1)];
							float lowest=Mathf.Infinity;
							for(int i=0; i<tgtList.Count; i++){
								if(tgtList[i].GetDistFromDestination()<lowest){
									lowest=tgtList[i].GetDistFromDestination();
									target=tgtList[i];
								}
							}
						}
					}
				}
				targetInLOS=false;
			}
			else{
				float dist=Vector3.Distance(thisT.position, target.thisT.position);
				if(target.dead || dist>GetRange()) target=null;
				
				if(target!=null && directionalTargeting){
					Quaternion tgtRotation=Quaternion.LookRotation(target.thisT.position-thisT.position);
					if(Quaternion.Angle(dirScanRot, tgtRotation)>=dirScanFOV*0.6f) target=null;
				}
			}
			
		}
		
		
		
		
		public delegate float PlayShootAnimation();
		public PlayShootAnimation playShootAnimation;
		
		
		private bool turretOnCooldown=false;
		public IEnumerator TurretRoutine(){
			for(int i=0; i<shootPoints.Count; i++){
				if(shootPoints[i]==null){ shootPoints.RemoveAt(i);	i-=1;	}
			}
			
			if(shootPoints.Count==0){
				Debug.LogWarning("ShootPoint not assigned for unit - "+unitName+", auto assigned", this);
				shootPoints.Add(thisT);
			}
			
			for(int i=0; i<stats.Count; i++){
				if(stats[i].shootObjectT!=null) ObjectPoolManager.New(stats[i].shootObjectT.gameObject, 3);
			}
			
			yield return null;
			
			while(true){
				while(target==null || stunned || IsInConstruction() || !targetInLOS) yield return null;
				turretOnCooldown=true;
				
				Unit currentTarget=target;
				
				float animationDelay=0;
				if(playShootAnimation!=null) animationDelay=playShootAnimation();
				if(animationDelay>0) yield return new WaitForSeconds(animationDelay);
				
				AttackInstance attInstance=new AttackInstance();
				attInstance.srcUnit=this;
				attInstance.tgtUnit=currentTarget;
				attInstance.Process();
				
				for(int i=0; i<shootPoints.Count; i++){
					Transform sp=shootPoints[i];
					Transform objT=(Transform)Instantiate(GetShootObjectT(), sp.position, sp.rotation);
					ShootObject shootObj=objT.GetComponent<ShootObject>();
					shootObj.Shoot(attInstance, sp);
					
					if(delayBetweenShootPoint>0) yield return new WaitForSeconds(delayBetweenShootPoint);
				}
				
				yield return new WaitForSeconds(GetCooldown()-animationDelay-shootPoints.Count*delayBetweenShootPoint);
				
				if(GameControl.ResetTargetAfterShoot()) target=null;
				turretOnCooldown=false;
			}
		}
		
		public void ApplyEffect(AttackInstance attInstance){
			if(dead) return;
			
			if(attInstance.missed) return;
			
			shield-=attInstance.damageShield;
			HP-=attInstance.damageHP;
			new TextOverlay(thisT.position, attInstance.damage.ToString("f0"), new Color(1f, 1f, 1f, 1f));
			
			if(onDamagedE!=null) onDamagedE(this);
			
			currentHPStagger=GetHPStaggerDuration(); 
			currentShieldStagger=GetShieldStaggerDuration();
			
			if(attInstance.destroy || HP<=0){
				Dead();
				return;
			}
			
			if(attInstance.breakShield) fullShield=0;
			if(attInstance.stunned) ApplyStun(attInstance.stun.duration);
			if(attInstance.slowed) ApplySlow(attInstance.slow);
			if(attInstance.dotted) ApplyDot(attInstance.dot);
		}
		
		public void ApplyStun(float duration){
			stunDuration=duration;
			if(!stunned) StartCoroutine(StunRoutine());
		}
		IEnumerator StunRoutine(){
			stunned=true;
			while(stunDuration>0){
				stunDuration-=Time.deltaTime;
				yield return null;
			}
			stunned=false;
		}
		
		public void ApplySlow(Slow slow){ StartCoroutine(SlowRoutine(slow)); }
		IEnumerator SlowRoutine(Slow slow){
			slowEffectList.Add(slow);
			ResetSlowMultiplier();
			yield return new WaitForSeconds(slow.duration);
			slowEffectList.Remove(slow);
			ResetSlowMultiplier();
		}
		
		void ResetSlowMultiplier(){
			if(slowEffectList.Count==0){
				slowMultiplier=1;
				return;
			}
			
			for(int i=0; i<slowEffectList.Count; i++){
				if(slowEffectList[i].slowMultiplier<slowMultiplier){
					slowMultiplier=slowEffectList[i].slowMultiplier;
				}
			}
			
			slowMultiplier=Mathf.Max(0, slowMultiplier);
		}
		void ResetSlow(){
			slowEffectList=new List<Slow>();
			ResetSlowMultiplier();
		}
		
		public void ApplyDot(Dot dot){ StartCoroutine(DotRoutine(dot)); }
		IEnumerator DotRoutine(Dot dot){
			int count=(int)Mathf.Floor(dot.duration/dot.interval);
			for(int i=0; i<count; i++){
				yield return new WaitForSeconds(dot.interval);
				if(dead) break;
				DamageHP(dot.value);
				if(HP<=0){ Dead();	break; }
			}
		}
		
		
		//for ability and what not
		public void ApplyDamage(float dmg){
			DamageHP(dmg);
			if(HP<=0) Dead();
		}
		public void RestoreHP(float value){
			new TextOverlay(thisT.position, value.ToString("f0"), new Color(0f, 1f, .4f, 1f));
			HP=Mathf.Clamp(HP+value, 0, fullHP);
		}
		
		
		//called when unit take damage
		void DamageHP(float dmg){
			HP-=dmg;
			new TextOverlay(thisT.position, dmg.ToString("f0"), new Color(1f, 1f, 1f, 1f));
			if(onDamagedE!=null) onDamagedE(this);
			
			currentHPStagger=HPStaggerDuration;
			currentShieldStagger=shieldStaggerDuration;
		}
		
		
		
		public List<Unit> buffedUnit=new List<Unit>();
		private  bool supportRoutineRunning=false;
		public IEnumerator SupportRoutine(){
			supportRoutineRunning=true;
			
			LayerMask maskTarget=0;
			if(subClass==_UnitSubClass.Tower){
				maskTarget=1<<LayerManager.LayerTower();
			}
			else if(subClass==_UnitSubClass.Creep){
				LayerMask mask1=1<<LayerManager.LayerCreep();
				LayerMask mask2=1<<LayerManager.LayerCreepF();
				maskTarget=mask1 | mask2;
			}
			
			while(true){
				yield return new WaitForSeconds(0.1f);
				
				if(!dead){
					List<Unit> tgtList=new List<Unit>();
					Collider[] cols=Physics.OverlapSphere(thisT.position, GetRange(), maskTarget);
					if(cols.Length>0){
						for(int i=0; i<cols.Length; i++){
							Unit unit=cols[i].gameObject.GetComponent<Unit>();
							if(!unit.dead) tgtList.Add(unit);
						}
					}
					
					for(int i=0; i<buffedUnit.Count; i++){
						Unit unit=buffedUnit[i];
						if(unit==null || unit.dead){
							buffedUnit.RemoveAt(i); i-=1;
						}
						else if(!tgtList.Contains(unit)){
							unit.UnBuff(GetBuff());
							buffedUnit.RemoveAt(i); i-=1;
						}
					}
					
					for(int i=0; i<tgtList.Count; i++){
						Unit unit=tgtList[i];
						if(!buffedUnit.Contains(unit)){
							unit.Buff(GetBuff());
							buffedUnit.Add(unit);
						}
					}
				}
			}
		}
		public void UnbuffAll(){
			for(int i=0; i<buffedUnit.Count; i++){
				buffedUnit[i].UnBuff(GetBuff());
			}
		}
		
		public List<Buff> activeBuffList=new List<Buff>();
		public void Buff(Buff buff){
			if(activeBuffList.Contains(buff)) return;
			
			activeBuffList.Add(buff);
			UpdateBuffStat();
		}
		public void UnBuff(Buff buff){
			if(!activeBuffList.Contains(buff)) return;
			
			activeBuffList.Remove(buff);
			UpdateBuffStat();
		}
		
		public float damageBuffMul=0f;
		public float cooldownBuffMul=0f;
		public float rangeBuffMul=0f;
		public float criticalBuffMod=0.1f;
		public float hitBuffMod=0.1f;
		public float dodgeBuffMod=0.1f;
		public float regenHPBuff=0.0f;
		
		void UpdateBuffStat(){
			ClearBuffStats();
			for(int i=0; i<activeBuffList.Count; i++){
				Buff buff=activeBuffList[i];
				if(damageBuffMul<buff.damageBuff) damageBuffMul=buff.damageBuff;
				if(cooldownBuffMul<buff.cooldownBuff) cooldownBuffMul=buff.cooldownBuff;
				if(!supportRoutineRunning && rangeBuffMul<buff.rangeBuff) rangeBuffMul=buff.rangeBuff;
				if(criticalBuffMod<buff.criticalBuff) criticalBuffMod=buff.criticalBuff;
				if(hitBuffMod<buff.hitBuff) hitBuffMod=buff.hitBuff;
				if(dodgeBuffMod<buff.dodgeBuff) dodgeBuffMod=buff.dodgeBuff;
				if(regenHPBuff<buff.regenHP) regenHPBuff=buff.regenHP;
			}
		}
		void ResetBuff(){
			activeBuffList=new List<Buff>();
			ClearBuffStats();
		}
		void ClearBuffStats(){
			damageBuffMul=0.0f;
			cooldownBuffMul=0.0f;
			rangeBuffMul=0.0f;
			criticalBuffMod=0f;
			hitBuffMod=0f;
			dodgeBuffMod=0f;
			regenHPBuff=0f;
		}
		
		
		public GameObject deadEffectObj;
		public void Dead(){
			dead=true;
			
			float delay=0;
			
			if(deadEffectObj!=null) ObjectPoolManager.Spawn(deadEffectObj, targetPoint.position, thisT.rotation);
			
			if(unitC!=null) delay=unitC.CreepDestroyed();
			if(unitT!=null) unitT.Destroy();
			
			if(supportRoutineRunning) UnbuffAll();
			
			if(onDestroyedE!=null) onDestroyedE(this);
			
			StartCoroutine(_Dead(delay));
		}
		public IEnumerator _Dead(float delay){
			yield return new WaitForSeconds(delay);
			ObjectPoolManager.Unspawn(thisObj);
		}
		
		
		public Transform GetTargetT(){
			return targetPoint!=null ? targetPoint : thisT; 
		}
		
		
		
		
		
		
		private float GetPerkMulHP(){					return IsTower() ? PerkManager.GetTowerHP(unitT.prefabID) : 0 ; } 
		private float GetPerkMulHPRegen(){ 			return IsTower() ? PerkManager.GetTowerHPRegen(unitT.prefabID) : 0 ; } 
		private float GetPerkMulHPStagger(){ 		return IsTower() ? PerkManager.GetTowerHPStagger(unitT.prefabID) : 0 ; } 
		private float GetPerkMulShield(){ 				return IsTower() ? PerkManager.GetTowerShield(unitT.prefabID) : 0 ; } 
		private float GetPerkMulShieldRegen(){	 	return IsTower() ? PerkManager.GetTowerShieldRegen(unitT.prefabID) : 0 ; } 
		private float GetPerkMulShieldStagger(){	return IsTower() ? PerkManager.GetTowerShieldStagger(unitT.prefabID) : 0 ; } 
		
		private float GetPerkMulDamage(){ 			return IsTower() ? PerkManager.GetTowerDamage(unitT.prefabID) : 0 ; } 
		private float GetPerkMulCooldown(){ 		return IsTower() ? PerkManager.GetTowerCD(unitT.prefabID) : 0 ; } 
		private float GetPerkMulClipSize(){ 			return IsTower() ? PerkManager.GetTowerClipSize(unitT.prefabID) : 0 ; } 
		private float GetPerkMulReloadDuration(){ 	return IsTower() ? PerkManager.GetTowerReloadDuration(unitT.prefabID) : 0 ; } 
		private float GetPerkMulRange(){ 				return IsTower() ? PerkManager.GetTowerRange(unitT.prefabID) : 0 ; } 
		private float GetPerkMulAOERadius(){ 		return IsTower() ? PerkManager.GetTowerAOERadius(unitT.prefabID) : 0 ; } 
		private float GetPerkModHit(){ 				return IsTower() ? PerkManager.GetTowerHit(unitT.prefabID) : 0 ; } 
		private float GetPerkModDodge(){ 			return IsTower() ? PerkManager.GetTowerDodge(unitT.prefabID) : 0 ; } 
		private float GetPerkModCritChance(){ 		return IsTower() ? PerkManager.GetTowerCritChance(unitT.prefabID) : 0 ; } 
		private float GetPerkModCritMul(){ 			return IsTower() ? PerkManager.GetTowerCritMultiplier(unitT.prefabID) : 0 ; } 
		
		private float GetPerkModShieldBreak(){ 		return IsTower() ? PerkManager.GetTowerShieldBreakMultiplier(unitT.prefabID) : 0 ; } 
		private float GetPerkModShieldPierce(){ 	return IsTower() ? PerkManager.GetTowerShieldPierceMultiplier(unitT.prefabID) : 0 ; } 
		
		private Stun ModifyStunWithPerkBonus(Stun stun){ return IsTower() ? PerkManager.ModifyStunWithPerkBonus(stun.Clone(), unitT.prefabID) : stun; }
		private Slow ModifySlowWithPerkBonus(Slow slow){ return IsTower() ? PerkManager.ModifySlowWithPerkBonus(slow.Clone(), unitT.prefabID) : slow; }
		private Dot ModifyDotWithPerkBonus(Dot dot){ 		return IsTower() ? PerkManager.ModifyDotWithPerkBonus(dot.Clone(), unitT.prefabID) : dot; }
		private InstantKill ModifyInstantKillWithPerkBonus(InstantKill instKill){ return IsTower() ? PerkManager.ModifyInstantKillWithPerkBonus(instKill.Clone(), unitT.prefabID) : instKill; }
		
		
		
		private float GetFullHP(){ return defaultHP * (1+GetPerkMulHP()); }
		private float GetFullShield(){ return defaultShield * (1+GetPerkMulShield()); }
		private float GetHPRegenRate(){ return HPRegenRate * (1+GetPerkMulHPRegen()); }
		private float GetShieldRegenRate(){ return shieldRegenRate * (1+GetPerkMulShieldRegen()); }
		private float GetHPStaggerDuration(){ return HPStaggerDuration * (1-GetPerkMulHPStagger()); }
		private float GetShieldStaggerDuration(){ return shieldStaggerDuration * (1-GetPerkMulShieldStagger()); }
		
		public float GetDamageMin(){ return Mathf.Max(0, stats[currentActiveStat].damageMin * (1+damageBuffMul+dmgABMul+GetPerkMulDamage())); }
		public float GetDamageMax(){ return Mathf.Max(0, stats[currentActiveStat].damageMax * (1+damageBuffMul+dmgABMul+GetPerkMulDamage())); }
		public float GetCooldown(){ return Mathf.Max(0.05f, stats[currentActiveStat].cooldown * (1-cooldownBuffMul-cdABMul-GetPerkMulCooldown())); }
		
		public float GetRangeMin(){ return stats[currentActiveStat].minRange; }
		public float GetRange(){ return Mathf.Max(0, stats[currentActiveStat].range * (1+rangeBuffMul+rangeABMul+GetPerkMulRange())); }
		public float GetAOERadius(){ return stats[currentActiveStat].aoeRadius * (1+GetPerkMulAOERadius()); }
		
		public float GetHit(){ return stats[currentActiveStat].hit + hitBuffMod + GetPerkModHit(); }
		public float GetDodge(){ return stats.Count==0 ? 0 : stats[currentActiveStat].dodge + dodgeBuffMod + GetPerkModDodge(); }
		
		public float GetCritChance(){ return stats[currentActiveStat].crit.chance + criticalBuffMod + GetPerkModCritChance(); }
		public float GetCritMultiplier(){ return stats[currentActiveStat].crit.dmgMultiplier + GetPerkModCritMul(); }
		
		public float GetShieldBreak(){ return stats[currentActiveStat].shieldBreak + GetPerkModShieldBreak(); }
		public float GetShieldPierce(){ return stats[currentActiveStat].shieldPierce + GetPerkModShieldPierce(); }
		public bool DamageShieldOnly(){ return stats[currentActiveStat].damageShieldOnly; }
		
		public Stun GetStun(){ return ModifyStunWithPerkBonus(stats[currentActiveStat].stun); }
		public Slow GetSlow(){ return ModifySlowWithPerkBonus(stats[currentActiveStat].slow); }
		public Dot 	GetDot(){ return ModifyDotWithPerkBonus(stats[currentActiveStat].dot); }
		public InstantKill GetInstantKill(){ return ModifyInstantKillWithPerkBonus(stats[currentActiveStat].instantKill); }
		
		
		
		
		
		
		public int GetShootPointCount(){ return shootPoints.Count; }
		
		public Transform GetShootObjectT(){
			if(stats[currentActiveStat].shootObjectT==null) return localShootObjectT;
			return stats[currentActiveStat].shootObjectT;
		}
		
		public List<int> GetResourceGain(){ return stats[currentActiveStat].rscGain; }
		
		public Buff GetBuff(){ return stats[currentActiveStat].buff; }
		
		
		
		//public string GetDespStats(){ return stats[currentActiveStat].desp; }
		public string GetDespGeneral(){ return desp; }
		
		public string GetDespStats(){ 
			if(!IsTower() || stats[currentActiveStat].useCustomDesp) return stats[currentActiveStat].desp;
			
			UnitTower tower=unitT;
			
			string text="";
			
			if(tower.type==_TowerType.Turret || tower.type==_TowerType.AOE || tower.type==_TowerType.Mine){
				float currentDmgMin=GetDamageMin();
				float currentDmgMax=GetDamageMax();
				if(currentDmgMax>0){
					if(currentDmgMin==currentDmgMax) text+="Damage:		 "+currentDmgMax.ToString("f0");
					else text+="Damage:		 "+currentDmgMin.ToString("f0")+"-"+currentDmgMax.ToString("f0");
				}
				
				float currentAOE=GetAOERadius();
				if(currentAOE>0) text+=" (AOE)";
				//if(currentAOE>0) text+="\nAOE Radius: "+currentAOE;
				
				if(tower.type!=_TowerType.Mine){
					float currentCD=GetCooldown();
					if(currentCD>0) text+="\nCooldown:	 "+currentCD.ToString("f1")+"s";
				}
				
				float critChance=GetCritChance();
				if(critChance>0) text+="\nCritical:		 "+(critChance*100).ToString("f0")+"%";
				
				if(text!="") text+="\n";
				
				Stun stun=GetStun();
				if(stun.IsValid()) text+="\nChance to stuns target";
					
				Slow slow=GetSlow();
				if(slow.IsValid()) text+="\nSlows target";
				
				Dot dot=GetDot();
				float dotDmg=dot.GetTotalDamage();
				if(dotDmg>0) text+="\nDeal "+dotDmg.ToString("f0")+" over "+dot.duration.ToString("f0")+"s";
				
				if(DamageShieldOnly()) text+="\nDamage target's shield only";
				if(GetShieldBreak()>0) text+="\nChance to break target's shield";
				if(GetShieldPierce()>0) text+="\nChance to pierce target's shield";
				
				InstantKill instKill=GetInstantKill();
				if(instKill.IsValid()) text+="\nChance to kill target instantly";
			}
			else if(tower.type==_TowerType.Support){
				Buff buff=GetBuff();
				
				if(buff.damageBuff>0) text+="Damage Buff: "+((buff.damageBuff)*100).ToString("f0")+"%";
				if(buff.cooldownBuff>0) text+="\nCooldown Buff: "+((buff.cooldownBuff)*100).ToString("f0")+"%";
				if(buff.rangeBuff>0) text+="\nRange Buff: "+((buff.rangeBuff)*100).ToString("f0")+"%";
				if(buff.criticalBuff>0) text+="\nRange Buff: "+((buff.criticalBuff)*100).ToString("f0")+"%";
				if(buff.hitBuff>0) text+="\nHit Buff: "+((buff.hitBuff)*100).ToString("f0")+"%";
				if(buff.dodgeBuff>0) text+="\nDodge Buff: "+((buff.dodgeBuff)*100).ToString("f0")+"%";
				
				if(text!="") text+="\n";
				
				if(buff.regenHP>0){
					float regenValue=buff.regenHP;
					float regenDuration=1;
					if(buff.regenHP<1){
						regenValue=1;
						regenDuration=1/buff.regenHP;
					}
					text+="\nRegen "+regenValue.ToString("f0")+ "HP every "+regenDuration.ToString("f0")+"s";
				}
			}
			else if(tower.type==_TowerType.Resource){
				text+="Regenerate resource overtime";
			}
			
			return text;
		}
		
		
		public float GetDistFromDestination(){ return unitC!=null ? unitC._GetDistFromDestination() : 0; }
		
		public bool IsInConstruction(){ return IsTower() ? unitT._IsInConstruction() : false; }
		
		
		//used by abilities
		private float dmgABMul=0;
		public void ABBuffDamage(float value, float duration){ StartCoroutine(ABBuffDamageRoutine(value, duration)); }
		IEnumerator ABBuffDamageRoutine(float value, float duration){
			dmgABMul+=value;
			yield return new WaitForSeconds(duration);
			dmgABMul-=value;
		}
		private float rangeABMul=0;
		public void ABBuffRange(float value, float duration){ StartCoroutine(ABBuffDamageRoutine(value, duration)); }
		IEnumerator ABBuffRangeRoutine(float value, float duration){
			rangeABMul+=value;
			yield return new WaitForSeconds(duration);
			rangeABMul-=value;
		}
		private float cdABMul=0;
		public void ABBuffCooldown(float value, float duration){ StartCoroutine(ABBuffCooldownRoutine(value, duration)); }
		IEnumerator ABBuffCooldownRoutine(float value, float duration){
			cdABMul+=value;
			yield return new WaitForSeconds(duration);
			cdABMul-=value;
		}
		
		
		
		void OnDrawGizmos(){
			if(target!=null){
				if(IsCreep()) Gizmos.DrawLine(transform.position, target.transform.position);
			}
		}
		
		
		
		
		
		
		
		//following function are used to replace ScanForTargetRoutine() and TurretRoutine() when the game are time sensitive, where the outcome must be same regardless of time scale
		/*
		private int targetFreqCount=0;
		void FixedTimeScanForTarget(){
			targetFreqCount+=1;
			if(targetFreqCount==10) targetFreqCount=0;
			else return;
			
			ScanForTarget();
		}
		
		private float attackCD=-1;
		private float reloadCD=-1;
		private int currentAmmo=-1;		//when enabled, make sure to set it reset it in init(), refer to turretRoutine
		void FixedTimeTurret(){
			if(reloadCD>0) reloadCD-=Time.fixedDeltaTime;
			if(attackCD>0) attackCD-=Time.fixedDeltaTime;
			
			if(attackCD>0 || reloadCD>0) return;
			
			if(target==null || stunned || IsInConstruction() || !targetInLOS) return;
			
			Unit currentTarget=target;
			
			AttackInstance attInstance=new AttackInstance();
			attInstance.srcUnit=this;
			attInstance.tgtUnit=currentTarget;
			
			for(int i=0; i<shootPoints.Count; i++){
				Transform sp=shootPoints[i];
				Transform objT=(Transform)Instantiate(GetShootObjectT(), sp.position, sp.rotation);
				ShootObject shootObj=objT.GetComponent<ShootObject>();
				shootObj.Shoot(attInstance, sp);
			}
			
			if(currentAmmo>-1){
				currentAmmo-=1;
				if(currentAmmo==0){
					reloadCD=GetReloadDuration();
					currentAmmo=GetClipSize();
				}
			}
			
			attackCD=GetCooldown();
		}
		*/
	}

}
