using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public partial class Player : MonoBehaviourPunCallbacks
{
    /// <summary> 게임에 온전히 로드된 경우 활성화 : 이외 공격 무효 </summary>
    bool m_ready = false;

    /// <summary> 게임에 온전히 로드된 경우 활성화 : 이외 공격 무효 </summary>
    Vector3 _targetPos;

    #region 플레이어 능력치 파라미터
    int _currHP = 0;
    int _maxHP = 100;

    float _speed = 5f;
    #endregion

    #region 플레이어 행동 트리거 및 변수
    bool isMove = false;
    float distance = 0f;
    Vector3 dir;
    #endregion

    #region 컴포넌트
    [SerializeField] Camera _mainCamera;
    [SerializeField] TextMeshPro _textNickName;
    [SerializeField] Image _imgHPBar;
    Animator _animator;
    #endregion

    private void Start()
    {
        Set_InitParameter();

        if (photonView.IsMine)
        {
            StartCoroutine(IE_PlayerController());
        }
        else
        {
            _mainCamera.gameObject.SetActive(false);
        }

        m_ready = true;

        //UI 설정
        photonView.RPC("CallbackRPC_SyncNickname", RpcTarget.All, photonView.Owner.NickName);
        //photonView.RPC("CallbackRPC_SyncHPBar", RpcTarget.All);
        _imgHPBar.fillAmount = _currHP / _maxHP;
    }
    
    private void Set_InitParameter()
    {
        //캐릭터에 따른 능력치 설정

        //체력 초기화
        _currHP = _maxHP;

        //트리거 & 변수 초기화
        isMove = false;
        distance = 0f;
        dir = Vector3.zero;
    }

    IEnumerator IE_PlayerController()
    {
        while(_currHP > 0)
        {
            //우클릭한 지면 좌표로 이동
            if(Input.GetMouseButtonDown(1))
            {
                //지면 확인
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                //Ray ray = _mainCamera.ViewportPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.tag.Equals("Ground"))
                    {
                        _targetPos = hit.point;
                        isMove = true;

                        //방향벡터 갱신
                        dir = _targetPos - transform.position;
                        dir.Normalize();
                        
                        //회전
                        _animator.transform.LookAt(new Vector3(_targetPos.x, transform.position.y, _targetPos.z));

                        //애니메이션
                    }
                }
            }

            //입력 좌표에 도달했는가
            if(isMove)
            {
                distance = Vector3.Distance(transform.position, _targetPos);
                if (distance < 0.1f) isMove = false;
                else
                {
                    transform.Translate(dir * _speed * Time.deltaTime, Space.World);
                    //transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime);
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// 애니메이터 컴포넌트를 설정합니다.
    /// </summary>
    public void SetAnimatorComponent()
    {
        if (_animator == null) _animator = transform.GetComponentInChildren<Animator>();
    }

    /// <summary> RPC 동기화 - 플레이어 닉네임 </summary>
    [PunRPC]
    private void CallbackRPC_SyncNickname(string value)
    {
        _textNickName.text = value;
    }

    /// <summary> RPC 동기화 - UI HP바 </summary>
    [PunRPC]
    private void CallbackRPC_SyncHPBar()
    {
        _imgHPBar.fillAmount = _currHP / _maxHP;
    }

    private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(this._imgHPBar.fillAmount);
        }
        else
        {
            this._imgHPBar.fillAmount = (float)stream.ReceiveNext();
        }
    }
}
