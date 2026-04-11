using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class WizardController : NetworkBehaviour
{
	public float moveSpeed = 5f;
	public float rotationSpeed = 720f;

	private CharacterController controller;
	private PlayerInput playerInput;
	private InputAction moveAction;
	private Transform camTransform;

	void Awake()
	{
		controller = GetComponent<CharacterController>();
		playerInput = GetComponent<PlayerInput>();

		// Link the "Move" action from your map to this variable
		moveAction = playerInput.actions["Move"];
	}

	void Start()
	{
		camTransform = Camera.main.transform;
	}

	void Update()
	{
		// Only move the wizard you actually own!
		if (!IsOwner) return;

		HandleMovement();
	}

	void HandleMovement()
	{
		// Read the WASD value as a Vector2 (X and Y)
		Vector2 input = moveAction.ReadValue<Vector2>();
		Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

		if (direction.magnitude >= 0.1f)
		{
			// Calculate direction relative to camera
			float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

			controller.Move(moveDir * moveSpeed * Time.deltaTime);

			// Smoothly rotate the wizard
			Quaternion targetRot = Quaternion.LookRotation(moveDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
		}
	}
}
