using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// NPC�� AI ���� ����: ���, ��ȸ, ����
public enum AIState
{
    Idle,       // ��� ����
    Wandering,  // ��ȸ ����
    Attacking   // ���� ����
}

public class NPC : MonoBehaviour, IDamagable
{
    [Header("Stat")]
    public int health;                // ü��
    public float walkSpeed;           // �ȱ� �ӵ�
    public float runSpeed;            // �޸��� �ӵ�
    public ItemData[] dropOnDeath;    // ��� �� �������� ������

    [Header("AI")]
    private NavMeshAgent agent;       // �׺���̼� ������Ʈ
    public float detectDistance;      // �÷��̾� Ž�� �Ÿ�
    private AIState aiState;          // ���� AI ����

    [Header("Wandering")]
    public float minWanderDistance;   // �ּ� ��ȸ �Ÿ�
    public float maxWanderDistance;   // �ִ� ��ȸ �Ÿ�
    public float minWanderWaitTime;   // �ּ� ��� �ð�
    public float maxWanderWaitTime;   // �ִ� ��� �ð�

    [Header("Combat")]
    public int damage;                // ���ݷ�
    public float attackRate;          // ���� ����
    private float lastAttackTime;     // ������ ���� �ð� ���
    public float attackDistance;      // ���� ���� �Ÿ�

    private float playerDistance;     // �÷��̾���� �Ÿ�

    public float fieldOfView = 120f;  // �þ߰�

    private Animator animator;        // �ִϸ����� ������Ʈ
    private SkinnedMeshRenderer[] meshRenderers;  // ���� �޽� ������ �迭

    private void Awake()
    {
        // ������Ʈ �ʱ�ȭ
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    void Start()
    {
        // ���۽� ��ȸ ���·� ��ȯ
        SetState(AIState.Wandering);
    }

    void Update()
    {
        // �÷��̾���� �Ÿ� ���
        playerDistance = Vector3.Distance(transform.position, CharacterManager.Instance.Player.transform.position);

        // �̵� ���ο� ���� �ִϸ����� �Ķ���� ����
        animator.SetBool("Moving", aiState != AIState.Idle);

        // ���� ���¿� ���� ������Ʈ ó��
        switch (aiState)
        {
            case AIState.Idle:
            case AIState.Wandering:
                PassiveUpdate();  // ��� �� ��ȸ ���� ó��
                break;
            case AIState.Attacking:
                AttackingUpdate();  // ���� ���� ó��
                break;
        }
    }

    // ���� ���� �޼���
    public void SetState(AIState state)
    {
        aiState = state;

        // ���¿� ���� �ӵ��� �̵� ���� ����
        switch (aiState)
        {
            case AIState.Idle:
                agent.speed = walkSpeed;
                agent.isStopped = true;  // ����
                break;
            case AIState.Wandering:
                agent.speed = walkSpeed;
                agent.isStopped = false; // �̵� ����
                break;
            case AIState.Attacking:
                agent.speed = runSpeed;
                agent.isStopped = false; // �߰� ��
                break;
        }

        // �ִϸ����� ��� �ӵ��� �̵� �ӵ��� ���� ����
        animator.speed = agent.speed / walkSpeed;
    }

    // ��� �� ��ȸ ���� ������Ʈ
    void PassiveUpdate()
    {
        // ��ȸ ���¿��� �������� �������� �� ��� ���·� ��ȯ �� ���ο� ��ȸ ��ġ�� �̵� ����
        if (aiState == AIState.Wandering && agent.remainingDistance < 0.1f)
        {
            SetState(AIState.Idle);
            Invoke("WanderToNewLocation", Random.Range(minWanderWaitTime, maxWanderWaitTime));
        }

        // �÷��̾ ���� �Ÿ� ���� ������ ���� ���·� ��ȯ
        if (playerDistance < detectDistance)
        {
            SetState(AIState.Attacking);
        }
    }

    // ���ο� ��ȸ ��ġ�� �̵��ϴ� �޼���
    void WanderToNewLocation()
    {
        // ���� ���°� Idle�� �ƴϸ� �������� ����
        if (aiState != AIState.Idle) return;

        SetState(AIState.Wandering);
        agent.SetDestination(GetWanderLocation());
    }

    // ��ȸ�� ���ο� ��ġ�� ��ȯ
    Vector3 GetWanderLocation()
    {
        NavMeshHit hit;
        // ������ ����� �Ÿ��� �̿��Ͽ� ��ġ ���ø�
        NavMesh.SamplePosition(transform.position + (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit, maxWanderDistance, NavMesh.AllAreas);

        int i = 0;
        // �÷��̾�� �ʹ� ����� ��ġ�� ����
        while (Vector3.Distance(transform.position, hit.position) < detectDistance)
        {
            NavMesh.SamplePosition(transform.position + (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit, maxWanderDistance, NavMesh.AllAreas);
            i++;
            if (i == 30) break;  // ���ѷ��� ����
        }

        return hit.position;
    }

    // ���� ���� ������Ʈ
    void AttackingUpdate()
    {
        // �÷��̾ ���� ���� ���� �ְ� �þ߿� ������ ���� ����
        if (playerDistance < attackDistance && IsPlayerInFieldOfView())
        {
            agent.isStopped = true;  // ���� �� �̵� ����
            if (Time.time - lastAttackTime > attackRate)
            {
                lastAttackTime = Time.time;
                // �÷��̾�� ���� �ֱ�
                CharacterManager.Instance.Player.controller.GetComponent<IDamagable>().TakePhysicalDamage(damage);
                animator.speed = 1;
                animator.SetTrigger("Attack");
            }
        }
        else
        {
            // �÷��̾ ������ ���� ���� ���� ���� �� �÷��̾� �߰�
            if (playerDistance < detectDistance)
            {
                agent.isStopped = false;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(CharacterManager.Instance.Player.transform.position, path))
                {
                    agent.SetDestination(CharacterManager.Instance.Player.transform.position);
                }
                else
                {
                    // ��� ��� ���� �� ��ȸ ���·� ��ȯ
                    agent.SetDestination(transform.position);
                    agent.isStopped = true;
                    SetState(AIState.Wandering);
                }
            }
            else
            {
                // �÷��̾ ���� ������ ����� ��ȸ ���·� ��ȯ
                agent.SetDestination(transform.position);
                agent.isStopped = true;
                SetState(AIState.Wandering);
            }
        }
    }

    // �÷��̾ �þ߿� ���Դ��� Ȯ��
    bool IsPlayerInFieldOfView()
    {
        Vector3 directionToPlayer = CharacterManager.Instance.Player.transform.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle < fieldOfView * 0.5f;
    }

    // ���� ���ظ� �Դ� �޼���
    public void TakePhysicalDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();  // ü���� 0 �����̸� ��� ó��
        }

        StartCoroutine(DamageFlash());  // �ǰ� �� ������ ȿ��
    }

    // ��� ó��: ������ ��� �� ������Ʈ �ı�
    void Die()
    {
        for (int i = 0; i < dropOnDeath.Length; i++)
        {
            Instantiate(dropOnDeath[i].dropPrefab, transform.position + Vector3.up * 2, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    // ���� ���� �� ������ ȿ���� �ִ� �ڷ�ƾ
    IEnumerator DamageFlash()
    {
        // ���� ���� (���� ȿ��)
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material.color = new Color(1.0f, 0.6f, 0.6f);
        }

        yield return new WaitForSeconds(0.1f);

        // ���� �������� ����
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material.color = Color.white;
        }
    }
}
