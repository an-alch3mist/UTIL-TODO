using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;

/// <summary>
/// Stats component for buildings with stats (speed, range, etc.).
/// Optional component for buildings that need dynamic stats.
/// </summary>
public class StatsComponent : MonoBehaviour
{
	[System.Serializable]
	public class Stat
	{
		public string statName;
		public float baseValue;
		public float currentValue;
		public float multiplier = 1f;

		public void RecalculateValue()
		{
			currentValue = baseValue * multiplier;
		}
	}

	[SerializeField] private List<Stat> stats = new List<Stat>();

	public float GetStat(string statName)
	{
		Stat stat = stats.Find(s => s.statName == statName);
		return stat?.currentValue ?? 0f;
	}

	public void SetStatBase(string statName, float value)
	{
		Stat stat = stats.Find(s => s.statName == statName);
		if (stat != null)
		{
			stat.baseValue = value;
			stat.RecalculateValue();
		}
	}

	public void SetStatMultiplier(string statName, float multiplier)
	{
		Stat stat = stats.Find(s => s.statName == statName);
		if (stat != null)
		{
			stat.multiplier = multiplier;
			stat.RecalculateValue();
		}
	}

	public void AddStat(string statName, float baseValue)
	{
		if (stats.Find(s => s.statName == statName) == null)
		{
			Stat newStat = new Stat
			{
				statName = statName,
				baseValue = baseValue,
				currentValue = baseValue,
				multiplier = 1f
			};
			stats.Add(newStat);
		}
	}
}

