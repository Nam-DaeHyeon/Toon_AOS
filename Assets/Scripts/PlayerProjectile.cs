using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] SphereCollider _myCollider;
    Transform _playerTr;
    Transform _playerAnimatorTr;
    Skill _setSkill;

    Vector3 colPos = Vector3.zero;

    private void Awake()
    {
        _myCollider = GetComponent<SphereCollider>();
    }

    public void SetInit_Parameters(Skill skill, Transform playerTr, Transform animatorTr)
    {
        _setSkill = skill;
        if(_myCollider == null) _myCollider = GetComponent<SphereCollider>();
        _myCollider.radius = _setSkill.skillDistance;

        _playerTr = playerTr;
        _playerAnimatorTr = animatorTr;
    }

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
    
    private void OnTriggerEnter(Collider other)
    {
        Player colplayer = other.GetComponent<Player>();
        if (colplayer == null) return;
        if (colplayer.photonView.IsMine) return;

        if (Check_InSkillArea(colplayer.transform.position))
        {
            colplayer.TakeDamage(_setSkill.Get_EffectiveDamage());
            colplayer.TakeMDamage(_setSkill.Get_EffectiveMagicDamage());
        }
    }
}
