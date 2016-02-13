#Working with unityserializer-ng and Mecanim#
##What is saved/loaded?##
* Parameters (all types)
* Current and next state (states, not blendtrees)
* Progress of the current and next state
* Layer weights

##What isn't saved/loaded?##
Basically the rest. The important part is that Mecanim transitions **are not**.
Due to the limitations of the Unity API, it is not possible to manually start a transition between two states at a specified point. As such, when you save while a transition between the states 'A' and 'B' occurs, you are given two options how to handle it.

`REVERT` means that after loading, the current state will be A and the transition will have to be triggered again.

`SKIP` means that after loading, the current state will be B, skipping the entire transition regardless of how long it would have taken.

Both options have their downsides and can mess up your state-machine, if you have complex dependencies with other components and rely on each transition being completed after triggered.
Fortunately, it is possible to transorm a Mecanim transition into an equivalent animation, which will be stored correctly, if a few conditions are met.

##Transforming a simple Mecanim transition into an animation##
`Capital letters represent states. Arrows (->) represent the connections of your state-machine. A lowercase t inside an arrow means that a Mecanim transition (blending) occurs.`

1. Starting with `A--t-->B`
2. Create a transition animation (e.g. `C`)
  1. Copy the end of `A` and paste it at the start of `C`
  2. Copy the start of `B` and paste it at the end of `C`
  3. Tweak the time and curves to your liking
3. Instert `C` between `A` and `B`
4. Make the transition from `A` to `C` and `C` to `B` instantaneous
5. The result will be `A->C->B`


It has to be noted that this trick has its own limitations, whenever `A` and `B` aren't merely states, but animations themselves. If they are ongoing/repeating and share properties, you will run into problems, if `A` can be interruped and doesn't have to finish, since `C` starts where `A` ends. If `C` can interrupt `A` and start at any point in time, `A` doesn't have a defintitive end-point anymore and animation glitches will occur.
On top of that, your state-machine will grow significantly in size and complexity.

The problem won't be fixed, until the Unity API is extended. The best way of dealing with them is to carefully think about which animations have to be stored correctly, and which can be just restarted/skipped.
