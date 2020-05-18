using KirinUtil;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.components;

public class MainManager : MonoBehaviour {

    [Separator("Debug")]
    public bool debug;
    public GameObject debugUI;
    public Text debugIpText;
    public GameObject debugBlobRectParentObj;
    public GameObject depthImage;

    [Separator("Manager")]
    public BlobManager blobManager;
    public FollowingManager followingManager;

    [Separator("Var")]
    public string rootDataDir = "/../../AppData/";
    public string settingDataDir = "Setting/";
    public string appXmlFileName = "app_setting.xml";
    private int screenWidth;
    private int screenHeight;
    private List<List<float>> idList;
    public Camera mainCamera;
    public Canvas mainCanvas;
    public float gravity;
    private int xylophoneSoundNum;
    public GameObject soundObj;

    [Separator("Ball")]
    public GameObject ballPrefab;
    public GameObject ballParentObj;
    public float ballStartPosY;
    public GameObject ballWallObj;
    public float marginX;
    [RangeAttribute(0, 360)] public int hLimitValue;
    public GameObject soundCircleParentObj;
    public GameObject soundCirclePrefab;
    private float ballScale;

    [Separator("UDP")]
    public string ip;
    public int receivePort;
    public string udpKey;
    private List<PhoneJson> phoneDataList;

    [System.Serializable]
    public class PhoneJson {
        public string key;
        public string ip;
        public string port;
        public string data;
        public string ball;
        public int direction;
    }

    #region init
    // Start is called before the first frame update
    void Start() {

        phoneDataList = new List<PhoneJson>();

        rootDataDir = Application.dataPath + rootDataDir;
        settingDataDir = rootDataDir + settingDataDir + "/";
        string appXmlContents = Util.file.OpenTextFile(settingDataDir + appXmlFileName);
        XmlParse(appXmlContents, "app");

        Util.BasicSetting(new Vector2(screenWidth, screenHeight), true, 60, false);
        LoadSound();
        UDPStart();
        BlobStart();
        BallStart();
        DebugStart();
    }


    private void LoadSound() {

        // se
        Util.sound.seSound = new SESound[xylophoneSoundNum + 1];
        for (int i = 0; i< xylophoneSoundNum; i++) {
            // sound登録
            Util.sound.seSound[i] = new SESound();
            Util.sound.seSound[i].id = "";
            Util.sound.seSound[i].fileName = "xylophone_" + i + ".wav";
            Util.sound.seSound[i].volume = 0.5f;
        }

        Util.sound.seSound[xylophoneSoundNum] = new SESound();
        Util.sound.seSound[xylophoneSoundNum].id = "";
        Util.sound.seSound[xylophoneSoundNum].fileName = "wood1.wav";
        Util.sound.seSound[xylophoneSoundNum].volume = 0.5f;


        Util.sound.SEInit();
        Util.sound.LoadSounds();
    }
    #endregion

    // Update is called once per frame
    void Update()
    {
        BlobUpdate();
        BallUpdate();
        DebugUpdate();
    }

    //----------------------------------
    //  blob
    //----------------------------------
    #region blob

    [System.Serializable]
    public class BlobData {
        public string id;
        public Vector3 pos;
    }
    private List<BlobData> blobDataList;
    public Text debugIdText;

    private void BlobStart() {
        blobDataList = new List<BlobData>();
    }

    private void BlobUpdate() {
        List<Vector3> blobPos = blobManager.GetBlobUIPos();
        List<Rect> blobRect = new List<Rect>();
        for (int i = 0; i < blobPos.Count; i++) {
            blobRect.Add(new Rect(blobPos[i].x, blobPos[i].y, blobPos[i].z, 0));
        }
        followingManager.Update2(blobRect);
        idList = followingManager.GetIdList();

        debugIdText.text = "";
        for (int i = 0; i < idList.Count; i++) {
            debugIdText.text += "ID: " + idList[i][0] + "  x:" + idList[i][1] + "  y:" + idList[i][2] + System.Environment.NewLine;
        }
    }
    #endregion

    //----------------------------------
    //  Ball
    //----------------------------------
    #region Ball
    private void BallStart() {
        soundCircleParentObj.transform.localPosition = Util.GetUIPos(mainCamera, mainCamera, mainCanvas, soundObj.transform.position);
    }


    private void BallUpdate() {

        Physics.gravity = new Vector3(0, gravity, 0);

        for (int i = 0; i < phoneDataList.Count; i++) {
            List<string> ballColorList = Util.GetSplitStringList(phoneDataList[i].ball, ",");
            if (ballColorList == null || ballColorList.Count == 0) continue;

            Vector3 ballPos = GetBlobPos(phoneDataList[i].direction);
            Util.PosX(ballWallObj, ballPos.x);

            for (int j = 0; j < ballColorList.Count; j++) {
                if (ballColorList[j] != "") {
                    AddBall(i, ballPos, ballColorList[i]);
                }
            }
        }
        phoneDataList.Clear();
    }

