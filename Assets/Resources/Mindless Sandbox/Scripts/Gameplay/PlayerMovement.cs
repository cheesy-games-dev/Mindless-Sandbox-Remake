using Unity.Netcode;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Net.NetworkInformation;

namespace MindlessSandbox
{
    // Some stupid rigidbody based movement by Dani

    using System;
    using UnityEngine;

    public class PlayerMovement : NetworkBehaviour
    {
        #region DefaultVariables

        [Header("Assignables")]
        public LayerMask whatIsGround;
        private Rigidbody rb;

        [Header("Rotation and look")]
        private float xRotation;
        [Tooltip("mouse/look sensitivity")]
        public float sensitivity = 50f;
        private float sensMultiplier = 1.5f;

        [Header("Movement")]
        [Tooltip("additive force amount. every physics update that forward is pressed, this force (multiplied by 1/tickrate) will be added to the player.")]
        public float moveSpeed = 4500;
        [Tooltip("maximum local velocity before input is cancelled")]
        public float maxSpeed = 20;
        [Tooltip("normal countermovement when not crouching.")]
        public float counterMovement = 0.175f;
        private float threshold = 0.01f;
        [Tooltip("the maximum angle the ground can have relative to the players up direction.")]
        public float maxSlopeAngle = 35f;
        private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
        private Vector3 playerScale;
        [Tooltip("forward force for when a crouch is started.")]
        public float slideForce = 400;
        [Tooltip("countermovement when sliding. this doesnt work the same way as normal countermovement.")]
        public float slideCounterMovement = 0.2f;
        private bool readyToJump = true;
        private float jumpCooldown = 0.25f;
        [Tooltip("this determines the jump force but is also applied when jumping off of walls, if you decrease it, you may end up being able to walljump and then get back onto the wall leading to infinite height.")]
        public float jumpForce = 550f;
        float x, y;
        bool jumping;
        private Vector3 normalVector = Vector3.up;

        [Header("Wallrunning")]
        private float actualWallRotation;
        private float wallRotationVel;
        private Vector3 wallNormalVector;
        [Tooltip("when wallrunning, an upwards force is constantly applied to negate gravity by about half (at default), increasing this value will lead to more upwards force and decreasing will lead to less upwards force.")]
        public float wallRunGravity = 1;
        [Tooltip("when a wallrun is started, an upwards force is applied, this describes that force.")]
        public float initialForce = 20f;
        [Tooltip("float to choose how much force is applied outwards when ending a wallrun. this should always be greater than Jump Force")]
        public float escapeForce = 600f;
        private float wallRunRotation;
        [Tooltip("how much you want to rotate the camera sideways while wallrunning")]
        public float wallRunRotateAmount = 10f;
        [Tooltip("a bool to check if the player is wallrunning because thats kinda necessary.")]
        public bool isWallRunning;
        [Tooltip("a bool to determine whether or not to actually allow wallrunning.")]
        public bool useWallrunning = true;

        [Header("Collisions")]
        [Tooltip("a bool to check if the player is on the ground.")]
        public bool grounded;
        [Tooltip("a bool to check if the player is currently crouching.")]
        public bool crouching;
        private bool surfing;
        private bool cancellingGrounded;
        private bool cancellingSurf;
        private bool cancellingWall;
        private bool onWall;
        private bool cancelling;

        #endregion

        #region NewVariables
        [Header("Mindless Exclusive")]
        public MoveCamera moveCamera;
        public Transform playerCam;
        public Transform playerHead;
        public Transform orientation;
        public MindlessControllerManager controller;
        public Animator animator;
        CapsuleCollider bodyCollider;
        float tempHeight;
        #endregion

        public static PlayerMovement Instance { get; private set; }

