using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

namespace SPACE_GAME
{
	public class DEBUG_Check : MonoBehaviour
	{
		private void Awake()
		{
			Debug.Log(C.method(this));
		}

		private void Update()
		{
			if (INPUT.M.InstantDown(0))
			{
				this.StopAllCoroutines();
				this.StartCoroutine(STIMULATE());
			}
		}

		[SerializeField] GameObject _prefab;
		[SerializeField] LayerMask _layerMask;
		IEnumerator STIMULATE()
		{
			while (true)
			{
				Vector3 pos = _prefab.transform.position;
				Quaternion rotation = _prefab.transform.rotation;
				Vector3 scale = _prefab.transform.localScale;

				// Check if space is occupied
				bool isSpaceOccupied = PHY.CollisionChecker.IsSpaceOccupied(
					_prefab,
					pos,
					rotation,
					scale,
					_layerMask
				);
				Debug.Log($"is space occupied: {isSpaceOccupied}".colorTag("cyan"));
				yield return null;
			}
			/*
			// Or try to place directly
			CollisionChecker.TryPlaceObject(
				_prefab,
				new Vector3(0, 0, 0),
				Quaternion.Euler(0, 45, 0),
				Vector3.one * 2f,
			);
			*/

			yield return null;
		}
	}
}