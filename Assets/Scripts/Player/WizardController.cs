using Unity.Cinemachine;
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

	[SerializeField] private CinemachineCamera localCamera;

	[Header ("Sound Effects")]
	[SerializeField] private AudioSource footstepAudioSource;
	[SerializeField] private AudioSource owlHootingAudioSource;
	[SerializeField] private int minStepsBeforeOwlHoot = 5;
	[SerializeField] private float owlHootChance = 0.2f; // 20% chance to hoot after min steps

	private int stepsSinceLastOwlHoot = 0;
	private float stepTimer = 0f;
	[SerializeField] private float stepInterval = 0.5f;

    private CharacterController controller;
	private PlayerInput playerInput;
	private InputAction moveAction;
	private InputAction lookAction;

	private float verticalRotation = 0f;

	private Vector3 playerVelocity;
	private bool isGrounded;
	public float gravityValue = -9.81f;


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
			Cursor.visible = false;

			// If you didn't drag it in, try to find it
			if (playerCameraHolder == null)
				playerCameraHolder = transform.Find("Camera");

			var brain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();
			if (brain == null)
			{
				Debug.LogError("Main Camera is missing a Cinemachine Brain!");
			}

			localCamera.gameObject.SetActive(true);
			localCamera.Priority = 10;
		}
		else
		{
			// Disable the PlayerInput component on characters we DON'T own
			// to prevent input "ghosting"
			playerInput.enabled = false;
			if (playerCameraHolder != null) playerCameraHolder.gameObject.SetActive(false);
			
			localCamera.Priority = 0;
			localCamera.gameObject.SetActive(false);
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
		isGrounded = controller.isGrounded;
		if (isGrounded && playerVelocity.y < 0)
		{
			playerVelocity.y = -2f;
		}

		Vector2 input = moveAction.ReadValue<Vector2>();
		if (input.sqrMagnitude > 0.001f)
		{
			Vector3 moveDir = (transform.forward * input.y) + (transform.right * input.x);

			// Move horizontally
			controller.Move(moveDir * moveSpeed * Time.deltaTime);

			footstepAudioSource.pitch = moveSpeed;
            if ((!footstepAudioSource.isPlaying))
				footstepAudioSource.Play();
			
			// Calculates min steps before running a random range to trigger owl sound
			stepTimer += Time.deltaTime;
			if (stepTimer >= stepInterval/(moveSpeed/2f))
			{
				stepTimer = 0f;
				stepsSinceLastOwlHoot++;

				if (stepsSinceLastOwlHoot >= minStepsBeforeOwlHoot && !owlHootingAudioSource.isPlaying)
				{
					if (Random.value < owlHootChance) {
                        owlHootingAudioSource.Play();
                        stepsSinceLastOwlHoot = 0;
                    }
                        
				}
			}

        }

		else
		{
			if (footstepAudioSource.isPlaying)
				footstepAudioSource.Stop();
        }

			// Apply Gravity (Vertical movement)
			playerVelocity.y += gravityValue * Time.deltaTime;
		controller.Move(playerVelocity * Time.deltaTime);
	}


	void HandleRotation()
	{
		// 1. Get Mouse Delta (X = side-to-side, Y = up/down)
		Vector2 lookInput = lookAction.ReadValue<Vector2>();

		// 2. HORIZONTAL: Rotate the whole body Left/Right (Y-axis)
		transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

		// 3. VERTICAL: Rotate the Camera/Eyes Up/Down (X-axis)
		verticalRotation -= lookInput.y * mouseSensitivity; // Subtracting makes the mouse 'Natural' (pull up to look up)

		// 4. CLAMP: Prevent the camera from flipping over
		verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

		// 5. APPLY: Only tilt the camera holder, not the wizard's feet!
		if (playerCameraHolder != null)
		{
			playerCameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
		}
	}
}