        void Awake()
        {

            Instance = this;

            rb = GetComponent<Rigidbody>();

            //Create a physic material with no friction to allow for wallrunning and smooth movement not being dependant
            //and smooth movement not being dependant on the in-built unity physics engine, apart from collisions.
            PhysicsMaterial mat = new PhysicsMaterial("tempMat");

            mat.bounceCombine = PhysicsMaterialCombine.Average;

            mat.bounciness = 0;

            mat.frictionCombine = PhysicsMaterialCombine.Minimum;

            mat.staticFriction = 0;
            mat.dynamicFriction = 0;

            gameObject.GetComponent<Collider>().material = mat;
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            bodyCollider = GetComponent<CapsuleCollider>();
            readyToJump = true;
            tempHeight = bodyCollider.height;
            moveCamera.Head = playerCam;
            NetworkObject.Spawn();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            MovementServerRpc(controller.Move, controller.Jump, controller.Crouch);
        }

        private void Update()
        {
            if (!IsOwner || Cursor.lockState != CursorLockMode.Locked)
            {
                x = 0;
                y = 0;
                jumping = false;
                return;
            }
            ControlAnimator();
            MyInput();
            LookRpc(controller.Look);
        }

        private void ControlAnimator()
        {
            if (!animator) return;

            // Animation
            if (IsOwner)
            {
                animator.speed = 1;
                animator.SetBool("Fall", grounded);
                animator.SetFloat("Crouch", crouching ? 1 : 0);
                animator.SetFloat("Move", controller.Move.magnitude);
            }
        }

        private void MyInput()
        {
            x = controller.Move.x;
            y = controller.Move.y;
            jumping = controller.Jump;
            if (controller.Crouch && !crouching)
            {
                crouching = true;
                StartCrouchRpc();
            }
            else if (!controller.Crouch && crouching)
            {
                crouching = false;
                StopCrouchRpc();
            }
            sensMultiplier = PlayerPrefs.GetFloat("Sens", 0.7f);
        }

        [Rpc(SendTo.Everyone)]
        private void StartCrouchRpc()
        {
            bodyCollider.height = tempHeight / 1.6f;
            bodyCollider.center = new Vector3(0, 0f, 0);
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
            if (rb.linearVelocity.magnitude > 0.5f)
            {
                if (grounded)
                {
                    rb.AddForce(orientation.transform.forward * slideForce);
                }
            }
        }

        [Rpc(SendTo.Everyone)]
        private void StopCrouchRpc()
        {
            bodyCollider.height = tempHeight;
            bodyCollider.center = new Vector3(0, 0f, 0);
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        }

        [Rpc(SendTo.Everyone)]
        private void LookRpc(Vector2 serverLook)
        {
            float mouseX = 0;
            float mouseY = 0;

            mouseX = serverLook.x * sensitivity * Time.fixedDeltaTime * sensMultiplier;
            mouseY = serverLook.y * sensitivity * Time.fixedDeltaTime * sensMultiplier;

            //Find current look rotation

            //Rotate, and also make sure we dont over- or under-rotate.
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -85f, 90f);

            //Perform the rotations
            transform.Rotate(Vector3.up * mouseX);
            playerHead.localRotation = Quaternion.Euler(xRotation, 0, 0);
        }

        [ServerRpc]
        private void MovementServerRpc(Vector2 serverMove, bool serverJump, bool serverCrouch)
        {
            float x = serverMove.x;
            float y = serverMove.y;
            bool jumping = serverJump;
            bool crouching = serverCrouch;

            //Extra gravity
            rb.AddForce(Vector3.down * Time.deltaTime * 10);

            //Find actual velocity relative to where player is looking
            Vector2 mag = FindVelRelativeToLook();
            float xMag = mag.x, yMag = mag.y;

            //Counteract sliding and sloppy movement
            CounterMovement(x, y, mag);

            //If holding jump && ready to jump, then jump
            if (readyToJump && jumping) Jump();

            //Set max speed
            float maxSpeed = this.maxSpeed;

            //If sliding down a ramp, add force down so player stays grounded and also builds speed
            if (crouching && grounded && readyToJump)
            {
                rb.AddForce(Vector3.down * Time.deltaTime * 3000);
                return;
            }

            //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
            if (x > 0 && xMag > maxSpeed) x = 0;
            if (x < 0 && xMag < -maxSpeed) x = 0;
            if (y > 0 && yMag > maxSpeed) y = 0;
            if (y < 0 && yMag < -maxSpeed) y = 0;

            //Some multipliers
            float multiplier = 1f, multiplierV = 1f;

            // Movement in air
            if (!grounded)
            {
                multiplier = 0.5f;
                multiplierV = 0.5f;
            }

            // Movement while sliding
            if (grounded && crouching) multiplierV = 0f;

            //Apply forces to move player
            rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
            rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
        }

