# DataConsumers folder

Data Consumers are objects that implement the IDataConsumer interface.  They listen for data change notifications from a Data Source and then retrieve those data changes to update view objects that present the changed data. 

Typical data consumers take variable text or numeric data to populate a user interface, but they can be much more sophisticated. They can take a variable URL, retrieve an image at that URL, then convert it to a Sprite object and assign that to a SpriteRenderer object. 

## Data Source

Any one data consumer is designed to communicate with a single data source.  For more complex cases, a data source aggregator can be used to combine data sets from multiple data sources. This feature does not exist yet.  The default means of associating a data consumer and its data source is to navigate up the game object hierarchy until a data source is found.

## Key Paths

A data consumer uses "key paths" to identify which datum is of interest in the overall data set managed by a Data Source.  A single data consumer can manage as many of these key paths as it wishes. 

## Collections

A data consumer can be designed to manage a collection of data sets where each item in the collection contains data that is relatively similar to other elements in the same list.  The data consumer can request from a data source, ranges if items from such a list. The data is actually the fully resolved keypaths of each item that can then further be used to populate each item. In this scenario, each item is typically a Unity Prefab with variable data fields such as TextMeshPro components that then each individually assign listeners for their own data.


