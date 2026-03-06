using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using SPACE_UTIL;

namespace SPACE_DOODLE_NPC
{
	public class GridManager : MonoBehaviour
	{
		private void Start()
		{
			this.StopAllCoroutines();
			this.StartCoroutine(STIMULATE());
		}
		IEnumerator STIMULATE()
		{
			this.Gather();
			this.Logic();
			yield return null;
		}

		void Logic()
		{

		}
		[SerializeField] GameObject _pfTMPro;
		[SerializeField] int w = 10, h = 10;
		Board<TextMeshPro> B;
		void Gather()
		{
			B = new Board<TextMeshPro>((this.w, this.h), null);
			for(int y = 0; y < B.h; y += 1)
				for(int x = 0; x < B.w; x += 1)
				{
					GameObject obj = GameObject.Instantiate(this._pfTMPro, C.prefabHolder);
					obj.transform.position = new Vector3(x, 0, y);
					B[(x, y)] = obj.gc<TextMeshPro>();
					B[(x, y)].text = $"{x},{y}";
				}
		}
	}
}