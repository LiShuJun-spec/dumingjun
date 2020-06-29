using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {

	//~ public enum _TowerType{TurretTower, AOETower, DirectionalAOETower, SupportTower, ResourceTower, Mine, Block}
	public enum _TowerType{Turret, AOE, Support, Resource, Mine, Block}
	public enum _TargetMode{Hybrid, Air, Ground}
	
	
	public class UnitTower : Unit {
		
		public delegate void TowerSoldHandler(UnitTower tower);
		public static event TowerSoldHandler onSoldE;									//listen by TDTK only
		
		public delegate void ConstructionStartHandler(UnitTower tower);
		public static event ConstructionStartHandler onConstructionStartE;		//listen by TDTK only
		
		public delegate void TowerUpgradedHandler(UnitTower tower);
		public static event TowerUpgradedHandler onUpgradedE;
		
		public delegate void ConstructionCompleteHandler(UnitTower tower);
		public static event ConstructionCompleteHandler onConstructionCompleteE;
		
		
		public delegate void PlayConstructAnimation();
		public PlayConstructAnimation playConstructAnimation;
		public delegate void PlayDeconstructAnimation();
		public PlayDeconstructAnimation playDeconstructAnimation;
		
		
		public _TowerType type=_TowerType.Turret;
		public _TargetMode targetMode=_TargetMode.Hybrid;
		
		
		public bool disableInBuildManager=false;	//when set to true, tower wont appear in BuildManager buildList
		
		private enum _Construction{None, Constructing, Deconstructing}
		private _Construction construction=_Construction.None;
		public bool _IsInConstruction(){ return construction==_Construction.None ? false : true; }


		public GameObject buildingEffect;
		public GameObject builtEffect;


		public override void Awake() {
			SetSubClass(this);
			
			base.Awake();
			
			if(stats.Count==0) stats.Add(new UnitStat());
		}
		
		public override void Start() {
			base.Start();
		}
		
		public void InitTower(int ID){
			Init();
			
			instanceID=ID;
			
			value=stats[currentActiveStat].cost;
			
			int rscCount=ResourceManager.GetResourceCount();
			for(int i=0; i<stats.Count; i++){
				UnitStat stat=stats[i];
				stat.slow.effectID=instanceID;
				stat.dot.effectID=instanceID;
				stat.buff.effectID=instanceID;
				if(stat.rscGain.Count!=rscCount){
					while(stat.rscGain.Count<rscCount) stat.rscGain.Add(0);
					while(stat.rscGain.Count>rscCount) stat.rscGain.RemoveAt(stat.rscGain.Count-1);
				}
			}
			
			if(type==_TowerType.Turret){
				StartCoroutine(ScanForTargetRoutine());
				StartCoroutine(TurretRoutine());
			}
			if(type==_TowerType.AOE){
				StartCoroutine(AOETowerRoutine());
			}
			if(type==_TowerType.Support){
				StartCoroutine(SupportRoutine());
			}
			if(type==_TowerType.Resource){
				StartCoroutine(ResourceTowerRoutine());
			}
			if(type==_TowerType.Mine){
				StartCoroutine(MineRoutine());
			}
		}
		
		
		[HideInInspector] public float builtDuration;
		[HideInInspector] public float buildDuration;
		public void UnBuild(){ StartCoroutine(Building(stats[currentActiveStat].unBuildDuration, true));	}
		public void Build(){ StartCoroutine(Building(stats[currentActiveStat].buildDuration));	}
		IEnumerator Building(float duration, bool reverse=false){		//reverse flag is set to true when selling (thus unbuilding) the tower
			construction=!reverse ? _Construction.Constructing : _Construction.Deconstructing;
			
			builtDuration=0;
			buildDuration=duration;
			
			if(onConstructionStartE!=null) onConstructionStartE(this);

			if(buildingEffect!=null) ObjectPoolManager.Spawn(buildingEffect, thisT.position, thisT.rotation);
			
			yield return null;
			if(!reverse && playConstructAnimation!=null) playConstructAnimation();
			else if(reverse && playDeconstructAnimation!=null) playDeconstructAnimation();
			
			while(true){
				yield return null;
				builtDuration+=Time.deltaTime;
				if(builtDuration>buildDuration) break;
			}
			
			construction=_Construction.None;
			
			if(!reverse) {
				if(onConstructionCompleteE != null) onConstructionCompleteE (this);
				if(builtEffect!=null) ObjectPoolManager.Spawn(builtEffect, thisT.position, thisT.rotation);
			}
			
			if(reverse){
				if(onSoldE!=null) onSoldE(this);
				
				if(occupiedPlatform!=null) occupiedPlatform.UnbuildTower(occupiedNode);
				ResourceManager.GainResource(GetValue());
				Dead();
			}


		}
		public float GetBuildProgress(){ 
			if(construction==_Construction.Constructing) return builtDuration/buildDuration;
			if(construction==_Construction.Deconstructing) return (buildDuration-builtDuration)/buildDuration;
			else return 0;
		}
		
		
		public void Sell(){
			UnBuild();
		}
		
		
		private bool isSampleTower;
		private UnitTower srcTower;
		public void SetAsSampleTower(UnitTower tower){
			isSampleTower=true;
			srcTower=tower;
			thisT.position=new Vector3(0, 9999, 0);
		}
		public bool IsSampleTower(){ return isSampleTower; }
		public IEnumerator DragNDropRoutine(){
			GameControl.SelectTower(this);
			yield return null;
			
			
			#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
				_TileStatus status=_TileStatus.NoPlatform;
				while(Input.touchCount>=1){
					Vector3 pos=Input.touches[0].position;
					
					status=BuildManager.CheckBuildPoint(pos, -1, prefabID);
					
					if(status==_TileStatus.Available){
						BuildInfo buildInfo=BuildManager.GetBuildInfo();
						thisT.position=buildInfo.position;
						thisT.rotation=buildInfo.platform.thisT.rotation;
					}
					else{
						Ray ray = Camera.main.ScreenPointToRay(pos);
						RaycastHit hit;
						if(Physics.Raycast(ray, out hit, Mathf.Infinity)) thisT.position=hit.point;
						//this there is no collier, randomly place it 30unit from camera
						else thisT.position=ray.GetPoint(30);
					}
					
					if(Input.touches[0].phase==TouchPhase.Ended){
						string exception=BuildManager.BuildTower(srcTower);
						if(exception!="") GameControl.DisplayMessage(exception);
						else BuildManager.ClearBuildPoint();
						break;
					}
					
					yield return null;
				}
				
				//if(status==_TileStatus.Available){
				//	string exception=BuildManager.BuildTower(srcTower);
				//	if(exception!="") GameControl.DisplayMessage(exception);
				//}
				//else{
				//	GameControl.DisplayMessage("cancelled");
				//	BuildManager.ClearBuildPoint();
				//}
				
				GameControl.ClearSelectedTower();
				thisObj.SetActive(false);
				
			#else
				while(true){
					Vector3 pos=Input.mousePosition;
					
					_TileStatus status=BuildManager.CheckBuildPoint(pos, -1, prefabID);
					
					if(status==_TileStatus.Available){
						BuildInfo buildInfo=BuildManager.GetBuildInfo();
						thisT.position=buildInfo.position;
						thisT.rotation=buildInfo.platform.thisT.rotation;
					}
					else{
						Ray ray = Camera.main.ScreenPointToRay(pos);
						RaycastHit hit;
						if(Physics.Raycast(ray, out hit, Mathf.Infinity)) thisT.position=hit.point;
						//this there is no collier, randomly place it 30unit from camera
						else thisT.position=ray.GetPoint(30);
					}
					
					
					//left-click, build
					if(Input.GetMouseButtonDown(0) && !UIUtilities.IsCursorOnUI()){
						//if current mouse point position is valid, build the tower
						if(status==_TileStatus.Available){
							string exception=BuildManager.BuildTower(srcTower);
							if(exception!="") GameControl.DisplayMessage(exception);
						}
						else{
							BuildManager.ClearBuildPoint();
						}
						GameControl.ClearSelectedTower();
						thisObj.SetActive(false);
						break;
					}
					
					//right-click, cancel
					if(Input.GetMouseButtonDown(1) || GameControl.GetGameState()==_GameState.Over){
						GameControl.ClearSelectedTower();
						BuildManager.ClearBuildPoint();
						thisObj.SetActive(false);
						break;
					}
					
					yield return null;
				}
			#endif
				
			thisT.position=new Vector3(0, 9999, 0);
		}
		
		public PlatformTD occupiedPlatform;
		public NodeTD occupiedNode;
		public void SetPlatform(PlatformTD platform, NodeTD node){
			occupiedPlatform=platform;
			occupiedNode=node;
		}
		
		public override void Update() {
			base.Update();
		}
		
		public override void FixedUpdate(){
			base.FixedUpdate();
		}
		
		
		
		
		IEnumerator AOETowerRoutine(){
			if(targetMode==_TargetMode.Hybrid){
				LayerMask mask1=1<<LayerManager.LayerCreep();
				LayerMask mask2=1<<LayerManager.LayerCreepF();
				maskTarget=mask1 | mask2;
			}
			else if(targetMode==_TargetMode.Air){
				maskTarget=1<<LayerManager.LayerCreepF();
			}
			else if(targetMode==_TargetMode.Ground){
				maskTarget=1<<LayerManager.LayerCreep();
			}
			
			while(true){
				yield return new WaitForSeconds(GetCooldown());
				
				while(stunned || IsInConstruction()) yield return null;
				
				Transform soPrefab=GetShootObjectT();
				if(soPrefab!=null) Instantiate(soPrefab, thisT.position, thisT.rotation);
				
				Collider[] cols=Physics.OverlapSphere(thisT.position, GetRange(), maskTarget);
				if(cols.Length>0){
					for(int i=0; i<cols.Length; i++){
						Unit unit=cols[i].transform.GetComponent<Unit>();
						if(unit==null && !unit.dead) continue;
						
						AttackInstance attInstance=new AttackInstance();
						attInstance.srcUnit=this;
						attInstance.tgtUnit=unit;
						attInstance.Process();
						
						unit.ApplyEffect(attInstance);
					}
				}
			}
		}
		
		IEnumerator ResourceTowerRoutine(){
			while(true){
				yield return new WaitForSeconds(GetCooldown());
				
				while(stunned || IsInConstruction()) yield return null;
				
				Transform soPrefab=GetShootObjectT();
				if(soPrefab!=null) Instantiate(soPrefab, thisT.position, thisT.rotation);
				
				ResourceManager.GainResource(GetResourceGain(), PerkManager.GetRscTowerGain());
			}
		}
		
		IEnumerator MineRoutine(){
			LayerMask maskTarget=1<<LayerManager.LayerCreep();
			while(true){
				if(!dead && !IsInConstruction()){
					Collider[] cols=Physics.OverlapSphere(thisT.position, GetRange(), maskTarget);
					if(cols.Length>0){
						
						Collider[] colls=Physics.OverlapSphere(thisT.position, GetAOERadius(), maskTarget);
						for(int i=0; i<colls.Length; i++){
							Unit unit=colls[i].transform.GetComponent<Unit>();
							if(unit==null && !unit.dead) continue;
							
							AttackInstance attInstance=new AttackInstance();
							attInstance.srcUnit=this;
							attInstance.tgtUnit=unit;
							attInstance.Process();
							
							unit.ApplyEffect(attInstance);
						}
						
						Transform soPrefab=GetShootObjectT();
						if(soPrefab!=null) Instantiate(soPrefab, thisT.position, thisT.rotation);
						
						Dead();
					}
				}
				yield return new WaitForSeconds(0.1f);
			}
		}
		
		
		
		private int level=1;
		public int GetLevel(){ return level; }
		public void SetLevel(int lvl){ level=lvl; }
		
		public UnitTower prevLevelTower;
		public List<UnitTower> nextLevelTowerList=new List<UnitTower>();
		public int ReadyToBeUpgrade(){
			if(currentActiveStat<stats.Count-1) return 1;
			if(nextLevelTowerList.Count>0){
				if(nextLevelTowerList.Count>=2 && nextLevelTowerList[1]!=null) return 2;
				else if(nextLevelTowerList.Count>=1 && nextLevelTowerList[0]!=null) return 1;
			}
			return 0;
		}
		public string Upgrade(int ID=0){	//ID specify which nextTower to use
			if(currentActiveStat<stats.Count-1) return UpgradeToNextStat();
			else if(nextLevelTowerList.Count>0) return UpgradeToNextTower(ID);
			return "Tower is at maximum level!";
		}
		public string UpgradeToNextStat(){
			List<int> cost=GetCost();
			int suffCost=ResourceManager.HasSufficientResource(cost);
			if(suffCost==-1){
				level+=1;
				currentActiveStat+=1;
				ResourceManager.SpendResource(cost);
				AddValue(stats[currentActiveStat].cost);
				Build();
				
				if(onUpgradedE!=null) onUpgradedE(this);
				return "";
			}
			return "Insufficient Resource";
		}
		public string UpgradeToNextTower(int ID=0){
			
			UnitTower nextLevelTower=nextLevelTowerList[Mathf.Clamp(ID, 0, nextLevelTowerList.Count)];
			
			List<int> cost=GetCost();
			int suffCost=ResourceManager.HasSufficientResource(cost);
			if(suffCost==-1){
				ResourceManager.SpendResource(cost);
				
				GameObject towerObj=(GameObject)Instantiate(nextLevelTower.gameObject, thisT.position, thisT.rotation);
				UnitTower towerInstance=towerObj.GetComponent<UnitTower>();
				towerInstance.InitTower(instanceID);
				towerInstance.SetPlatform(occupiedPlatform, occupiedNode);
				towerInstance.AddValue(value);
				towerInstance.SetLevel(level+1);
				towerInstance.Build();
				GameControl.SelectTower(towerInstance);
				
				if(onUpgradedE!=null) onUpgradedE(towerInstance);
				
				Destroy(thisObj);
				
				return "";
			}
			return "Insufficient Resource";
		}
		
		
		//only use cost from sample towers or in game tower instance, not the prefab
		//ID is for upgrade path
		public List<int> GetCost(int ID=0){
			List<int> cost=new List<int>();
			float multiplier=1;
			if(isSampleTower){
				multiplier=GetBuildCostMultiplier();
				cost=new List<int>(stats[currentActiveStat].cost);
			}
			else{
				multiplier=GetUpgradeCostMultiplier();
				if(currentActiveStat<stats.Count-1) cost=new List<int>( stats[currentActiveStat+1].cost );
				else if(ID<nextLevelTowerList.Count && nextLevelTowerList[ID]!=null) cost=new List<int>( nextLevelTowerList[ID].stats[0].cost );
			}
			for(int i=0; i<cost.Count; i++) cost[i]=(int)Mathf.Round(cost[i]*multiplier);
			return cost;
		}
		private float GetBuildCostMultiplier(){ return 1-PerkManager.GetTowerBuildCost(prefabID); }
		private float GetUpgradeCostMultiplier(){ return 1-PerkManager.GetTowerUpgradeCost(prefabID); }
		
		
		public List<int> value=new List<int>();
		//apply the refund ratio from gamecontrol
		public List<int> GetValue(){
			List<int> newValue=new List<int>();
			for(int i=0; i<value.Count; i++) newValue.Add((int)(value[i]*GameControl.GetSellTowerRefundRatio()));
			return newValue;
		}
		//called when tower is upgraded to bring the value forward
		public void AddValue(List<int> list){
			for(int i=0; i<value.Count; i++){
				value[i]+=list[i];
			}
		}
		
		
		public int FPSWeaponID=-1;
		
		
		public bool DealDamage(){
			if(type==_TowerType.Turret || type==_TowerType.AOE || type==_TowerType.Mine) return true;
			return false;
		}
		
		
		public void Destroy(){
			if(occupiedPlatform!=null) occupiedPlatform.UnbuildTower(occupiedNode);
		}
		
		
		//not compatible with PointNBuild mode
		void OnMouseEnter(){ 
			if(UIUtilities.IsCursorOnUI()) return;
			if(AbilityManager.InTargetSelectMode()) return;
			BuildManager.ShowIndicator(this);
		}
		void OnMouseExit(){ BuildManager.HideIndicator();}
	}

}