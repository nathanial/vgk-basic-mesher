using System;
using System.Collections.Generic;
using UnityEngine;

namespace VGK{

  public class BasicMesherConfiguration : Configuration {
    public BasicMesherConfiguration(){
      SetDefault("Mesher", typeof(BasicMesher));
    }
  }

  public class BasicMesher : Mesher {
    readonly IDictionary<Position, GameObject> _lights;
    Chunk _chunk;
    readonly Transform _transform;
    readonly ChunkMaterial _material;

    public BasicMesher(IDictionary<Position, GameObject> lights, Transform transform, ChunkMaterial material){
      _lights = lights;
      _transform = transform;
      _material = material;
    }

    public override MeshInfo Mesh(Chunk chunk){
      _chunk = chunk;
      MeshInfo meshInfo = new MeshInfo ();

      for (int x = 0; x < Chunk.Size; x++) {
        for (int y = 0; y < Chunk.Size; y++) {
          for (int z = 0; z < Chunk.Size; z++) {
            byte brick = _chunk[x, y, z];
            var position = new Position(x, y, z);
            if (brick == 0) {
              if (_lights.ContainsKey(position)) {
                var light = _lights[position];
                UnityEngine.Object.Destroy(light);
                _lights.Remove(position);
              }
              continue;
            }
            var definition = Assets.Voxels[brick];
            var material = Assets.Materials[definition.MaterialName];
            if (material != _material) {
              continue;
            }

            if (IsTransparent(x - 1, y, z)) {
              //left wall
              BuildFace(brick, BlockSide.Side, new Vector3(x, y, z), Vector3.up, Vector3.forward, false, meshInfo);
            }
            if (IsTransparent(x + 1, y, z)) {
              //right wall
              BuildFace(brick, BlockSide.Side, new Vector3(x + 1, y, z), Vector3.up, Vector3.forward, true, meshInfo);
            }
            if (IsTransparent(x, y - 1, z)) {
              //bottom wall
              BuildFace(brick, BlockSide.Bottom, new Vector3(x, y, z), Vector3.forward, Vector3.right, false, meshInfo);
            }
            if (IsTransparent(x, y + 1, z)) {
              //top wall
              BuildFace(brick, BlockSide.Top, new Vector3(x, y + 1, z), Vector3.forward, Vector3.right, true, meshInfo);
            }
            if (IsTransparent(x, y, z - 1)) {
              //back wall
              BuildFace(brick, BlockSide.Side, new Vector3(x, y, z), Vector3.up, Vector3.right, true, meshInfo);
            }
            if (IsTransparent(x, y, z + 1)) {
              //front wall
              BuildFace(brick, BlockSide.Side, new Vector3(x, y, z + 1), Vector3.up, Vector3.right, false, meshInfo);
            }
            if (definition.Lights.Count > 0 && !_lights.ContainsKey(position)) {
              _lights[new Position(x,y,z)] = BuildLight(definition.Lights[0], new Vector3(x+.5f, y+.5f, z+.5f));
            }
          }
        }
      }

      return meshInfo;
    }


    bool IsTransparent(int x, int y, int z) {
      byte brick = GetByte(x, y, z);
      if (brick == 0) {
          return true;
      }
      VoxelDefinition definition = Assets.Voxels [brick];
      return definition.Transparent || definition.Lights.Count > 0;
    }

    byte GetByte(int x, int y, int z) {
      if (x < 0 || y < 0 || z < 0 || x >= Chunk.Size || z >= Chunk.Size || y >= Chunk.Size) {
          return 0;
      }
      return _chunk[x, y, z];
    }


    void BuildFace(byte brick, BlockSide side, Vector3 corner, Vector3 up, Vector3 right, bool reversed, MeshInfo meshInfo) {
      int index = meshInfo.Vertices.Count;
      meshInfo.Vertices.Add(corner);
      meshInfo.Vertices.Add(corner + up);
      meshInfo.Vertices.Add(corner + up + right);
      meshInfo.Vertices.Add(corner + right);

      var padding = 0.00f;
      Rect r;
      if (side == BlockSide.Bottom) {
          r = Assets.Voxels[brick].Bottom;
      } else if (side == BlockSide.Side) {
          r = Assets.Voxels[brick].Side;
      } else if (side == BlockSide.Top) {
          r = Assets.Voxels[brick].Top;
      } else {
          throw new UnityException("ooops");
      }
      var startX = r.x - padding;
      var startY = r.y - padding;
      var endX = startX + r.width + padding * 2;
      var endY = startY + r.height + padding * 2;
      meshInfo.TextureCoords.Add(new Vector2(startX, startY));
      meshInfo.TextureCoords.Add(new Vector2(startX, endY));
      meshInfo.TextureCoords.Add(new Vector2(endX, endY));
      meshInfo.TextureCoords.Add(new Vector2(endX, startY));

      if (reversed) {
          meshInfo.Triangles.Add(index + 0);
          meshInfo.Triangles.Add(index + 1);
          meshInfo.Triangles.Add(index + 2);
          meshInfo.Triangles.Add(index + 2);
          meshInfo.Triangles.Add(index + 3);
          meshInfo.Triangles.Add(index + 0);
      } else {
          meshInfo.Triangles.Add(index + 1);
          meshInfo.Triangles.Add(index + 0);
          meshInfo.Triangles.Add(index + 2);
          meshInfo.Triangles.Add(index + 3);
          meshInfo.Triangles.Add(index + 2);
          meshInfo.Triangles.Add(index + 0);
      }
    }


    GameObject BuildLight(VoxelLight light, Vector3 position) {
      var lightGameObject = new GameObject("Voxel Light");
      var lightComp = lightGameObject.AddComponent<Light>();
      lightComp.color = VoxelLight.HexToColor(light.Color);
      lightComp.range = (float)light.Range;
      lightComp.intensity = (float)light.Intensity;
      lightComp.shadows = LightShadows.Hard;
      lightComp.renderMode = LightRenderMode.ForceVertex;
      lightComp.cullingMask = (1 << LayerMask.NameToLayer("Everything")) | ~(1 << LayerMask.NameToLayer("Lights"));
      lightGameObject.transform.position = position;
      lightGameObject.layer = LayerMask.NameToLayer ("Lights");
      lightGameObject.transform.SetParent(_transform, false);
      return lightGameObject;
    }
  }
}
