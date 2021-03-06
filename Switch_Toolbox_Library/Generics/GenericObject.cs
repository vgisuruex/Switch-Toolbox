﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using System.Windows.Forms;
using Switch_Toolbox.Library.Rendering;

namespace Switch_Toolbox.Library
{
    public class STGenericObject : TreeNodeCustom
    {
        public STGenericObject()
        {
            Checked = true;
        }
        public override void OnClick(TreeView treeView)
        {

        }

        public bool HasPos;
        public bool HasNrm;
        public bool HasUv0;
        public bool HasUv1;
        public bool HasUv2;
        public bool HasWeights;
        public bool HasIndices;
        public bool HasBitans;
        public bool HasTans;
        public bool HasVertColors;
        public int MaxSkinInfluenceCount;
        public string ObjectName;
        public int BoneIndex;
        public int MaterialIndex;
        public int VertexBufferIndex;
        public int DisplayLODIndex;
        public int Offset;

        public int GetMaxSkinInfluenceCount()
        {
            return vertices.Max(t => t.boneIds.Count);
        }

        public Vector3 GetOrigin()
        {
            Vector3 pos = Vector3.Zero;

            foreach (Vertex vert in vertices)
            {

            }
            return pos;
        }

        public List<string[]> bones = new List<string[]>();
        public List<float[]> weightsT = new List<float[]>();

        public List<string> boneList = new List<string>();
        public List<Vertex> vertices = new List<Vertex>();
        public List<LOD_Mesh> lodMeshes = new List<LOD_Mesh>();
        public class LOD_Mesh
        {
            public STPolygonType PrimitiveType = STPolygonType.Triangle;
            public STIndexFormat IndexFormat = STIndexFormat.UInt16;
            public uint FirstVertex;

            public List<SubMesh> subMeshes = new List<SubMesh>();
            public class SubMesh
            {
                public uint size;
                public uint offset;
            }

            public void GenerateSubMesh()
            {
                subMeshes.Clear();
                SubMesh subMesh = new SubMesh();
                subMesh.offset = 0;
                subMesh.size = (uint)faces.Count;
                subMeshes.Add(subMesh);
            }

            public int index = 0;
            public int strip = 0x40;
            public int displayFaceSize = 0;

            public List<int> faces = new List<int>();

            public override string ToString()
            {
                return "LOD Mesh " + index;
            }

            public List<int> getDisplayFace()
            {
                if ((strip >> 4) == 4)
                {
                    displayFaceSize = faces.Count;
                    return faces;
                }
                else
                {
                    List<int> f = new List<int>();

                    int startDirection = 1;
                    int p = 0;
                    int f1 = faces[p++];
                    int f2 = faces[p++];
                    int faceDirection = startDirection;
                    int f3;
                    do
                    {
                        f3 = faces[p++];
                        if (f3 == 0xFFFF)
                        {
                            f1 = faces[p++];
                            f2 = faces[p++];
                            faceDirection = startDirection;
                        }
                        else
                        {
                            faceDirection *= -1;
                            if ((f1 != f2) && (f2 != f3) && (f3 != f1))
                            {
                                if (faceDirection > 0)
                                {
                                    f.Add(f3);
                                    f.Add(f2);
                                    f.Add(f1);
                                }
                                else
                                {
                                    f.Add(f2);
                                    f.Add(f3);
                                    f.Add(f1);
                                }
                            }
                            f1 = f2;
                            f2 = f3;
                        }
                    } while (p < faces.Count);

                    displayFaceSize = f.Count;
                    return f;
                }
            }
        }
        public List<int> faces = new List<int>();

        #region Methods

