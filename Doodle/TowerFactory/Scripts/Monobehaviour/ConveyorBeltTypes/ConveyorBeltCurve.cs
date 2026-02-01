using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;


/// <summary>
/// Curved conveyor belt with 90° bezier turn.
/// Can auto-detect curve direction based on adjacent belts.
/// </summary>
public class ConveyorBeltCurve : ConveyorBelt
{
	[Header("Curve Settings")]
	[SerializeField] private bool isClockwise = true; // +90° or -90°

	protected override void Awake()
	{
		base.Awake();
		// Arc length of 90° curve is approximately 1.57 (π/2)
		pathLength = 1.57f;
	}

	protected override Vector3 GetPositionOnPath(float t)
	{
		// Cubic bezier for smooth 90° turn
		Vector3 p0 = GetStartPosition();
		Vector3 p3 = GetEndPosition();

		// Control points for smooth curve
		Vector3 p1 = p0 + GetOrientationDirection(InputOrientation) * 0.5f;
		Vector3 p2 = p3 - GetOrientationDirection(OutputOrientation) * 0.5f;

		return GetBezierPoint(p0, p1, p2, p3, t);
	}

	private Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float u = 1 - t;
		float tt = t * t;
		float uu = u * u;
		float uuu = uu * u;
		float ttt = tt * t;

		Vector3 p = uuu * p0;
		p += 3 * uu * t * p1;
		p += 3 * u * tt * p2;
		p += ttt * p3;

		return p;
	}

	protected override void UpdateBeltType()
	{
		// Auto-detect curve direction based on input/output orientations
		// If input is North and output is East, it's a clockwise curve
		int inputInt = (int)InputOrientation;
		int outputInt = (int)OutputOrientation;
		int diff = (outputInt - inputInt + 4) % 4;

		isClockwise = (diff == 1); // 90° clockwise
	}
}