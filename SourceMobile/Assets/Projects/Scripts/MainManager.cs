using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.RemoteConfig;
using KirinUtil;
using UnityEngine.UI;

public class MainManager : MonoBehaviour {

    [Header("Config")]
    public string mainPcIp;
    public int sendPort;
    public float baseGravity;
    public float displayAngle;
    public bool debug = false;
    public float ballScale;
    public int addBallNum;

    [Header("Vars")]
    public Vector3 phoneAngle;
    public float topPos;
    //public GameObject wallTopObj;
    private int phoneDirection;
    private string phoneIp;

    [Header("Camera")]
    public int cameraNum;
    public float captureAreaSize;
    public Image pickColorImageObj;
    private WebCamTexture webcamTexture;
    public RawImage cameraImage;
    public RawImage cameraFgImage;
    public int cameraWidth = 1080;
    public int cameraHeight = 1920;
    public AudioSource takePicSound;

    [Header("Ball")]
    public GameObject ballPrefab;
    public GameObject ballParentObj;
    public Vector3 ballInitPos;
    private Vector3 acceleration;
    private List<GameObject> ballObjList;
    private bool addedBall;
    public GameObject addBallWallObj;
    private List<string> removeBallColorList;
    public Camera mainCamera;
    public GameObject wallLeftObj;
    public GameObject wallRightObj;

    [Header("UI")]
    public GameObject cameraUI;
    public float alphaLimitAngle = 45;
    public GameObject debugUI;
    public Text debugText;


    public UDPSendManager udpSendManager;
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    

    //----------------------------------
    //  init
    //----------------------------------
    #region init
    void Awake() {
        RemoteConfigAwake();
    }

    // Start is called before the first frame update
    void Start() {
        ballObjList = new List<GameObject>();
        addedBall = false;
        ConfigManager.FetchConfigs<userAttributes, appAttributes>(new userAttributes(), new appAttributes());
    }

    // RemoteConfigで設定を読み取った後
    private void Start2() {
        UIStart();
        CameraStart();
        BallStart();
        GyroStart();
        UdpStart();
        DebugStart();
    }
    #endregion

    // Update is called once per frame
    void Update(){
        CameraUpdate();
        GyroUpdate();
        BallUpdate();
        RemoteConfigUpdate();
        UdpSendUpdate();
        DebugUpdate();
    }

    //----------------------------------
    //  Camera
    //----------------------------------
    #region Camera
    private void CameraStart() {
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(devices[cameraNum].name, cameraWidth, cameraHeight, 30);
        cameraImage.texture = webcamTexture;
        cameraFgImage.texture = webcamTexture;
        webcamTexture.Play();

        Util.media.SetUISize(cameraImage, new Vector2(webcamTexture.width, webcamTexture.height));

    }

    private void CameraUpdate() {
        ColorPickUpdate();
    }

    private void ColorPickUpdate() {

        if (webcamTexture == null) return;
        
        Color32[] colors = webcamTexture.GetPixels32();

        float centerX = webcamTexture.width * 0.5f;
        float centerY = webcamTexture.height * 0.5f;
        float colorNum = captureAreaSize * captureAreaSize;

        Vector3 targetAllColors = Vector3.zero;
        for (int x = Util.RoundToInt(centerX - captureAreaSize / 2); x < Util.RoundToInt(centerX + captureAreaSize / 2); x++) {
            for (int y = Util.RoundToInt(centerY - captureAreaSize / 2); y < Util.RoundToInt(centerY + captureAreaSize / 2); y++) {
                int index = x + y * webcamTexture.width;
                targetAllColors.x += colors[index].r;
                targetAllColors.y += colors[index].g;
                targetAllColors.z += colors[index].b;
            }
        }

        Color32 targetColorAvg = new Color32(
            (byte)Util.RoundToInt(targetAllColors.x / colorNum),
            (byte)Util.RoundToInt(targetAllColors.y / colorNum),
            (byte)Util.RoundToInt(targetAllColors.z / colorNum),
            255
        );

        pickColorImageObj.color = targetColorAvg;
    }

    public void TakePic() {
        print("TakePic");
        takePicSound.Play();
        AddBall(pickColorImageObj.color);
    }

    #endregion

    //----------------------------------
    //  Ball
    //----------------------------------
    #region Ball
    private void BallStart() {
        //AddBall();

        // 解像度に合わせて壁を移動させる
        Vector3 min = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Util.PosX(wallLeftObj, min.x - 0.5f);
        Util.PosX(wallRightObj, max.x + 0.5f);

    }

