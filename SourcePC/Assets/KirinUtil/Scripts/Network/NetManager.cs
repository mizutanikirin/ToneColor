﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace KirinUtil {
    public class NetManager : MonoBehaviour {

        //----------------------------------
        //  LocalIPを取得
        //----------------------------------
        public string GetLocalIPAddress() {
            System.Net.IPHostEntry host;
            string localIP = "0.0.0.0";
            host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (System.Net.IPAddress ip in host.AddressList) {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        //----------------------------------
        //  OSC
        //----------------------------------
        #region OSC

        #region var

        public class OSCData {
            public string Key;
            public string Address;
            public string Data;
        }
        private Dictionary<string, ServerLog> servers = null;
        private bool isRun = false;
        private int oscReceiveLogCount = 0;
        public bool receiveOn;

        #endregion

        #region event
        [System.Serializable]
        public class OSCReceiveEvent : UnityEvent<List<OSCData>> { }
        public OSCReceiveEvent oscReceiveEvent;

        void OnEnable() {
            oscReceiveEvent.AddListener(ReceivedOSC);
        }

        void OnDisable() {
            oscReceiveEvent.RemoveListener(ReceivedOSC);
        }

        void ReceivedOSC(List<OSCData> data) {
            //print("ReceivedOSC");
        }
        #endregion

        #region start / stop
        public void OSCStart(string ipAddress, int inComingPort, int outGoingPort) {
            OSCHandler.Instance.Init(ipAddress, outGoingPort, inComingPort, "OSC1");
            servers = new Dictionary<string, ServerLog>();
            isRun = true;
        }

        public void OSCStart2(string _targetAddr, int _inComingPort, int _outGoingPort, int _inComingPort2, int _outGoingPort2) {
            OSCHandler.Instance.Init(_targetAddr, _outGoingPort, _inComingPort, "OSC1");
            OSCHandler.Instance.Init(_targetAddr, _outGoingPort2, _inComingPort2, "OSC2");
            servers = new Dictionary<string, ServerLog>();
            isRun = true;
        }

        public void OSCStop() {
            OSCHandler.Instance.OnApplicationQuit();
            isRun = false;
        }
        #endregion

        #region update(send)
        public void OSCSend(string address, string message, string key) {
            // データ送信部
            OSCHandler.Instance.SendMessageToClient(key, address, message);
        }
        #endregion

        #region update(receive)
        private void Update() {
            if (receiveOn && isRun) OSCReceiveUpdate();
        }

        private List<OSCData> OSCReceiveUpdate() {

            List<OSCData> oscDataList = new List<OSCData>();

            // must be called before you try to read value from osc server
            OSCHandler.Instance.UpdateLogs();

            // データ受信
            servers = OSCHandler.Instance.Servers;

            foreach (KeyValuePair<string, ServerLog> item in servers) {
                // If we have received at least one packet,
                // show the last received from the log in the Debug console
                //print("servers: " + item.Value.log.Count);
                //print(oscReceiveLogCount + "  " + item.Value.log.Count);
                //if (item.Value.log.Count > 0) {
                if (oscReceiveLogCount != item.Value.log.Count) {
                    int lastPacketIndex = item.Value.packets.Count - 1;
                    int arrayNum = item.Value.packets[lastPacketIndex].Data.Count;

                    if (arrayNum > 0) {
                        OSCData oscData = new OSCData();

                        // Server name
                        oscData.Key = item.Key;

                        // OSC address
                        oscData.Address = item.Value.packets[lastPacketIndex].Address;

                        // First data value
                        string dataStr = "";
                        int dataCount = item.Value.packets[lastPacketIndex].Data.Count;
                        for (int i = 0; i < dataCount; i++) {
                            if (i == dataCount - 1)
                                dataStr += item.Value.packets[lastPacketIndex].Data[i];
                            else
                                dataStr += item.Value.packets[lastPacketIndex].Data[i] + ",";
                        }
                        oscData.Data = dataStr;

                        oscDataList.Add(oscData);
                    }

                }

                oscReceiveLogCount = item.Value.log.Count;
            }

            if (oscDataList.Count > 0) {
                oscReceiveEvent.Invoke(oscDataList);
                print(oscDataList[0].Data);
            }

            return oscDataList;
        }
        #endregion

        #endregion


    }
}
