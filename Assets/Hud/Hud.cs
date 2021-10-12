using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoNotModify
{

	public class Hud : MonoBehaviour
	{
		public RectTransform gameOver;
		public Text countdown;

		public Slider slider1;
		public Slider slider2;
		public Text score1;
		public Text score2;
		public Text scoreBreakdown1;
		public Text scoreBreakdown2;
		public Text playerName1;
		public Text playerName2;

		void Awake()
		{
			gameOver.gameObject.SetActive(false);
			string playerName1 = GameManager.Instance.GetPlayerName(0);
			string playerName2 = GameManager.Instance.GetPlayerName(1);
			SetPlayerNames(playerName1, playerName2);
		}

		void SetPlayerNames(string name1, string name2)
		{
			playerName1.text = name1;
			playerName2.text = name2;
		}

		// Update is called once per frame
		void Update()
		{
			GameData gameData = GameManager.Instance.GetGameData();
			score1.text = "" + GameManager.Instance.GetScoreForPlayer(0);
			score2.text = "" + GameManager.Instance.GetScoreForPlayer(1);
			scoreBreakdown1.text = "" + GameManager.Instance.GetWayPointScoreForPlayer(0) + " - " + GameManager.Instance.GetHitScoreForPlayer(0);
			scoreBreakdown2.text = "" + GameManager.Instance.GetWayPointScoreForPlayer(1) + " - " + GameManager.Instance.GetHitScoreForPlayer(1);

			slider1.value = gameData.GetSpaceShipForOwner(0).Energy;
			slider2.value = gameData.GetSpaceShipForOwner(1).Energy;
			countdown.text = ((int)gameData.timeLeft).ToString();
			if (!gameOver.gameObject.activeSelf && GameManager.Instance.IsGameFinished())
			{
				gameOver.gameObject.SetActive(true);
			}
		}
	}

}