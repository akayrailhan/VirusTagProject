using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector2 input;
    private Vector2 lastDir = Vector2.down; // idle yönü için

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Top-down için yerçekimi kapalı
        rb.gravityScale = 0f;
    }

    private void Update()
    {
        // Input (Raw: anında tepki, smoothing yok)
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // Diagonal speed boost olmasın
        if (input.sqrMagnitude > 1f)
            input = input.normalized;

        bool isMoving = input != Vector2.zero;
        if (isMoving)
            lastDir = input;

        // Yürürken input yönü, durunca son yön (idle yönü)
        Vector2 dirForAnim = isMoving ? input : lastDir;

        // Animator parametreleri
        anim.SetFloat("MoveX", dirForAnim.x);
        anim.SetFloat("MoveY", dirForAnim.y);
        anim.SetFloat("Speed", input.sqrMagnitude); // 0 -> idle, >0 -> walk

        // Sağ animasyon yoksa: sola yürümeyi sağa giderken aynala
        // Eğer ters çalışırsa true/false'u değiştir.
        if (dirForAnim.x > 0.01f)
            sr.flipX = true;          // sağ
        else if (dirForAnim.x < -0.01f)
            sr.flipX = false;         // sol
        // x ~ 0 iken flip'i değiştirmiyoruz (yukarı/aşağı giderken sabit kalsın)
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }
}
