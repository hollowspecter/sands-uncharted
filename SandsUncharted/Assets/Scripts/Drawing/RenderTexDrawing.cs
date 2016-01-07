﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RenderTexDrawing : MonoBehaviour
{

    #region cursor variables
    [SerializeField]
    private Transform _cursor;

    Vector3 speed;
    float acceleration = 25f;
    Vector3 offsetCursor;

    #endregion

    #region capture variables

    [SerializeField]
    private Camera _renderCam;

    [SerializeField]
    private GameObject captureTarget;

    private int captureResolution = 1024;

    #endregion

    #region stamping variables
    StampManager _stampManager;
    [SerializeField]
    private Sprite[] _images;
    [SerializeField]
    private GameObject _stampPrefab;
    #endregion

    #region splineTool variables

    [SerializeField]
    private GameObject _splineRenderTarget;
    [SerializeField]
    private Sprite circleCursor;
    #endregion

    [SerializeField]
    private MeshLine _line;
    private ToolMenu _toolMenu;

    private ITool activeTool;
    private SplineTool splineTool;
    private StampTool stampTool;


    // Use this for initialization
    void Start()
    {
        splineTool = new SplineTool(this, _splineRenderTarget, _line, circleCursor);
        stampTool = new StampTool(this, _images, _stampPrefab);

        activeTool = splineTool;
        splineTool.Activate();
        _toolMenu = GetComponent<ToolMenu>();
        _stampManager = GetComponent<StampManager>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        //pressing Y opens the Toolmenu, this blocks all other input
        if (Input.GetButtonDown("Y"))
        {
            _toolMenu.Activate();

        }

        if (Input.GetButtonUp("Y"))
        {
            _toolMenu.Deactivate();
        }

        //If the Toolmenu is not open, process other inputs
        if(!Input.GetButton("Y"))
        { 
            //The Left-Stick movement is used for CursorMovement
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            UpdateCursorPosition(h, v);

            offsetCursor = new Vector3(_cursor.position.x, _cursor.position.y, _cursor.position.z + 0.1f);

            //All a,b,x button inputs are delegated to the active tool
            activeTool.Update(offsetCursor, _cursor.localRotation, _cursor.localScale.x);
            if(Input.GetButton("A"))
                activeTool.ButtonA();
            if (Input.GetButtonDown("A"))
                activeTool.ButtonADown();
            if (Input.GetButtonUp("A"))
                activeTool.ButtonAUp();
            if (Input.GetButton("B"))
                activeTool.ButtonB();
            if (Input.GetButtonDown("B"))
                activeTool.ButtonBDown();
            if (Input.GetButtonUp("B"))
                activeTool.ButtonBUp();

            if (Input.GetButton("X"))
                activeTool.ButtonX();
            if (Input.GetButtonDown("X"))
                activeTool.ButtonXDown();
            if (Input.GetButtonUp("X"))
                activeTool.ButtonXUp();




            //The Right-Stick movement is used for CursorRotation(xAxis) and -Scaling(yAxis) MOVETO:StampTool
            float rX = Input.GetAxis("RightStickX");
            float rY = Input.GetAxis("RightStickY");
            if (Mathf.Abs(rX) > 0.3f || Mathf.Abs(rY) > 0.3f)
                activeTool.RightStick(rX, rY);
            
        }

        speed *= 0.75f;//friction
    }
    
    void OnDrawGizmos()
    {

    }


    //Saves the current RenderTexture to the backgroundTexture by snapshotting a temporary RenderTexture with the CaptureCamera
    public void CaptureRenderTex()
    {
        //initialize camera and texture
        Camera captureCamera = transform.Find("CaptureCamera").GetComponent<Camera>();
        captureCamera.enabled = true;
        RenderTexture rt = new RenderTexture(captureResolution, captureResolution, 24);

        //activate rendertexture and link camera
        captureCamera.targetTexture = rt;
        captureCamera.Render();
        RenderTexture original = RenderTexture.active;
        RenderTexture.active = rt;      

        //snapshot
        Texture2D t = new Texture2D(captureResolution, captureResolution, TextureFormat.ARGB32, true);
        t.ReadPixels(new Rect(0, 0, captureResolution, captureResolution), 0, 0);
        t.Apply(true);
        captureTarget.GetComponent<MeshRenderer>().materials[0].mainTexture = t;
        
        //remove the temporary stuff
        captureCamera.targetTexture = null;
        captureCamera.enabled = false;
        RenderTexture.active = original;
        DestroyImmediate(rt);
        _stampManager.RemoveAll();
    }


    //move the cursor
    void UpdateCursorPosition(float h, float v)
    {
        Vector3 axisVector = h * transform.right + v * transform.up;
        axisVector = axisVector.sqrMagnitude >= 0.03 ? axisVector : new Vector3();

        /****move Reticle based on acceleration and left stick vector****/
        speed += acceleration * axisVector * Time.deltaTime; //make velocityvector
        _cursor.position += speed * Time.deltaTime; //make movementvector
    }

    //Rotate(xAxis) and Scale(yAxis) the cursor
    public void RotateAndScaleCursor(float rX, float rY, float rotationSpeed, float scaleSpeed)
    {

        if (Mathf.Abs(rX) > 0.5f && Mathf.Abs(rY) < 0.5f)
        {
            _cursor.Rotate(new Vector3(0f, 0f, Mathf.Sign(rX) * rotationSpeed * Time.deltaTime));
        }
        else if (Mathf.Abs(rX) < 0.5f && Mathf.Abs(rY) > 0.5f)
        {
            _cursor.localScale += -Mathf.Sign(rY) * new Vector3(scaleSpeed, scaleSpeed, scaleSpeed) * Time.deltaTime;
            float clamped = Mathf.Clamp(_cursor.localScale.x, 0.5f, 4f);
            _cursor.localScale = new Vector3(clamped, clamped, clamped);
        }
    }

    public void SetCursorImage(Sprite s)
    {
        _cursor.GetComponentInChildren<SpriteRenderer>().sprite = s;
    }

    public void ResetCursorRotScal()
    {
        _cursor.localRotation = Quaternion.identity;
        _cursor.localScale = new Vector3(1, 1, 1);
    }

    public Vector3 GetSpeed()
    {
        return speed;
    }
}
