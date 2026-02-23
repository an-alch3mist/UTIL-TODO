using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SPACE_UTIL;

namespace SPACE_DOODLE_TILE
{
	public class DEBUG_DoodleTile : MonoBehaviour
	{
		private void Update()
		{
			if(INPUT.M.InstantDown(0))
			{
				this.StopAllCoroutines();
				this.StartCoroutine(STIMULATE());
			}
		}
		//
		IEnumerator STIMULATE()
		{

			yield return null;
		}
	}
}