﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PumpkinGeneration : MeshGeneratorBehaviour
{
    public float radius;

    public Vector3 pumpkinScale;

    public float minPumpkinFactor = 0.8f;
    public float maxPumpkinFactor = 1f;

    public float minPumpkinSectionFactor = 0.8f, maxPumpkinSectionFactor = 1.2f;
    public int nPumpkinSections = 5;
    public int nbLong;
    public int nbLat;

    protected int[] pumpkinSectionNb;
    protected int[] pumpkinSectionSize;
    protected int nbPumpkin;

    protected void SetupPumpkinSections()
    {
        nbPumpkin = nbLong / nPumpkinSections;

        pumpkinSectionNb = new int[nbLong+1];
        pumpkinSectionSize = new int[nbLong+1];

        int currentSectionNb = -1;
        int currentSectionSize = -1;

        for (int i = 0; i <= nbLong; i++)
        {
            if (currentSectionNb <= 0)
            {
                currentSectionSize = Random.Range((int) (minPumpkinSectionFactor*nbPumpkin),
                                                    (int) (maxPumpkinSectionFactor*nbPumpkin));
                if (currentSectionSize > nbLong - i)
                {
                    currentSectionSize = nbLong - i;
                } else if (currentSectionSize > nbLong - i - nbPumpkin)
                {
                    currentSectionSize = (nbLong - i) / 2;
                }
                currentSectionNb = currentSectionSize;
            }

            pumpkinSectionSize[i] = currentSectionSize;
            pumpkinSectionNb[i] = currentSectionSize - currentSectionNb;
            currentSectionNb--;
        }
    }

    protected float GetSectionRadiusFactor(int lat, int lon)
    {
        

        // (lon % 7 == 0? 0.9f * radius : radius), 
        // (lon % nbPumpkin == 0? minPumpkinFactor * radius : radius * (maxPumpkinFactor - 
        //         ((maxPumpkinFactor - minPumpkinFactor)/(nbPumpkin/2)) * 
        //             Mathf.Abs(lon % nbPumpkin - nbPumpkin/2))), 

        // float radiusFactor = 
        //     (lon % nbPumpkin == 0? minPumpkinFactor * radius : radius * (minPumpkinFactor + 
        //         ((maxPumpkinFactor - minPumpkinFactor) * Mathf.Sin(0.1f + 0.49f * Mathf.PI *
        //             (1f - Mathf.Abs(lon % nbPumpkin - (nbPumpkin/2)) * (1f/(nbPumpkin/2)))
        //     ))));

        float radiusFactor = 
            (pumpkinSectionNb[lon] == 0? minPumpkinFactor * radius : radius * (minPumpkinFactor + 
                ((maxPumpkinFactor - minPumpkinFactor) * Mathf.Sin(0.1f + 0.49f * Mathf.PI *
                    (1f - Mathf.Abs(pumpkinSectionNb[lon] - (pumpkinSectionSize[lon]/2)) * (1f/(pumpkinSectionSize[lon]/2)))
            ))));

        return radiusFactor;
    }

    protected override Mesh GenerateMesh()
    {
        SetupPumpkinSections();

        MeshBuilder pumpkinMeshBuilder =  new MeshBuilder();

        #region Vertices
        Vector3[] vertices = new Vector3[(nbLong+1) * nbLat + 2];
        float _pi = Mathf.PI;
        float _2pi = _pi * 2f;

        vertices[0] = Vector3.up * radius;
        for( int lat = 0; lat < nbLat; lat++ )
        {
            float a1 = _pi * (float)(lat+1) / (nbLat+1);
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);

            for( int lon = 0; lon <= nbLong; lon++ )
            {
                float a2 = _2pi * (float)(lon == nbLong ? 0 : lon) / nbLong;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);

                vertices[ lon + lat * (nbLong + 1) + 1] = 

                    Vector3.Scale(new Vector3( sin1 * cos2, cos1, sin1 * sin2 ) * 
                            GetSectionRadiusFactor(lat, lon)
                        , 
                        // radius,
                         pumpkinScale);
            }
        }
        vertices[vertices.Length-1] = Vector3.up * -radius;
        #endregion

        #region Normales
        Vector3[] normales = new Vector3[vertices.Length];
        for( int n = 0; n < vertices.Length; n++ )
            normales[n] = vertices[n].normalized;
        #endregion

        #region UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = Vector2.up;
        uvs[uvs.Length-1] = Vector2.zero;
        for( int lat = 0; lat < nbLat; lat++ )
            for( int lon = 0; lon <= nbLong; lon++ )
                uvs[lon + lat * (nbLong + 1) + 1] = new Vector2( (float)lon / nbLong, 1f - (float)(lat+1) / (nbLat+1) );
        #endregion

        #region Triangles
        int nbFaces = vertices.Length;
        int nbTriangles = nbFaces * 2;
        int nbIndexes = nbTriangles * 3;
        int[] triangles = new int[ nbIndexes ];

        //Top Cap
        int i = 0;
        for( int lon = 0; lon < nbLong; lon++ )
        {
            triangles[i++] = lon+2;
            triangles[i++] = lon+1;
            triangles[i++] = 0;
        }

        //Middle
        for( int lat = 0; lat < nbLat - 1; lat++ )
        {
            for( int lon = 0; lon < nbLong; lon++ )
            {
                int current = lon + lat * (nbLong + 1) + 1;
                int next = current + nbLong + 1;

                triangles[i++] = current;
                triangles[i++] = current + 1;
                triangles[i++] = next + 1;

                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = next;
            }
        }

        //Bottom Cap
        for( int lon = 0; lon < nbLong; lon++ )
        {
            triangles[i++] = vertices.Length - 1;
            triangles[i++] = vertices.Length - (lon+2) - 1;
            triangles[i++] = vertices.Length - (lon+1) - 1;
        }
        #endregion

        for (int vi = 0; vi < vertices.Length; vi++)
        {
            pumpkinMeshBuilder.Vertices.Add(vertices[vi]);
            pumpkinMeshBuilder.UVs.Add(uvs[vi]);
        }

        for (int t = 0; t < triangles.Length/3; t++)
        {
            pumpkinMeshBuilder.AddTriangle(triangles[3*t],
                triangles[3*t+1], triangles[3*t+2]);
        }

        return pumpkinMeshBuilder.CreateMesh();
    } 
    
}

#if UNITY_EDITOR
[CustomEditor(typeof(PumpkinGeneration))]
public class PumpkinGenerationEditor : MeshGeneratorEditor {

}
#endif
