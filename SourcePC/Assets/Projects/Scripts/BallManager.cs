using KirinUtil;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BallManager : MonoBehaviour
{

    private float removePosY = -65;
    private int soundNum;
    private int woodSoundNum;
    private GameObject soundCircleParentObj;
    private GameObject soundCirclePrefab;
    private Color ballColor;
    private float effectTime = 2f;
    private bool effectEnd = false;
    private bool soundPlayed = false;
    private bool woodSoundPlayed = false;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void SetValue(int _soundNum, int _woodSoundNum, Color _color, GameObject _circlePrefab, GameObject _uiParentObj) {
        soundNum = _soundNum;
        woodSoundNum = _woodSoundNum;
        ballColor = _color;
        soundCirclePrefab = _circlePrefab;
        soundCircleParentObj = _uiParentObj;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.transform.position.y < removePosY && effectEnd) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision) {

        string collisionObjName = collision.gameObject.name;

        if (collisionObjName.IndexOf("wood") >= 0) {
            if (!woodSoundPlayed) {
                KirinUtil.Util.sound.PlaySE(woodSoundNum);
                woodSoundPlayed = true;
            }
        }
        if (collisionObjName.IndexOf("Block") == -1) return;
        if (soundPlayed) return;

        soundPlayed = true;
        KirinUtil.Util.sound.PlaySE(soundNum);

        SoundEffect();

        //Debug.Log("Hit: " + collisionObjName + "  " + soundNum);
    }

    private void SoundEffect() {
        GameObject effectObj = Util.media.CreateUIObj(soundCirclePrefab, soundCircleParentObj, "soundCircle", Vector3.zero, Vector3.zero, new Vector3(0.01f, 0.01f, 1));
        effectObj.GetComponent<Image>().color = ballColor;

        Util.media.FadeOutUI(effectObj, effectTime, 0, iTween.EaseType.easeOutQuart);
        iTween.ScaleTo(effectObj,
            iTween.Hash(
                "x", 8,
                "y", 8,
                "time", effectTime,
                "islocal", true,
                "EaseType", iTween.EaseType.easeOutQuart
            )
        );

        /*Util.media.FadeOutUI(effectObj, effectTime, effectTime + 0.1f);
        iTween.ScaleTo(effectObj,
            iTween.Hash(
                "x", 0.01f,
                "y", 0.01f,
                "time", effectTime,
                "delay", effectTime + 0.1f,
                "islocal", true,
                "EaseType", iTween.EaseType.easeInOutQuart
            )
        );*/

        StartCoroutine(DestroyEffect(effectObj));
    }

    private IEnumerator DestroyEffect(GameObject uiObj) {
        yield return new WaitForSeconds(effectTime + 0.1f /*effectTime * 2 + 0.2f*/);  // effectTime * 2 + 0.2f
        Destroy(uiObj);
        effectEnd = true;
    }
}
