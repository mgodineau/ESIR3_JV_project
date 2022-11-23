using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxBehaviour : MonoBehaviour
{
    
    
    private VoxModel _model;
    /*public VoxModel Model {
        get {return _model;}
        set {
            _model = value;
            MeshComponent.mesh = CreateMeshFromModel(_model);
            // MeshComponent.mesh = new Mesh();
        }
    }*/
    
    
    private MeshFilter _meshComponent;
    public MeshFilter MeshComponent {
        get {
            if( _meshComponent == null ) {
                _meshComponent = GetComponent<MeshFilter>();
            }
            return _meshComponent;
        }
    }
    
    
    public Mesh CreateMeshFromModel( VoxModel model ) {
        
        Mesh mesh = new Mesh();
        
        LinkedList<VoxModel.Voxel> voxels = model.GetVoxels();
        //IEnumerable<VoxModel.Voxel> filteredVoxels = System.Linq.Enumerable.Where<VoxModel.Voxel>(voxels, (VoxModel.Voxel voxel ) => voxel.value != 0);
        
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        foreach(VoxModel.Voxel voxel in voxels) {
            if( voxel.depth != 0 ) {
                //continue;
            }

            float halfSize = voxel.size / 2;
            vertices.Add(new Vector3(1, 1, 0) * halfSize + voxel.position);
            vertices.Add(new Vector3(-1, 1, 0) * halfSize + voxel.position);
            vertices.Add(new Vector3(1, -1, 0) * halfSize + voxel.position);

            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);

            /*
            int[] offsets = {-1, 1};
            for( int i=0; i<8; i++ ) {
				int xOffset = offsets[i%2];
				int yOffset = offsets[(i/2)%2];
				int zOffset = offsets[(i/4)%2];
				
				Vector3 nextPosition = voxel.position;
				nextPosition += Vector3.right * xOffset * voxel.size;
				nextPosition += Vector3.up * yOffset * voxel.size;
				nextPosition += Vector3.forward * zOffset * voxel.size;
				
                if( model.Get(nextPosition) == 0 ) {
                    // TODO build a face
                    
                }
			}*/

        }
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        
        return mesh;
    }
    
    
}
