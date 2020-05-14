using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FollowingManager : MonoBehaviour {

    private List<Rect> blobPosList;
    private int updateCounter;
    public int chaeckFrame = 30;
    private int blobNumPre;
    private int idCounter;
    public float sameLimitDistance;
    private List<List<Rect>> blobSmoothList;
    public int smoothNum;


    // 0:id  1:x  2:y  3:width  4:height
    [System.NonSerialized] public List<List<float>> idList;
    private List<List<float>> idListPre;

    #region event

    [System.Serializable]
    public class AddEvent : UnityEvent<int> { }
    public AddEvent addEvent;


    [System.Serializable]
    public class RemoveEvent : UnityEvent<int> { }
    public RemoveEvent removeEvent;

    void OnEnable() {
        addEvent.AddListener(Added);
        removeEvent.AddListener(Removed);
    }

    void OnDisable() {
        addEvent.RemoveListener(Added);
        removeEvent.RemoveListener(Removed);
    }

    void Added(int id) {
        print("Added" + id);
    }

    void Removed(int id) {
        print("Added" + id);
    }

    #endregion

    // Start is called before the first frame update
    void Start() {
        Init();
    }

    public void Init() {
        blobPosList = new List<Rect>();
        updateCounter = 0;
        idCounter = 0;
        blobSmoothList = new List<List<Rect>>();
        idList = new List<List<float>>();
        blobNumPre = 0;
    }

    public void Update2(List<Rect> posList) {

        // set data
        blobPosList.Clear();
        for (int i = 0; i < posList.Count; i++) {
            blobPosList.Add(posList[i]);
        }

        // データの更新
        UpdateId();

        if (updateCounter % chaeckFrame == 0) {
            AddId();
            RemoveId();

            //print("b: " + blobPosList.Count);
            blobNumPre = blobPosList.Count;

            idListPre = new List<List<float>>();
            for (int i = 0; i < idList.Count; i++) {
                List<float> nowList = new List<float>();
                for (int j = 0; j < idList[i].Count; j++) {
                    nowList.Add(idList[i][j]);
                }
                idListPre.Add(nowList);
            }
        }

        updateCounter++;

    }


    //----------------------------------
    //  id管理(dlib)
    //----------------------------------
    #region id管理
    private void AddId() {
        //print("a: " + blobPosList.Count + " " + userNumPre);

        if (blobPosList.Count <= blobNumPre) return;

        int addIdNum = blobPosList.Count - blobNumPre;

        for (int k = 0; k < addIdNum; k++) {

            // id, x, y追加
            List<float> thisIdList = new List<float>();
            thisIdList.Add(idCounter);  // ID
            string debugStr = "Add Id: " + idCounter;
            bool added = false;

            for (int i = 0; i < blobPosList.Count; i++) {
                bool exist = false;
                for (int j = 0; j < idList.Count; j++) {
                    if (idList[j].Count > 1) {
                        float distance = Vector2.Distance(new Vector2(blobPosList[i].x, blobPosList[i].y), new Vector2(idList[j][1], idList[j][2]));
                        if (distance < sameLimitDistance) {
                            exist = true;
                            break;
                        }
                    }
                }

                if (!exist) {
                    thisIdList.Add(blobPosList[i].x);         // X
                    thisIdList.Add(blobPosList[i].y);         // Y
                    thisIdList.Add(blobPosList[i].width);     // width
                    thisIdList.Add(blobPosList[i].height);    // height

                    List<Rect> smooth = new List<Rect>();
                    smooth.Add(new Rect(blobPosList[i].x, blobPosList[i].y, blobPosList[i].width, blobPosList[i].height));
                    blobSmoothList.Add(smooth);

                    debugStr += "  (" + blobPosList[i].x + ", " + blobPosList[i].y + ")";
                    added = true;
                    break;
                }
            }

            if (added) {
                idList.Add(thisIdList);
                print(debugStr);
                addEvent.Invoke(idCounter);
                idCounter++;
            }
        }
    }

    private void RemoveId() {
        //if (idList != null ) print("-----------------------------r: " + idList.Count + " " + blobNumPre);

        if (idList.Count > blobNumPre) {
            for (int i = 0; i < idList.Count; i++) {
                bool samePos = false;
                for (int j = 0; j < idListPre.Count; j++) {
                    if (idList[i].Count > 1 && idListPre[j].Count > 1) {
                        if (idList[i][1] == idListPre[j][1] &&
                            idList[i][2] == idListPre[j][2] &&
                            idList[i][3] == idListPre[j][3] &&
                            idList[i][4] == idListPre[j][4] ) {
                            samePos = true;
                            break;
                        }
                    }
                }

                if (samePos) {
                    print("Remove Use: " + idList[i][0]);
                    removeEvent.Invoke((int)idList[i][0]);

                    idList[i] = null;
                    idList.RemoveAt(i);
                    idListPre[i] = null;
                    idListPre.RemoveAt(i);

                    blobSmoothList[i] = null;
                    blobSmoothList.RemoveAt(i);

                    break;
                }
            }
        }
    }

    private void UpdateId() {
        bool[] changed = new bool[idList.Count];
        for (int i = 0; i < changed.Length; i++) {
            changed[i] = false;
        }

        for (int i = 0; i < blobPosList.Count; i++) {

            float minDistance = Screen.width;
            int id = -1;
            for (int j = 0; j < idList.Count; j++) {
                if (changed[j]) continue;
                if (idList[j].Count <= 1) continue;

                float distance = Vector2.Distance(new Vector2(blobPosList[i].x, blobPosList[i].y), new Vector2(idList[j][1], idList[j][2]));
                if (distance < sameLimitDistance && distance < minDistance) {
                    id = j;
                    minDistance = distance;
                }

            }

            if (id >= 0) {
                Rect data = GetNowSmoothData(id);
                idList[id][1] = data.x;
                idList[id][2] = data.y;
                idList[id][3] = data.width;
                idList[id][4] = data.height;

                //print(idList[id][1] + ", " + idList[id][2]);

                blobSmoothList[id].Add(new Rect(blobPosList[i].x, blobPosList[i].y, blobPosList[i].width, blobPosList[i].height));
                if (blobSmoothList[id].Count > smoothNum) blobSmoothList[id].RemoveAt(0);

                changed[id] = true;
            }
        }
    }

    private Rect GetNowSmoothData(int id) {
        float x = 0;
        float y = 0;
        float width = 0;
        float height = 0;

        for (int i = 0; i < blobSmoothList[id].Count; i++) {
            x += blobSmoothList[id][i].x;
            y += blobSmoothList[id][i].y;
            width += blobSmoothList[id][i].width;
            height += blobSmoothList[id][i].height;
        }
        x = x / (float)blobSmoothList[id].Count;
        y = y / (float)blobSmoothList[id].Count;
        width = width / (float)blobSmoothList[id].Count;
        height = height / (float)blobSmoothList[id].Count;

        return new Rect(x, y, width, height);
    }

    public List<List<float>> GetIdList() {
        return idList;
    }
    #endregion
}
