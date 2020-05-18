# ToneColor
Tone ColorはMIZUTANI KIRINが制作したインタラクション作品です。  
そのコードを公開します。開発はUnity 2019.3.0f6でしています。  

# インストール
1. 機器用意し、必要なAssetをインポートします。
2. 後述している「PCアプリのProject設定」「AndroidアプリのProject設定」をします。

[使用機器]
- Windows10 Pro
- Azure Kinect
- Android端末
- Wifiルーター

[必要なAsset]
- [Azure Kinect Examples for Unity](https://assetstore.unity.com/packages/tools/integration/azure-kinect-examples-for-unity-149700) (ver1.10)
- [iTween](https://assetstore.unity.com/packages/tools/animation/itween-84?locale=ja-JP)  
  
![howto](https://user-images.githubusercontent.com/4795806/82172941-b3959680-9906-11ea-8eda-5a84f02d6122.png)

# PCアプリのProject設定
PCアプリでは以下4つの設定が必要になります。Projectで使っているファイルは基本的にAssets/Projects/に入っています。まずはAssets/Projects/Scene/Main.unityを開いてください。これがメインのシーンになります。  

1. KinectManagerの設置/設定
2. BlobManager.csの編集
3. BlobManagerのInspector設定
4. app_setting.xmlの設定
5. 各Managerの説明/Inspector設定

## 1. KinectManager.csの編集
KinectManager.csに以下を追加してください。

### 295行目追加
```
        public void SetSensorMinDistance(int sensorIndex, float distance) {
            KinectInterop.SensorData sensorData = GetSensorData(sensorIndex);
            if (sensorData != null && sensorData.sensorInterface != null) {
                ((DepthSensorBase)sensorData.sensorInterface).minDistance = distance;
            }
        }

        public void SetSensorMaxDistance(int sensorIndex, float distance) {
            KinectInterop.SensorData sensorData = GetSensorData(sensorIndex);
            if (sensorData != null && sensorData.sensorInterface != null) {
                ((DepthSensorBase)sensorData.sensorInterface).maxDistance = distance;
            }
        }
```

## 2. BlobManager.csの編集
PCアプリ側のプロジェクトファイルではBlobManager.csの編集が必要になってきます。元々は「Azure Kinect Examples」に入っているBlobDetector.csを変更したものであるためGitHubにはBlobManager.csは空ファイルにしています。次の項のようにBlobManager.csのコードを変更・追加してください。

### 2.1. BlobDetector.csのコードをコピーする
まず`Assets\AzureKinectExamples\KinectDemos\BlobDetectionDemo\Scripts\`に入っているBlobDetector.csのコードをまるごとBlobManager.csにコピーしてください。

### 2.2. コピーした内容からの変更点
以下の項目を追加・変更してください。

#### 10行目変更
```
BlobDetector
↓
BlobManager
```

#### 63行目変更
```
private Rect foregroundImgRect;
↓
[System.NonSerialized] public Rect foregroundImgRect;
```

#### 74行目追加
```
        [System.NonSerialized] public Rect displayAreaRect;
        
        public float kinectMinDistance = 0.5f;
        public float kinectMaxDistance = 0.8f;

        [Range(0, 5000)]
        public int maxPixelsInBlob = 500;

        [Range(0, 1920)]
        public int minX = 0;

        [Range(0, 512)]
        public int maxX = 512;

        [Range(0, 1080)]
        public int minY = 0;

        [Range(0, 512)]
        public int maxY = 512;

        public GameObject rangeRect;
        [Range(-1080, 1080)]
        public int debugRectMarginY;
        public GameObject debugBlobRectObj;
        
        private void RangeRect() {
            float width = maxX - minX;
            float height = maxY - minY;
            float rateX = Screen.width / (float)depthImageWidth;
            float rateY = Screen.height / (float)depthImageHeight;

            Util.media.SetUISize(rangeRect, new Vector2(width * rateX, height * rateY));
            rangeRect.transform.localPosition = new Vector3(
                (Screen.width / 2 - width * rateX / 2 - minX * rateX), (Screen.height / 2 - height * rateY / 2 - minY * rateY), 0
            );

            displayAreaRect = new Rect(
                rangeRect.transform.localPosition.x,
                rangeRect.transform.localPosition.y, 
                width * rateX, 
                height * rateY
            );
        }
        
        public void DrawRect(GameObject parentObj) {
            float scaleX = foregroundImgRect.width / depthImageWidth;
            float scaleY = foregroundImgRect.height / depthImageHeight;
            int bi = 0;

            Util.media.DeleteAllGameObject(parentObj, false);
            foreach (var b in blobs) {
                Vector3 blobCenter = b.GetBlobCenter();
                float x = (depthScale.x >= 0f ? blobCenter.x : depthImageWidth - blobCenter.x) * scaleX;  // blobCenter.x * scaleX;
                float y = (depthScale.y >= 0f ? blobCenter.y : depthImageHeight - blobCenter.y) * scaleY;  // blobCenter.y* scaleY;
                Rect rectBlob = new Rect(x, y, (b.maxx - b.minx) * scaleX, (b.maxy - b.miny) * scaleY);
                GameObject rectObj = Util.media.CreateUIObj(
                    debugBlobRectObj, parentObj, "blob" + bi, 
                    new Vector3(rectBlob.x - Screen.width * 0.5f, rectBlob.y - Screen.height * 0.5f + debugRectMarginY, 0), 
                    Vector3.zero, Vector3.one
                );

                Util.media.SetUISize(rectObj, new Vector2(rectBlob.width, rectBlob.height));

                bi++;
            }

        }

        public List<Vector3> GetBlobUIPos() {
            float scaleX = foregroundImgRect.width / depthImageWidth;
            float scaleY = foregroundImgRect.height / depthImageHeight;
            List<Vector3> blobPos = new List<Vector3>();

            foreach (var b in blobs) {
                Vector3 blobCenter = b.GetBlobCenter();
                float x = (depthScale.x >= 0f ? blobCenter.x : depthImageWidth - blobCenter.x) * scaleX;  // blobCenter.x * scaleX;
                float y = (depthScale.y >= 0f ? blobCenter.y : depthImageHeight - blobCenter.y) * scaleY;  // blobCenter.y* scaleY;
                Rect rectBlob = new Rect(x, y, (b.maxx - b.minx) * scaleX, (b.maxy - b.miny) * scaleY);

                blobPos.Add(new Vector3(
                    rectBlob.x - Screen.width * 0.5f, 
                    rectBlob.y - Screen.height * 0.5f + debugRectMarginY,
                    b.minz
                ));
            }

            return blobPos;
        }
```

#### 171行目変更
```
//kinectManager.SetSensorMinDistance(sensorIndex, 0.25f);
↓
kinectManager.SetSensorMinDistance(sensorIndex, kinectMinDistance);
kinectManager.SetSensorMaxDistance(sensorIndex, kinectMaxDistance);
```

#### 171行目変更
```
depth = (depth >= minDistanceMm && depth <= maxDistanceMm) ? depth : (ushort)0;
↓
depth = ((depth >= minDistanceMm && depth <= maxDistanceMm) && (x > minX && x < maxX) && (y > minY && y < maxY)) ? depth : (ushort)0;
```

#### 277行目変更
```
var smallBlobs = blobs.Where(x => x.pixels < minPixelsInBlob).ToList();
↓
var smallBlobs = blobs.Where(x => (x.pixels < minPixelsInBlob || x.pixels > maxPixelsInBlob)).ToList();
```

#### 332行目変更
void OnRenderObject()関数をコメントアウト

## 3. BlobManagerのInspector設定
以下のように設定します。  
![blobManager](https://user-images.githubusercontent.com/4795806/82172643-b8a61600-9905-11ea-9419-6d52eb7bcd8d.png)

## 4. app_setting.xmlの設定
[AppData\Setting\app_setting.xml]でPCアプリのポート設定などができます。  

## 5. 各ManagerのInspector設定
重要なManagerだけ説明します。  

### MainManager
メインのScriptです。ここにメインのプログラムが書かれています。  
IP, SendPort, ReceivePortはapp_setting.xmlから読み込んだ数値が入ります。  
![mainManager](https://user-images.githubusercontent.com/4795806/82004568-32d46180-969e-11ea-95b3-61900d5461f4.png)

### BlobManager
BlobManagerはKinectでDepthを取得した後に範囲内の塊を検出するスクリプトです。  
  
Inspectorではスマホの位置取得の範囲を指定できます。X方向はminX～maxX、y方向はminY～maxYで指定します。  
アプリ起動中/EditorでPlay中にDボタンを押すとデバッグモードになり設定がしやすくなります。  
範囲内にある認識された物体は白く表示されます。  
![debugmode](https://user-images.githubusercontent.com/4795806/82003188-b0966e00-969a-11ea-834b-674697f04057.png)

### FollowingManager
BlobManagerで検出した塊にIDを付けるScriptです。  
![blob_follow](https://user-images.githubusercontent.com/4795806/82172731-fb67ee00-9905-11ea-95c0-2c2f31d43ebd.png)

### Kinect4AzureInterface
Azure Kinectを制御するスクリプトです。ここでKinectの奥行きの最小/最大値が変更できますが、BlobManager側のInspectorで設定お願いします。  
![azureKinect](https://user-images.githubusercontent.com/4795806/82004813-c60d9700-969e-11ea-979d-e4e143813855.png)  

### soundManager
音の制御をするKirinUtilのスクリプトです。音自体はMainManagerで読み込んでいます。  

# AndroidアプリのProject設定
プロジェクトではipアドレスなど数値を変更するためにUnity Remote Configを使用しています。Unity Servicesで自分のUnityアカウントと連携をしてください。  
連携してRemote Configウィンドウを表示すると以下のようになります。IPアドレスなど設定してください。  
![remoteconfig](https://user-images.githubusercontent.com/4795806/82172676-d1aec700-9905-11ea-91ad-0ca28c4ede01.png)

# アプリ実行について

1. 機器を設置します。  
2. PCアプリを起動します。  
3. 次にandroidアプリを起動させます。  
※ 途中でどちらかのアプリを再起動したい場合、念のため両方起動し直してください。  
※ PCアプリでDボタンを押すとデバッグモードになり物体が反応しているか見ることができます。  
※ windowsのファイアウォールを切るか、除外設定をしないとPCとandroidアプリの連携ができません。  