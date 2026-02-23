# Tile System For Building
suppose consider the following grid via Board<Tile>:
```
........
........
..BBE..
..BOE...
..O.....
........
```
here the 3B you see are 1 prefab which occupy 3 coords
the 2 o's you see are 2 different prefab which occupy 2 different tile and 2E are 2 different prefab too
and btw the origin(pivot for rotation of building while palcing is at top-right E)
when i say prefab i mean a multi tiled mesh with collider componenet to gameobject which may occupy 1 x 1 or 2 x 1 or 2 x 2 jagged (could be customised by providing all the v2 occupied via List<v2>)
the entire
```
BBE
BOE
O
```
is 1 building scriptable object, which could be placed and rotated on 3D grid of Board<Tile> 
and this building can be moved in future too when selected
could you build ntire how scriptable object should be architectured and tile etc(for a tower defence factory style grid based game ofcourse now focus on just grid placement)
btw here is `UTIL.cs` which i built as Tool for any game built in Unity3D 2020.3+.