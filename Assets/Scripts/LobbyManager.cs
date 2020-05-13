using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    //서버 설정값
    private readonly string m_gameVersion = "1";

    //유니티 컴포넌트
    [SerializeField] InputField _nickNameInputField;
    [SerializeField] Button _joinButton;
    [SerializeField] Text _gameLogText;

    private void Awake()
    {
        //마스터 서버에 접속 명령
        PhotonNetwork.GameVersion = m_gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        //UI 상태 설정 : 게임참여 버튼 비활성화, 로그 변경
        _joinButton.interactable = false;
        _gameLogText.text = "Connecting To Master Server...";

    }

    private void Start()
    {
        //UI 업데이트 : 유저 닉네임 로드
        _nickNameInputField.text = GameManager.USER_NICKNAME;
    }

    //마스터 서버에 접속이 성공했다.
    public override void OnConnectedToMaster()
    {
        //base.OnConnectedToMaster();

        _joinButton.interactable = true;
        _gameLogText.text = "Online : Connected to Master Server";
    }

    //마스터 서버에 접속이 실패했다.
    public override void OnDisconnected(DisconnectCause cause)
    {
        //base.OnDisconnected(cause);

        _joinButton.interactable = false;
        _gameLogText.text = $"Offline : Connection Failed. {cause.ToString()}";

        PhotonNetwork.ConnectUsingSettings();
    }

    //게임 참여에 실패했다. 비어있는 방을 생성합니다.
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        //base.OnJoinRandomFailed(returnCode, message);

        _gameLogText.text = "There is no empty room. Creating new room";

        //현재(20.05.14) 작업 중에선 방 제목을 보고 플레이어가 입장하는 것이 아니기 때문에 방제목을 NULL로 설정한다.
        PhotonNetwork.CreateRoom(roomName: null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        //동기화가 아닌 독자적인 씬을 불러오는 방식이다. 때문에 멀티에선 사용해선 안된다.
        //SceneManager.LoadScene("Main");

        _gameLogText.text = "Connected with Room.";
        PhotonNetwork.LoadLevel("Main");
    }

    //JOIN(게임참여) 버튼을 눌렀습니다.
    public void UI_Button_Join()
    {
        _joinButton.interactable = false;

        if(PhotonNetwork.IsConnected)
        {
            _gameLogText.text = "Connecting to Random Room...";

            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            _gameLogText.text = "Connection Failed. Try Reconnecting...";

            PhotonNetwork.ConnectUsingSettings();
        }
    }

    //InputField에 입력된 값에 따라 클라이언트 유저 명을 변경합니다.
    public void SetUpdate_NickName()
    {
        GameManager.USER_NICKNAME = _nickNameInputField.text;
        PlayerPrefs.SetString("NICKNAME", GameManager.USER_NICKNAME);
    }
}
