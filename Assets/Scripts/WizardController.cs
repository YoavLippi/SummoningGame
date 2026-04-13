using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class WizardController : NetworkBehaviour
{
	[Header("Movement Settings")]
	public float moveSpeed = 5f;
	public float mouseSensitivity = 0.1f;

	[Header("References")]
	// Drag the "Head" or "Camera" object from your PREFAB here
	[SerializeField] private Transform playerCameraHolder;

	private CharacterController controller;
	private PlayerInput playerInput;
	private InputAction moveAction;
	private InputAction lookAction;

	private float verticalRotation = 0f;

	void Awake()
	{
		controller = GetComponent<CharacterController>();
		playerInput = GetComponent<PlayerInput>();
	}

	
	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			moveAction = playerInput.actions["Move"];
			lookAction = playerInput.actions["Look"];
			Cursor.lockState = CursorLockMode.Locked;

			// If you didn't drag it in, try to find it
			if (playerCameraHolder == null)
				playerCameraHolder = transform.Find("Camera");

			var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
			if (brain == null)
			{
				Debug.LogError("Main Camera is missing a Cinemachine Brain!");
			}
		}
		else
		{
			// Disable the PlayerInput component on characters we DON'T own
			// to prevent input "ghosting"
			playerInput.enabled = false;
		}
	}

	void Update()
	{
		if (!IsOwner) return;

		HandleRotation();
		HandleMovement();
	}

	void HandleMovement()
	{
		Vector2 input = moveAction.ReadValue<Vector2>();

		// Move relative to WHERE THE PLAYER IS LOOKING (transform.forward)
		Vector3 moveDir = (transform.forward * input.y) + (transform.right * input.x);

		if (moveDir.magnitude > 0.1f)
		{
			controller.Move(moveDir * moveSpeed * Time.deltaTime);
		}
	}

	void HandleRotation()
	{
		Vector2 lookInput = lookAction.ReadValue<Vector2>();

		// 1. Horizontal Rotation (Whole Body)
		transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

		// 2. Vertical Rotation (Camera Only)
		verticalRotation -= lookInput.y * mouseSensitivity;
		verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f);

		if (playerCameraHolder != null)
		{
			playerCameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
		}
	}
}