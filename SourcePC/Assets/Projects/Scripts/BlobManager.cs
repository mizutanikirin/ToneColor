using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.kinect;
using KirinUtil;

namespace com.rfilkov.components {
    public class BlobManager : MonoBehaviour {
        [Tooltip("Depth sensor index - 0 is the 1st one, 1 - the 2nd one, etc.")]
        public int sensorIndex = 0;

        [Tooltip("Camera used to estimate the overlay positions of 3D-objects over the background. By default it is the main camera.")]
        public Camera foregroundCamera;

        [Tooltip("Blob prefab, used to represent the blob in the 3D space.")]
        public GameObject blobPrefab;

        [Tooltip("The blobs root object.")]
        public GameObject blobsRootObj;

        [Range(0, 500)]
        [Tooltip("Max X and Y distance to blob, in pixels, to consider a pixel part of it.")]
        public int xyDistanceToBlob = 10;

        [Range(0, 500)]
        [Tooltip("Max Z-distance to blob, in mm, to consider a pixel part of it.")]
        public int zDistanceToBlob = 50;

        [Range(0, 500)]
        [Tooltip("Minimum amount of pixels in a blob.")]
        public int minPixelsInBlob = 50;

        [Range(1, 10)]
        [Tooltip("Increment in X & Y directions, when analyzing the raw depth image.")]
        public int xyIncrement = 3;

        [Range(0, 5)]
        [Tooltip("Time between the checks for blobs, in seconds.")]
        public float timeBetweenChecks = 0.1f;

        [Tooltip("UI-Text to display info messages.")]
        public UnityEngine.UI.Text infoText;

        // reference to KM
        private KinectManager kinectManager = null;

        // depth image resolution
        private int depthImageWidth;
        private int depthImageHeight;

        // depth scale
        private Vector3 depthScale = Vector3.one;

        // min & max distance tracked by the sensor
        private float minDistance = 0f;
        private float maxDistance = 0f;

        // screen rectangle taken by the foreground image (in pixels)
        [System.NonSerialized] public Rect foregroundImgRect;

        // last depth frame time
        private ulong lastDepthFrameTime = 0;
        private float lastCheckTime = 0;

        // list of blobs
        private List<Blob> blobs = new List<Blob>();
        // list of cubes
        private List<GameObject> blobObjects = new List<GameObject>();

        //////////////////////////////////////////////////////////////////////////////////////////////////
        ///  mizutani
        //////////////////////////////////////////////////////////////////////////////////////////////////
        [Separator("Kirin added")]

        [System.NonSerialized] public Rect displayAreaRect;

        [Range(0, 5000)]
        public int maxPixelsInBlob = 500;

        public float kinectMinDistance = 0.5f;
        public float kinectMaxDistance = 0.8f;

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


        //////////////////////////////////////////////////////////////////////////////////////////////////
        ///
        //////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of detected blobs.
        /// </summary>
        /// <returns>Number of blobs.</returns>
        public int GetBlobsCount() {
            return blobs.Count;
        }


        /// <summary>
        /// Gets the blob with the given index.
        /// </summary>
        /// <param name="i">Blob index.</param>
        /// <returns>The blob.</returns>
        public Blob GetBlob(int i) {
            if (i >= 0 && i < blobs.Count) {
                return blobs[i];
            }

            return null;
        }


        /// <summary>
        /// Gets distance to the blob with the given index.
        /// </summary>
        /// <param name="i">Blob index.</param>
        /// <returns>Distance to the blob.</returns>
        public float GetBlobDistance(int i) {
            if (i >= 0 && i < blobs.Count) {
                Vector3 blobCenter = blobs[i].GetBlobCenter();
                return blobCenter.z / 1000f;

            }

            return 0f;
        }


        /// <summary>
        /// Gets position on the depth image of the given blob. 
        /// </summary>
        /// <param name="i">Blob index.</param>
        /// <returns>Depth image position of the blob.</returns>
        public Vector2 GetBlobImagePos(int i) {
            if (i >= 0 && i < blobs.Count) {
                Vector3 blobCenter = blobs[i].GetBlobCenter();
                return (Vector2)blobCenter;

            }

            return Vector2.zero;
        }


