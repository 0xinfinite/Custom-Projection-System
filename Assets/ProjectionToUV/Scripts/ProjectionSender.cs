using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public struct CustomProjector
{
    public Camera cam;
    public RenderTexture depthMap;
    public bool enableShadow;
    public Texture2D decal;
    public Material projectionMat;
    

    public void SendMatrix(Material targetMat)
    {
        Matrix4x4 matrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        targetMat.SetMatrix("_Projection", matrix);
        targetMat.SetVector("_ProjectorWorldPos", cam.transform.position);

    }
}

[System.Serializable]
public struct RenderSet
{
    public Renderer render;
    //public MeshFilter mesh;
    public RenderTexture captureTex;
    public RenderTexture dilateTex;
    public Material dilateMat;
    public Texture2D baseMap;
    //public void SetBaseMap(Texture2D tex)
    //{
    //    Debug.Log(tex.name);
    //    baseMap = tex;
    //}
    public Material sharedMat;
    public Texture2D origBaseMap;
    public Texture2D prevTex;
    public RenderTexture prevRender;

    public MeshFilter meshF;
}

[ExecuteInEditMode]
public class ProjectionSender : MonoBehaviour
{
    public Camera cam;
    public RenderTexture depthMap;
    //public Renderer render;
    public Material _uvMaskMaterial;

   
    public RenderSet[] renderSet;

    private void Awake()
    {
        cam.depthTextureMode = DepthTextureMode.Depth;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (/*!render*/renderSet.Length<=0 || !cam || !_uvMaskMaterial /*|| !captureTex*/)
            return;


        GetProjectionTextureAndApply();

    }

  

    //private void OnRenderObject()
    //{
    //    GetProjectionTextureAndApply();

    //}

    // Update is called once per frame
    void LateUpdate()
    {
        GetProjectionTextureAndApply();
    }

    static int projectionTextureId = Shader.PropertyToID("_ProjectionTexture");
    static int projectionPassName = Shader.PropertyToID("_ProjectionPass");
    //public RenderTexture captureTex;
    //public RenderTexture dilateTex;
    public Material dilateMat;
    public bool isDilateMatNeedProjectorPos;

    void SendMatrix()
    {
        Matrix4x4 matrix = cam.projectionMatrix * cam.worldToCameraMatrix;
        _uvMaskMaterial.SetMatrix("_Projection", matrix);
        _uvMaskMaterial.SetVector("_ProjectorWorldPos", cam.transform.position);

        //if (dilateMat&&isDilateMatNeedProjectorPos)
        //{
        //    dilateMat.SetVector("isDilateMatNeedProjectorPos", cam.transform.position);
        //}
    }

    //public MeshFilter mesh;

    void GetProjectionTextureAndApply()
    {
        if (/*!render*/renderSet.Length <= 0 || !cam || !_uvMaskMaterial /*|| !captureTex*/)
            return;


        if (depthMap)
        {
            cam.SetTargetBuffers(depthMap.colorBuffer, depthMap.depthBuffer);
            cam.Render();
            for (int i = 0; i < renderSet.Length; ++i)
            {
                _uvMaskMaterial.SetTexture("_DepthMap", depthMap);
            }
            cam.RemoveAllCommandBuffers();
        }

        SendMatrix();

        CommandBuffer command = new CommandBuffer();
        command.Clear();
        command.name = "UV Space Renderer";
        for (int i = 0; i < renderSet.Length; ++i)
        {
            if (!renderSet[i].render || /*!renderSet[i].mesh || */!renderSet[i].captureTex)
            {
                continue;
            }
            command.SetRenderTarget(renderSet[i].captureTex);
            if (renderSet[i].baseMap)
            {
                _uvMaskMaterial.SetTexture("_BaseMap", renderSet[i].baseMap);
            }
            else
            {
                _uvMaskMaterial.SetTexture("_BaseMap", null);
            }
            command.DrawRenderer(renderSet[i].render, _uvMaskMaterial, 0);
            if (renderSet[i].dilateTex && dilateMat)
            {
                command.Blit(renderSet[i].captureTex, renderSet[i].dilateTex, dilateMat);
            }
            Graphics.ExecuteCommandBuffer(command);
            command.Clear();
        }
        //Graphics.ExecuteCommandBuffer(command);

        
    }

    public Texture2D _tex;
    public RenderTexture _renderTex;
    public void ExtractTexture()
    {
        Debug.Log("Try to Extract Texture");


        Renderer render = renderSet[0].render;

                if (render)
                {
                    _renderTex = render.sharedMaterial.GetTexture("_MainTex") as RenderTexture;
                    RenderTextureToTexture2D(_renderTex);
                    //Texture2D tex = render.material.GetTexture("_MainTex") as Texture2D;
                    //_tex = tex;
                    byte[] bytes = _tex.EncodeToPNG();
                    string fileName = Application.dataPath + "/ProjectionToUV/Example/ExampleTexture.png";
                    System.IO.File.WriteAllBytes(fileName, bytes);
              
                }

    }

    private void RenderTextureToTexture2D(RenderTexture myRenderTexture)
    {
        _tex = new Texture2D(_renderTex.width, _renderTex.height);
        RenderTexture.active = myRenderTexture;
        _tex.ReadPixels(new Rect(0, 0, myRenderTexture.width, myRenderTexture.height), 0, 0);
        _tex.Apply();
    }

   
}
