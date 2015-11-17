﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class DrawingScript : MonoBehaviour 
{

    [SerializeField]
    protected int texSize = 512;
    [SerializeField]
    protected Texture2D brush;
    [SerializeField]
    protected GameObject brushCircle;
    [SerializeField]
    protected string textureSlot = "_FrontDrawTex";
    [SerializeField]
    protected float reticleAcceleration = 2.8f;
    [SerializeField]
    protected float reticleScale = 12f;
    [SerializeField]
    protected float border = 0.12f;
    Texture2D texture;

    enum DrawTools
    {
        Line,
        Stamp,
        Erase,
        Spline
    }

    DrawTools currentTool;
    bool isToolbarActive;

    Color clearColor;
    Camera cam;

	bool first;
	Vector2 oldUV;
	Vector2 pixelUV;

    Vector3 axisVector;
	Vector3 speed = new Vector3();

    float turnAxis;
    float turnSpeed = 75f;

	float paintinterval = 1f; // how many pixels between two drawpositions

	Color[] brushPx;
	Color[] currentPx;
	Color[] scaledBrushPx;

	int scaledBrushW;
	int scaledBrushH;
	float scaleFactor = 0.5f;
	float setScale = 0.5f;
    float eraseScale = 1f;
    float brushScale = 0.5f;
	float scaleSpeed = 1f;
	float minScale = 0.25f;
	float maxScale = 1f;


	float minPX = 16f;
	float maxPX = 64f;

	MeshRenderer mesh;
	float meshWidth;
	float meshHeight;

	Transform maxCorner;
	Transform minCorner;


    Color[][] backups;
    int maxBackups = 5;
    int numBackups = 0;
    int currBackup  = 0;

    /* Input Variables */
    private float leftX = 0f;
    private float leftY = 0f;

    //public GameObject uiPanel;

    /*DrawingScript(int textureSize = 512, string textureName = "_FrontDrawTex")
    {
        texSize = textureSize;
        textureName = textureSlot;
    }*/

    void Awake() 
	{
        maxCorner = transform.Find("posCorner");
        minCorner = transform.Find("negCorner");
        if (maxCorner == null)
            Debug.LogError("no maxcorner");

        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        float ratioWH = 1f;
		texture = new Texture2D((int)(texSize * ratioWH), texSize, TextureFormat.ARGB32, false);
		clearColor  = new Color(0f,0f,0f,0f);
		FloodTexture (clearColor, texture);
		mesh = GetComponent<MeshRenderer> ();
		if (mesh == null) 
		{
			Debug.LogError("no mesh");
		}
		mesh.material.SetTexture(textureSlot, texture);

		meshWidth = mesh.bounds.size.x;
		meshHeight = mesh.bounds.size.z;

        currentTool = DrawTools.Line;

		brushPx = brush.GetPixels ();
		oldUV = pixelUV = new Vector2 ();
		first = true;
		brushCircle.transform.localScale = new Vector3(scaleFactor * reticleScale, scaleFactor * reticleScale, scaleFactor * reticleScale);
		BrushRescale (scaleFactor, brush);

        backups = new Color[maxBackups][];
        for(int i = 0; i < maxBackups; ++i)
        {
            backups[i] = new Color[(int)((texSize * ratioWH) * texSize)];
            FloodArray(clearColor, backups[i]);
        }

        NewBackup();
    }

	void Update() 
	{
        MakeMoveVector(leftX, leftY);

        TurnMap();

        /****move Reticle based on acceleration and right stick vector****/
        speed += reticleAcceleration * axisVector * Time.deltaTime; //make velocityvector
        brushCircle.transform.position += speed * Time.deltaTime; //make movementvector
        //brushCircle.transform.rotation = transform.parent.rotation; //negate the maps rotation to keep the pencil straight (not functional as the brush doesnt actually rotate)
        speed *= 0.75f;//friction
        

        if (mesh == null) 
		{
			Debug.LogError("no mesh in update");
		}
		//clamp brushposition to mapborder
        float minX = Mathf.Min(minCorner.localPosition.x, maxCorner.localPosition.x);
        float maxX = Mathf.Max(minCorner.localPosition.x, maxCorner.localPosition.x);
        float minZ = Mathf.Min(minCorner.localPosition.z, maxCorner.localPosition.z);
        float maxZ = Mathf.Max(minCorner.localPosition.z, maxCorner.localPosition.z);
		brushCircle.transform.localPosition = new Vector3 (Mathf.Clamp (brushCircle.transform.localPosition.x, minX + border, maxX - border), brushCircle.transform.localPosition.y, Mathf.Clamp(brushCircle.transform.localPosition.z, minZ + border, maxZ - border));

        //rescale based on speed slow = big / fast = small
        scaleFactor = Mathf.Clamp(scaleFactor + (((1f - axisVector.magnitude * axisVector.magnitude) * 2f) - 1f) /* * Random.Range(-0.2f, 1f)*/ * Time.deltaTime, setScale * 0.75f, setScale * 1.25f);
    }


    void FreeDraw()
    {  
        if (!first) 
		{
            //Debug.Log("notfirststamp");
			Vector2[] positions = FindStampPositions (oldUV, pixelUV);
			for (int i = 0; i < positions.Length; ++i) 
			{
				Vector2 p = positions [i];
				Stamp ((int)p.x - scaledBrushW / 2, (int)p.y - scaledBrushH / 2);
			}
		} 
		else 
		{
            //Debug.Log("first");
            oldUV = pixelUV;
            setScale = brushScale;
            BrushRescale(scaleFactor, brush);
			Stamp ((int)pixelUV.x - scaledBrushW / 2, (int)pixelUV.y - scaledBrushH / 2);
            first = false;
        }
        texture.Apply(true);
    }

	void Erase()
	{
        if (!first)
        {
			Vector2[] positions = FindStampPositions (oldUV, pixelUV);
			//Debug.Log (positions.Length.ToString ());
			for (int i = 0; i < positions.Length; ++i) 
			{
				Vector2 p = positions [i];
				if (p.x >= 0 && p.x + scaledBrushW <= texture.width && p.y >= 0 && p.y + scaledBrushH <= texture.height)
					EraseStamp((int)p.x - scaledBrushW / 2, (int)p.y - scaledBrushH / 2);
			}
		}
        else
        {
            //Debug.Log("first");
            oldUV = pixelUV;
            setScale = eraseScale;
            BrushRescale(scaleFactor, brush);
            EraseStamp((int)pixelUV.x - scaledBrushW / 2, (int)pixelUV.y - scaledBrushH / 2);
            first = false;
        }

		texture.Apply(true);
	}

    bool DrawRaycast()
    {
        RaycastHit reticleHit;
        if (Physics.Raycast(new Ray(brushCircle.transform.position, -brushCircle.transform.up), out reticleHit))
        {
            oldUV = pixelUV;
            pixelUV = reticleHit.textureCoord;
            pixelUV.x *= texture.width;
            pixelUV.y *= texture.height;
            return true;
        }

        return false;
    }
	
	
	Vector2[] FindStampPositions(Vector2 v1, Vector2 v2)
	{
		
		Vector2 dir = (v2 - v1);
		Vector2[] tmp = new Vector2[(int)(Mathf.Max(dir.magnitude / paintinterval,1f))];
		for (int i = 0; i < tmp.Length; ++i) 
		{
			//interpolate the points between the first and last position by moving the position paintinterval px to the next position
			tmp[i] = v1 + i * dir.normalized * paintinterval;
		}
		
		return tmp;
	}
	
	
	void Stamp(int x, int y)
	{
        //Debug.Log ("stamp" + x + "|" + y);
	    BrushRescale(scaleFactor, brush);

		UpdateScaledBrush(x, y);
		texture.SetPixels(x, y, scaledBrushW, scaledBrushH, currentPx);
	}

	void EraseStamp(int x, int y)
	{
		//Debug.Log ("stamp" + x + "|" + y);
	    BrushRescale(scaleFactor, brush);
		
		UpdateScaledEraser(x, y);
		texture.SetPixels(x, y, scaledBrushW, scaledBrushH, currentPx);
	}


	void UpdateScaledBrush(int x, int y)
	{
		for (int i = 0; i < scaledBrushW; i++) 
		{
			for (int j = 0; j < scaledBrushH; j++)
			{
				Color c = scaledBrushPx[i + scaledBrushW * j];
				currentPx[scaledBrushW * j + i] = texture.GetPixel(x + i, y + j) + c * c.a;//Color.Lerp(texture.GetPixel(x + i, y + j), c, c.a);
			}
		}
	}

	void UpdateScaledEraser(int x, int y)
	{
		for (int i = 0; i < scaledBrushW; i++) 
		{
			for (int j = 0; j < scaledBrushH; j++)
			{
				Color c = scaledBrushPx[i + scaledBrushW * j];
				if(c.a >= 0.3)
					currentPx[scaledBrushW * j + i] = clearColor;
				else
				{
					currentPx[scaledBrushW * j + i] = texture.GetPixel(x + i, y + j);
				}
				//Debug.Log (currentPx[scaledBrushW * j + i].ToString());
			}
		}
	}

    void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.green;
		Gizmos.DrawRay(brushCircle.transform.position, speed);
        
		Gizmos.color = Color.red;
		if(maxCorner != null)
			Gizmos.DrawLine (transform.position, maxCorner.position);
		Gizmos.color = Color.blue;
		if(minCorner != null)
			Gizmos.DrawLine (transform.position, minCorner.position);
        */
    }

    //make the moveVector for the brush movement
    void MakeMoveVector(float h, float v)
    {
        axisVector = h * transform.parent.right + v * transform.parent.forward;
        axisVector = axisVector.sqrMagnitude >= 0.03 ? axisVector : new Vector3();
    }

    //Rescale the brush based on an axis value (Right stick not needed)
    void FreeBrushSize(float scaleAxis)
    {
        if (Mathf.Abs(scaleAxis) > 0.2)
        {
            if (scaleAxis < 0)
            {
                scaleFactor = Mathf.Clamp(scaleFactor + (-1.0f * scaleSpeed * Time.deltaTime), minPX / brush.width, maxPX / brush.width);
                setScale = scaleFactor;
            }
            else
            {
                scaleFactor = Mathf.Clamp(scaleFactor + (1.0f * scaleSpeed * Time.deltaTime), minPX / brush.width, maxPX / brush.width);
                setScale = scaleFactor;
            }

            brushCircle.transform.localScale = new Vector3(scaleFactor * reticleScale, scaleFactor * reticleScale, scaleFactor * reticleScale);

        }
    }

    void BrushRescale(float factor, Texture2D b)
	{
		scaledBrushW = (int) (b.width * factor);
		scaledBrushH = (int) (b.height * factor);

		scaledBrushPx = new Color[scaledBrushW * scaledBrushH];
		currentPx = new Color[scaledBrushW * scaledBrushH];
		//Nearest Neighbor testing
		for (int x = 0; x < scaledBrushW; x++) 
		{
			for(int y = 0; y < scaledBrushW; y++)
			{
				//Debug.Log ("newX/Y: " + x + "/" + y + " oldX/Y: " + Mathf.RoundToInt(x/factor) + "/" + Mathf.RoundToInt(y/factor));
				scaledBrushPx[x + scaledBrushW * y] = b.GetPixel(Mathf.RoundToInt(x/factor), Mathf.RoundToInt(y/factor));
			}
		}
	}

	void FloodTexture(Color color, Texture2D tex)
	{
		Color[] c = new Color[tex.width * tex.height];
		for(int i = 0; i < c.Length; i++)
		{
			c[i] = color;
		}

		tex.SetPixels (c);
        tex.Apply();
	}

    void FloodArray(Color color, Color[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = color;
        }
    }

    protected void ClearTexture()
    {
       // Debug.Log("clear texture");
        FloodTexture(clearColor, texture);
        NewBackup();
    }

    void SaveTexture()
    {
        Debug.Log("save");
        Texture2D tex;
        tex = texture;


        // encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();

        // set path variables
        string tPath = "/Resources/Map/";

        // create the default path for textures
        System.IO.Directory.CreateDirectory(Application.dataPath + tPath);

        // save the encoded texture file
        File.WriteAllBytes(Application.dataPath + tPath + "map" + ".png", bytes);
    }

    void LoadTexture()
    {
        Debug.Log("load");
    }

    void CopyToTexture(Color[] source, Texture2D target)
    {
        if(source.Length == target.width * target.height)
        {
            target.SetPixels(source);
            target.Apply();
        }    
    }

    void CopyArray(Color[] source, Color[] target)
    {
        for(int i = 0; i < target.Length; ++i)
        {
            target[i] = source[i];
        }
    }

    //BACKUP METHODS for undoing
    void NewBackup()
    {
        if (currBackup != 0)
        {
            //Shift the current backup to slot 0
            ResetBackupPositions();
        }
        
        //Debug.Log("Backup");
        //Move the Backups over one slot
        ShiftBackups();
        //Copy the texture into the first slot
        CopyArray(texture.GetPixels(), backups[0]);
        //set the backup to -1 to indicate that there was a new backup saved
        currBackup = 0;

        numBackups = (int)Mathf.Min((numBackups + 1), maxBackups-1);
        //Debug.Log("num: " + numBackups);
    }

    void GetBackup(bool undo)
    {
        //if we are in slot 0 we cannot redo
        if(currBackup == 0 && !undo)
        {
            //Debug.Log("No newer versions");
        }
        //if we are in the last slot we cannot undo
        else if(currBackup == numBackups && undo)
        {
            //Debug.Log("No older version");
        }
        //else, we just switch over to the next or last backup by changing currbackup
        else
        {
            if(undo)
            {
               currBackup = (int)Mathf.Min(currBackup + 1, numBackups);
            }

            else
            {
                currBackup = (int)Mathf.Max(currBackup - 1, 0f);
            }
            //Debug.Log((undo ? "undo " : "redo ") + currBackup);
            CopyToTexture(backups[currBackup], texture);
        }
    }

    //Move all backups over one slot 0->1, 1->2 etc. last backup is thrown away
    void ShiftBackups()
    {
        for(int i = maxBackups-2; i >= 0; --i)
        {
            CopyArray(backups[i], backups[i + 1]);
            //Debug.Log(i + " -> " + (i +1));
        }
    }

    //Move your current backup to slot 1 because you changed in while of undoing
    void ResetBackupPositions()
    {
        int offset = (int) Mathf.Max(currBackup, 0);
        for(int i = currBackup; i <= numBackups; ++i)
        {
            CopyArray(backups[i], backups[i - offset]);
            FloodArray(clearColor, backups[i]);
            //Debug.Log(i + " -> " + (i - offset));
        }

        numBackups = numBackups - offset;
        //Debug.Log("num: " + numBackups);
    }

    //Turn the map left or right 
    void TurnMap()
    {
        float angle = -turnAxis * turnSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up * angle);
    }


    public void SetReticleAcc(float acc)
    {
        reticleAcceleration = acc;
    }


    void ChooseToolfromRadial(float x, float y)
    {
        //top slot chosen
        if(y > 0.5 && Mathf.Abs(x) < 0.3)
        {
            currentTool = DrawTools.Line;
        }
        //right slot chosen
        else if (x > 0.5 && Mathf.Abs(y) < 0.3)
        {
            currentTool = DrawTools.Erase;
        }
        //bottom slot chosen
        else if (x > 0.5 && Mathf.Abs(y) < 0.3)
        {
            //clear? spline?
        }
        //left slot chosen
        else if (x > 0.5 && Mathf.Abs(y) < 0.3)
        {
            //currentTool = DrawTools.Stamp;
        }
    }


    #region Input Managing Methods

    protected virtual void OnOverrideEnable()
    {
        MapState.MoveCursor += ReceiveLeftStickInput;
        MapState.TurnMap += ReceiveTriggerInput;
        MapState.Draw += OnDraw;
        MapState.Erase += OnErase;
        MapState.OnDrawExit += OnDrawExit;
        MapState.Clear += OnClear;
        MapState.Undo += Undo;
        MapState.Redo += Redo;
        MapState.LiftedPen += OnLiftedPen;
    }

    protected virtual void OnOverrideDisable()
    {
        MapState.MoveCursor -= ReceiveLeftStickInput;
        MapState.TurnMap -= ReceiveTriggerInput;
        MapState.Draw -= OnDraw;
        MapState.Erase -= OnErase;
        MapState.OnDrawExit -= OnDrawExit;
        MapState.Clear -= OnClear;
        MapState.Undo -= Undo;
        MapState.Redo -= Redo;
        MapState.LiftedPen -= OnLiftedPen;
    }

    void OnEnable()
    {
        OnOverrideEnable();
    }

    void OnDisable()
    {
        OnOverrideDisable();
    }

    protected void OnDraw()
    {
        switch (currentTool)
        {
            case DrawTools.Line:
                if (DrawRaycast())
                    FreeDraw();
                break;
            case DrawTools.Erase:
                if (DrawRaycast())
                    Erase();
                break;

        }
    }

    protected void OnErase()
    {
        if (currentTool == DrawTools.Line)
            currentTool = DrawTools.Erase;
        else
            currentTool = DrawTools.Line;
     //   if(DrawRaycast())
       //     Erase();
    }

    protected void OnClear()
    {
        //CLEAR BUTTON USED FOR RADIAL MENU
        //Debug.Log("clear");
        //ClearTexture();


    }

    protected void OnDrawExit()
    {
        SaveTexture();
    }

    protected void Undo()
    {
        //Debug.Log("undo");
        GetBackup(true);
    }

    protected void Redo()
    {
        //Debug.Log("redo");
        GetBackup(false);
    }

    protected void OnLiftedPen()
    {
        //Debug.Log("lift");
        first = true;
        NewBackup();
    }

    protected void ReceiveLeftStickInput(float x, float y)
    {
        if(!isToolbarActive)
        {
            leftX = x;
            leftY = y;
        }
        else
        {

        }
    }

    void ReceiveTriggerInput(float axis, float nothing)
    {
        //Debug.Log("turninput" + axis);
        turnAxis = axis;
    }
    #endregion
}