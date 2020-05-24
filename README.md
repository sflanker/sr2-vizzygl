## What is it?

VizzyGL is a mod for SimpleRockets 2 that adds a set of Vizzy instructions for creating persistent 3d graphics positioned relative to in game objects such as crafts or planets. The objects are automatically repositioned as their parent moves, and can also be manually repositioned and rotated with Vizzy.

## Why does it exist?

The primary motivation of this mod is to create visual aids and to visualize mathematical concepts, however there are probably numerous conceivable purposes. The one thing this is not meant to be is a way to dynamically add physical parts to your craft. All of the graphics produced by VizzyGL have collisions disabled and should be thought of as holographic projections.

## Programming Interface

Graphics are created by first setting various drawing context properties, such as `Color`, `Rotation`, `Origin`, etc using the `Set ...` commands. Once settings have been established the graphic can be created using a `Draw ...` instruction specifying position relative to the origin and a name. Only one graphic can exist with a given name, so if you create another graphic with the same name the previous graphic will be removed automatically.

After they are created graphics can be updated using the `Update Object` command. However it is not possible to change a graphic's `Origin` after it is created.

Graphics can be removed from the scene using the `Remove Object` instruction.

## TODO

* __Support for displaying objects in Map view.__
* Line graphics (lines between two anchored points, like from Craft A to Craft B)
* More primitives:
    * Cone
    * Pyramid
    * Torus
    * Teapot
* Add center of rotation.
* Boolean geometry?

## Known Issues

* Graphics disappear after relaunching.