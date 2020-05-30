using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class AutoProjectionManager : MonoBehaviour
{
    public static AutoProjectionManager instance;

    [SerializeField] AutoProjector[] projectorsArray;
    private List<AutoProjector> projectors;
    public List<AutoProjector> Projectors { get { return projectors; } }
    [SerializeField] bool registRenderSetManually;
    //struct RenderCulling
    //{
    //    public RenderSet renderSet;
    //    public bool isCull;
    //}
    private List<RenderSet> renderList; //Dictionary<Renderer, RenderSet> renderSetDict;
    //private List<bool> isCullList;
    //public void AddOnRenderSetList(RenderSet set)
    //{

    //}

    public Material _uvMaskMaterial;
  //  [SerializeField] Material projectionMaterial;
    [SerializeField] Material globalDilateMat;

    [SerializeField, Tooltip("This option will cause performance issue when projection per frame")] bool convertRenderTextureToTexture2D;

    private void Awake()
    {
        if (instance)
        {
            Debug.LogWarning("AutoProjectionManager already exist somewhere!");
            return;
        }
        instance = this;
        projectors = new List<AutoProjector>();
        
        Init();
    }

    private void Init()
    {
        //renderSetDict = new Dictionary<Renderer, RenderSet>();
        //renderSetList = new List<RenderSet>();
        // isCullList = new List<bool>();
        renderList = new List<RenderSet>();

        
        if (!registRenderSetManually)
        {
            Renderer[] renders = GameObject.FindObjectsOfType<Renderer>();

            for (int i = 0; i < renders.Length; ++i)
            {
                Texture2D baseMap = renders[i].sharedMaterial.GetTexture("_MainTex") as Texture2D;
                RenderTexture prevRender = new RenderTexture(baseMap ? baseMap.width / 2 : 1024, baseMap ? baseMap.height / 2 : 1024, 0);
                RenderTexture.active = prevRender;
                Graphics.Blit(baseMap, prevRender);
                RenderSet set;
                if (baseMap != null)
                {
                    set = new RenderSet()
                    {
                        render = renders[i],
                        baseMap = baseMap,
                        captureTex = new RenderTexture(baseMap.width, baseMap.height, 0, RenderTextureFormat.ARGB32),
                        dilateMat = globalDilateMat,
                        dilateTex = new RenderTexture(baseMap.width, baseMap.height, 0, RenderTextureFormat.ARGB32),
                        sharedMat = renders[i].sharedMaterial,
                        origBaseMap = baseMap,
                        prevTex = baseMap,
                        prevRender = prevRender,
                        meshF = renders[i].GetComponent<MeshFilter>()
                    };
                }
                else
                {
                    set = new RenderSet()
                    {
                        render = renders[i],
                        //baseMap = null,
                        captureTex = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32),
                        dilateMat = globalDilateMat,
                        dilateTex = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32),
                        sharedMat = renders[i].sharedMaterial,
                        prevRender = prevRender,
                        meshF = renders[i].GetComponent<MeshFilter>()
            };
                }
               
                //RenderCulling renderCull = new RenderCulling() { renderSet = set, isCull = false };
                //renderSetDict.Add(renders[i], set);
                renderList.Add(/*renderCull*/set);
            }
        }
    }

    private void OnDestroy()
    {
        for(int i =0; i < renderList.Count; ++i)
        {
            renderList[i].sharedMat.SetTexture("_MainTex", renderList[i].origBaseMap);
        }
        instance = null;
    }

 //   public RenderTexture colorMap;
   // public RenderTexture depthMap;

    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Decal();
        }

        if (Application.isPlaying)
        {
            ProjectionOnList();
        }
        else
        {
            ProjectionOnArray();
        }

    }

    public void ProjectionOnList()
    {
        for (int i = 0; i < projectors.Count; ++i)
        {
            CustomProjector p = projectors[i].projector;

            if (p.enableShadow)
            {
                p.cam.SetTargetBuffers(p.depthMap.colorBuffer, p.depthMap.depthBuffer);
                p.cam.Render();
                p.cam.RemoveAllCommandBuffers();
            }

            projectors[i].cullingCount = 0;
        }
        for (int j = 0; j < renderList.Count; ++j)
        {
            {

                if (convertRenderTextureToTexture2D)
                {
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        CustomProjector p = projectors[i].projector;

                        if (!p.projectionMat)
                            continue;

                        RenderSet render = renderList[j];

                        Plane[] plane = GeometryUtility.CalculateFrustumPlanes(p.cam);
                        if(!GeometryUtility.TestPlanesAABB(plane, render.render.bounds))
                        {
                            continue;
                        }

                        //EFrustumIntersection result = p.frustum.TestPoint(render.render.bounds.center);//(render.render.bounds);
                        //Debug.Log(p.cam.name + "'s " + render.render.name + " Culling Result is " + result);

                        //if(result == EFrustumIntersection.Outside)
                        //{
                        //    continue;
                        //}
                        projectors[i].cullingCount++;

                        p.SendMatrix(p.projectionMat);
                        p.projectionMat.SetTexture("_DepthMap", p.depthMap);

                        p.projectionMat.SetTexture("_MainTex", p.decal);

                        p.projectionMat.SetTexture("_BaseMap", render.prevTex);

                        Transform t = renderList[j].render.transform;
                        Matrix4x4 mx = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
                        Graphics.DrawMesh(renderList[j].meshF.sharedMesh, mx, p.projectionMat, LayerMask.NameToLayer("Default"));
                    }
                }
                else
                {
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        CustomProjector p = projectors[i].projector;

                        if (!p.projectionMat)
                            continue;

                        RenderSet render = renderList[j];

                        Plane[] plane = GeometryUtility.CalculateFrustumPlanes(p.cam);
                        if (!GeometryUtility.TestPlanesAABB(plane, render.render.bounds))
                        {
                            continue;
                        }

                        //EFrustumIntersection result = p.frustum.TestPoint(render.render.bounds.center);//(render.render.bounds);
                        //Debug.Log(p.cam.name + "'s " + render.render.name + " Culling Result is " + result);

                        //if(result == EFrustumIntersection.Outside)
                        //{
                        //    continue;
                        //}
                        projectors[i].cullingCount++;

                        p.SendMatrix(p.projectionMat);
                        p.projectionMat.SetTexture("_DepthMap", p.depthMap);

                        // bool isDilated = false;

                        p.projectionMat.SetTexture("_MainTex", p.decal);

                        p.projectionMat.SetTexture("_BaseMap", render.prevRender);

                        Transform t = renderList[j].render.transform;
                        Matrix4x4 mx = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
                        Graphics.DrawMesh(renderList[j].meshF.sharedMesh, mx, p.projectionMat, LayerMask.NameToLayer("Default"));

                    }
                }


            }


        }
    }

    public void ProjectionOnArray()
    {
        for (int i = 0; i < projectorsArray.Length; ++i)
        {
            CustomProjector p = projectorsArray[i].projector;

            if (p.enableShadow)
            {
                p.cam.SetTargetBuffers(p.depthMap.colorBuffer, p.depthMap.depthBuffer);
                p.cam.Render();
                p.cam.RemoveAllCommandBuffers();
            }

        }
        for (int j = 0; j < renderList.Count; ++j)
        {
            {

                if (convertRenderTextureToTexture2D)
                {
                    for (int i = 0; i < projectorsArray.Length; ++i)
                    {
                        CustomProjector p = projectorsArray[i].projector;

                        if (!p.projectionMat||!p.cam.gameObject.activeInHierarchy)
                            continue;

                        RenderSet render = renderList[j];
                        p.SendMatrix(p.projectionMat);
                        p.projectionMat.SetTexture("_DepthMap", p.depthMap);

                        p.projectionMat.SetTexture("_MainTex", p.decal);

                        p.projectionMat.SetTexture("_BaseMap", render.prevTex);

                        Transform t = renderList[j].render.transform;
                        Matrix4x4 mx = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
                        Graphics.DrawMesh(renderList[j].meshF.sharedMesh, mx, p.projectionMat, LayerMask.NameToLayer("Default"));
                    }
                }
                else
                {
                    for (int i = 0; i < projectorsArray.Length; ++i)
                    {
                        CustomProjector p = projectorsArray[i].projector;

                        if (!p.projectionMat|| !p.cam.gameObject.activeInHierarchy)
                            continue;

                        RenderSet render = renderList[j];
                        p.SendMatrix(p.projectionMat);
                        p.projectionMat.SetTexture("_DepthMap", p.depthMap);

                        // bool isDilated = false;

                        p.projectionMat.SetTexture("_MainTex", p.decal);

                        p.projectionMat.SetTexture("_BaseMap", render.prevRender);

                        Transform t = renderList[j].render.transform;
                        Matrix4x4 mx = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
                        Graphics.DrawMesh(renderList[j].meshF.sharedMesh, mx, p.projectionMat, LayerMask.NameToLayer("Default"));

                    }
                }


            }


        }
    }

    public void Decal()
    {
        for (int i = 0; i < projectors.Count; ++i)
        {
            CustomProjector p = projectors[i].projector;

            if (p.enableShadow)
            {
                //Debug.Log(p.cam);
                p.cam.SetTargetBuffers(p.depthMap.colorBuffer, p.depthMap.depthBuffer);
                //p.cam.SetTargetBuffers(colorMap.colorBuffer, depthMap.depthBuffer);
                p.cam.Render();
                // for (int j = 0; j < renderSet.Length; ++j)
                //{
                // _uvMaskMaterial.SetTexture("_DepthMap", p.depthMap);
                //}
                p.cam.RemoveAllCommandBuffers();
            }

            // p.SendMatrix(_uvMaskMaterial);


            //int count = GeometryUtility.Physics.OverlapSphereNonAlloc(
            //    directionalRangeAttackArea[currentRangeWeaponNum].bounds.center,
            //    directionalRangeAttackArea[currentRangeWeaponNum].bounds.extents.magnitude, 
            //    neighbours, 
            //    enemyLayerMask);
            //Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(p.cam);



        }
        CommandBuffer command = new CommandBuffer();
        command.Clear();
        command.name = "UV Space Renderer";
        for (int j = 0; j < renderList.Count; ++j)
        {
            // if (GeometryUtility.TestPlanesAABB(frustumPlanes, renderList[i].render.bounds))
            {
                //Texture2D prevTex = renderList[j].baseMap;
                //RenderTexture prevRender = renderList[0].captureTex;
                //Texture prevTex = renderList[j].baseMap;
                //RenderTexture.active = prevRender;
                if (convertRenderTextureToTexture2D)
                {
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        CustomProjector p = projectors[i].projector;

                        RenderSet render = renderList[j];
                        Plane[] plane = GeometryUtility.CalculateFrustumPlanes(p.cam);
                        if (!GeometryUtility.TestPlanesAABB(plane, render.render.bounds))
                        {
                            continue;
                        }

                        p.SendMatrix(_uvMaskMaterial);
                        _uvMaskMaterial.SetTexture("_DepthMap", p.depthMap);

                        bool isDilated = false;

                        command.SetRenderTarget(render.captureTex);
                        _uvMaskMaterial.SetTexture("_MainTex", p.decal);
                        //if (render.baseMap)

                        _uvMaskMaterial.SetTexture("_BaseMap", render.prevTex);

                        // _uvMaskMaterial.SetTexture("_BaseMap", convertRenderTextureToTexture2D?prevTex:prevRender/* render.baseMap*/);

                        command.DrawRenderer(render.render, _uvMaskMaterial, 0);
                        if (render.dilateTex && render.dilateMat)
                        {
                            command.Blit(render.captureTex, render.dilateTex, render.dilateMat);
                            isDilated = true;
                        }
                        Graphics.ExecuteCommandBuffer(command);

                        Texture2D tex = RenderTextureToTexture2D(isDilated ? render.dilateTex : render.captureTex);
                        render.render.sharedMaterial.SetTexture("_MainTex", tex/* isDilated ? render.dilateTex : render.captureTex*/);
                        render.prevTex = tex;
                        renderList[j] = render;

                        command.Clear();
                    }
                }
                else
                {
                    //RenderTexture prevRender = new RenderTexture(prevTex?prevTex.width / 2:1024, prevTex?prevTex.height / 2:1024, 0);
                    //RenderTexture.active = prevRender;
                    //Graphics.Blit(prevTex, prevRender);
                    for (int i = 0; i < projectors.Count; ++i)
                    {
                        CustomProjector p = projectors[i].projector;

                        RenderSet render = renderList[j];
                        Plane[] plane = GeometryUtility.CalculateFrustumPlanes(p.cam);
                        if (!GeometryUtility.TestPlanesAABB(plane, render.render.bounds))
                        {
                            continue;
                        }

                        p.SendMatrix(_uvMaskMaterial);
                        _uvMaskMaterial.SetTexture("_DepthMap", p.depthMap);

                        bool isDilated = false;

                        command.SetRenderTarget(render.captureTex);
                        _uvMaskMaterial.SetTexture("_MainTex", p.decal);

                        _uvMaskMaterial.SetTexture("_BaseMap", render.prevRender);

                        command.DrawRenderer(render.render, _uvMaskMaterial, 0);
                        if (render.dilateTex && render.dilateMat)
                        {
                            command.Blit(render.captureTex, render.dilateTex, render.dilateMat);
                            isDilated = true;
                        }
                        Graphics.ExecuteCommandBuffer(command);

                        if (isDilated)
                        {
                            render.captureTex = render.dilateTex;
                        }
                        //RenderTexture rt = isDilated ? render.dilateTex : render.captureTex;
                        render.render.sharedMaterial.SetTexture("_MainTex", render.captureTex /*rt*/);
                        render.prevRender = render.captureTex;// rt;
                        renderList[j] = render;
                        command.Clear();
                    }
                }


            }


        }
    }

    public static Texture2D RenderTextureToTexture2D(RenderTexture myRenderTexture)
    {
        Texture2D _tex = new Texture2D(myRenderTexture.width, myRenderTexture.height);
        RenderTexture.active = myRenderTexture;
        _tex.ReadPixels(new Rect(0, 0, myRenderTexture.width, myRenderTexture.height), 0, 0);
        _tex.Apply();

        return _tex;
    }
}
