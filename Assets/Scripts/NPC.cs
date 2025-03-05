using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// NPC의 AI 상태 정의: 대기, 배회, 공격
public enum AIState
{
    Idle,       // 대기 상태
    Wandering,  // 배회 상태
    Attacking   // 공격 상태
}

public class NPC : MonoBehaviour, IDamagable
{
    [Header("Stat")]
    public int health;                // 체력
    public float walkSpeed;           // 걷기 속도
    public float runSpeed;            // 달리기 속도
    public ItemData[] dropOnDeath;    // 사망 시 떨어지는 아이템

    [Header("AI")]
    private NavMeshAgent agent;       // 네비게이션 에이전트
    public float detectDistance;      // 플레이어 탐지 거리
    private AIState aiState;          // 현재 AI 상태

    [Header("Wandering")]
    public float minWanderDistance;   // 최소 배회 거리
    public float maxWanderDistance;   // 최대 배회 거리
    public float minWanderWaitTime;   // 최소 대기 시간
    public float maxWanderWaitTime;   // 최대 대기 시간

    [Header("Combat")]
    public int damage;                // 공격력
    public float attackRate;          // 공격 간격
    private float lastAttackTime;     // 마지막 공격 시간 기록
    public float attackDistance;      // 공격 가능 거리

    private float playerDistance;     // 플레이어와의 거리

    public float fieldOfView = 120f;  // 시야각

    private Animator animator;        // 애니메이터 컴포넌트
    private SkinnedMeshRenderer[] meshRenderers;  // 모델의 메쉬 렌더러 배열

    private void Awake()
    {
        // 컴포넌트 초기화
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    void Start()
    {
        // 시작시 배회 상태로 전환
        SetState(AIState.Wandering);
    }

    void Update()
    {
        // 플레이어와의 거리 계산
        playerDistance = Vector3.Distance(transform.position, CharacterManager.Instance.Player.transform.position);

        // 이동 여부에 따른 애니메이터 파라미터 설정
        animator.SetBool("Moving", aiState != AIState.Idle);

        // 현재 상태에 따른 업데이트 처리
        switch (aiState)
        {
            case AIState.Idle:
            case AIState.Wandering:
                PassiveUpdate();  // 대기 및 배회 상태 처리
                break;
            case AIState.Attacking:
                AttackingUpdate();  // 공격 상태 처리
                break;
        }
    }

    // 상태 변경 메서드
    public void SetState(AIState state)
    {
        aiState = state;

        // 상태에 따라 속도와 이동 제어 설정
        switch (aiState)
        {
            case AIState.Idle:
                agent.speed = walkSpeed;
                agent.isStopped = true;  // 정지
                break;
            case AIState.Wandering:
                agent.speed = walkSpeed;
                agent.isStopped = false; // 이동 시작
                break;
            case AIState.Attacking:
                agent.speed = runSpeed;
                agent.isStopped = false; // 추격 중
                break;
        }

        // 애니메이터 재생 속도를 이동 속도에 맞춰 조정
        animator.speed = agent.speed / walkSpeed;
    }

    // 대기 및 배회 상태 업데이트
    void PassiveUpdate()
    {
        // 배회 상태에서 목적지에 도착했을 때 대기 상태로 전환 후 새로운 배회 위치로 이동 예약
        if (aiState == AIState.Wandering && agent.remainingDistance < 0.1f)
        {
            SetState(AIState.Idle);
            Invoke("WanderToNewLocation", Random.Range(minWanderWaitTime, maxWanderWaitTime));
        }

        // 플레이어가 감지 거리 내에 들어오면 공격 상태로 전환
        if (playerDistance < detectDistance)
        {
            SetState(AIState.Attacking);
        }
    }

    // 새로운 배회 위치로 이동하는 메서드
    void WanderToNewLocation()
    {
        // 현재 상태가 Idle이 아니면 실행하지 않음
        if (aiState != AIState.Idle) return;

        SetState(AIState.Wandering);
        agent.SetDestination(GetWanderLocation());
    }

    // 배회할 새로운 위치를 반환
    Vector3 GetWanderLocation()
    {
        NavMeshHit hit;
        // 랜덤한 방향과 거리를 이용하여 위치 샘플링
        NavMesh.SamplePosition(transform.position + (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit, maxWanderDistance, NavMesh.AllAreas);

        int i = 0;
        // 플레이어와 너무 가까운 위치는 피함
        while (Vector3.Distance(transform.position, hit.position) < detectDistance)
        {
            NavMesh.SamplePosition(transform.position + (Random.onUnitSphere * Random.Range(minWanderDistance, maxWanderDistance)), out hit, maxWanderDistance, NavMesh.AllAreas);
            i++;
            if (i == 30) break;  // 무한루프 방지
        }

        return hit.position;
    }

    // 공격 상태 업데이트
    void AttackingUpdate()
    {
        // 플레이어가 공격 범위 내에 있고 시야에 들어오면 공격 실행
        if (playerDistance < attackDistance && IsPlayerInFieldOfView())
        {
            agent.isStopped = true;  // 공격 시 이동 정지
            if (Time.time - lastAttackTime > attackRate)
            {
                lastAttackTime = Time.time;
                // 플레이어에게 피해 주기
                CharacterManager.Instance.Player.controller.GetComponent<IDamagable>().TakePhysicalDamage(damage);
                animator.speed = 1;
                animator.SetTrigger("Attack");
            }
        }
        else
        {
            // 플레이어가 여전히 감지 범위 내에 있을 때 플레이어 추격
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
                    // 경로 계산 실패 시 배회 상태로 전환
                    agent.SetDestination(transform.position);
                    agent.isStopped = true;
                    SetState(AIState.Wandering);
                }
            }
            else
            {
                // 플레이어가 감지 범위를 벗어나면 배회 상태로 전환
                agent.SetDestination(transform.position);
                agent.isStopped = true;
                SetState(AIState.Wandering);
            }
        }
    }

    // 플레이어가 시야에 들어왔는지 확인
    bool IsPlayerInFieldOfView()
    {
        Vector3 directionToPlayer = CharacterManager.Instance.Player.transform.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle < fieldOfView * 0.5f;
    }

    // 물리 피해를 입는 메서드
    public void TakePhysicalDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();  // 체력이 0 이하이면 사망 처리
        }

        StartCoroutine(DamageFlash());  // 피격 시 깜빡임 효과
    }

    // 사망 처리: 아이템 드랍 후 오브젝트 파괴
    void Die()
    {
        for (int i = 0; i < dropOnDeath.Length; i++)
        {
            Instantiate(dropOnDeath[i].dropPrefab, transform.position + Vector3.up * 2, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    // 피해 입을 때 깜빡임 효과를 주는 코루틴
    IEnumerator DamageFlash()
    {
        // 색상 변경 (피해 효과)
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material.color = new Color(1.0f, 0.6f, 0.6f);
        }

        yield return new WaitForSeconds(0.1f);

        // 원래 색상으로 복구
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material.color = Color.white;
        }
    }
}
