using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class PlayerController : MonoBehaviour
{
    [Header("Patrol:"), SerializeField] bool patrol;
    int colID;
    Canvas canv;
    [Header("Room Camera"), SerializeField] Camera roomCam;
    Joystick joystick;
    [Header("Main Data:"), Range(0.0f, 20.0f)]
    public float maxMS = 5.0f;
    public float curMS { get; set; }
    public float minMS { get; set; }

    CamController camCont;
    [Header("Render Camera:"), SerializeField]
    Camera renderCam;

    [Header("Paint Amount Display:"), SerializeField]
    Image[] paintAmountDisplay;

    Animator anim;  //Use this later...
    public bool startPaint { get; set; }
    public bool gameEnded { get; set; }

    bool noColor, dizzy;
    Rigidbody rb;

    [Header("Paint Trails:")]
    public Transform[] hands; //0 = R, 1 = L
    [Space] 
    public Transform[] trails; //0 = R, 1 = L
    [Space]
    public Gradient[] sprayColors;
    public int gunID { get; set; }
    ParticleSystem[] pSys = new ParticleSystem[2];
    ParticleSystem.MainModule[] paintDrops = new ParticleSystem.MainModule[2];
    ParticleSystemRenderer[] paintRend = new ParticleSystemRenderer[2];
    GameObject[] paints = new GameObject[2];
    Vector3 trailPos = new Vector3(-40, 1, 0);
    [Header("Indicators:"), SerializeField]
    GameObject[] indicators;    //Right and Left :D
    [Header("Painting Data:"), Range(5.0f, 30.0f)]
    public float paintAmount = 10;    //1 for each second! xD
    float startPaintAmount;
    bool isPainting;    //False at start, when not on canvas and if paintAmount = 0 :|
    public bool offCanv { get; set; }
    public bool canTap { get; set; }    //When interacting with UI elements! >:\
    private void Start()
    {
        canv = FindObjectOfType<Canvas>();
        startPaintAmount = paintAmount;
        anim = GetComponentInChildren<Animator>();
        canTap = true;
        for (int i = 0; i < hands.Length; i++)
        {
            pSys[i] = hands[i].GetComponentInChildren<ParticleSystem>();
            paintDrops[i] = pSys[i].main;
            paintRend[i] = hands[i].GetComponentInChildren<ParticleSystemRenderer>();
            paints[i] = hands[i].GetChild(0).gameObject;
            paints[i].SetActive(false);
            indicators[i].SetActive(true);
        }
        //SetSprayColor();
        SetSprayGun(0);

        rb = GetComponent<Rigidbody>();
        joystick = FindObjectOfType<Joystick>();
        camCont = FindObjectOfType<CamController>();
        minMS = maxMS / 2;
        curMS = minMS;
        for (int i = 0; i < trails.Length; i++)
            if (Physics.Raycast(hands[i].position, paints[i].transform.forward, out RaycastHit hit))
                trails[i].position = trailPos + hit.point;
    }

    private void Update()
    {
        if (!gameEnded && !dizzy)
        {
            if (!isPainting)
                CheckPaint();

            if (joystick.Horizontal != 0 || joystick.Vertical != 0)
            {

                if (!startPaint)
                {
                    startPaint = true;
                    return;
                }
                anim.SetFloat("Speed", Mathf.Abs(joystick.Horizontal) + Mathf.Abs(joystick.Vertical));
            }
            if (startPaint && canTap)
            {
#if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0))
                {
#elif UNITY_ANDROID
                if(Input.touchCount > 0)
                if(Input.touches[0].phase == TouchPhase.Began)
                {
#endif
                    if (isPainting) PausePaint();
                }

#if UNITY_EDITOR
                if (Input.GetMouseButtonUp(0))
                {
#elif UNITY_ANDROID
                if(Input.touchCount > 0)
                if(Input.touches[0].phase == TouchPhase.Ended)
                {
#endif
                    anim.Play("Spin");
                    if (startPaint && !isPainting) StartPaint();
                }
            }
        }
    }

    #region Paint Funcs:
    public void CheckPaint(int handId = 0, Vector3 target = new Vector3())
    {
        if (!isPainting)
        {
            for (int i = 0; i < trails.Length; i++)
                if (Physics.Raycast(hands[i].position, hands[i].forward, out RaycastHit hit))
                    indicators[i].transform.position = new Vector3(hit.point.x, 0.5f, hit.point.z);
        }
        else
        {
            transform.Rotate(Vector3.up, 100 * Time.fixedDeltaTime);
            trails[handId].position = trailPos + target;
        }
    }

    void StartPaint()
    {
        patrolling = false;
        isPainting = paintAmount != 0;
        Timing.KillCoroutines("SP");
        Timing.RunCoroutine(_StartPaint().CancelWith(gameObject), "SP");
    }

    IEnumerator<float> _StartPaint()
    {
        CreateSpray(trails);
        yield return Timing.WaitForSeconds(.25f);
        for (int i = 0; i < trails.Length; i++)
        {
            //trails[i].transform.position = ;
            paints[i].SetActive(true);
            indicators[i].SetActive(false);
        }
        if (trails[0].GetComponent<TrailRenderer>() != null)
        {
            yield return Timing.WaitForSeconds(.5f);
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].GetComponent<TrailRenderer>().emitting = true;
            }
        }
        Timing.RunCoroutine(_CheckPaintAmount().CancelWith(gameObject));
    }
    #endregion

    IEnumerator<float> _CheckPaintAmount()
    {
        yield return Timing.WaitForSeconds(.5f);
        while (paintAmount > 0 && isPainting)
        {
            paintAmount -=.2f;
            for (int i = 0; i < paintAmountDisplay.Length; i++)
                paintAmountDisplay[i].fillAmount = paintAmount / startPaintAmount;
            if (paintAmount <= 0) 
            { 
                paintAmount = 0.0f;
                for (int i = 0; i < paintAmountDisplay.Length; i++)
                    paintAmountDisplay[i].fillAmount = 0.0f;
                break;
            }
            yield return Timing.WaitForSeconds(.2f);
        }
        if (paintAmount == 0)
        {
            canv.gameObject.SetActive(false);
            offCanv = true;
            gameEnded = true;
            anim.Play("End");
            StopPaint();
            yield return Timing.WaitForSeconds(1f);
            LeanTween.moveLocalY(floor, 1, .5f).setEaseOutBack();
            yield return Timing.WaitForSeconds(.75f);
            camCont.gameObject.SetActive(false);
            roomCam.gameObject.SetActive(true);
        }
    }

    void StopPaint()
    {
        renderCam.enabled = false;
        anim.applyRootMotion = true;
        for (int i = 0; i < trails.Length; i++)
        {
            //trails[i].emitting = false;
            paintDrops[i].loop = false;
            hands[i].GetComponentInChildren<ParticleSystem>().Stop();
        }
        gameEnded = true;
    }
    [SerializeField] GameObject floor;
    private void OnCollisionEnter(Collision floor)
    {
        if(startPaint)
            if (floor.transform.CompareTag("Floor"))
            {
                anim.applyRootMotion = true;
                anim.Play("Spin");
                //CheckPaint();
                camCont.moveAble = true;
                offCanv = false;
                StartPaint();
            }
    }


    private void OnCollisionExit(Collision floor)
    {
        if (floor.transform.CompareTag("Floor"))
        {
            anim.applyRootMotion = false;
            anim.Play("Walk");
            camCont.moveAble = false;
            offCanv = true;
            PausePaint();
        }
    }

    void PausePaint()
    {
        if (!gameEnded)
            Patrol();
        isPainting = false;
        anim.Play("Idle");
        for (int i = 0; i < paints.Length; i++)
        {
            trails[i].GetComponent<TrailRenderer>().emitting = false;
            paints[i].SetActive(false);
            indicators[i].SetActive(true);
        }
    }

    #region Create Spray:

    void CreateSpray(Transform[] newTrails)
    {
        for (int i = 0; i < newTrails.Length; i++)
        {
            var t = Instantiate(newTrails[i]);
            newTrails[i] = t;
            newTrails[i].GetComponent<TrailRenderer>().emitting = false;
        }
        trails = newTrails;
        SetSprayColor();
        CreateSprayGun();
    }

    //This should be editable from the UI! >:)
    public void SprayCol(int colID) => this.colID = colID;
    public void SetSprayColor()
    {

        int col1 = 0, col2 = 0;
        switch (colID)
        {
            case 0:
                col1 = 0;
                col2 = 1;
                break;

            case 1:
                col1 = 2;
                col2 = 3;
                break;

            case 2:
                col1 = 4;
                col2 = 5;
                break;
        }
        paintDrops[0].startColor = sprayColors[col1];
        paintAmountDisplay[0].color = sprayColors[col1].Evaluate(0);
        paintDrops[1].startColor = sprayColors[col2];
        paintAmountDisplay[1].color = sprayColors[col2].Evaluate(0);

        if (trails[0].GetComponent<TrailRenderer>() != null)
        {
            trails[0].GetComponent<TrailRenderer>().colorGradient = sprayColors[col1];
            trails[1].GetComponent<TrailRenderer>().colorGradient = sprayColors[col2];
            return;
        }

        if (trails[col1].GetComponent<ParticleSystem>() != null)
        {
            var ps1 = trails[0].GetComponent<ParticleSystem>().main;
            ps1.startColor = sprayColors[col1];
            var ps2 = trails[1].GetComponent<ParticleSystem>().main;
            ps2.startColor = sprayColors[col2];
            return;
        }
    }

    //Ink, Spray, Paint:
    [Header("Particle Trail Mats:"), SerializeField]
    Material[] particleTrailMats;

    [Header("Paint Trail Mats:"), SerializeField]
    Material[] paintTrailMats;

    public void SetSprayGun(int gunID) => this.gunID = gunID;
    void CreateSprayGun()
    {
        for (int i = 0; i < paintDrops.Length; i++)
        {
            //var a = pSys[i].emission;
            //var b = pSys[i].shape;
            switch (gunID)
            {
                case 0:
                    //Ink gun...
                    pSys[i].transform.localScale = Vector3.one * .1f;
                    paintRend[i].trailMaterial = particleTrailMats[0];
                    trails[i].GetComponent<TrailRenderer>().startWidth = .1f;
                    trails[i].GetComponent<TrailRenderer>().material = paintTrailMats[0];
                    break;

                case 1:
                    //Spray gun...
                    pSys[i].transform.localScale = Vector3.one * .25f;
                    paintRend[i].trailMaterial = particleTrailMats[1];
                    trails[i].GetComponent<TrailRenderer>().startWidth = .25f;
                    trails[i].GetComponent<TrailRenderer>().material = paintTrailMats[1];
                    break;

                case 2:
                    //Paint gun...
                    pSys[i].transform.localScale = Vector3.one * .25f;
                    paintRend[i].trailMaterial = particleTrailMats[2];
                    trails[i].GetComponent<TrailRenderer>().startWidth = .25f;
                    trails[i].GetComponent<TrailRenderer>().material = paintTrailMats[2];
                    break;
            }
        }
    }

    public void CanTap()
    {
        canTap = false;
        if(renderCam.enabled)
            PausePaint();
        Timing.KillCoroutines("CanTap");
        Timing.RunCoroutine(_CanTap().CancelWith(gameObject), "CanTap");
    }
    IEnumerator<float> _CanTap()
    {
        yield return Timing.WaitForSeconds(.25f);
        canTap = true;
    }
    #endregion create spray />

    #region Patrol:
    bool patrolling;
    [Header("Patrol Pos:"), SerializeField]
    Vector3[] patrolPos;
    void Patrol()
    {
        Timing.KillCoroutines("Patrol");
        Timing.RunCoroutine(_Patrol().CancelWith(gameObject), "Patrol");
    }
    IEnumerator<float> _Patrol()
    {
        patrolling = true;
        yield return Timing.WaitForSeconds(3.0f);
        int patPoint = Random.Range(0, patrolPos.Length); 
        var dist = Vector3.Distance(transform.localPosition, patrolPos[patPoint]);
        if (dist > 0.2f)
        {
            anim.Play("Walk");
            while (dist > 0.2f)
            {
                if (!patrolling) break;
                //Rotation:
                var lookDir = patrolPos[patPoint] - transform.localPosition;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    var lookRot = Quaternion.LookRotation(lookDir);
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, lookRot, 3.5f * Time.fixedDeltaTime);
                }
                //Movement:
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    patrolPos[patPoint],
                    2f * Time.fixedDeltaTime);
                dist = Vector3.Distance(transform.localPosition, patrolPos[patPoint]);

                yield return Timing.WaitForSeconds(.025f);
            }
            anim.Play("Idle");
            if (dist < 0.25f)
                transform.localPosition = patrolPos[patPoint];
        }
    }
    #endregion patrol />
}