        private void Jump()
        {
            if (grounded && readyToJump)
            {
                readyToJump = false;

                //Add jump forces
                rb.AddForce(Vector2.up * jumpForce * 1.5f);
                rb.AddForce(normalVector * jumpForce * 0.5f);

                //If jumping while falling, reset y velocity.
                Vector3 vel = rb.linearVelocity;
                if (rb.linearVelocity.y < 0.5f)
                    rb.linearVelocity = new Vector3(vel.x, 0, vel.z);
                else if (rb.linearVelocity.y > 0)
                    rb.linearVelocity = new Vector3(vel.x, vel.y / 2, vel.z);

                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        private void ResetJump()
        {
            readyToJump = true;
        }

        private void CounterMovement(float x, float y, Vector2 mag)
        {
            if (!grounded || jumping) return;

            //Slow down sliding
            if (crouching)
            {
                rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
                return;
            }

            //Counter movement
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
            }

            //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
            if (Mathf.Sqrt((Mathf.Pow(rb.linearVelocity.x, 2) + Mathf.Pow(rb.linearVelocity.z, 2))) > maxSpeed)
            {
                float fallspeed = rb.linearVelocity.y;
                Vector3 n = rb.linearVelocity.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
            }
        }

        /// <summary>
        /// Find the velocity relative to where the player is looking
        /// Useful for vectors calculations regarding movement and limiting movement
        /// </summary>
        /// <returns></returns>
        public Vector2 FindVelRelativeToLook()
        {
            float lookAngle = orientation.transform.eulerAngles.y;
            float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

            float u = Mathf.DeltaAngle(lookAngle, moveAngle);
            float v = 90 - u;

            float magnitue = rb.linearVelocity.magnitude;
            float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
            float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

            return new Vector2(xMag, yMag);
        }

        private bool IsFloor(Vector3 v)
        {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }

        /// <summary>
        /// Handle ground detection
        /// </summary>
        private void OnCollisionStay(Collision other)
        {
            //Make sure we are only checking for walkable layers
            int layer = other.gameObject.layer;
            if (whatIsGround != (whatIsGround | (1 << layer))) return;

            //Iterate through every collision in a physics update
            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;
                //FLOOR
                if (IsFloor(normal))
                {
                    grounded = true;
                    cancellingGrounded = false;
                    normalVector = normal;
                    CancelInvoke(nameof(StopGrounded));
                }
            }

            //Invoke ground/wall cancel, since we can't check normals with CollisionExit
            float delay = 3f;
            if (!cancellingGrounded)
            {
                cancellingGrounded = true;
                Invoke(nameof(StopGrounded), Time.deltaTime * delay);
            }
        }

        private void StopGrounded()
        {
            grounded = false;
        }
    }
}

