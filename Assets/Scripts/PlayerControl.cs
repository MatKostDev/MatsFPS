﻿using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [Header("Dashing")]
    [SerializeField] private float dashSpeed    = 30f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("General Movement")]
    [SerializeField] private float accelerationRate      = 70f;
    [SerializeField] private float decelerationRate      = 90f;
    [SerializeField] private float maxBasicMovementSpeed = 16f;

    [Header("Aerial Movement")]
    [SerializeField] private float jumpHeight       = 2.3f;
    [SerializeField] private float doubleJumpHeight = 1.8f;
    [SerializeField] private float gravityStrength  = 48f;
    [SerializeField] private float terminalVelocity = 55f;

    public float dashCooldownCountdown { get; private set; }

    const float GROUNDED_VELOCITY_Y             = -2f;
    const float POST_UPWARDS_DASH_VELOCITY_Y    = 4.5f;
    const float POST_HORIZONTAL_DASH_VELOCITY_Y = 1f;

    const float COYOTE_TIME = 0.1f;

    const float SLOPE_RIDE_DISTANCE_LIMIT           = 1f;  //the max distance above a slope where the player can be considered to be "on" it
    const float SLOPE_RIDE_DOWNWARDS_FORCE_STRENGTH = 20f; //the strength of the downwards force applied to pull the player onto a slope that they're going down

    Vector3 m_velocity;

    Vector3 m_dashDir;

    CharacterController m_characterController;
    float               m_initialSlopeLimit;

    float m_dashDurationCountdown;

    float m_lastTimeGrounded;
    bool  m_isGrounded;
    bool  m_isDoubleJumpAvailable;

    public Vector3 GetVelocity()
    {
        return m_velocity;
    }

    public bool IsGrounded()
    {
        return m_isGrounded;
    }

    public float GetGroundedVelocityY()
    {
        return GROUNDED_VELOCITY_Y;
    }

    void Start()
    {
        m_characterController = GetComponent<CharacterController>();
        m_initialSlopeLimit   = m_characterController.slopeLimit;

        m_isDoubleJumpAvailable = true;
    }

    void Update()
    {
        PerformGroundCheck();

        int     verticalAxis;
        int     horizontalAxis;
        Vector3 moveDir;

        ProcessBasicMovement(out verticalAxis, out horizontalAxis, out moveDir);

        ApplyDeceleration(verticalAxis, horizontalAxis);

        //clamp magnitude of velocity on the xz plane
        Vector3 velocityXZ = new Vector3(m_velocity.x, 0f, m_velocity.z);
        velocityXZ = Vector3.ClampMagnitude(velocityXZ, maxBasicMovementSpeed);

        m_velocity = new Vector3(velocityXZ.x, m_velocity.y, velocityXZ.z);

        //if the player is moving upwards, increase slope limit so they dont get caught on objects
        m_characterController.slopeLimit = m_velocity.y > 0f ? 90f : m_initialSlopeLimit;

        m_isDoubleJumpAvailable = m_isGrounded || m_isDoubleJumpAvailable;

        m_velocity.y -= gravityStrength * Time.deltaTime; //apply gravity

        //if the player is grounded, downwards velocity should be reset
        if (m_isGrounded && m_velocity.y < 0f)
            m_velocity.y = GROUNDED_VELOCITY_Y; //don't set it to 0 or else the player might float above the ground a bit

        PerformJumpLogic();

        PerformDashLogic(moveDir);

        m_velocity.y = Mathf.Max(m_velocity.y, -terminalVelocity); //clamp velocity to downwards terminal velocity

        m_characterController.Move(m_velocity * Time.deltaTime);

        //after standard movement stuff is done, check if the player should be glued to a slope
        PerformOnSlopeLogic();
    }

    void PerformGroundCheck()
    {
        m_isGrounded = m_characterController.isGrounded;

        if (m_isGrounded)
            m_lastTimeGrounded = Time.time;
    }

    void ProcessBasicMovement(out int a_verticalAxis, out int a_horizontalAxis, out Vector3 a_moveDir)
    {
        //movement axis
        a_horizontalAxis = 0;
        a_verticalAxis   = 0;

        if (Input.GetKey(KeyCode.W))
            a_verticalAxis += 1;
        if (Input.GetKey(KeyCode.S))
            a_verticalAxis -= 1;

        if (Input.GetKey(KeyCode.D))
            a_horizontalAxis += 1;
        if (Input.GetKey(KeyCode.A))
            a_horizontalAxis -= 1;

        //calculate movement direction
        a_moveDir = transform.right   * a_horizontalAxis
                  + transform.forward * a_verticalAxis;

        //normalize movement
        if (a_moveDir.sqrMagnitude > 1f)
            a_moveDir = Vector3.Normalize(a_moveDir);

        //apply basic movement
        m_velocity += a_moveDir * accelerationRate * Time.deltaTime;
    }

    void ApplyDeceleration(int a_verticalAxis, int a_horizontalAxis)
    {
        float   frameIndependentDeceleration = decelerationRate * Time.deltaTime;
        Vector3 localVelocity                = transform.InverseTransformDirection(m_velocity);

        //apply deceleration if the axis isn't being moved on
        if (a_verticalAxis == 0)
        {
            if (Mathf.Abs(localVelocity.z) > frameIndependentDeceleration)
                m_velocity -= transform.forward * Mathf.Sign(localVelocity.z) * frameIndependentDeceleration;
            else
                m_velocity -= transform.forward * localVelocity.z;
        }

        if (a_horizontalAxis == 0)
        {
            if (Mathf.Abs(localVelocity.x) > frameIndependentDeceleration)
                m_velocity -= transform.right * Mathf.Sign(localVelocity.x) * frameIndependentDeceleration;
            else
                m_velocity -= transform.right * localVelocity.x;
        }
    }

    void PerformJumpLogic()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (m_lastTimeGrounded + COYOTE_TIME >= Time.time)
                m_velocity.y = Mathf.Sqrt(jumpHeight * -2f * -gravityStrength); //calculate velocity needed in order to reach desired jump height

            //double jump
            else if (m_isDoubleJumpAvailable)
            {
                m_isDoubleJumpAvailable = false;

                float jumpStrength = Mathf.Sqrt(doubleJumpHeight * -2f * -gravityStrength); //formula to calculate velocity needed in order to reach desired jump height

                //if the player is moving down, replace the current downwards velocity with the double jump velocity
                if (m_velocity.y < 0f)
                {
                    m_velocity.y = jumpStrength;
                }
                //player is moving up, take a fraction of the current y velocity and add the jump strength to it
                else
                {
                    m_velocity.y *= 0.3f;
                    m_velocity.y += jumpStrength;
                }
            }
        }
    }

    void PerformDashLogic(Vector3 a_moveDir)
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (dashCooldownCountdown < 0f)
            {
                //dash up if no direction is input
                if (a_moveDir.sqrMagnitude < 0.7f)
                    m_dashDir = Vector3.up;

                //dash based on direction input
                else
                    m_dashDir = a_moveDir;

                //set character to dashing
                m_dashDurationCountdown = dashDuration;
                //set cooldown time
                dashCooldownCountdown = dashCooldown;
            }
        }

        //reduce dash cooldown timer
        dashCooldownCountdown -= Time.deltaTime;

        //check if player is dashing
        if (m_dashDurationCountdown > 0f)
        {
            m_dashDurationCountdown -= Time.deltaTime;

            m_velocity = m_dashDir * dashSpeed;

            //if dash is finished on this frame, reset vertical velocity
            if (m_dashDurationCountdown <= 0f)
            {
                if (m_dashDir == Vector3.up)
                    m_velocity.y = POST_UPWARDS_DASH_VELOCITY_Y; //add some velocity after an upwards dash to prevent jerkiness
                else
                    m_velocity.y = POST_HORIZONTAL_DASH_VELOCITY_Y;
            }
        }
    }

    //this function should be called AFTER standard movement is applied for the current update
    void PerformOnSlopeLogic()
    {
        //calculate new isGrounded since player movement was just updated
        bool wasGrounded   = m_isGrounded;
        bool newIsGrounded = m_characterController.isGrounded;

        //glue the player to the slope if they're moving down one (fixes bouncing when going down slopes)
        if (!newIsGrounded && wasGrounded && m_velocity.y < 0f)
        {
            Vector3 pointAtBottomOfPlayer = transform.position - (Vector3.down * m_characterController.height / 2f);

            RaycastHit hit;
            if (Physics.Raycast(pointAtBottomOfPlayer, Vector3.down, out hit, SLOPE_RIDE_DISTANCE_LIMIT))
            {
                m_characterController.Move(Vector3.down * SLOPE_RIDE_DOWNWARDS_FORCE_STRENGTH * Time.deltaTime);
            }
        }
    }
}