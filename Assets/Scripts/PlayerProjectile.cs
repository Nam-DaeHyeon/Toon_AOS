using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PROJECTILE_OPTION
{
    단순충돌목적,
    투사체,
    투사체충돌발생,
    투사체충돌없음,
}

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] SphereCollider _myCollider;
    Transform _playerTr;
    Transform _playerAnimatorTr;
    Skill _setSkill;

    Vector3 colPos = Vector3.zero;
    
    public PROJECTILE_OPTION myOption { get; set; }

    /// <summary>
    /// 프로젝타일과 충돌한 플레이어.. 상태이상 관련 처리를 위해 따로 저장한다.
    /// </summary>
    public List<Player> colliderPlayers { get; set; } = new List<Player>();

    private void Awake()
    {
        _myCollider = GetComponent<SphereCollider>();
    }

    private void OnEnable()
    {
        colliderPlayers.Clear();
    }

    /// <summary>
    /// 해당 프로젝타일에 스킬, 플레이어 좌표값 / 회전값을 정의합니다.
    /// </summary>
    /// <param name="skill">지정된 스킬</param>
    /// <param name="playerTr">플레이어 좌표값. 트랜스폼</param>
    /// <param name="animatorTr">플레이어 회전값. 애니메이터 오브젝트를 회전시켜 시선을 지정했다.</param>
    public void SetInit_Parameters(Skill skill, Transform playerTr, Transform animatorTr)
    {
        _setSkill = skill;
        if(_myCollider == null) _myCollider = GetComponent<SphereCollider>();
        _myCollider.radius = _setSkill.skillDistance;

        _playerTr = playerTr;
        _playerAnimatorTr = animatorTr;
    }

    /// <summary>
    /// 콜라이더 반지름을 재설정합니다.
    /// </summary>
    public void Modify_ColliderRadius(float newRadius)
    {
        _myCollider.radius = newRadius;
    }

    /// <summary>
    /// 트리거 충돌 목적으로만 프로젝타일을 활용합니다. 플레이어의 좌표값을 가집니다.
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    private bool Check_InSkillArea(Vector3 targetPos)
    {
        //일직선으로 날아가는 또는 플레이어 주변에서 원형으로 폭발하는 스킬은 각도 계산을 생략한다.
        if (_setSkill.skillAngle % 360 == 0) return true;

        colPos = new Vector3(targetPos.x, _playerTr.position.y, targetPos.z);

        //탱크로부터 타겟까지의 단위벡터
        Vector3 dirToTarget = (targetPos - _playerTr.position).normalized;

        // transform.forward와 dirToTarget은 모두 단위벡터이므로 내적값은 두 벡터가 이루는 각의 Cos값과 같다.
        // 내적값이 시야각/2의 Cos값보다 크면 시야에 들어온 것이다.
        if (Vector3.Dot(_playerAnimatorTr.forward, dirToTarget) > Mathf.Cos((_setSkill.skillAngle * 0.5F) * Mathf.Deg2Rad))
        {
            return true;
        }
        else return false;
    }
    
    /// <summary>
    /// 단순 트리거 충돌체로 변환합니다.
    /// </summary>
    public void SetPlay_SimpleThings()
    {
        myOption = PROJECTILE_OPTION.단순충돌목적;
    }

    /// <summary>
    /// 투사체를 발사합니다.
    /// </summary>
    public void SetPlay_Missile()
    {
        _myCollider.radius = _setSkill.skillDistance;

        transform.rotation = _playerAnimatorTr.rotation;
        myOption = PROJECTILE_OPTION.투사체;

        //투사체가 맵에 부딪히는 것을 판단하기 위해 리지드바디를 추가한다.
        if(GetComponent<Rigidbody>() == null)
        {
            Rigidbody tempRigid = gameObject.AddComponent<Rigidbody>();
            tempRigid.useGravity = false;
            tempRigid.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }

        StartCoroutine(IE_PlayMissile());
    }

    private IEnumerator IE_PlayMissile()
    {
        float intime = _setSkill.skillMissileExistTime;
        float speed = _setSkill.skillMissileSpeed;

        while (myOption.Equals(PROJECTILE_OPTION.투사체))
        {
            transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);
            intime -= Time.deltaTime;
            if (intime <= 0)
            {
                myOption = PROJECTILE_OPTION.투사체충돌없음;
                yield break;
            }

            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name + " (" + LayerMask.LayerToName(other.gameObject.layer) + ")");
        switch(myOption)
        {
            default:
                TriggerEnter_SimpleThings(other);
                break;
            case PROJECTILE_OPTION.투사체:
                TriggerEnter_Missile(other);
                break;
        }
    }

    /// <summary>
    /// 단순히 다른 플레이어가 충돌했고, 유효 각도에 있는지 확인한다.
    /// </summary>
    private void TriggerEnter_SimpleThings(Collider other)
    {
        Player colplayer = other.GetComponent<Player>();
        if (colplayer == null) return;
        if (colplayer.photonView.IsMine) return;

        if (Check_InSkillArea(colplayer.transform.position))
        {
            colliderPlayers.Add(colplayer);

            colplayer.TakeDamage(_setSkill.Get_EffectiveDamage());
            colplayer.TakeMDamage(_setSkill.Get_EffectiveMagicDamage());
        }
    }
    
    /// <summary>
    /// 투사체가 다른 플레이어 혹 맵에 충돌했는지 확인한다.
    /// </summary>
    /// <param name="other"></param>
    private void TriggerEnter_Missile(Collider other)
    {
        //플레이어와 충돌
        if(other.GetComponent<Player>() != null)
        {
            //자신에게 충돌했을 경우 무시
            Player colplayer = other.GetComponent<Player>();
            if (colplayer.photonView.IsMine) return;

            colliderPlayers.Add(colplayer);
            myOption = PROJECTILE_OPTION.투사체충돌발생;
        }
        //다른 프로젝타일과 충돌의 경우 무시
        if(other.GetComponent<PlayerProjectile>() != null)
        {
            return;
        }
        //나머지는 맵과 충돌한 경우..
        else
            myOption = PROJECTILE_OPTION.투사체충돌발생;
    }
}
