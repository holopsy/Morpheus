using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    public enum Mode { Static, Patrol }

    [Header("Basic")]
    public Mode mode = Mode.Patrol;
    [Tooltip("Slow walk speed left/right.")]
    public float speed = 0.8f;
    [Tooltip("How far from the spawn X the enemy may move (left & right).")]
    public float halfRange = 3f;

    [Header("Optional explicit bounds (leave empty to use halfRange)")]
    public Transform leftBound;
    public Transform rightBound;

    [Header("Animator")]
    [SerializeField] private Animator animator; // Visual child
    [SerializeField] private string speedParam = "Speed";

    private Rigidbody2D rb;
    private float spawnX;
    private int dir = -1;            // -1 left, +1 right
    private bool paused;
    private float leftX, rightX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        spawnX = transform.position.x;
        CalcBounds();
        dir = -1; // start by walking left

        if (mode == Mode.Static)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator) animator.SetFloat(speedParam, 0f);
        }
    }

    void FixedUpdate()
    {
        if (mode == Mode.Static)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator) animator.SetFloat(speedParam, 0f);
            return;
        }

        if (paused)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            // move slowly in current direction
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);

            // flip sprite based on dir
            ApplyFacing(dir);

            // Bound handling without jitter: clamp and flip if we step outside
            float x = transform.position.x;
            if (x < leftX)
            {
                SnapX(leftX);
                StartTurn();
            }
            else if (x > rightX)
            {
                SnapX(rightX);
                StartTurn();
            }
        }

        if (animator) animator.SetFloat(speedParam, Mathf.Abs(rb.linearVelocity.x));
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (mode == Mode.Static) return;

        // If we bonk a wall head-on, flip cleanly
        foreach (var c in col.contacts)
        {
            if (Mathf.Abs(c.normal.x) > 0.5f)
            {
                StartTurn();
                break;
            }
        }
    }

    // --- helpers ---

    void CalcBounds()
    {
        if (leftBound && rightBound)
        {
            leftX = Mathf.Min(leftBound.position.x, rightBound.position.x);
            rightX = Mathf.Max(leftBound.position.x, rightBound.position.x);
        }
        else
        {
            leftX = spawnX - Mathf.Abs(halfRange);
            rightX = spawnX + Mathf.Abs(halfRange);
        }
    }

    void ApplyFacing(int direction)
    {
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (direction >= 0 ? 1f : -1f);
        transform.localScale = s;
    }

    void SnapX(float x)
    {
        // Put exactly on the boundary and clear horizontal so we don't overshoot
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Nudge a hair back inside the patrol range to avoid staying in contact
        const float nudge = 0.01f;
        // Decide which side we are: compare distance to left vs right
        float dLeft = Mathf.Abs(x - leftX);
        float dRight = Mathf.Abs(x - rightX);
        if (dLeft < dRight)       // at (or near) left bound -> nudge right
            transform.position += new Vector3(+nudge, 0f, 0f);
        else                      // at (or near) right bound -> nudge left
            transform.position += new Vector3(-nudge, 0f, 0f);
    }

    void StartTurn()
    {
        // Flip direction and add a tiny pause so it feels organic
        dir = -dir;
        StopAllCoroutines();
        StartCoroutine(PauseTiny_Unscaled(0.12f, 0.30f));
    }

    // Uses real time so slow-mo (Time.timeScale) doesn't freeze the enemy
    IEnumerator PauseTiny_Unscaled(float min, float max)
    {
        paused = true;

        float duration = Random.Range(min, max);     // 0.12–0.30s
        float deadline = Time.realtimeSinceStartup + duration;
        // Failsafe: auto-unpause after 0.6s real time
        float hardDeadline = Time.realtimeSinceStartup + 0.6f;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (Time.realtimeSinceStartup > hardDeadline) break;
            yield return null;
        }

        paused = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        CalcBounds();
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(leftX, transform.position.y), new Vector3(rightX, transform.position.y));
    }
#endif
}