    private void BallUpdate() {
        // remove ball
        if(!addedBall) RemoveBallUpdate();

        // ボールをシャッフルする
        if (Shake()) ShuffleBall();
    }


    private void RemoveBallUpdate() {
        removeBallColorList = new List<string>();

        for (int i = 0; i < ballObjList.Count; i++) {
                if (topPos < ballObjList[i].transform.position.y) {
                    Color thisColor = ballObjList[i].GetComponent<Renderer>().material.color;
                    print(thisColor);
                    removeBallColorList.Add(ColorUtility.ToHtmlStringRGB(thisColor));
                    Destroy(ballObjList[i]);
                    ballObjList.RemoveAt(i);
                }
        }
    }

    private void AddBall(Color32 ballColor) {

        print("AddBall");

        bool added = false;
        if (addBallNum > 0) added = true;
        for (int i = 0; i < addBallNum; i++) {
            GameObject ballObj = Util.media.CreateObj(
                ballPrefab, ballParentObj, "ball" + i,
                new Vector3(ballInitPos.x + (i % 10) * 0.3f - 2, ballInitPos.y, ballInitPos.z),
                Vector3.zero, Vector3.one * ballScale
            );
            ballObj.GetComponent<Renderer>().material.color = ballColor;

            ballObjList.Add(ballObj);
        }

        if(added) StartCoroutine(AddedBallFalse());

        //StartCoroutine(WallTopWait());
    }

    private IEnumerator AddedBallFalse() {
        addedBall = true;
        yield return new WaitForSeconds(1.0f);
        addedBall = false;
    }

    private void ShuffleBall() {

        int[] ballShuffleListNum = new int[ballObjList.Count];
        List<Vector3> ballPos = new List<Vector3>();
        for (int i = 0; i < ballObjList.Count; i++) {
            ballShuffleListNum[i] = i;
            ballPos.Add(ballObjList[i].transform.position);
        }
        ballShuffleListNum = Util.ShuffleArray(ballShuffleListNum);

        for (int i = 0; i < ballObjList.Count; i++) {
            ballObjList[i].transform.position = ballPos[ballShuffleListNum[i]];
        }
    }

    /*private IEnumerator WallTopWait() {
        wallTopObj.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        wallTopObj.SetActive(false);
    }*/


    private bool Shake() {
        Vector3 preAcceleration = acceleration;
        acceleration = Input.acceleration;

        if (Vector3.Dot(acceleration, preAcceleration) < 0) return true;
        else return false;
    }
    #endregion

    //----------------------------------
    //  Gyro
    //----------------------------------
    #region Gyro
    private void GyroStart() {
        Input.gyro.enabled = true;
    }

    private void GyroUpdate() {

        Quaternion gattitude = Input.gyro.attitude;
        gattitude.x *= -1;
        gattitude.y *= -1;
        gattitude = Quaternion.Euler(90, 0, 0) * gattitude;
        phoneAngle = gattitude.eulerAngles;

        debugText.text =
            "px: " + phoneAngle.x + System.Environment.NewLine +
            "py: " + phoneAngle.y + System.Environment.NewLine +
            "pz: " + phoneAngle.z + System.Environment.NewLine;

        //phoneAngle = Input.gyro.attitude.eulerAngles;
        //print(phoneAngle);

        // 重力方向変更
        float gravityAngle = phoneAngle.z;
        Physics.gravity = new Vector3(
            -Mathf.Sin(gravityAngle * Mathf.Deg2Rad) * baseGravity,
            -Mathf.Cos(gravityAngle * Mathf.Deg2Rad) * baseGravity,
            0
        );
        debugText.text += System.Environment.NewLine + Physics.gravity;

        gravityAngle = Util.To360Angle(gravityAngle);
        if (gravityAngle > 180) phoneDirection = 0;
        else phoneDirection = 1;
        ChangeUIAlpha(gravityAngle);

    }
    #endregion

    //----------------------------------
    //  UDP
    //----------------------------------
    #region UDP

    private void UdpStart() {
        udpSendManager.Init(mainPcIp, sendPort);

        List<int> ipList = Util.GetSplitIntList(phoneIp, ".");
        int endIp = ipList[ipList.Count - 1];
    }

    #region send
    private string sendColorsPre = "";
    private void UdpSendUpdate() {
        if (!initRemoteApply) return;

        string sendColors = Util.GetSeparatedString(removeBallColorList, ",");
        if (sendColors == sendColorsPre) return;

        List<string> keyList = new List<string>() { "ball", "direction" };
        List<string> dataList = new List<string>() { sendColors, phoneDirection.ToString() };
        SendData("phoneData", keyList, dataList);

        sendColorsPre = sendColors;
    }

