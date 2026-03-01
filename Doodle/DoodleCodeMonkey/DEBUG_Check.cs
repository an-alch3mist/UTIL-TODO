using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;
namespace SPACE_DOODLE_CODEMONKEY
{
	public class DEBUG_Check : MonoBehaviour
	{
		private void Update()
		{
			if(INPUT.M.InstantDown(0))
			{
				StopAllCoroutines();
				StartCoroutine(STIMULATE());
			}
		}

		IEnumerator STIMULATE()
		{


			yield return null;
		}
	}
}