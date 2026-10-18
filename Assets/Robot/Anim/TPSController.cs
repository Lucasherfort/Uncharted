using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TPSController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    // Référence à votre classe d'Input générée (remplacez "PlayerInputActions" par le nom exact de votre asset d'input si besoin)
    // private PlayerInputActions input; 
    // Note : Conservez la déclaration de votre objet d'input selon votre nom de classe actuel.

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private PlayerInputActions input;

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        // Initialisation de vos inputs (à adapter selon le nom de votre classe générée)
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        // Si votre variable s'appelle autrement, adaptez "input"
        input.Player.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += ctx => lookInput = ctx.ReadValue<Vector2>();

        input.Player.Jump.performed += ctx => Jump();
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        // 1. Utilisation directe de moveInput (Vector2 : x = horizontal, y = vertical) fourni par le New Input System
        float moveX = moveInput.x;
        float moveZ = moveInput.y;

        // 2. Calcul de la direction du mouvement par rapport à l'orientation du joueur
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // 3. Déplacement horizontal
        controller.Move(move * moveSpeed * Time.deltaTime);

        // 4. Gestion de la gravité
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 5. Calcul précis de la vitesse réelle par déplacement de position
        Vector3 currentPosition = transform.position;
        Vector3 displacement = currentPosition - lastPosition;
        displacement.y = 0f; 
        
        float currentSpeed = displacement.magnitude / Time.deltaTime;
        lastPosition = currentPosition;

        // 6. Transmission de la vitesse à l'Animator pour le Blend Tree
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
        }
    }

    void Jump()
    {
        if (controller.isGrounded)
        {
            // Logique de saut si besoin
            // velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}