        public void FlipUvsVertical()
        {
            foreach (Vertex v in vertices)
            {
                v.uv0 = new Vector2(v.uv0.X, 1 - v.uv0.Y);
            }

        }
        public void FlipUvsHorizontal()
        {
            foreach (Vertex v in vertices)
            {
                v.uv0 = new Vector2(1 - v.uv0.X, v.uv0.Y);
            }
        }
        public void CalculateTangentBitangent()
        {
            List<int> f = lodMeshes[DisplayLODIndex].getDisplayFace();
            Vector3[] tanArray = new Vector3[vertices.Count];
            Vector3[] bitanArray = new Vector3[vertices.Count];

            CalculateTanBitanArrays(f, tanArray, bitanArray);
            ApplyTanBitanArray(tanArray, bitanArray);
        }

        private void ApplyTanBitanArray(Vector3[] tanArray, Vector3[] bitanArray)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex v = vertices[i];
                Vector3 newTan = tanArray[i];
                Vector3 newBitan = bitanArray[i];

                // The tangent and bitangent should be orthogonal to the normal. 
                // Bitangents are not calculated with a cross product to prevent flipped shading  with mirrored normal maps.
                v.tan = new Vector4(Vector3.Normalize(newTan - v.nrm * Vector3.Dot(v.nrm, newTan)), 1);
                v.bitan = new Vector4(Vector3.Normalize(newBitan - v.nrm * Vector3.Dot(v.nrm, newBitan)), 1);
                v.bitan *= -1;
            }
        }

        private void CalculateTanBitanArrays(List<int> faces, Vector3[] tanArray, Vector3[] bitanArray)
        {
            for (int i = 0; i < lodMeshes[DisplayLODIndex].displayFaceSize; i += 3)
            {
                Vertex v1 = vertices[faces[i]];
                Vertex v2 = vertices[faces[i + 1]];
                Vertex v3 = vertices[faces[i + 2]];

                bool UseUVLayer2 = false;
                float x1 = v2.pos.X - v1.pos.X;
                float x2 = v3.pos.X - v1.pos.X;
                float y1 = v2.pos.Y - v1.pos.Y;
                float y2 = v3.pos.Y - v1.pos.Y;
                float z1 = v2.pos.Z - v1.pos.Z;
                float z2 = v3.pos.Z - v1.pos.Z;

                float s1, s2, t1, t2;
                if (UseUVLayer2)
                {
                    s1 = v2.uv1.X - v1.uv1.X;
                    s2 = v3.uv1.X - v1.uv1.X;
                    t1 = v2.uv1.Y - v1.uv1.Y;
                    t2 = v3.uv1.Y - v1.uv1.Y;
                }
                else
                {

                    s1 = v2.uv0.X - v1.uv0.X;
                    s2 = v3.uv0.X - v1.uv0.X;
                    t1 = v2.uv0.Y - v1.uv0.Y;
                    t2 = v3.uv0.Y - v1.uv0.Y;
                }


                float div = (s1 * t2 - s2 * t1);
                float r = 1.0f / div;

                // Fix +/- infinity from division by 0.
                if (r == float.PositiveInfinity || r == float.NegativeInfinity)
                    r = 1.0f;

                float sX = t2 * x1 - t1 * x2;
                float sY = t2 * y1 - t1 * y2;
                float sZ = t2 * z1 - t1 * z2;
                Vector3 s = new Vector3(sX, sY, sZ) * r;

                float tX = s1 * x2 - s2 * x1;
                float tY = s1 * y2 - s2 * y1;
                float tZ = s1 * z2 - s2 * z1;
                Vector3 t = new Vector3(tX, tY, tZ) * r;

                // Prevents black tangents or bitangents due to having vertices with the same UV coordinates. 
                float delta = 0.00075f;
                bool sameU, sameV;
                if (UseUVLayer2)
                {
                    sameU = (Math.Abs(v1.uv1.X - v2.uv1.X) < delta) && (Math.Abs(v2.uv1.X - v3.uv1.X) < delta);
                    sameV = (Math.Abs(v1.uv1.Y - v2.uv1.Y) < delta) && (Math.Abs(v2.uv1.Y - v3.uv1.Y) < delta);
                }
                else
                {
                    sameU = (Math.Abs(v1.uv0.X - v2.uv0.X) < delta) && (Math.Abs(v2.uv0.X - v3.uv0.X) < delta);
                    sameV = (Math.Abs(v1.uv0.Y - v2.uv0.Y) < delta) && (Math.Abs(v2.uv0.Y - v3.uv0.Y) < delta);
                }

                if (sameU || sameV)
                {
                    // Let's pick some arbitrary tangent vectors.
                    s = new Vector3(1, 0, 0);
                    t = new Vector3(0, 1, 0);
                }

                // Average tangents and bitangents.
                tanArray[faces[i]] += s;
                tanArray[faces[i + 1]] += s;
                tanArray[faces[i + 2]] += s;

                bitanArray[faces[i]] += t;
                bitanArray[faces[i + 1]] += t;
                bitanArray[faces[i + 2]] += t;
            }
        }

        public void SmoothNormals()
        {
            Vector3[] normals = new Vector3[vertices.Count];

            List<int> f = lodMeshes[DisplayLODIndex].getDisplayFace();

            for (int i = 0; i < lodMeshes[DisplayLODIndex].displayFaceSize; i += 3)
            {
                Vertex v1 = vertices[f[i]];
                Vertex v2 = vertices[f[i + 1]];
                Vertex v3 = vertices[f[i + 2]];
                Vector3 nrm = CalculateNormal(v1, v2, v3);

                normals[f[i + 0]] += nrm;
                normals[f[i + 1]] += nrm;
                normals[f[i + 2]] += nrm;
            }

            for (int i = 0; i < normals.Length; i++)
                vertices[i].nrm = normals[i].Normalized();

            // Compare each vertex with all the remaining vertices. This might skip some.
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex v = vertices[i];

                for (int j = i + 1; j < vertices.Count; j++)
                {
                    Vertex v2 = vertices[j];

                    if (v == v2)
                        continue;
                    float dis = (float)Math.Sqrt(Math.Pow(v.pos.X - v2.pos.X, 2) + Math.Pow(v.pos.Y - v2.pos.Y, 2) + Math.Pow(v.pos.Z - v2.pos.Z, 2));
                    if (dis <= 0f) // Extra smooth
                    {
                        Vector3 nn = ((v2.nrm + v.nrm) / 2).Normalized();
                        v.nrm = nn;
                        v2.nrm = nn;
                    }
                }
            }
        }

        public void CalculateNormals()
        {
            Vector3[] normals = new Vector3[vertices.Count];

            for (int i = 0; i < normals.Length; i++)
                normals[i] = new Vector3(0, 0, 0);

            List<int> f = lodMeshes[DisplayLODIndex].getDisplayFace();

            for (int i = 0; i < lodMeshes[DisplayLODIndex].displayFaceSize; i += 3)
            {
                Vertex v1 = vertices[f[i]];
                Vertex v2 = vertices[f[i + 1]];
                Vertex v3 = vertices[f[i + 2]];
                Vector3 nrm = CalculateNormal(v1, v2, v3);

                normals[f[i + 0]] += nrm * (nrm.Length / 2);
                normals[f[i + 1]] += nrm * (nrm.Length / 2);
                normals[f[i + 2]] += nrm * (nrm.Length / 2);
            }

            for (int i = 0; i < normals.Length; i++)
                vertices[i].nrm = normals[i].Normalized();
        }

        private Vector3 CalculateNormal(Vertex v1, Vertex v2, Vertex v3)
        {
            Vector3 U = v2.pos - v1.pos;
            Vector3 V = v3.pos - v1.pos;

            // Don't normalize here, so surface area can be calculated. 
            return Vector3.Cross(U, V);
        }

        public void SetVertexColor(Vector4 intColor)
        {
            // (127, 127, 127, 255) is white.
            foreach (Vertex v in vertices)
            {
                v.col = intColor;
            }
        }

        #endregion
    }
}
