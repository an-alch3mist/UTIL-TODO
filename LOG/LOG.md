# LOG.md created, perform LOG.SaveLog(str, format) to append text here:

```sceneGameObject-hierarchy
=== Component Abbreviations ===
dmc = MeshFilter | MeshRenderer
bc = BoxCollider
================================

./GameObject With COLLIDER/(scale:1.0 | no components)
├ Cube (scale:1.0 | dmc)
└ COLLIDER (scale:1.0 | no components)
  └ collider (scale:1.0 | bc)

```

```sceneGameObject-hierarchy
=== Component Abbreviations ===
dmc = MeshFilter | MeshRenderer
bc = BoxCollider
================================

./GameObject With COLLIDER/(scale:1.0 | no components)
├ Cube (scale:1.0 | dmc)
└ COLLIDER (scale:1.0 | no components)
  └ collider (scale:1.0 | bc)

```

```sceneGameObject-hierarchy
=== Component Abbreviations ===
dmc = MeshFilter | MeshRenderer
bc = BoxCollider
alstn = AudioListener
cam = Camera
================================

./camRig/(scale:1.0 | RTSCameraController)
├ cube (scale:(0.1,0.5,0.1) | dmc, bc)
├ camPivot (scale:1.0 | no components)
│ └ CM vcam1 (scale:1.0 | CinemachineVirtualCamera)
│   └ cm (scale:1.0 | CinemachinePipeline, CinemachineTransposer)
└ Main Camera (scale:1.0 | alstn, cam, UniversalAdditionalCameraData, CinemachineBrain)

```

