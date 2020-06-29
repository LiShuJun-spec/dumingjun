using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK{

	public class UIUtilities : MonoBehaviour {
		
		//inputID=-1 - mouse cursor, 	inputID>=0 - touch finger index
		public static bool IsCursorOnUI(int inputID=-1){
			EventSystem eventSystem = EventSystem.current;
			return ( eventSystem.IsPointerOverGameObject( inputID ) );
		}
		
	}

}