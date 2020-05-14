﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KirinUtil;

[CustomEditor(typeof(Util))]
public class KRNUtilEditor : Editor {

    private bool basicOpen = true;
    private bool optionOpen = true;
    private Util util;
    private GUIStyle titleStyle;
    private GameObject utilObj;

    //----------------------------------
    //  init
    //----------------------------------
    public override void OnInspectorGUI() {

        util = target as Util;
        utilObj = util.gameObject;

        // make style
        titleStyle = new GUIStyle();
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.7058824f, 0.7058824f, 0.7058824f);

        // title
        GUILayout.Label("Add Component", titleStyle);

        // Add Component
        BasicComponent();
        OptionComponent();

    }

    //----------------------------------
    //  Basic Component
    //----------------------------------
    private void BasicComponent() {
        bool isBasicOpen = EditorGUILayout.Foldout(basicOpen, "Basic");

        if (basicOpen != isBasicOpen) {
            basicOpen = isBasicOpen;
        }

        if (isBasicOpen) {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
            {

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("NetManager", GUILayout.Width(150))) {
                        if (utilObj.GetComponent<NetManager>() == null)
                            utilObj.AddComponent<NetManager>();
                    }
                    if (GUILayout.Button("Log", GUILayout.Width(150))) {
                        if (utilObj.GetComponent<Log>() == null)
                            utilObj.AddComponent<Log>();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("SoundManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("soundManager");
                        if (thisObj.GetComponent<SoundManager>() == null) {
                            Debug.Log("Added SoundManager");
                            thisObj.AddComponent<SoundManager>();
                        }
                    }

                    if (GUILayout.Button("ImageManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("imageManager");
                        if (thisObj.GetComponent<ImageManager>() == null) {
                            Debug.Log("Added ImageManager");
                            thisObj.AddComponent<ImageManager>();
                        }
                    }
                }
                GUILayout.EndHorizontal();

            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
    }

    //----------------------------------
    //  Option Component
    //----------------------------------
    private void OptionComponent() {
        bool isOptionOpen = EditorGUILayout.Foldout(optionOpen, "Option");

        if (optionOpen != isOptionOpen) {
            optionOpen = isOptionOpen;
        }

        if (isOptionOpen) {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
            {
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("CaptureManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("captureManager");
                        if (thisObj.GetComponent<CaptureManager>() == null) {
                            Debug.Log("Added CaptureManager");
                            thisObj.AddComponent<CaptureManager>();
                        }
                    }

                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("BalloonMessageManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("balloonMessageManager");
                        if (thisObj.GetComponent<BalloonMessageManager>() == null) {
                            Debug.Log("Added BalloonMessageManager");
                            thisObj.AddComponent<BalloonMessageManager>();
                        }
                    }
                    if (GUILayout.Button("DialogManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("dialogManager");
                        if (thisObj.GetComponent<DialogManager>() == null) {
                            Debug.Log("Added DialogManager");
                            thisObj.AddComponent<DialogManager>();
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("ProcessManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("processManager");
                        if (thisObj.GetComponent<ProcessManager>() == null) {
                            Debug.Log("Added ProcessManager");
                            thisObj.AddComponent<ProcessManager>();
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("CountDown", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("countDown");
                        if (thisObj.GetComponent<CountDown>() == null) {
                            Debug.Log("Added CountDown");
                            thisObj.AddComponent<CountDown>();
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("UDPSendManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("udpManager");
                        if (thisObj.GetComponent<UDPSendManager>() == null) {
                            Debug.Log("Added UDPSendManager");
                            thisObj.AddComponent<UDPSendManager>();
                        }
                    }
                    if (GUILayout.Button("UDPReceiveManager", GUILayout.Width(150))) {
                        GameObject thisObj = ExistComponent("udpManager");
                        if (thisObj.GetComponent<UDPReceiveManager>() == null) {
                            Debug.Log("Added UDPReceiveManager");
                            thisObj.AddComponent<UDPReceiveManager>();
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
    }

    //----------------------------------
    //  function
    //----------------------------------
    private GameObject ExistComponent(string componentName) {
        Transform trf = utilObj.transform.Find(componentName);

        if (trf != null) {
            return trf.gameObject;
        } else {
            GameObject obj = new GameObject();
            obj.name = componentName;
            obj.transform.SetParent(utilObj.transform, false);
            return obj;
        }
    }

}
