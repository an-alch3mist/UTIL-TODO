using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using SPACE_UTIL;

namespace SPACE_GAME
{
	[DefaultExecutionOrder(-50)] // just after UnityEngine.InputSystem Init()
	public class GameStore : MonoBehaviour
	{
		[SerializeField] InputActionAsset _IA;
		public static PlayerStats playerStats;
		public static InputActionAsset IA;

		private void Awake()
		{
			Debug.Log(C.method(this));
			this.LoadInit();
		}

		void LoadInit()
		{
			GameStore.IA = this._IA;
			GameStore.IA.tryLoadBindingOverridesFromJson(LOG.LoadGameData(GameDataType.inputKeyBindings));
			GameStore.playerStats = LOG.LoadGameData<PlayerStats>(GameDataType.playerStats);
		}

		#region LOG.SaveGameData Used As:
		float currTime = 0f;
		private void Update()
		{
			currTime += Time.unscaledDeltaTime;
		}
		private void OnApplicationQuit()
		{
			Debug.Log(C.method(this, "orange"));
			GameStore.playerStats.gameTime += currTime;
			GameStore.playerStats.HISTORY.Add($"{SceneManager.GetActiveScene().name} --> {currTime}");
			GameStore.playerStats.Save();
		}
		#endregion
	}

	[System.Serializable]
	public class PlayerStats
	{
		public float gameTime;
		public List<string> HISTORY = new List<string>();
		public void Save()
		{
			LOG.SaveGameData(GameDataType.playerStats, this.ToJson());
		}
	}

	// Global ENUM >>
	public enum GameDataType
	{
		inputKeyBindings,
		playerStats,
	}

	// << Global ENUM
}