        /// <summary>
        /// Gets position in the 3d space of the given blob.
        /// </summary>
        /// <param name="i">Blob index.</param>
        /// <returns>Space position of the blob.</returns>
        public Vector3 GetBlobSpacePos(int i) {
            if (i >= 0 && i < blobs.Count) {
                Vector3 blobCenter = blobs[i].GetBlobCenter();
                Vector3 spacePos = kinectManager.MapDepthPointToSpaceCoords(sensorIndex, (Vector2)blobCenter, (ushort)blobCenter.z, true);

                return spacePos;

            }

            return Vector3.zero;
        }


        // internal methods

        void Start() {
            kinectManager = KinectManager.Instance;

            if (kinectManager) {
                depthImageWidth = kinectManager.GetDepthImageWidth(sensorIndex);
                depthImageHeight = kinectManager.GetDepthImageHeight(sensorIndex);

                depthScale = kinectManager.GetDepthImageScale(sensorIndex);

                kinectManager.SetSensorMinDistance(sensorIndex, kinectMinDistance);
                kinectManager.SetSensorMaxDistance(sensorIndex, kinectMaxDistance);
            }

            if (blobsRootObj == null) {
                blobsRootObj = new GameObject("BlobsRoot");
            }

            if (foregroundCamera == null) {
                // by default use the main camera
                foregroundCamera = Camera.main;
            }

            // calculate the foreground rectangle
            //foregroundImgRect = kinectManager.GetForegroundRectDepth(sensorIndex, foregroundCamera);
        }


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

        void Update() {
            if (kinectManager == null || !kinectManager.IsInitialized())
                return;

            RangeRect();

            // calculate the foreground rectangle
            foregroundImgRect = kinectManager.GetForegroundRectDepth(sensorIndex, foregroundCamera);

            if (lastDepthFrameTime != kinectManager.GetDepthFrameTime(sensorIndex)) {
                lastDepthFrameTime = kinectManager.GetDepthFrameTime(sensorIndex);

                if ((Time.time - lastCheckTime) >= timeBetweenChecks) {
                    lastCheckTime = Time.time;

                    // detect blobs of pixel in the raw depth image
                    DetectBlobsInRawDepth();
                }
            }

            if (blobPrefab) {
                // instantiates representative blob objects for each blog
                InstantiateBlobObjects();
            }
        }

        // detects blobs of pixel in the raw depth image
        private void DetectBlobsInRawDepth() {
            ushort[] rawDepth = kinectManager ? kinectManager.GetRawDepthMap(sensorIndex) : null;
            blobs.Clear();

            if (rawDepth == null)
                return;

            minDistance = kinectManager.GetSensorMinDistance(sensorIndex);
            maxDistance = kinectManager.GetSensorMaxDistance(sensorIndex);

            ushort minDistanceMm = (ushort)(minDistance * 1000f);
            ushort maxDistanceMm = (ushort)(maxDistance * 1000f);

            for (int y = 0, di = 0; y < depthImageHeight; y += xyIncrement) {
                di = y * depthImageWidth;

                for (int x = 0; x < depthImageWidth; x += xyIncrement, di += xyIncrement) {
                    ushort depth = rawDepth[di];
                    depth = ((depth >= minDistanceMm && depth <= maxDistanceMm) && (x > minX && x < maxX) && (y > minY && y < maxY)) ? depth : (ushort)0;


                    if (depth != 0) {
                        bool blobFound = false;
                        foreach (var b in blobs) {
                            if (b.IsNearOrInside(x, y, depth, xyDistanceToBlob, zDistanceToBlob)) {
                                b.AddDepthPixel(x, y, depth);
                                blobFound = true;
                                break;
                            }
                        }

                        if (!blobFound) {
                            Blob b = new Blob(x, y, depth);
                            blobs.Add(b);
                        }
                    }
                }
            }

            // remove inside blobs
            var insideblobs = new List<Blob>();
            foreach (var b in blobs)
                foreach (var b2 in blobs)
                    if (b.IsInside(b2) && !insideblobs.Contains(b) && b != b2)
                        insideblobs.Add(b);

            for (int i = 0; i < insideblobs.Count; i++)
                if (blobs.Contains(insideblobs[i]))
                    blobs.Remove(insideblobs[i]);

            // remove small blobs
            var smallBlobs = blobs.Where(x => (x.pixels < minPixelsInBlob || x.pixels > maxPixelsInBlob)).ToList();
            for (int i = 0; i < smallBlobs.Count; i++)
                if (blobs.Contains(smallBlobs[i]))
                    blobs.Remove(smallBlobs[i]);

            if (infoText) {
                string sMessage = blobs.Count + " blobs detected.\n";

                for (int i = 0; i < blobs.Count; i++) {
                    Blob b = blobs[i];
                    //sMessage += string.Format("x1: {0}, y1: {1}, x2: {2}, y2: {3}\n", b.minx, b.miny, b.maxx, b.maxy);
                    sMessage += string.Format("Blob {0} at {1}\n", i, GetBlobSpacePos(i));
                }

                //Debug.Log(sMessage);
                infoText.text = sMessage;
            }
        }


