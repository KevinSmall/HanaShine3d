# Unity Client Architecture

## Table of Contents  
* [Getting Started](#start)  
* [Project Structure](#3-project-structure)  
* [Interacting with PO Boxes](#comms)
* [Creating PO Boxes](#createpos)

<a name="start"/>
## 1) Getting Started

### 1.1) Unity and C# #
If you are new to either Unity or C#, and haven't already done so, at least do the "Interface and Essentials" and "Scripting" tutorials from here: http://unity3d.com/learn/tutorials.  You need to be able to examine the Unity Console output and debug a running Unity project in Visual Studio.  You need to know enough C# to understand how events and callbacks work - these are used throughout the code.

### 1.2) SimpleJSON
To understand SimpleJSON [see examples here](http://wiki.unity3d.com/index.php/SimpleJSON).

<a name="3-project-structure"/>
## 2) Project structure

![](archsimple590.png?raw=true)

The diagram above gives an overview of the relationship between the different objects.

Communication between objects is all event driven.  Objects raise events and other objects listen to events that interest them, and react as needed.  This design lends itself well to dealing with the asynchronous nature of HTTP/OData calls - an OData call can be made, and then an event raised when the data comes back.  Any object interested in seeing that data can listen for the event, and then read the data.

All the interesting "PO box creation" stuff happens in the PoFactory class, which makes calls to the GameManager class to get the data it needs.  The key concepts come from understanding the relationship between these two objects. This diagram shows the detail of a single request for data made by, in this example, the PO Factory:

![](dataflowdetail.png?raw=true)

### 2.1) GameManager
The [GameManager](https://github.com/KevinSmall/HanaShine3d/blob/master/Epm3d/Assets/_Scripts/GameManager/GameManager.cs) is a singleton object, globally visible, and it provides the service of handling all HTTP/OData calls and provides nicely formatted data.  Other objects are "customers" of this service, and they use it like this:
* First they call GetEpmData_* to request some specific data, eg get a mass PO list or a single PO.
* The GameManager then makes an asynchronous call to get the data.  Once that data arrives, the GameManager raises the event On\*DataChanged to say "hey! data has arrived, you can come get it now".
* Then the customer can call GetEpmResponse_* to get the actual data - and this is when the parsing happens.  Parsing is only done on demand.

The GameManager is totally separate from any in-game Unity GameObjects, it touches nothing in-game.  It is up to the PoFactory and ChartFactory objects to make any changes to in-game objects.  To see how this happens, we can look at the process from the "customer" side and take a look at the PoFactory class.

### 2.2) PoFactory
The [PoFactory](https://github.com/KevinSmall/HanaShine3d/blob/master/Epm3d/Assets/_Scripts/Po/PoFactory.cs) is a customer of the GamaManager service described above.  The PoFactory operates in two phases.  In phase one it gets a list of PO items to create (just the PO item keys).  Then in phase two, it loops through that list and gets more details for each PO, enough information to actually create that PO item as a Unity GameObject.

If you look inside the PoFactory class, you'll see this pattern:

#### a) Hook GameManager Events
In the Start() method, the PoFactory hooks (listens to) two events exposed by the GameManager:
```csharp
GameManager.Instance.OnSinglePODataWithItemsChanged += OnSinglePODataWithItemsChanged;
GameManager.Instance.OnMassListPOChanged += OnMassListPOChanged;
```
The above means that the PO factory will react to hearing the event "GameManager.Instance.OnMassListPOChanged" by calling this, which is what starts the process of creation:
```csharp
private void OnMassListPOChanged(object sender, EventArgs e)
{
  //print("PoFactory has received event OnMassListPOChanged");
  CreateMassPos();
}
```
#### b) Ask GameManager to get data
Later in the Start() method, you'll see a call to DoProductionRun() which itself makes a call to the GameManager requesting the "mass list" of PO data (this is still phase one):

```csharp
// init the factory and spawn POs
_factoryState = FactoryState.Working;
_posCreatedSoFar = 0;
print("Po Factory is starting a production run");
GameManager.Instance.GetEpmData_MassListPO(DesiredPoItemCount);
```
#### c) Wait for results then get them
That last call in the code snippet above causes the GameManager to make an asynchronous HTTP/OData call.  If and when that call returns data to the GameManager, the GameManager will store that data (as an unformatted string) and raise the event OnMassListPOChanged.  If there is no internet connection, or something falls over, then the event OnMassListPOChanged might never be raised.

When the PoFactory hears "OnMassListPOChanged" it knows there is data to get, so it goes and gets it using this line you can see at the start of the method CreateMassPos():
```csharp
EpmPoDataMass poDataMass = GameManager.Instance.GetEpmResponse_MassListPO();
```
It is only at this point that the GameManager attempts to parse the string it is storing internally.  It is only here that the SimpleJSON code is used to try to break out the JSON string.

Now phase two beings, and it follows a very similar pattern.  The PoFactory makes repeated calls to the GameManager asking for more data on each PO one at a time.  The PoFactory class listens to the event being raised saying data is ready, and then creates the PO boxes in the game.

This is kicked off towards the end of the CreateMassPos() method using the Unity method Invoke() to form a kind of queue:
```csharp
// And we're off!! This will create each PO one by one
Invoke("AskForPoData", PoCreateDelayInSeconds);
```

<a name="comms"/>
## 3) Interacting with PO Boxes
POs are created as described in the previous section.  Once created they move around on their own (controlled by the [PoMover](https://github.com/KevinSmall/HanaShine3d/blob/master/Epm3d/Assets/_Scripts/Po/PoMover.cs) script) and can be interacted with (highlighted) by the player. The interaction works as follows.  The main camera has a script attached to it ([Highlight_WhatIsLookedAt](https://github.com/KevinSmall/HanaShine3d/blob/master/Epm3d/Assets/_Scripts/System/Highlight_WhatIsLookedAt.cs)) that does raycasting.  If the player's gaze hits any object that has a [Highlight_IsHighlightable](https://github.com/KevinSmall/HanaShine3d/blob/master/Epm3d/Assets/_Scripts/System/Highlight_IsHighlightable.cs) script attached, then that object gets it's "IsHighlighted" property set to true.  This property change causes an event "OnHighlighted" to be raised by the Highlight_IsHighlightable script.  So far all this code is doing is the mechanics of an object being looked at (gazed at) and that object then receiving an event.

What actually happens when that event is raised is handled in the [PoManager](https://github.com/KevinSmall/HanaShine3d/blob/master/Epm3d/Assets/_Scripts/Po/PoManager.cs) script.  Think of the PO manager as the brain of each PO (so each PO has its own PO manager).  It is the PO manager that decides what, if anything, to do when an event is received.  The PO manager reacts to receiving an OnHighlighted event like this:

```csharp
private void OnHighlighted(object sender, EventArgs e)
{
   if (_poMover != null)
   {
      _poMover.enabled = false;
   }
   // po gui script is a required cpt, dont need to check existence
   _poGui.enabled = true;
   }
```

This causes the PO box to stop moving (if it has a mover script attach) and causes it's detail GUI to appear.  The final part of PO box interaction, is the approval or rejection.  This is handled in the PoManager.Update() method:

```csharp
if (Input.GetButtonUp("Approve"))
{
   GameManager.Instance.GetEpmData_ApprovePO(PoBusinessDataWithItems.PurchaseOrderId);
   _poManagerState = PoManagerState.BusyRequestingApprove
}
               
if (Input.GetButtonUp("Reject"))
{
    GameManager.Instance.GetEpmData_RejectPO(PoBusinessDataWithItems.PurchaseOrderId);
    _poManagerState = PoManagerState.BusyRequestingRejected;                  
}
```

The "GetButtonUp" for "Approve" as shown above uses standard Unity input mappings, so will react to mouse click or whatever keys were assigned.  To edit controls at runtime, hold down the Shift key (Windows) or the Alt key (Mac) when you start the executable and an additional dialog appears.  This allows you to specify screen resolution and graphics quality, and also to edit the controls.

<a name="createpos"/>
## 4) Creating PO Boxes
The SHINE OData service provides PO line items.  The PO GameObject boxes that appear in the game are created one per PO header.  It is the CreateMassPos() method that takes the initial "mass PO list" and summarises it to PO header level, then arranges for a queue of further HTTP/OData calls to happen via method AskForPoData().  In AskForPoData() you can see this call:

```csharp
GameManager.Instance.GetEpmData_SinglePOwithItems(pok.Doc, 20);
```

It is only when the event "GameManager.Instance.OnSinglePODataWithItemsChanged" is raised saying that data is available from that call, that the actual PO can be created.

```csharp
private void OnSinglePODataWithItemsChanged(object sender, EventArgs e)
{
    //print("PoFactory has received event OnSinglePODataWithItemsChanged");
    CreatePoWithItems();
}
```

```csharp
private void CreatePoWithItems()
{
  //-------------------------------------------------------------------------
  // Instatiate PO gameobject
  //-------------------------------------------------------------------------
  GameObject gParent = GameObject.FindWithTag("PoBucket");
  Vector3 spawnPosition = _posToCreate[_posCreatedSoFar].Pos; 
  Quaternion spawnRotation = _posToCreate[_posCreatedSoFar].Rot; 
  etc etc
```
