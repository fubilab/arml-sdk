using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlipNormals : MonoBehaviour
{
    [ContextMenu( "Flip" )]
    public void FlipMeshContext ( ) {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh. triangles = mesh. triangles. Reverse( ). ToArray( );
    }
}
