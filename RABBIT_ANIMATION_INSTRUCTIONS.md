# How to Make the Rabbit Dance

I've created the animation files, but the Rabbit needs to be properly set up in Unity. Here's how to do it:

## Step 1: Add Rabbit to Scene
1. Open Unity and load the `SampleScene`
2. In the Project window, navigate to `Assets/3D Models/`
3. Find `Rabbit.fbx`
4. **Drag `Rabbit.fbx` directly into the Scene view or Hierarchy panel**

## Step 2: Add the Dance Animation
1. Select the Rabbit in the Hierarchy
2. In the Inspector, look for the **Animator** component
   - If there's no Animator component, click "Add Component" and add "Animator"
3. In the Animator component, find the **Controller** field
4. In the Project window, navigate to `Assets/Animations/`
5. **Drag `RabbitAnimator.controller` into the Controller field**

## Step 3: Test the Animation
1. Press the **Play** button in Unity
2. The Rabbit should now:
   - Spin 360 degrees continuously
   - Bounce up and down rhythmically

## Files Created
- `Assets/Animations/RabbitDance.anim` - The dance animation (4 seconds, looping)
- `Assets/Animations/RabbitAnimator.controller` - The animator controller
- `Assets/Scripts/RabbitDanceController.cs` - Helper script (optional)

## Troubleshooting
- If the Rabbit is too small or not visible, adjust its **Scale** in the Transform component
- If the Rabbit isn't at the right position, adjust its **Position** in the Transform component
- The animation makes the rabbit spin on the Y-axis and bounce up and down

## Alternative Method (If Above Doesn't Work)
1. Create an empty GameObject in the scene (GameObject > Create Empty)
2. Name it "Rabbit"
3. Add an Animator component
4. Set the Animator Controller to `RabbitAnimator.controller`
5. As a child of this GameObject, add the Rabbit model