    private void SendData(string mainKeyName, List<string> keyList, List<string> dataList) {

        string sendData =
            "{" +
            "\"key\":\"" + mainKeyName + "\"," +
            "\"ip\":\"" + phoneIp + "\"";

        if (keyList == null || keyList.Count == 0) {
        } else {
            sendData += ",";
        }

        if (keyList != null) {
            print(mainKeyName + ": " + keyList.Count + "  " + dataList.Count);
            for (int i = 0; i < keyList.Count; i++) {
                sendData += "\"" + keyList[i] + "\":\"" + dataList[i] + "\"";
                if (i != keyList.Count - 1) sendData += ",";
            }
        }
        sendData += "}";

        print(sendData);

        udpSendManager.UDPSend(sendData);
    }
    #endregion

    #endregion
































    //----------------------------------
    //  ui / switch mode
    //----------------------------------
    #region ui
    private void UIStart() {
        cameraUI.SetActive(true);
        addBallWallObj.SetActive(true);
    }

    //[RangeAttribute(0, 360)]
    //public float gravityAngleDebug;
    private void ChangeUIAlpha(float gravityAngle) {
        //gravityAngle = gravityAngleDebug;

        // visible
        if (gravityAngle > alphaLimitAngle && gravityAngle < 360 - alphaLimitAngle) 
            cameraUI.SetActive(false);
        else
            cameraUI.SetActive(true);

        // alpha
        if (cameraUI.activeSelf) {
            if (gravityAngle < alphaLimitAngle) 
                cameraUI.GetComponent<CanvasGroup>().alpha = 1 - gravityAngle / alphaLimitAngle;
            else
                cameraUI.GetComponent<CanvasGroup>().alpha = 1 - (360 - gravityAngle) / alphaLimitAngle;
        }
    }
    #endregion

    //----------------------------------
    //  Remote Config
    //----------------------------------
    #region RemoteConfig
    public struct userAttributes {}
    public struct appAttributes {}

    private Timer remoteConfigTimer;
    private bool initRemoteApply;

    private void RemoteConfigAwake() {
        // remote config 
        ConfigManager.FetchCompleted += ApplyRemoteSettings;
        //ConfigManager.SetCustomUserID("Test1");
        ConfigManager.FetchConfigs<userAttributes, appAttributes>(new userAttributes(), new appAttributes());
        remoteConfigTimer = new Timer();
        remoteConfigTimer.LimitTime = 5;
        initRemoteApply = false;
    }

    private void RemoteConfigUpdate() {
        if (remoteConfigTimer.Update()) {
            ConfigManager.FetchConfigs<userAttributes, appAttributes>(new userAttributes(), new appAttributes());
        }
    }

    private void ApplyRemoteSettings(ConfigResponse configResponse) {

        // Conditionally update settings, depending on the response's origin:
        switch (configResponse.requestOrigin) {
            case ConfigOrigin.Default:
                Debug.Log("No settings loaded this session; using default values.");
                break;
            case ConfigOrigin.Cached:
                Debug.Log("No settings loaded this session; using cached values from a previous session.");
                break;
            case ConfigOrigin.Remote:
                Debug.Log("New settings loaded this session; update values accordingly.");
                SetVars();

                if (!initRemoteApply) Start2();
                initRemoteApply = true;
                break;
        }
    }

    private void SetVars() {
        mainPcIp = ConfigManager.appConfig.GetString("mainPcIp");
        sendPort = ConfigManager.appConfig.GetInt("sendPort");        
        phoneIp = ConfigManager.appConfig.GetString("phoneIp");
        baseGravity = ConfigManager.appConfig.GetFloat("baseGravity");
        displayAngle = ConfigManager.appConfig.GetFloat("displayAngle");
        debug = ConfigManager.appConfig.GetBool("debug");
        ballScale = ConfigManager.appConfig.GetFloat("ballScale");
        addBallNum = ConfigManager.appConfig.GetInt("addBallNum");


        debugUI.SetActive(debug);
    }
    #endregion

    //----------------------------------
    //  Debug
    //----------------------------------
    private void DebugStart() {
        debugUI.SetActive(debug);
    }

    private void DebugUpdate() {
        if (Input.GetKeyUp(KeyCode.Space)) {
            ShuffleBall();
        }
    }
}
