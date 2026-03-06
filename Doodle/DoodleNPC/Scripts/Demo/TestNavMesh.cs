using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.AI;
using SPACE_UTIL;

namespace SPACE_DOODLE_NPC
{
	public class TestNavMesh : MonoBehaviour
	{
		[SerializeField] NavMeshAgent _navMeshAgent;

		private void Update()
		{
			if(INPUT.M.InstantDown(0))
			{
				this._navMeshAgent.speed = 1.5f;
				this._navMeshAgent.stoppingDistance = 1f;
				INPUT.M.up = Vector3.up;
				this._navMeshAgent.SetDestination(INPUT.M.getPos3D);
				Debug.Log(INPUT.M.getPos3D.ToString().colorTag("cyan"));
			}
		}
	}
}