        // instantiates representative blob objects for each blob
        private void InstantiateBlobObjects() {
            int bi = 0;
            foreach (var b in blobs) {
                while (bi >= blobObjects.Count) {
                    var cub = Instantiate(blobPrefab, new Vector3(0, 0, -10), Quaternion.identity);
                    //cub.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);  // to match the dimensions of a ball

                    blobObjects.Add(cub);
                    cub.transform.parent = blobsRootObj.transform;
                }

                Vector3 blobCenter = b.GetBlobCenter();
                Vector3 blobSpacePos = kinectManager.GetPosDepthOverlay((int)blobCenter.x, (int)blobCenter.y, (ushort)blobCenter.z, sensorIndex, foregroundCamera, foregroundImgRect);

                blobObjects[bi].transform.position = blobSpacePos;
                blobObjects[bi].name = "Blob" + bi;

                bi++;
            }

            // remove the extra cubes
            for (int i = blobObjects.Count - 1; i >= bi; i--) {
                Destroy(blobObjects[i]);
                blobObjects.RemoveAt(i);
            }
        }


        /*void OnRenderObject() {
            int rectX = (int)foregroundImgRect.xMin;
            //int rectY = (int)foregroundImgRect.yMax;
            int rectY = (int)foregroundImgRect.yMin;

            float scaleX = foregroundImgRect.width / depthImageWidth;
            float scaleY = foregroundImgRect.height / depthImageHeight;

            // draw grid
            //DrawGrid();

            // display blob rectangles
            int bi = 0;

            foreach (var b in blobs) {
                float x = (depthScale.x >= 0f ? b.minx : depthImageWidth - b.maxx) * scaleX;  // b.minx * scaleX;
                float y = (depthScale.y >= 0f ? b.miny : depthImageHeight - b.maxy) * scaleY;  // b.maxy * scaleY;

                Rect rectBlob = new Rect(rectX + x, rectY + y, (b.maxx - b.minx) * scaleX, (b.maxy - b.miny) * scaleY);
                KinectInterop.DrawRect(rectBlob, 2, Color.white);

                Vector3 blobCenter = b.GetBlobCenter();
                x = (depthScale.x >= 0f ? blobCenter.x : depthImageWidth - blobCenter.x) * scaleX;  // blobCenter.x * scaleX;
                y = (depthScale.y >= 0f ? blobCenter.y : depthImageHeight - blobCenter.y) * scaleY;  // blobCenter.y* scaleY; // 

                Vector3 blobPos = new Vector3(rectX + x, rectY + y, 0);
                KinectInterop.DrawPoint(blobPos, 3, Color.green);

                bi++;
            }
        }*/


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


        // draws coordinate grid on screen
        private void DrawGrid() {
            int rectX = (int)foregroundImgRect.xMin;
            int rectY = (int)foregroundImgRect.yMin;

            float scaleX = foregroundImgRect.width / depthImageWidth;
            float scaleY = foregroundImgRect.height / depthImageHeight;

            // draw grid
            float c = 0.3f;
            for (int x = 0; x < depthImageWidth; x += 100) {
                int sX = (int)(x * scaleX);
                int sMaxY = (int)((depthImageHeight - 1) * scaleY);

                Color clrLine = new Color(c, 0, 0, 1);
                KinectInterop.DrawLine(rectX + sX, rectY, rectX + sX, rectY + sMaxY, 1, clrLine);
                c += 0.1f;
            }

            c = 0.3f;
            for (int y = 0; y < depthImageHeight; y += 100) {
                int sY = (int)((depthImageHeight - y) * scaleY);
                int sMaxX = (int)((depthImageWidth - 1) * scaleX);

                Color clrLine = new Color(0, c, 0, 1);
                KinectInterop.DrawLine(rectX, rectY + sY, rectX + sMaxX, rectY + sY, 1, clrLine);
                c += 0.1f;
            }
        }

    }
}
