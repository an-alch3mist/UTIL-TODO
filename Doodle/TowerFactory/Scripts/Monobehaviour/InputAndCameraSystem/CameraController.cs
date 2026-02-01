using System;
using UnityEngine;
using UnityEngine.InputSystem;

using SPACE_UTIL;

/// <summary>
/// Simple camera controller for grid-based game.
/// </summary>
public class CameraController : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float moveSpeed = 10f;
	[SerializeField] private float edgeScrollBorder = 10f;

	[Header("Zoom")]
	[SerializeField] private float zoomSpeed = 5f;
	[SerializeField] private float minZoom = 5f;
	[SerializeField] private float maxZoom = 30f;

	[Header("Rotation")]
	[SerializeField] private float rotateSpeed = 100f;

	private Camera cam;

	void Awake()
	{
		cam = GetComponent<Camera>();
	}

	void Update()
	{
		HandleMovement();
		HandleZoom();
		HandleRotation();
	}

	private void HandleMovement()
	{
		Vector3 moveDir = Vector3.zero;

		// WASD keys
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
			moveDir += transform.forward;
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
			moveDir += -transform.forward;
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
			moveDir += -transform.right;
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
			moveDir += transform.right;

		// Edge scrolling
		Vector3 mousePos = Input.mousePosition;
		if (mousePos.x < edgeScrollBorder)
			moveDir += Vector3.left;
		if (mousePos.x > Screen.width - edgeScrollBorder)
			moveDir += Vector3.right;
		if (mousePos.y < edgeScrollBorder)
			moveDir += Vector3.back;
		if (mousePos.y > Screen.height - edgeScrollBorder)
			moveDir += Vector3.forward;

		// Middle mouse button drag
		if (Input.GetMouseButton(2))
		{
			float h = -Input.GetAxis("Mouse X");
			float v = -Input.GetAxis("Mouse Y");
			moveDir += new Vector3(h, 0, v) * 2f;
		}

		if (moveDir != Vector3.zero)
		{
			transform.position += (transform.right * moveDir.x + transform.forward * moveDir.z).normalized * moveSpeed * Time.deltaTime;
		}
	}

	private void HandleZoom()
	{
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0)
		{
			Vector3 pos = transform.position;
			pos.y -= scroll * zoomSpeed;
			pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
			transform.position = pos;
		}
	}

	private void HandleRotation()
	{
		// Q and E keys to rotate camera
		if (Input.GetKey(KeyCode.Q))
		{
			transform.RotateAround(transform.position, Vector3.up, rotateSpeed * Time.deltaTime);
		}
		if (Input.GetKey(KeyCode.E))
		{
			transform.RotateAround(transform.position, Vector3.up, -rotateSpeed * Time.deltaTime);
		}
	}
}