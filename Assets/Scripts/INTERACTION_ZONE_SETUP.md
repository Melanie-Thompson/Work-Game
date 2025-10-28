# Interaction Zone Setup Guide

This guide explains how to set up interaction zones for carousel objects.

## What is an Interaction Zone?

An **Interaction Zone** is a designated screen area where users can interact with objects on the carousel (like dials, levers, monitors). Input outside the interaction zone will be ignored by these objects.

## Benefits

✅ Prevents accidental clicks on carousel objects
✅ Creates a clear "interaction area" for users
✅ Doesn't interfere with swipe zones
✅ Easy to visualize and adjust in the editor

## Setup Instructions

### Step 1: Create an Interaction Zone GameObject

1. In the Hierarchy, right-click → **Create Empty**
2. Name it `"InteractionZone"`
3. Add Component → **InteractionZone** script
4. Add Component → **Box Collider** (if not auto-added)

### Step 2: Position and Size the Zone

1. In the **Scene view**, select the InteractionZone
2. Adjust the **BoxCollider** to cover the center area of your screen where objects should be clickable
3. The zone should cover the carousel objects but NOT overlap with swipe zones

**Example Setup:**
- Position: Center of screen (where carousel items appear)
- Size: Cover the middle 50-60% of screen width/height
- Leave top and bottom areas for swipe zones

### Step 3: Configure the Collider

The BoxCollider is automatically set as a **trigger** (non-physical). You can adjust:
- **Center**: Offset the zone position
- **Size**: Change the dimensions of the zone

### Step 4: Visualize the Zone

1. Select the InteractionZone GameObject
2. In the Inspector, enable **Show Gizmos** (enabled by default)
3. In the Scene view, you'll see a **semi-transparent blue box** showing the interaction area

### Step 5: Test It

**What should happen:**
- ✅ Clicks INSIDE the blue zone → Objects respond (dial rotates, lever moves, etc.)
- ✅ Clicks OUTSIDE the blue zone → Objects ignore the input
- ✅ Swipes in swipe zones → Carousel rotates normally

## Advanced: Multiple Interaction Zones

You can create separate interaction zones for different carousel objects:

1. Create multiple InteractionZone GameObjects
2. Parent each zone to the corresponding carousel object
3. Each interactive script will automatically find its parent's InteractionZone

**Example:**
```
CarouselObject1
  └─ DialPhone
  └─ InteractionZone1  ← Dial will use this zone

CarouselObject2
  └─ Lever
  └─ InteractionZone2  ← Lever will use this zone
```

## Troubleshooting

**Problem: Objects not responding at all**
- Check that the InteractionZone has a BoxCollider
- Verify the BoxCollider covers the object's position
- Look at console logs for "outside interaction zone" messages

**Problem: Can't see the gizmo**
- Enable "Show Gizmos" on the InteractionZone component
- Make sure Gizmos are enabled in the Scene view (button in top-right)

**Problem: Zone too small/large**
- Adjust the BoxCollider size in the Inspector
- Use the Scene view to visually resize it

## Script References

Interactive scripts that support InteractionZone:
- `Lever_Drag_Slide.cs`
- `DialRotaryPhone.cs`
- `ClickableMonitor.cs` (Monitor_Zoom.cs)

All these scripts automatically:
1. Find the InteractionZone in the scene
2. Check input position before responding
3. Log messages when input is rejected
