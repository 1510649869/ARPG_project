﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using TaiDouCommon;
using TaiDouCommon.Model;
using TaiDouCommon.Tools;
using System.Net;

public class PhotonEngine : MonoBehaviour,IPhotonPeerListener {

    private static PhotonEngine _instance;
    public static PhotonEngine Instance
    {
        get {return _instance;}
    }
    public  ConnectionProtocol protocol = ConnectionProtocol.Tcp;
    private string serverAddress = ":15321";
    private string applicationName = "TaiDouServer";
    private Dictionary<byte, ControllerBase> controllers = new Dictionary<byte, ControllerBase>();
    public  PhotonPeer peer;
    public  bool isConnected=false;
    public  delegate void OnConnectedToServerEvent();
    public  event OnConnectedToServerEvent OnconnectedToServer;
    public  float time = 3f;
    private float timer;
    public  static User user;
    public  Role role;
    public  List<Role> rolelist;
    void Awake()
    {
        //Dns.GetHostName();
        IPAddress ipAddr = Dns.GetHostEntry("1462z254b3.iok.la").AddressList[0];
        string ip = ipAddr.ToString();//"1462z254b3.iok.la";//ipAddr.ToString();
        Debug.Log(ip);
        _instance = this;
        peer = new PhotonPeer(this, protocol);
        peer.Connect(ip+serverAddress, applicationName);
        DontDestroyOnLoad(this.gameObject);
    }
    void Update()
    {

        if (!isConnected)
        {
            timer += Time.deltaTime;
            if(timer>time)
            {
                peer.Connect(serverAddress, applicationName);
                timer = 0;
            }
        }
        if (peer != null)
            peer.Service();//向服务器发起请求
    }

    public void RegisterController(OperationCode opCode,ControllerBase controll)
    {
        if (!controllers.ContainsKey((byte)opCode))
        {
            controllers.Add((byte)opCode, controll);
        }
    }

    public void UnRegisterController(OperationCode opCode)
    {
        controllers.Remove((byte)opCode);
    }

    public void SendRequest(OperationCode opCode,Dictionary<byte,object> parameters)
    {
        //Debug.Log("sendrequest to server,opCode" + opCode);
        peer.OpCustom((byte)opCode, parameters,true);//向服务器发送消息
    }
    //opCode---表示在哪一个模块----subCode---表示某个子模块
    public void SendRequest(OperationCode opCode, SubCode subCode, Dictionary<byte, object> parameters)
    {
        //Debug.Log("sendrequest to server,opCode: " + opCode+"SubCode: "+subCode);
        parameters.Add((byte)ParameterCode.SubCode,subCode);
        peer.OpCustom((byte)opCode, parameters, true);//向服务器发送消息
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        //Debug.Log(level + ":" + message);
    }

    public void OnEvent(EventData eventData)
    {
        ControllerBase controller;
        OperationCode opCode = ParameterTool.GetParameters<OperationCode>(eventData.Parameters, ParameterCode.OperationCode, false);
        controllers.TryGetValue((byte)opCode, out controller);
        if (controller != null)
            controller.OnEvent(eventData);
        else
            Debug.LogWarning("receieve unknown event+OperationCode:"+opCode);
    }
    public void OnOperationResponse(OperationResponse operationResponse)
    {
        ControllerBase controller;
        controllers.TryGetValue(operationResponse.OperationCode, out controller);
        if(controller!=null)
        {
            controller.OnOperationResponse(operationResponse);
        }
        else
        {
            Debug.Log("Receieve a unknow response :"+operationResponse.OperationCode);
        }
    }
    public void OnStatusChanged(StatusCode statusCode)
    {
        switch(statusCode)
        {
          case StatusCode.Connect:
                isConnected = true;
                OnconnectedToServer();
                break;
            case StatusCode.Disconnect:
                isConnected = false;
                MessageManager._instance.ShowMessage("与服务器失去连接...");
                break;
            case StatusCode.TimeoutDisconnect:
                MessageManager._instance.ShowMessage("连接超时...");
                break;
            case StatusCode.DisconnectByServer:
                MessageManager._instance.ShowMessage("连接超时...");
                break;
            case StatusCode.DisconnectByServerLogic:
                MessageManager._instance.ShowMessage("1...");
                break;
            case StatusCode.ExceptionOnConnect:
                MessageManager._instance.ShowMessage("2...");
                break;
            case StatusCode.SendError:
                MessageManager._instance.ShowMessage("未连接，请求失败");
                break;
            default:
                isConnected = false;
                break;
        }
    }
}