/*
public class OldPlayerMovement : NetworkBehaviour
{
    #region PlayerVariables

    [Header("Assignables")]
    public MoveCamera moveCamera;
    public Transform playerCam;
    public Transform playerHead;
    public Transform orientation;
    public LayerMask whatIsGround;
    private Rigidbody rb;
    [Header("Input")]
    public MindlessControllerManager controller;
    public Animator animator;
    private float xRotation;
    public float sensitivity = 50f;
    private float sensMultiplier = 1.5f;

    [Header("Movement")]
    public float moveSpeed = 4500f;
    public float maxSpeed = 20f;
    public float counterMovement = 0.2f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;
    [NonSerialized] public float x, y;
    [NonSerialized] public bool jumping, crouching;
    private Vector3 normalVector = Vector3.up;

    [Header("Collisions")]
    public bool grounded;
    CapsuleCollider bodyCollider;
    float tempSpeed;
    float tempHeight;
    float tempMass;

    // Slide Force
    public float slideForce = 250;
    public float slideCounterMovement = 0.2f;

    #endregion

    public static OldPlayerMovement Instance { get; private set; }

    void Awake()
    {

        Instance = this;

        rb = GetComponent<Rigidbody>();
        PhysicsMaterial mat = new PhysicsMaterial("tempMat");

        mat.bounceCombine = PhysicsMaterialCombine.Average;

        mat.bounciness = 0;

        mat.frictionCombine = PhysicsMaterialCombine.Minimum;

        mat.staticFriction = 0;
        mat.dynamicFriction = 0;

        tempMass = rb.mass;

        gameObject.GetComponent<Collider>().material = mat;

    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        bodyCollider = GetComponent<CapsuleCollider>();
        readyToJump = true;
        tempSpeed = maxSpeed;
        tempHeight = bodyCollider.height;
        moveCamera.Head = playerCam;
        NetworkObject.Spawn();
    }


    private void FixedUpdate()
    {
        Movement();
    }

    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer)))
            return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void Update()
    {
        ControlAnimator();
        if (!IsOwner || Cursor.lockState != CursorLockMode.Locked)
        {
            x = 0;
            y = 0;
            jumping = false;
            return;
        }
        MyInput();
        Look();
    }

    private void ControlAnimator()
    {
        if (!animator) return;

        // Animation
        if (IsOwner)
        {
            animator.speed = 1;
            animator.SetBool("Fall", grounded);
            animator.SetFloat("Crouch", crouching ? 1 : 0);
            animator.SetFloat("Move", controller.Move.magnitude);
        }
    }

    private void MyInput()
    {
        x = controller.Move.x;
        y = controller.Move.y;
        jumping = controller.Jump;
        if (controller.Move.magnitude == 0)
        {
            maxSpeed = tempSpeed;
        }
        if (controller.Crouch && !crouching)
        {
            crouching = true;
            StartCrouchRpc();
        }
        else if (!controller.Crouch && crouching)
        {
            crouching = false;
            StopCrouchRpc();
        }
        sensMultiplier = PlayerPrefs.GetFloat("Sens", 0.7f);
    }

    [Rpc(SendTo.Everyone)]
    private void StartCrouchRpc()
    {
        bodyCollider.height = tempHeight / 1.6f;
        bodyCollider.center = new Vector3(0, 0f, 0);
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            if (grounded)
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void StopCrouchRpc()
    {
        bodyCollider.height = tempHeight;
        bodyCollider.center = new Vector3(0, 0f, 0);
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.linearVelocity;
            if (rb.linearVelocity.y < 0.5f)
                rb.linearVelocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private float desiredX;
    private void Look()
    {
        float mouseX = 0;
        float mouseY = 0;

        if (!IsOwner) return;

        mouseX = controller.Look.x * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        mouseY = controller.Look.y * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 90f);

        //Perform the rotations
        transform.Rotate(Vector3.up * mouseX);
        playerHead.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping)
            return;

        //Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.linearVelocity.x, 2) + Mathf.Pow(rb.linearVelocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.linearVelocity.y;
            Vector3 n = rb.linearVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.linearVelocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    private void StopGrounded()
    {
        grounded = false;
    }

    void Climb()
    {
        Debug.Log("Climbing");
        if (jumping)
        {
            rb.mass = tempMass;
            rb.AddForce(Vector3.up, ForceMode.Impulse);
        }
        else
        {
            rb.mass = tempMass;
        }
    }

}*/