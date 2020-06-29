using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {
	
	public class AbilityManager : MonoBehaviour {
		
		public delegate void AddNewAbilityHandler(Ability ab);
		public static event AddNewAbilityHandler onAddNewAbilityE;		//fire when a new ability is added via perk
		
		//public delegate void AbilityReadyHandler(Ability ab);
		//public static event AbilityReadyHandler onAbilityReadyE;			//listen by TDTK only
		
		public delegate void ABActivatedHandler(Ability ab);
		public static event ABActivatedHandler onAbilityActivatedE;		//fire when an ability is used
		
		public delegate void ABTargetSelectModeHandler(bool flag);
		public static event ABTargetSelectModeHandler onTargetSelectModeE;	//fire when enter/exit target selection for ability
		
		//public delegate void EnergyHandler(float valueChanged);
		//public static event EnergyHandler onEnergyE;					//listen by TDTK only
		
		//public delegate void EnergyFullHandler();
		//public static event EnergyFullHandler onEnergyFullE;			//listen by TDTK only
		
		
		//[HideInInspector] 
		public List<int> unavailableIDList=new List<int>();	//ID list of perk available for this level, modified in editor
		//[HideInInspector] 
		public List<Ability> abilityList=new List<Ability>();	//actual ability list, filled in runtime based on unavailableIDList
		public static List<Ability> GetAbilityList(){ return instance.abilityList; }
		
		public Transform defaultIndicator;		//generic indicator use for ability without any specific indicator
		
		private bool inTargetSelectMode=false;
		public static bool InTargetSelectMode(){ return instance==null ? false : instance.inTargetSelectMode; }
		
		private bool validTarget=false;	//used for targetSelectMode, indicate when the cursor is in a valid position or on a valid target
		
		public int selectedAbilityID=-1;
		public Transform currentIndicator;		//active indicator in used
		
		public bool startWithFullEnergy=false;
		public bool onlyChargeOnSpawn=false;
		public float energyRate=2;
		public float fullEnergy=100;
		public float energy=0;
		
		
		private Transform thisT;
		private static AbilityManager instance;
		
		public static bool IsOn(){ return instance==null ? false : true; }
		
		void Awake(){
			instance=this;
			thisT=transform;
			
			if(startWithFullEnergy) energy=fullEnergy;
			
			List<Ability> dbList=AbilityDB.Load();
			
			abilityList=new List<Ability>();
			for(int i=0; i<dbList.Count; i++){
				if(!unavailableIDList.Contains(dbList[i].ID)){
					abilityList.Add(dbList[i].Clone());
				}
			}
			
			List<Ability> newList=PerkManager.GetUnlockedAbility();
			for(int i=0; i<newList.Count; i++) abilityList.Add(newList[i]);
			
			for(int i=0; i<abilityList.Count; i++) abilityList[i].ID=i;
			
			if(defaultIndicator){
				defaultIndicator=(Transform)Instantiate(defaultIndicator);
				defaultIndicator.parent=thisT;
				defaultIndicator.gameObject.SetActive(false);
			}
		}
		
		void OnDestroy(){ instance=null; }
		
		
		public static void AddNewAbility(Ability ab){ instance._AddNewAbility(ab); }
		public void _AddNewAbility(Ability ab){ 
			for(int i=0; i<abilityList.Count; i++){ if(ab.ID==abilityList[i].ID) return; }
			abilityList.Add(ab.Clone());
			if(onAddNewAbilityE!=null) onAddNewAbilityE(ab);
		}
		
		
		
		
		void FixedUpdate(){
			if((onlyChargeOnSpawn && GameControl.IsGameStarted()) || !onlyChargeOnSpawn){
				if(energy<fullEnergy){
					float valueGained=Time.fixedDeltaTime*GetEnergyRate();
					energy+=valueGained;
					energy=Mathf.Min(energy, GetEnergyFull());
				}
			}
		}
		
		// Update is called once per frame
		void Update () {
			SelectAbilityTarget();
		}
		
		
		//called in every frame, execute if there's an ability is selected and pending target selection
		//use only mouse input atm.
		void SelectAbilityTarget(){
			if(selectedAbilityID<0) return;
			
			//only cast on terrain and platform
			LayerMask mask=1<<LayerManager.LayerPlatform();
			int terrainLayer=LayerManager.LayerTerrain();
			if(terrainLayer>=0) mask|=1<<terrainLayer;
			
			Ability ability=abilityList[selectedAbilityID];
			
			if(ability.singleUnitTargeting){
				if(ability.targetType==Ability._TargetType.Hybrid){
					mask|=1<<LayerManager.LayerTower();
					mask|=1<<LayerManager.LayerCreep();
				}
				else if(ability.targetType==Ability._TargetType.Friendly) mask|=1<<LayerManager.LayerTower();
				else if(ability.targetType==Ability._TargetType.Hostile) mask|=1<<LayerManager.LayerCreep();
			}
			
			
			#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8
				Unit targetUnit=null;
				if(Input.touchCount>=1){
					Camera mainCam=Camera.main;
					if(mainCam!=null){
						Ray ray = mainCam.ScreenPointToRay(Input.touches[0].position);
						RaycastHit hit;
						if(Physics.Raycast(ray, out hit, Mathf.Infinity, mask)){
							currentIndicator.position=hit.point+new Vector3(0, 0.1f, 0);
							validTarget=true;
							
							if(ability.singleUnitTargeting){
								targetUnit=hit.transform.GetComponent<Unit>();
								if(targetUnit!=null) currentIndicator.position=targetUnit.thisT.position;
								else validTarget=false;
							}
						}
						else validTarget=false;
					}
				}
				else{
					if(validTarget) ActivateAbility(ability, currentIndicator.position, targetUnit);
					else GameControl.DisplayMessage("Invalid target for ability");
					ClearSelectedAbility();
				}
			#else
				Camera mainCam=Camera.main;
				if(mainCam!=null){
					Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
					RaycastHit hit;
					if(Physics.Raycast(ray, out hit, Mathf.Infinity, mask)){
						currentIndicator.position=hit.point+new Vector3(0, 0.1f, 0);
						
						Unit targetUnit=null;
						
						validTarget=true;
						if(ability.singleUnitTargeting){
							targetUnit=hit.transform.GetComponent<Unit>();
							if(targetUnit!=null) currentIndicator.position=targetUnit.thisT.position;
							else validTarget=false;
						}
						
						if(Input.GetMouseButtonDown(0)){
							if(validTarget){
								ActivateAbility(ability, currentIndicator.position, targetUnit);
								ClearSelectedAbility();
							}
							else GameControl.DisplayMessage("Invalid target for ability");
						}
					}
				}
				
				
				if(Input.GetMouseButtonDown(1)){
					ClearSelectedAbility();
				}
			#endif
		}
		
		
		//called by ability button from UI, select an ability
		public static string SelectAbility(int ID){ return instance._SelectAbility(ID); }
		public string _SelectAbility(int ID){
			Ability ab=abilityList[ID];
			
			Debug.Log(ab.name);
			
			string exception=ab.IsAvailable();
			if(exception!="") return exception;
			
			if(!ab.requireTargetSelection) ActivateAbility(ab);		//no target selection required, fire it away
			else{
				if(onTargetSelectModeE!=null) onTargetSelectModeE(true);	//enter target selection phase
				
				inTargetSelectMode=true;
				validTarget=false;
				
				selectedAbilityID=ID;
				
				if(ab.indicator!=null) currentIndicator=ab.indicator;
				else{
					currentIndicator=defaultIndicator;
					if(ab.autoScaleIndicator){
						if(ab.singleUnitTargeting){
							float gridSize=BuildManager.GetGridSize();
							currentIndicator.localScale=new Vector3(gridSize, 1, gridSize);
						}
						else currentIndicator.localScale=new Vector3(ab.GetAOERadius()*2, 1, ab.GetAOERadius()*2);
					}
				}
				
				currentIndicator.gameObject.SetActive(true);
			}
				
			return "";
		}
		public static void ClearSelectedAbility(){ instance._ClearSelectedAbility(); }
		public void _ClearSelectedAbility(){
			currentIndicator.gameObject.SetActive(false);
			selectedAbilityID=-1;
			currentIndicator=null;
			
			inTargetSelectMode=false;
			
			if(onTargetSelectModeE!=null) onTargetSelectModeE(false);
		}
		
		
		//called when an ability is fired, reduce the energy, start the cooldown and what not
		public void ActivateAbility(Ability ab, Vector3 pos=default(Vector3), Unit unit=null){
			ab.usedCount+=1;
			energy-=ab.GetCost();
			StartCoroutine(ab.CooldownRoutine());
			
			CastAbility(ab, pos, unit);
			
			if(onAbilityActivatedE!=null) onAbilityActivatedE(ab);
		}
		
		//called from ActivateAbility, cast the ability, visual effect and actual effect goes here
		public void CastAbility(Ability ab, Vector3 pos, Unit unit=null){
			if(ab.effectObj!=null){
				ObjectPoolManager.Spawn(ab.effectObj, pos, Quaternion.identity);
			}
			
			if(ab.useDefaultEffect){
				StartCoroutine(ApplyAbilityEffect(ab, pos, unit));
			}
		}
		
		
		//apply the ability effect, damage, stun, buff and so on 
		IEnumerator ApplyAbilityEffect(Ability ab, Vector3 pos, Unit tgtUnit=null){
			yield return new WaitForSeconds(ab.effectDelay);
			
			LayerMask mask1=1<<LayerManager.LayerTower();
			LayerMask mask2=1<<LayerManager.LayerCreep();
			LayerMask mask3=1<<LayerManager.LayerCreepF();
			LayerMask mask=mask1 | mask2 | mask3;
			
			List<Unit> creepList=new List<Unit>();
			List<Unit> towerList=new List<Unit>();
			
			if(tgtUnit==null){
				float radius=ab.requireTargetSelection ? ab.GetAOERadius() : Mathf.Infinity;
				Collider[] cols=Physics.OverlapSphere(pos, radius, mask);
				
				if(cols.Length>0){
					for(int i=0; i<cols.Length; i++){
						Unit unit=cols[i].gameObject.GetComponent<Unit>();
						if(unit.unitC!=null) creepList.Add(unit.unitC);
						if(unit.unitT!=null) towerList.Add(unit.unitT);
					}
				}
			}
			else{
				creepList.Add(tgtUnit);
				towerList.Add(tgtUnit);
			}
				
			AbilityEffect eff=ab.GetActiveEffect();
			
			for(int n=0; n<creepList.Count; n++){
				if(eff.damageMax>0){
					creepList[n].ApplyDamage(Random.Range(eff.damageMin, eff.damageMax));
				}
				else if(eff.stunChance>0 && eff.duration>0){
					if(Random.Range(0f, 1f)<eff.stunChance) creepList[n].ApplyStun(eff.duration);
				}
				else if(eff.slow.IsValid()){
					creepList[n].ApplySlow(eff.slow);
				}
				else if(eff.dot.GetTotalDamage()>0){
					creepList[n].ApplyDot(eff.dot);
				}
			}
			for(int n=0; n<towerList.Count; n++){
				if(eff.duration>0){
					if(eff.damageBuff>0){
						towerList[n].ABBuffDamage(eff.damageBuff, eff.duration);
					}
					else if(eff.rangeBuff>0){
						towerList[n].ABBuffRange(eff.rangeBuff, eff.duration);
					}
					else if(eff.cooldownBuff>0){
						towerList[n].ABBuffCooldown(eff.cooldownBuff, eff.duration);
					}
				}
				else if(eff.HPGainMax>0){
					towerList[n].RestoreHP(Random.Range(eff.HPGainMin, eff.HPGainMax));
				}
			}
			
		}
		
		
		
		
		public static void GainEnergy(int value){ if(instance!=null) instance._GainEnergy(value); }
		public void _GainEnergy(int value){
			energy+=value;
			energy=Mathf.Min(energy, GetEnergyFull());
		}
		
		
		public static float GetAbilityCurrentCD(int ID){ return instance.abilityList[ID].currentCD; }
		
		public static float GetEnergyFull(){ return instance.fullEnergy+PerkManager.GetEnergyCapModifier(); }
		public static float GetEnergy(){ return instance.energy; }
		
		private float GetEnergyRate(){ return energyRate+PerkManager.GetEnergyRegenModifier(); }
	}

	
	
	
	
	
	
	
}