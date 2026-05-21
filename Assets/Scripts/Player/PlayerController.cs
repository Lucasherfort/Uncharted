using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

[RequireComponent(typeof(Photon.Pun.PhotonView))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform cameraTransform;
    public Camera playerCamera;

    [Header("Movement")]
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float smoothTime = 0.08f;

    [Header("Jump / Gravity")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public float lookSmoothTime = 0.03f;

    private PlayerInputActions input;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private Vector2 currentMoveVelocity;
    private Vector2 currentMove;

    private Vector2 currentLookVelocity;
    private Vector2 currentLook;

    private float xRotation = 0f;
    private Vector3 velocity;

    private Photon.Pun.PhotonView photonView;

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        input.Player.Jump.performed += ctx => Jump();
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Start()
    {
        photonView = GetComponent<Photon.Pun.PhotonView>();

        if (!photonView.IsMine)
        {
            playerCamera.enabled = false;   
            playerCamera.GetComponent<AudioListener>().enabled = false;  
            // Sécurité si un joueur n'a pas de Canvas local sur son prefab
            Canvas localCanvas = GetComponentInChildren<Canvas>();
            if (localCanvas != null) localCanvas.enabled = false;
        }
        else
    {
        // Cache et verrouille le curseur
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        HandleLook();
        HandleMove();
        ApplyGravity();
    }

    // 🎮 MOVEMENT
    void HandleMove()
    {
        float currentSpeed = input.Player.Sprint.IsPressed() ? sprintSpeed : speed;

        currentMove = Vector2.SmoothDamp(
            currentMove,
            moveInput,
            ref currentMoveVelocity,
            smoothTime
        );

        Vector3 move = transform.right * currentMove.x + transform.forward * currentMove.y;

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    // 🎥 CAMERA LOOK
    void HandleLook()
    {
        currentLook = Vector2.SmoothDamp(
            currentLook,
            lookInput,
            ref currentLookVelocity,
            lookSmoothTime
        );

        float mouseX = currentLook.x * mouseSensitivity;
        float mouseY = currentLook.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Jump()
    {
        if (controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // --- CORRECTION DU SPAM / CORRUPTION DE POSITION ---
    // Cette méthode est appelée par PlayerHealth lors du RPC_Respawn
    public void ResetVerticalVelocity()
    {
        // On remet la gravité à zéro pour couper l'effet de chute accumulée
        velocity = Vector3.zero;

        // Sécurité anti-glissade : on vide les inputs accumulés pendant l'état de mort
        moveInput = Vector2.zero;
        currentMove = Vector2.zero;
        currentMoveVelocity = Vector2.zero;
        
        Debug.Log("<color=yellow>[PlayerController]</color> Vélocité et inputs réinitialisés pour le Respawn.");
    }
}