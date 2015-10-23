﻿///<summary>
///<copyright>(c) Vivien Baguio</copyright>
///http://www.vivienbaguio.com
///</summary>

using UnityEngine;
using System.Collections;

/// <summary>
/// The Unity Gameobject that has handles to shape
/// </summary>
[ExecuteInEditMode]
public class CubicBezier3DObject : MonoBehaviour
{
    #region variables (private)
    [SerializeField]
    private BezierHandle startHandle;
    [SerializeField]
    private BezierHandle endHandle;

    private Transform startTransform;
    private Transform endTransform;
    private Bezier bezier = new Bezier();
    #endregion

    #region Properties (public)
    public Bezier Bezier { get { return bezier; } }
    #endregion

    #region Unity event functions

    ///<summary>
    ///Use this for very first initialization
    ///</summary>
    void Awake()
    {
    }

    ///<summary>
    ///Use this for initialization
    ///</summary>
    void Start()
    {

    }

    void Update()
    {
        //initialise
        if (bezier.pts == null)
            bezier.pts = new Vector3[4];

        if (startHandle == null) {
            startHandle = transform.GetChild(0).GetComponent<BezierHandle>();
            startTransform = startHandle.transform;
        }
        if (endHandle == null) {
            endHandle = transform.GetChild(1).GetComponent<BezierHandle>();
            endTransform = endHandle.transform;
        }

        bezier.pts[0] = startHandle.transform.position;
        bezier.pts[1] = startHandle.transform.localScale;
        bezier.pts[2] = endHandle.transform.localScale;
        bezier.pts[3] = endHandle.transform.position;
    }

    ///<summary>
    ///Debugging information should be put here
    ///</summary>
    void OnDrawGizmos()
    {
        // bezier path
        foreach (OrientedPoint p in Bezier.GetBezierPath(30)) {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(p.position, Vector3.one * 0.1f);
        }
        for (int i = 0; i < Bezier.pts.Length; i++) {
            Gizmos.color = Color.red;
            if (i != Bezier.pts.Length - 1)
                Gizmos.DrawLine(Bezier.pts[i], Bezier.pts[(i + 1) % 4]);
        }
    }

    #endregion

    #region Methods

    #endregion
}