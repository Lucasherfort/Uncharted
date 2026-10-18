using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

[RequireComponent(typeof(Photon.Pun.PhotonView))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform camPivot;      
    public Transform playerBody;    
    public Camera playerCamera;
    public Transform weaponHolder;

    [Header("Movement")]
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float smoothTime = 0.08f;
    public float rotationSpeed = 15f; 

    [Header("Camera & Aim Settings")]
    public float mouseSensitivity = 2f;
    public float minPitch = -40f;       
    public float maxPitch = 70f;   
    public float lookSmoothTime = 0.03f;     
    
    [Header("Camera Offsets (Normal vs Aim)")]
    private Vector3 normalCamPos = new Vector3(1.269f, 0.4f, -3.5f); // Position épaule droite normale
    private Vector3 aimCamPos = new Vector3(1.269f, 0.4f, -0.9f);     // Position rapprochée pour viser
    public float camTransitionSpeed = 10f;                          // Vitesse de transition du zoom

    [Header("Jump / Gravity")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private PlayerInputActions input;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isAiming = false; // État de visée

    private Vector2 currentMoveVelocity;
    private Vector2 currentMove;

    private Vector2 currentLookVelocity;
    private Vector2 currentLook;

    private float yaw = 0f;   
    private float pitch = 0f; 
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

        // Utilisation directe de votre MatchHUDManager
        input.Player.Aim.performed += ctx => 
        {
            isAiming = true;
            if (MatchHUDManager.Instance.LocalPlayerHUD != null) 
            {
                MatchHUDManager.Instance.LocalPlayerHUD.ToggleCrosshair(true);
            }
        };
        
        input.Player.Aim.canceled += ctx => 
        {
            isAiming = false;
            if (MatchHUDManager.Instance.LocalPlayerHUD != null) 
            {
                MatchHUDManager.Instance.LocalPlayerHUD.ToggleCrosshair(false);
            }
        };      
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
            if (playerCamera != null)
            {
                playerCamera.enabled = false;   
                if (playerCamera.GetComponent<AudioListener>() != null)
                    playerCamera.GetComponent<AudioListener>().enabled = false;  
            }
        }
        else
        {
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

    // 🎥 CAMERA LOOK & ZOOM AIM
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

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 1. Le pivot suit la position de la capsule
        if (camPivot != null)
        {
            camPivot.position = transform.position + new Vector3(0f, 1.5f, 0f);
            camPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);

            // 2. Zoom de la caméra en mode Aim
            if (playerCamera != null)
            {
                Vector3 targetPos = isAiming ? aimCamPos : normalCamPos;
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition, 
                    targetPos, 
                    camTransitionSpeed * Time.deltaTime
                );
            }
        }

        // 3. Orientation de l'arme (WeaponHolder)
        if (weaponHolder != null)
        {
            if (isAiming)
            {
                // En mode Aim : L'arme suit l'inclinaison complète de la caméra (Pitch + Yaw)
                weaponHolder.rotation = Quaternion.Slerp(
                    weaponHolder.rotation, 
                    Quaternion.Euler(pitch, yaw, 0f), 
                    20f * Time.deltaTime
                );
            }
            else
            {
                // En mode Normal : L'arme revient à plat alignée avec la rotation de la capsule/playerBody
                weaponHolder.localRotation = Quaternion.Slerp(
                    weaponHolder.localRotation, 
                    Quaternion.identity, 
                    15f * Time.deltaTime
                );
            }
        }
    }

    // 🎮 MOVEMENT & AIM ROTATION LOCK
    void HandleMove()
    {
        float currentSpeed = input.Player.Sprint.IsPressed() && !isAiming ? sprintSpeed : speed;
        // (Optionnel : on empêche de sprinter en visant)

        currentMove = Vector2.SmoothDamp(
            currentMove,
            moveInput,
            ref currentMoveVelocity,
            smoothTime
        );

        // Direction du mouvement calculée par rapport à la caméra
        float targetAngle = Mathf.Atan2(currentMove.x, currentMove.y) * Mathf.Rad2Deg + yaw;

        if (playerBody != null)
        {
            if (isAiming)
            {
                // **MODE AIM** : Le joueur pivote INSTANTANÉMENT avec la caméra (face à l'écran)
                // On force l'angle du corps à s'aligner sur le Yaw de la caméra en permanence
                Quaternion aimRotation = Quaternion.Euler(0f, yaw, 0f);
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, aimRotation, 20f * Time.deltaTime);
            }
            else if (currentMove.magnitude >= 0.1f)
            {
                // **MODE NORMAL** : Le joueur s'oriente vers là où il court
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Déplacement du CharacterController
        // En mode Aim, on peut se déplacer dans toutes les directions (strafe) par rapport à la caméra
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        
        if (currentMove.magnitude >= 0.1f || isAiming)
        {
            // Si on vise ou qu'on bouge, on applique le mouvement
            Vector3 finalMoveDir = isAiming ? (transform.right * currentMove.x + transform.forward * currentMove.y) : moveDir;
            // Note: en mode aim, le transform.forward est aligné sur la caméra si le corps suit le yaw.
        }
        
        // On conserve ta logique de déplacement propre :
        if (currentMove.magnitude >= 0.1f)
        {
            Vector3 moveDirFinal = isAiming ? (Quaternion.Euler(0f, yaw, 0f) * new Vector3(currentMove.x, 0f, currentMove.y)) : (Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward);
            controller.Move(moveDirFinal * currentSpeed * Time.deltaTime);
        }

        // Transmission de la vitesse au Blend Tree
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            animator.SetFloat("Speed", horizontalVelocity.magnitude, 0.1f, Time.deltaTime);
        }
    }

    void Jump()
    {
        if (controller.isGrounded && !isAiming) // Souvent on bloque le saut en mode visée
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

    public void ResetVerticalVelocity()
    {
        velocity = Vector3.zero;
        moveInput = Vector2.zero;
        currentMove = Vector2.zero;
        currentMoveVelocity = Vector2.zero;
    }
}