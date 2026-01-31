using UnityEngine;
using System.Collections.Generic;

public class CollisionChecker
{
	#region private API
	/// <summary>
	/// Checks a single collider for overlaps at the target transform
	/// </summary>
	private static bool CheckSingleCollider(Collider collider, Transform rootTransform,
		Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale, int layerMask, HashSet<Collider> excludedColliders)
	{
		// Calculate the world transform of the collider
		Matrix4x4 rootMatrix = Matrix4x4.TRS(targetPosition, targetRotation, targetScale);
		Matrix4x4 localMatrix = GetLocalMatrix(collider.transform, rootTransform);
		Matrix4x4 worldMatrix = rootMatrix * localMatrix;

		Vector3 worldPos = worldMatrix.GetColumn(3);
		Quaternion worldRot = worldMatrix.rotation;
		Vector3 worldScale = worldMatrix.lossyScale;

		return CheckColliderAtTransform(collider, worldPos, worldRot, worldScale, layerMask, excludedColliders);
	}
	private static bool CheckColliderAtTransform(Collider collider, Vector3 worldPos, Quaternion worldRot, Vector3 worldScale, int layerMask, HashSet<Collider> excludedColliders)
	{
		Collider[] hits = null;

		// Check based on collider type
		if (collider is BoxCollider)
		{
			BoxCollider box = collider as BoxCollider;
			Vector3 center = worldPos + worldRot * Vector3.Scale(box.center, worldScale);
			Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, worldScale);
			hits = Physics.OverlapBox(center, halfExtents, worldRot, layerMask, QueryTriggerInteraction.Ignore);
		}
		else if (collider is SphereCollider)
		{
			SphereCollider sphere = collider as SphereCollider;
			Vector3 center = worldPos + Vector3.Scale(sphere.center, worldScale);
			float radius = sphere.radius * Mathf.Max(worldScale.x, worldScale.y, worldScale.z);
			hits = Physics.OverlapSphere(center, radius, layerMask, QueryTriggerInteraction.Ignore);
		}
		else if (collider is CapsuleCollider)
		{
			CapsuleCollider capsule = collider as CapsuleCollider;
			Vector3 center = worldPos + worldRot * Vector3.Scale(capsule.center, worldScale);
			float radius = capsule.radius * Mathf.Max(worldScale.x, worldScale.z);
			float height = capsule.height * worldScale.y;

			Vector3 direction = capsule.direction == 0 ? Vector3.right : (capsule.direction == 1 ? Vector3.up : Vector3.forward);
			direction = worldRot * direction;

			float halfHeight = Mathf.Max(0, (height * 0.5f) - radius);
			Vector3 point1 = center + direction * halfHeight;
			Vector3 point2 = center - direction * halfHeight;

			hits = Physics.OverlapCapsule(point1, point2, radius, layerMask, QueryTriggerInteraction.Ignore);
		}
		else if (collider is MeshCollider)
		{
			Debug.LogWarning("MeshCollider overlap checking is limited. Consider using primitive colliders.");
			MeshCollider meshCollider = collider as MeshCollider;
			Bounds bounds = meshCollider.bounds;
			Vector3 center = worldPos;
			Vector3 size = Vector3.Scale(bounds.size, worldScale);
			hits = Physics.OverlapBox(center, size * 0.5f, worldRot, layerMask, QueryTriggerInteraction.Ignore);
		}
		else
		{
			Debug.LogWarning($"Collider type {collider.GetType()} not supported");
			return false;
		}

		// Filter out excluded colliders (the object itself and its children)
		if (hits != null)
		{
			foreach (Collider hit in hits)
			{
				if (!excludedColliders.Contains(hit))
				{
					return true; // Found a collision with another object
				}
			}
		}

		return false;
	}
	private static Matrix4x4 GetLocalMatrix(Transform child, Transform root)
	{
		if (child == root)
			return Matrix4x4.identity;

		Matrix4x4 matrix = Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale);

		if (child.parent != root && child.parent != null)
		{
			matrix = GetLocalMatrix(child.parent, root) * matrix;
		}

		return matrix;
	}

	#endregion

	#region Public API
	/// <summary>
	/// Checks if placing an object at the given position/rotation/scale would collide with OTHER objects in the scene
	/// (excludes the checkObject itself and its children)
	/// </summary>
	/// <param name="checkObject">The object to check (can be scene object or prefab)</param>
	/// <param name="position">World position to check</param>
	/// <param name="rotation">World rotation to check</param>
	/// <param name="scale">World scale to check</param>
	/// <param name="layerMask">Layers to check against (optional)</param>
	/// <returns>True if would collide with other objects, false if clear</returns>
	public static bool IsSpaceOccupied(GameObject checkObject, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask = ~0)
	{
		// Get all colliders from the object (including children)
		Collider[] objectColliders = checkObject.GetComponentsInChildren<Collider>(true);

		if (objectColliders.Length == 0)
		{
			Debug.LogWarning("Object has no colliders to check!");
			return false;
		}

		// Create a set of colliders to exclude (the object itself and its children)
		HashSet<Collider> excludedColliders = new HashSet<Collider>(objectColliders);

		// Check each collider against the scene
		foreach (Collider col in objectColliders)
		{
			if (CheckSingleCollider(col, checkObject.transform, position, rotation, scale, layerMask, excludedColliders))
			{
				return true; // Would collide with something else
			}
		}

		return false; // Space is clear
	}
	/// <summary>
	/// Try to place object if space is clear
	/// </summary>
	public static bool TryPlaceObject(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, int layerMask, out GameObject instance)
	{
		instance = null;

		if (!IsSpaceOccupied(prefab, position, rotation, scale, layerMask))
		{
			instance = GameObject.Instantiate(prefab, position, rotation);
			instance.transform.localScale = scale;
			return true;
		}

		Debug.Log("Cannot place object - space is occupied by other objects!");
		return false;
	}
	#endregion
}