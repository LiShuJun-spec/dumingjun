using UnityEngine;
using System.Collections;

namespace TDTK {

	public class LayerManager {
		
		private static int layerCreep=31;
		private static int layerCreepF=30;
		private static int layerTower=29;
		private static int layerShootObj=28;
		
		private static int layerPlatform=27;
		private static int layerTerrain=26;
		
		
		
		
		public static int LayerCreep(){ return layerCreep; }
		public static int LayerCreepF(){ return layerCreepF; }
		public static int LayerTower(){ return layerTower; }
		public static int LayerShootObject(){ return layerShootObj; }
		public static int LayerPlatform(){ return layerPlatform; }
		
		public static int LayerTerrain(){ return layerTerrain; }
		public static int LayerUI(){ return 5; }	//layer5 is named UI by Unity's default
		
	}

}