    private void AddBall(int num, Vector3 ballPos, string hexColor) {
        //float xPos = GetBlobPos(phoneDataList[i].id).x;
        GameObject ballObj = Util.media.CreateObj(
            ballPrefab, ballParentObj, "ball" + num,
            ballPos,
            Vector3.zero, Vector3.one * ballScale
        );

        Color32 ballColor = Util.media.HexToColor(hexColor);
        ballObj.GetComponent<Renderer>().material.color = ballColor;
        ballObj.GetComponent<BallManager>().SetValue(GetBallSoundNum(ballColor), xylophoneSoundNum, ballColor, soundCirclePrefab, soundCircleParentObj);
    }


    private Vector3 GetBlobPos(int direction) {
        Vector3 uiPos = Vector3.zero;

        float rateX = Screen.width / blobManager.displayAreaRect.width;
        print("id count: " + idList.Count + "  rateX: " + rateX);
        for (int i = 0; i < idList.Count; i++) {
            uiPos.x = (idList[i][1] - blobManager.displayAreaRect.x) * rateX;
                uiPos.y = idList[i][2];
                uiPos.z = idList[i][3];
        }
        print("uiPos: " + uiPos.x);

        // margin
        if (direction > 0) uiPos.x -= marginX;
        else uiPos.x += marginX;

        Vector3 pos = Util.GetWorldPos(mainCamera, mainCanvas, uiPos, 100);
        pos.y = ballStartPosY;

        return pos;
    }
    #endregion

    #region Color
    private int GetBallSoundNum(Color32 color) {
        int soundNum = 0;

        float thisH;
        float thisS;
        float thisV;
        Color.RGBToHSV(color, out thisH, out thisS, out thisV);

        if (thisH > hLimitValue) {
            soundNum = xylophoneSoundNum;
        } else {
            float oneSoundHRange = xylophoneSoundNum / (float)hLimitValue;
            for (int i = 0; i < xylophoneSoundNum; i++) {
                float nowH = oneSoundHRange * i;
                if (nowH > thisH) {
                    soundNum = i;
                    break;
                }
            }
        }

        return soundNum;
    }
    #endregion

    //----------------------------------
    //  UDP
    //----------------------------------
    #region UDP

    private void UDPStart() {
        ip = Util.net.GetLocalIPAddress();
    }

    #region receive
    public void UDPReceived(string data) {
        print("UDPReceived: " + data);
        ReadJson(data);
    }

    private void ReadJson(string data) {

        PhoneJson jsonData = JsonUtility.FromJson<PhoneJson>(data);
        print(data);

        if (jsonData.key == udpKey) {
            PhoneDataListUpdate(jsonData);
        }

    }

    private void PhoneDataListUpdate(PhoneJson phoneData) {

        bool exist = false;
        int listNum = 0;
        for (int i = 0; i < phoneDataList.Count; i++) {
            if (phoneDataList[i].ip == phoneData.ip) {
                exist = true;
                listNum = i;
                break;
            }
        }

        if (exist) {
            phoneDataList[listNum].ip = phoneData.ip;
            phoneDataList[listNum].ball = phoneData.ball;
            phoneDataList[listNum].direction = phoneData.direction;
        } else {
            phoneDataList.Add(phoneData);
        }
    }

    #endregion

    #endregion

    //----------------------------------
    //  xml
    //----------------------------------
    #region xml main
    private void XmlParse(string xmlString, string type) {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        // system
        XmlNodeList nodes = xmlDoc.GetElementsByTagName("setting");

        foreach (XmlNode node in nodes) {
            foreach (XmlNode nodeItems in node) {
                if (type == "app") AppXmlParse(nodeItems);
            }
        }
    }
    #endregion

    #region AppXmlParse
    private void AppXmlParse(XmlNode node) {

        // Common
        if (node.Name == "screenWidth") {
            screenWidth = int.Parse(node.InnerText);
        } else if (node.Name == "screenHeight") {
            screenHeight = int.Parse(node.InnerText);
        } else if (node.Name == "receivePort") {
            receivePort = int.Parse(node.InnerText);
        } else if (node.Name == "xylophoneSoundNum") {
            xylophoneSoundNum = int.Parse(node.InnerText);
        } else if (node.Name == "ballScale") {
            ballScale = float.Parse(node.InnerText);
        }

    }

    #endregion

    //----------------------------------
    //  debug
    //----------------------------------
    #region Debug
    private void DebugStart() {
        debugUI.SetActive(false);
        depthImage.SetActive(false);
        debugIpText.text = Util.net.GetLocalIPAddress();
    }

    private void DebugUpdate() {
        if (Input.GetKeyUp(KeyCode.D)) {
            debug = !debug;
            debugUI.SetActive(debug);
            depthImage.SetActive(debug);
        }

        if (debug) {
            blobManager.DrawRect(debugBlobRectParentObj);

        }


        if (Input.GetMouseButtonDown(0)) {
            DebugAddBall();
        }
    }

    private void DebugAddBall() {

        float h = Random.Range(0f, 1f);
        Color color = Color.HSVToRGB(h, 1, 1);

        Vector3 addPos = Util.GetWorldMousePos(mainCamera, 100);
        addPos.y = ballStartPosY;
        for (int i = 0; i < 10; i++) {
            AddBall(0, addPos, ColorUtility.ToHtmlStringRGBA(color));
        }
    }
    #endregion

}