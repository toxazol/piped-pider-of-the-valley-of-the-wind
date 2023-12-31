using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable, IKnockbackable, IFallable
{
    [SerializeField] private DetectionZone detectionZone;
    [SerializeField] private DetectionZone attackDetectionZone;
    [SerializeField] private float moveForce = 500f;
    [SerializeField] private int attackDamage = 5;
    [SerializeField] private string enemyName = "blob";
    [SerializeField] private float knockbackResist = 0f;
    [SerializeField] private int armor = 0;
    [SerializeField] private int fallGravityScale = 10;
    [SerializeField] private bool isDummy = false;
    [SerializeField] private LevelManager levelManager;

    Blinking blinking;
    Animator animator;
    Rigidbody2D rb;

    private bool isAttack = false;
    
    private HealthBar healthBar;
    private Health health;
    private DamageDisplay damageDisplay;
    private SpriteRenderer sr;

    private float knockbackPower;
    private int knockbackTicks = 0;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        blinking = GetComponent<Blinking>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        sr = GetComponent<SpriteRenderer>();
        healthBar = GetComponentInChildren<HealthBar>();
        damageDisplay = GetComponentInChildren<DamageDisplay>();
        healthBar.Invoke("setFullHp", 0f);
    }

    public string GetEnemyName()
    {
        return enemyName;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void FixedUpdate()
    {
        if (animator.IsDestroyed() || isDummy) return;
        if (detectionZone.detectedObjs.Count == 0)
        {
            PlayBlockable("idle");
            return;
        }

        TurnToDetected(detectionZone.detectedObjs[0]);
        
        if (attackDetectionZone.detectedObjs.Count > 0)
        {
            foreach (Collider2D detectedObj in attackDetectionZone.detectedObjs)
            {
                Attack(detectedObj.gameObject);
            }
        }

        PlayBlockable("move");

        if (knockbackPower > 0f && knockbackTicks < 10)
        {
            var dir = (-(detectionZone.detectedObjs[0].transform.position - transform.position).normalized);
            rb.AddForce((knockbackPower - knockbackResist) * Time.fixedDeltaTime * dir);
            knockbackTicks++;
        }
        else
        {
            var dir = (detectionZone.detectedObjs[0].transform.position - transform.position).normalized;
            rb.AddForce(moveForce * Time.fixedDeltaTime * dir);
            knockbackTicks = 0;
            knockbackPower = 0;
        }
    }

    private void TurnToDetected(Collider2D detectedObject)
    {
        sr.flipX = (detectedObject.transform.position - transform.position).normalized.x < 0;
    }

    public void OnKnockback(float knockbackPower)
    {
        this.knockbackPower = knockbackPower;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // if (collision.gameObject.tag != "Player" || isDummy) return;
        // IDamageable damageableObj = collision.collider.GetComponent<IDamageable>();
        // damageableObj.OnHit(attackDamage);
    }

    public void Attack(GameObject target)
    {
        if (!isAttack)
        {
            PlayBlockable("attack");
            target.GetComponent<IDamageable>().OnHit(attackDamage);
            isAttack = true;
        }
    }

    private void PlayBlockable(string animation)
    {
        PlayBlockable(animation, false);
    }
    
    private void PlayBlockable(string animation, bool block)
    {
        if (!isAttack)
        {
            animator.Play(animation);
            isAttack = block;
        }
    }
    public void EndAttack()
    {
        isAttack = false;
    }

    public void OnFall(EdgeCollider2D border)
    {
        rb.gravityScale = fallGravityScale;
        gameObject.GetComponent<Collider2D>().enabled = false;
        // var downRay = gameObject.AddComponent<EdgeCollider2D>();
        // var blobPoint = new Vector2(transform.position.x, transform.position.y);
        // var downPoint = new Vector2(blobPoint.x, blobPoint.y-1000000);
        // downRay.SetPoints(new List<Vector2>{blobPoint, downPoint});
    }

    public void OnHit(int damage)
    {
        var calculatedDamage = damage - armor/2;
        health.Hit(calculatedDamage);
        PlayBlockable("damage", true);
        damageDisplay.AddDamage(calculatedDamage);
        healthBar.updateHp(health);

        if (health.IsDead())
        {
            OnDie();
        }
        else
        {
            blinking.Blink();
        }
    }

    public void OnDie()
    {
        animator.Play("death");
        // blinking.Blink(false);
        isDummy = true;
    }

    private void DestroyEnemy()
    {
        var countEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (countEnemies == 1)
        {
            levelManager.LoadEndMenu();
        }
        Destroy(gameObject);
    }
}