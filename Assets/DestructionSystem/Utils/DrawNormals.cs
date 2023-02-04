using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class DrawNormals : MonoBehaviour
{

    [SerializeField]
    private Color verticesColor = Color.blue;
    [SerializeField]
    private Color triangleNormalColor = new Color(1, 0, 1);
    [SerializeField]
    private Color triangleColor = Color.red;
    [SerializeField] [Range(0, 5)]
    private float normalLength = 1.0f;
    
    private MeshFilter filter;

    public bool drawVerticesNormals = true;
    public bool drawTrianglesNormals = true;
    public bool drawTriangle = true;
    
    public float debugRatio = 100;
    
    
    private void OnDrawGizmos()
    {
        
        if (filter == null)
        {
            filter = GetComponent<MeshFilter>();
        }
        
        Mesh mesh = filter.sharedMesh;
        if (mesh == null) {return;}
        
        if ( drawVerticesNormals ) {
            DrawVertices(mesh);
        }
        if ( drawTrianglesNormals ) {
            DrawTrianglesNormals(mesh);
        }
        if ( drawTriangle ) {
            DrawTriangle(mesh);
        }
        
    }
    
    private void DrawVertices(Mesh mesh) {
        Gizmos.color = verticesColor;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 globalVertex = transform.TransformPoint(vertices[i]);
            Vector3 globalNormal = transform.TransformDirection(normals[i]);
            Gizmos.DrawLine(globalVertex, globalVertex + globalNormal * normalLength);
        }
    }
    
    private void DrawTrianglesNormals( Mesh mesh ) {
        Gizmos.color = triangleNormalColor;
        Vector3[] vertices = mesh.vertices;
        Vector3[] verticesBig = (new List<Vector3>(vertices).ConvertAll(vert => vert*debugRatio)).ToArray();
        int[] triangles = mesh.triangles;
        
        for (int i = 0; i < triangles.Length; i+=3)
        {
            Vector3 center = (vertices[triangles[i]] + vertices[triangles[i+1]] + vertices[triangles[i+2]]) / 3;
            // Vector3 normal = Vector3.Cross( vertices[triangles[i+1]] - vertices[triangles[i]], vertices[triangles[i+2]] - vertices[triangles[i]] );
            Vector3 normal = GetNormal(vertices, triangles, i);
            
            center = transform.TransformPoint(center);
            normal = transform.TransformDirection(normal.normalized);
            if ( normal == Vector3.zero ) {
                normal = GetNormal(verticesBig, triangles, i);
                normal = transform.TransformDirection(normal.normalized);
            }
            Gizmos.DrawLine(center, center + normal * normalLength);
        }
    }
    
    private Vector3 GetNormal( Vector3[] vertices, int[] triangles, int i ) {
        return Vector3.Cross( vertices[triangles[i+1]] - vertices[triangles[i]], vertices[triangles[i+2]] - vertices[triangles[i]] );
    }
    
    private void DrawTriangle( Mesh mesh ) {
        Gizmos.color = triangleColor;
        
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        
        for (int i = 0; i < triangles.Length; i+=3)
        {
            List<Vector3> currentVertices = new List<Vector3>();
            for( int j=0; j<3; j++ ) {
                currentVertices.Add(transform.TransformPoint( vertices[triangles[i+j]]));
            }
            Gizmos.DrawLine(currentVertices[0], currentVertices[1]);
            Gizmos.DrawLine(currentVertices[0], currentVertices[2]);
            Gizmos.DrawLine(currentVertices[1], currentVertices[2]);
        }
    }

}
