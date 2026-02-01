using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual resource display entry showing icon and amount.
/// </summary>
public class ResourceDisplayEntry : MonoBehaviour
{
	[SerializeField] private Image iconImage;
	[SerializeField] private TextMeshProUGUI amountText;

	private ResourceData resource;

	public void Initialize(ResourceData resourceData, int amount)
	{
		resource = resourceData;

		if (iconImage != null && resourceData.icon != null)
		{
			iconImage.sprite = resourceData.icon;
		}

		UpdateAmount(amount);
	}

	public void UpdateAmount(int amount)
	{
		if (amountText != null)
		{
			amountText.text = amount.ToString();
		}

		// Hide if amount is 0
		if (amount == 0)
		{
			gameObject.SetActive(false);
		}
		else
		{
			gameObject.SetActive(true);
		}
	}
}