﻿using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.HLODSystem.Cache
{
    class SimplifiedCache
    {
        private const string k_CachePath = "Assets/HLODSystem/Cache/";

        #region Interface
        public static void Clear()
        {
            Instance.ClearImpl();
        }

        public static void Update(Type simplifierType, Mesh originalMesh, Mesh simplifiedMesh, float qualtiy)
        {
            Instance.UpdateImpl(simplifierType, originalMesh, simplifiedMesh, qualtiy);
        }
        public static Mesh Get(Type simplifierType, Mesh mesh, float quality)
        {
            return Instance.GetImpl(simplifierType, mesh, quality);
        }
        #endregion

        #region Singleton
        private static SimplifiedCache s_Instance;

        private static SimplifiedCache Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new SimplifiedCache();
                return s_Instance;
            }
        }
        #endregion

        private void ClearImpl()
        {
            DeleteDirectory(k_CachePath);
        }
        private void UpdateImpl(Type simplifiedType, Mesh originalMesh, Mesh simplifiedMesh, float quality)
        {
            string path = AssetDatabase.GetAssetPath(originalMesh);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (string.IsNullOrEmpty(guid) == true)
                return;

            DateTime time = System.IO.File.GetLastWriteTime(path);
            SimplifiedMeshList meshList = GetMeshList(guid, time.ToFileTimeUtc());
            var mesh = meshList.GetMesh(quality, simplifiedType);
            if (mesh != null)
            {
                mesh.Mesh = simplifiedMesh;
            }
            else
            {
                meshList.AddMesh(quality, simplifiedType, simplifiedMesh);
            }

            AssetDatabase.AddObjectToAsset(simplifiedMesh, meshList);

            EditorUtility.SetDirty(meshList);
        }
        private Mesh GetImpl(Type simplifierType, Mesh mesh, float quality)
        {
            string path = AssetDatabase.GetAssetPath(mesh);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (string.IsNullOrEmpty(guid) == true)
                return null;

            DateTime time = System.IO.File.GetLastWriteTime(path);

            return GetMesh(simplifierType, mesh, quality, guid, time.ToFileTimeUtc());
        }

        private Mesh GetMesh(Type simplifiedType, Mesh source, float quality, string guid, long time)
        {
            SimplifiedMeshList meshList = GetMeshList(guid, time);

            var mesh = meshList.GetMesh(quality, simplifiedType);
            if (mesh != null)
            {
                return mesh.Mesh;
            }
            else
            {
                return null;
            }
        }

        private SimplifiedMeshList GetMeshList(string guid, long time)
        {
            var meshList = LoadMeshList(guid);
            if (meshList == null || meshList.Timestamp != time)
            {
                RemoveMeshList(guid);
                meshList = NewMeshList(guid, time);
            }

            return meshList;
        }

        private SimplifiedMeshList NewMeshList(string guid, long time)
        {
            SimplifiedMeshList meshList = SimplifiedMeshList.CreateInstance<SimplifiedMeshList>();
            meshList.Timestamp = time;
            meshList.name = guid;

            string path = GetAssetPath(guid);
            string dir = Path.GetDirectoryName(path);
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.CreateAsset(meshList, path);
            return meshList;
        }
        private SimplifiedMeshList LoadMeshList(string guid)
        {
            string path = GetAssetPath(guid);
            if (System.IO.File.Exists(path) == false)
                return null;

            return AssetDatabase.LoadAssetAtPath<SimplifiedMeshList>(path);
        }

        private void RemoveMeshList(string guid)
        {
            string path = GetAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }

       
        private string GetAssetPath(string guid)
        {
            return k_CachePath + guid + ".asset";
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path) == false)
                return;

            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                DeleteDirectory(dir.FullName);
            }

            di.Delete();
			
        }
    }
}
