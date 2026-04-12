using UnityEngine;
using Unity.Netcode;

public class MP_ThirdPersonMovement : NetworkBehaviour
{
    public CharacterController controller;
    public Transform cam;

    [Header("Movement")]
    public float speed = 8f;
    public float runSpeed = 17f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Stamina")]
    public float maxStamina = 5f;
    public float staminaDrainRate = 1.5f;
    public float staminaRegenRate = 1f;
    private float currentStamina;
    private bool canRun = true;

    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private float turnSmoothVelocity;
    public float turnSmoothTime = 0.05f;

    // ADD THIS — syncs animation state across network
    private NetworkVariable<float> networkAnimSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        canRun = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (cam != null)
                cam.gameObject.SetActive(false);

            // Don't disable the whole script — we need it for animation sync
            // enabled = false; ← REMOVE THIS
            animator = GetComponentInChildren<Animator>();

            // Subscribe to animation changes from network
            networkAnimSpeed.OnValueChanged += OnAnimSpeedChanged;
            return;
        }

        currentStamina = maxStamina;
        animator = GetComponentInChildren<Animator>();
        // Safe camera find
        Camera foundCam = GetComponentInChildren<Camera>(true);
        if (foundCam != null)
            cam = foundCam.transform;
        else
            Debug.LogWarning("[Player] No camera found in children!");
    }

    // Called on non-owner clients when animation speed changes
    void OnAnimSpeedChanged(float oldVal, float newVal)
    {
        if (animator != null)
            animator.SetFloat("Speed", newVal);
    }

    void Update()
    {
        if (!IsOwner) return;

        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Gravity
        if (velocity.y < 0)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // Stamina
        bool isTryingToRun = Input.GetKey(KeyCode.LeftShift) && direction.magnitude >= 0.1f;
        if (isTryingToRun && canRun)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                canRun = false;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                canRun = true;
            }
        }

        float currentSpeed = (isTryingToRun && canRun) ? runSpeed : speed;

        // Rotation
        float targetAngle = cam.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
            ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        // Movement
        if (direction.magnitude >= 0.1f)
        {
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            controller.Move(moveDir * currentSpeed * Time.deltaTime);
        }

        // Animator — sync to network
        float animSpeed = direction.magnitude >= 0.1f ? 1f : 0f;
        animator.SetFloat("Speed", animSpeed);

        // ADD THIS — broadcast animation state to other clients
        networkAnimSpeed.Value = animSpeed;
    }

    public float GetStaminaNormalized() => currentStamina / maxStamina;
    public bool IsActuallyRunning() => Input.GetKey(KeyCode.LeftShift) && canRun &&
        new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).magnitude >= 0.1f;
}