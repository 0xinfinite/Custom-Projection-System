using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class AutoProjector : MonoBehaviour
{
    //private Camera cam;

    public CustomProjector projector;

    //[SerializeField] AutoProjectionManager manager;

    [SerializeField] bool initDepthmap;

    public int cullingCount;

    // Start is called before the first frame update
    void Awake()
    {
        if(projector.cam==null)
        projector.cam = GetComponent<Camera>();

        if (projector.enableShadow && initDepthmap)
        {
            projector.depthMap = new RenderTexture(projector.cam.pixelWidth, projector.cam.pixelHeight, 24, RenderTextureFormat.Depth);
        }
        
    }

    private void OnEnable()
    {
        
            StartCoroutine("AddProjector");
        
    }

    IEnumerator AddProjector()
    {
        while (AutoProjectionManager.instance == null)
        {
            yield return null;
        }
        AutoProjectionManager.instance.Projectors.Add(this);

    }

    private void OnDisable()
    {
        if(AutoProjectionManager.instance)
        AutoProjectionManager.instance.Projectors.Remove(this);
    }

    // Update is called once per frame
    //void Update()
    //{

    //}



}
