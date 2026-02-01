using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject defining a resource type (wood, stone, processed items, etc.).
/// </summary>
[CreateAssetMenu(fileName = "new-ResourceData", menuName = "TowerFactory/ResourceData")]
public class ResourceData : ScriptableObject
{
	public string resourceName = "resourceName";
	public Sprite icon;
	public GameObject itemPrefab; // 3D model for items on belts
	public int maxStackSize = 100;
	public Color resourceColor = Color.white;
}
