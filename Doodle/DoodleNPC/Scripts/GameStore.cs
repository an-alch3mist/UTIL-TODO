using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using SPACE_UTIL;

namespace SPACE_DOODLE_NPC
{
	[DefaultExecutionOrder(-50)] // just after InputSystem is init()
	public class GameStore : MonoBehaviour
	{
		[SerializeField] InputActionAsset _IA;

		public static InputActionAsset IA;
		public static PlayerStats playerStats;
		private void Awake()
		{
			Debug.Log(C.method(this));
			GameStore.LoadAllInit();
		}

		static void LoadAllInit()
		{
			GameStore.playerStats = LOG.LoadGameData<PlayerStats>(GameDataType.playerStats);
			GameStore.IA.tryLoadBindingOverridesFromJson(LOG.LoadGameData(GameDataType.inputKeyBinding));
		}

		#region store in runtime used as
		float currTime = 0f;
		private void Update()
		{
			currTime += Time.unscaledDeltaTime;
		}
		#endregion

		private void OnApplicationQuit()
		{
			Debug.Log(C.method(this, "orange"));
			string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

			GameStore.playerStats.gameTime += this.currTime;
			GameStore.playerStats.HISTORY.Add($"{sceneName} --> {this.currTime}");
			GameStore.playerStats.Save(); // SaveGameData(), could be done at anyTime during runTime or when applicationQuit is done
		}
	}

	[System.Serializable]
	public class PlayerStats
	{
		public float gameTime;
		public List<string> HISTORY;

		public PlayerStats() // for default load when `*.json` not found in file
		{
			this.gameTime = 0f;
			this.HISTORY = new List<string>();
		}

		public void Save()
		{
			LOG.SaveGameData(GameDataType.playerStats, this.ToJson(true));
		}
	}

	// GLOBAL ENUM >>
	public enum GameDataType
	{
		playerStats,
		inputKeyBinding,
	}
	// << GLOBAL ENUM
}
