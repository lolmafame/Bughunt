using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;

    [Header("Movement")]
    public float speed = 8f;
    public float runSpeed = 17f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;
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

    Vector3 velocity;
    bool isGrounded;

    float turnSmoothVelocity;
    public float turnSmoothTime = 0.1f;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        // ===== GROUND CHECK =====
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // ===== JUMP =====
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        }

        // ===== GRAVITY =====
        if (velocity.y < 0)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // ===== MOVEMENT INPUT =====
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // ===== STAMINA LOGIC =====
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

        // ===== ROTATION & MOVE =====
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle =
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

            float angle =
                Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }
    }

    // OPTIONAL (for UI later)
    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
    }
}
