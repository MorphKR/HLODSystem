using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem;
using Unity.HLODSystem.Simplifier;
using UnityEditor;
using UnityEngine;
using UltimateGameTools.MeshSimplifier;
using Unity.HLODSystem.Utils;
using System;
using Unity.Collections;

namespace UltimateGameTools.MeshSimplifier
{
    public class UGToolsMeshSimplify : ISimplifier
    {
        static int s_nLastProgress = -1;
        static string s_strLastTitle = "";
        static string s_strLastMessage = "";

        private dynamic m_options;

        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            //This simplifier should be first always.
            SimplifierTypes.RegisterType(typeof(UGToolsMeshSimplify), 1);
        }

        public UGToolsMeshSimplify(SerializableDynamicObject simplifierOptions)
        {
            m_options = simplifierOptions;
        }

        void Progress(string strTitle, string strMessage, float fT)
        {
            int nPercent = Mathf.RoundToInt(fT * 100.0f);

            if (nPercent != s_nLastProgress || s_strLastTitle != strTitle || s_strLastMessage != strMessage)
            {
                s_strLastTitle = strTitle;
                s_strLastMessage = strMessage;
                s_nLastProgress = nPercent;

                if (EditorUtility.DisplayCancelableProgressBar(strTitle, strMessage, fT))
                {
                    Simplifier.Cancelled = true;
                }
            }
        }

        public IEnumerator Simplify(HLODBuildInfo buildInfo)
        {
            for (int i = 0; i < buildInfo.WorkingObjects.Count; ++i)
            {
                WorkingMesh mesh = buildInfo.WorkingObjects[i].Mesh;

                yield return GetSimplifiedMesh(mesh, (m) =>
                {
                    buildInfo.WorkingObjects[i].SetMesh(m);
                });

            }
        }

        public void SimplifyImmidiate(HLODBuildInfo buildInfo)
        {
            //IEnumerator routine = Simplify(buildInfo);
            //CustomCoroutine coroutine = new CustomCoroutine(routine);
            //while (coroutine.MoveNext())
            //{
            //
            //}
        }

        public IEnumerator GetSimplifiedMesh(WorkingMesh origin, Action<WorkingMesh> resultCallback)
        {
            Simplifier simplifier = new Simplifier();

            simplifier.SetSimplifyMesh(CreateNewEmptyMesh(simplifier));

            simplifier.UseEdgeLength = m_options.m_bUseEdgeLength;
            simplifier.UseCurvature = m_options.m_bUseCurvature;
            simplifier.ProtectTexture = m_options.m_bProtectTexture;
            simplifier.LockBorder = m_options.m_bLockBorder;

            IEnumerator progressiveEnumerator = simplifier.ProgressiveMesh(origin.ToMesh(),  null, origin.name, Progress);

            while (progressiveEnumerator.MoveNext())
            {
                if (Simplifier.Cancelled)
                {
                    yield break;
                }
            }

            IEnumerator compuseEnumerator = simplifier.ComputeMeshWithVertexCount(Mathf.RoundToInt(m_options.m_fVertexAmount * simplifier.GetOriginalMeshUniqueVertexCount()), origin.name + " Simplified");

            while (compuseEnumerator.MoveNext())
            {
                if(Simplifier.Cancelled)
                {
                    yield break;
                }
            }

            Mesh outputMesh = simplifier.GetSimplifyMesh();

            WorkingMesh nwm = MeshExtensions.ToWorkingMesh(outputMesh, Allocator.Persistent);
            //nwm.name = origin.name;

            //nwm.vertices = outputMesh.vertices
            //nwm.normals = meshSimplifier.Normals;
            //nwm.tangents = meshSimplifier.Tangents;
            //nwm.uv = meshSimplifier.UV1;
            //nwm.uv2 = meshSimplifier.UV2;
            //nwm.uv3 = meshSimplifier.UV3;
            //nwm.uv4 = meshSimplifier.UV4;
            //nwm.colors = meshSimplifier.Colors;
            //nwm.subMeshCount = meshSimplifier.SubMeshCount;
            //for (var submesh = 0; submesh < nwm.subMeshCount; submesh++)
            //{
            //    nwm.SetTriangles(meshSimplifier.GetSubMeshTriangles(submesh), submesh);
            //}

            if (resultCallback != null)
            {
                resultCallback(nwm);
            }

            yield break;
        }

        private static Mesh CreateNewEmptyMesh(Simplifier simplifier)
        {
            if (simplifier.GetSimplifyMesh() == null)
            {
                return new Mesh();
            }

            Mesh meshOut = Mesh.Instantiate(simplifier.GetSimplifyMesh());
            meshOut.Clear();
            return meshOut;
        }

        public static void OnGUI(SerializableDynamicObject simplifierOptions)
        {
            EditorGUI.indentLevel += 1;

            dynamic options = simplifierOptions;

            if (options.m_bGenerateIncludeChildren == null)
                options.m_bGenerateIncludeChildren = true;

            if (options.m_bOverrideRootSettings == null)
                options.m_bOverrideRootSettings = false;

            if (options.m_bUseEdgeLength == null)
                options.m_bUseEdgeLength = true;

            if (options.m_bUseCurvature == null)
                options.m_bUseCurvature = true;

            if (options.m_bProtectTexture == null)
                options.m_bProtectTexture = true;

            if (options.m_bLockBorder == null)
                options.m_bLockBorder = true;

            if (options.m_fVertexAmount == null)
                options.m_fVertexAmount = 1.0f;

            options.m_bGenerateIncludeChildren = EditorGUILayout.Toggle("Recurse Into Children", options.m_bGenerateIncludeChildren);
            options.m_bOverrideRootSettings = EditorGUILayout.Toggle("Enable Prefab Usage", options.m_bOverrideRootSettings);
            options.m_bUseEdgeLength = EditorGUILayout.Toggle("Use Edge Length", options.m_bUseEdgeLength);
            options.m_bUseCurvature = EditorGUILayout.Toggle("Use Curvature", options.m_bUseCurvature);
            options.m_bProtectTexture = EditorGUILayout.Toggle("Protect Texture", options.m_bProtectTexture);
            options.m_bLockBorder = EditorGUILayout.Toggle("Keep Borders", options.m_bLockBorder);

            float fVertexAmount = EditorGUILayout.Slider(new GUIContent("Vertex %", "The percentage of vertices from the original mesh to keep when simplifying it"), options.m_fVertexAmount * 100.0f, 0.0f, 100.0f);
            options.m_fVertexAmount = Mathf.Clamp01(fVertexAmount / 100.0f);


            EditorGUI.indentLevel -= 1;
        }
    }
}