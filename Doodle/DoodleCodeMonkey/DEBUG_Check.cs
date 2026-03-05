using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;
namespace SPACE_DOODLE_CODEMONKEY
{
	public class DEBUG_Check : MonoBehaviour
	{
		private void Start()
		{
			StopAllCoroutines();
			StartCoroutine(CheckHealthSystemBot());
		}

		[SerializeField] GameObject BOT_HOLDER;
		[SerializeField] List<Bot> BOT; // just for log
		IEnumerator CheckHealthSystemBot()
		{
			Debug.Log($"found healthSystemBase: {this.gameObject.GetComponent<HealthSystemBase>()}");
			BOT = new List<Bot>();
			foreach (var obj in this.BOT_HOLDER.getLeavesGen1())
				BOT.Add(new Bot()
				{
					hSystemBase = obj.gc<HealthSystemBase>(),
					hReactionBase = obj.gc<HealthReactionBase>(),
				});
			BOT.forEach(bot => bot.Init());

			while(true)
			{
				if (INPUT.K.InstantDown(KeyCode.Q)) BOT.forEach(bot => bot.hSystemBase.TakeDamage(50f));
				if (INPUT.K.InstantDown(KeyCode.W)) BOT.forEach(bot => bot.hSystemBase.Heal(50f));
				if (INPUT.K.InstantDown(KeyCode.E)) BOT.forEach(bot => bot.hSystemBase.RepairArmour(50f));
				if (INPUT.K.InstantDown(KeyCode.R)) BOT.forEach(bot => bot.hSystemBase.Revive(Random.Range(50, 100)));
				if (INPUT.K.InstantDown(KeyCode.T)) BOT.forEach(bot => bot.hSystemBase.TakeDamage((float)1e3));

				yield return null;
			}

			yield return null;
		}

	}
	[System.Serializable]
	public class Bot
	{
		public HealthSystemBase hSystemBase;
		public HealthReactionBase hReactionBase;
		public void Init()
		{
			if (this.hSystemBase != null && this.hReactionBase != null)
				this.hReactionBase.Bind(this.hSystemBase);
		}
	}
}