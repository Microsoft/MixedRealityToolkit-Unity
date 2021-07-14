# ItemPlacers folder

The classes in this folder implement the IDataCollectionItemPlacer interface.

Item placers are used by data consumers that manage collections of data sets. The item placer can request items in a list in the form of game objects, usually a prefab.  These prefabs contain their own data consumers which will have been assigned the same data source as the data consumer that manages the collection itself. 

As it is provided with these modified prefabs, it can use any strategy to manage the items and make them visible to the user. Typically, they will be added to a UX view collection with scroll bars, page next and previous, swipe up/down, or other means of navigating the list.

Item placers also support virtualization by requesting only those objects that are currently visible. It also supports data pooling, where any list items that go out of visible range can be returned back to the pool for re-use, thus reducing garbage collection and the overhead of instantiating complex prefabs for every item in a large collection.

