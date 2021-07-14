# Data Binding Framework

**NOTE: This framework is still under development and APIs may change. Also see Known Limitations section below for more information.**

Welcome to the data binding framework of MRTK. This framework is designed to make it easy to create 
visual elements that can be populated dynamically at runtime with data being provided from an external
data source, and also dynamically update as the supplied data changes.
It enables the following functionality:

1. Use dynamic data to alter visual elements. Currently supported:
  - TextMeshPro text
  - TextMesh text
  - Sprite - via sprite lookup
  - Sprite - via image replacement
  - Lists - dynamically populated prefabs containing the above variable elements
2. Provide variable data via a variety of data sources:
  - JSON text
  - Dictionary of variable data elements
  - Programmatically altered data
  - Node based structured data
3. Map differing variable name spaces between views and data sources
4. List paging - data is only fetched when visible
5. List prefab pooling - prefabs are reused with new varaible data to reduce GC and instantiation time.

## Key Concepts

### Data Source

A data source is any managed set of data of arbitrary type(s) and complexity
that can be used to populate data views via data consumers. The data managed by a data source

can be static or dynamic. Any changes to data items will be reported to any data consumers
that have registered to receive change notifications.

### Data Source Provider

A simple interface that has a single method to retrieve a data source. This is designed to allow a MonoBehavior scripting component to be auto-discovered in the game object hierarchy by data consumer components, but without the need to implement a data source directly on the game object itself.  This is useful when an existing MonoBehaviour must derive from another class and multiple inheritence prevents deriving from DataSourceGOBase. It also allows more code to have no Unity dependencies.


### Key Path (string)

A key path is the key to uniquely identifying a piece of information in a data source.

Although a key path can be any unique identifier per data item, all
current impmlementations use the concept of a logical user readable
specifier that indicates the navigational position of the data of interest
relative to the entire structured data set. It is modelled on javascript's concept of 
lists, dictionaries and primitives, such that key paths are syntactically correct javascript
statements for accessing data that can be represented in JSON. The advantage 
of this approach is that it correlates very well with both JSON and XML, 
which are the two most prevalent means of transfering information from back-end services.

Example key paths:

- temperature
- contacts[10].firstName
- contacts
- contacts[10].addresses[3].city
- [10].title
- kingdom.animal.mammal.aardvark.diet.foodtypes.termites

Given that a key path is an arbitrary string with no required taxonomy, the actual
data specifiers could be any method of describing what data to retrieve. XML's XPath is an
example of a viable key path schema that would work with data sources. As long as key paths provided by the data consumer
are consistent with the keypaths expected by the data source, everything will work. 
Furthermore Key Path Mappers can be implemented to translate between different schemas.

### Resolving a Key Path

Resolving a key path means combining two keypaths:
1. An absolute keypath that describes how to access a specific
subset of a larger dataset, such as one entry in a list of many entries. 
2. A partial (relative) keypath that represents a specific datum within that list or map entry.

This makes it possible to treat a subset of the data in such a way that it does
not matter where in a larger data set hierarchy it actually exists. The most critical
use of this ability is to describe the data of a single entry in a list without worrying about which
entry in that list the current instance is referencing. 

Since a "fully resolved" Key path is always generated and consumed by a DataSource and should never (or at least rarely) be modified by a DataConsumer or other external component, it can have any structure that makes sense to the DataSource.  For example, if a prefab to show a list entry for a photo and it's title, date taken and other attributes, the local key path in the prefab might look like this:

- "photo_url"
- "title"
- "date_taken"
- "description"

The fully resolved key paths for one prefab entry in a list might look like this:

- "f3cb1906-d8b3-489d-9f74-725e5542b55d/photo_url"
- "f3cb1906-d8b3-489d-9f74-725e5542b55d/title"
- "f3cb1906-d8b3-489d-9f74-725e5542b55d/date_taken"
- "f3cb1906-d8b3-489d-9f74-725e5542b55d/description"


### Key Path Mapper (IDataKeyPathMapper)

A key path mapper allows data sources and data consumers to use different namespaces and conventions for key paths and still work together.

A prefab for a commonly used element, such as a slate to show a persons contact information, can cantain variable fields managed by data consumers. To make this possible , the identifier used for any variable aspect of the prefab needs a way to map to the identifier for the correct datum in the data source that will, in each use of the prefab, determine the contents of that variable element. The Key Path Mapper makes this possible.

The prefab may be used with different data sources where the data is stored in a different organizationl structure and uses field names. To use a
template prefab with each data source, a key path mapper can resolve any differences in how the data is organized.


### Data Consumer (IDataConsumer)

An object that knows how to consume information being managed by
a data source and use that data to populate data views.

Data Consumers can register with a data source to be notified of any changes to a
data item that exists at a specified key path in a dataset. Whenever the data specified has changed (or suspected to have changed), the 
Data Consumer(s) will be notified.

### Data Consumer Collection

A collection data consumer has the added ability to manage a list of similar items. This list can be the entire data set 
managed by a data source, or just a subset. Typically the data for each item in the list contains similar types of information, but this
is not a requirement. Data sources and data consumers can support nested lists, such as a list of keywords
associated with each photo in a list of photos associated with each person in a contact list. The keypath for the keywords would be relative to the photo, 
and the keypath for the photos would be relative to the person, and the keypath of the person would be relative 
to either the nearest parent list, or the root of the data set.

When processing collections, the correct resolved keypath for the specific entry in the collection is assigned to each data consumer found in the prefab
that is instantiaged for each collection item. That is then used to full resolve the key path for any relative (local) view data within that prefab.

### Data Collection Item Placer

A collection data consumer needs a means to populate user experiences with lists of repeating visual elements, such as might be found in a scrollable
list of products, photos, or contacts. 

This is accomplished by assigning an item placer to the collection data consumer. This item placer is the logic tha knows how to request list items, accept prefabs 
that have been populated with variable data, and then present them to the user, typically by inserting them into a list managed by a ux layout component for lists.

## Getting Started

### Requirements

- Unity 2019.4
- TextMeshPro 2.1.4 or greater


### Sample Scene

For a first step, take a close look at the "Data Binding Test" scene that demonstrates a variety of variable data scenarios.  Simply load the scene and play. A few things to notice:

- The Text Input field of TextMeshPro components contain variables that look like this: {{ firstName }}

- Game objects for sprites and text have some form of Data Consumer component that manages receiving data and updating views.

- A single Data Consumer may be shared by multiple components of the same type by being placed higher in the GO hierarchy.

- A Data Consumer can find its own Data Source so long as it is on the same game object or higher in the GO hierarchy.

- A parent game object has a Data Source component that provides data for all child game objects that present a related set of variable information.

- A collection Data Consumer specifies a prefab which itself contains data consumers that will be used to populate that prefab with variable data.

### First Data Binding project

Here's a simple example to help you get started quickly:

Create a new scene. On the Mixed Reality Toolkit menu, select "Add to Scene and Configure" option.

Create an empty game object and rename it to "Data Binding". Add a DataSourceJsonTest component. 

In the inspector, change the Url to: https://www.boredapi.com/api/activity

Add a UI -> Text - TextMeshPro object to the Data Binding game object.  It will add a canvas and then a "Text (TMP)" object.

Select the Text (TMP) object, and in the inspector change the Text Input to: 

`{{ activity }}. It's {{ type }}.`

Select the canvas object and add a Data Consumer Text component to it.

Run the project.  Every 15 seconds, a different activity will be shown.

Congratulations. You've created your first Data Binding project with MRTK!

### Writing a new Data Source

A data source provides data to one or more data consumers.  It's data can be anything: algorithmically generated, in RAM, on disk, or fetched from a central database. 

All Data sources must provide the IDataSource interface. Some of the basic functionality is offered in a base class called DataSourceBase. You most likely want to derive from this class to add the specific data management funcationality specific to your need. 

To make it possible to drop a data source as a component onto a game object, another base object exists called DataSourceGOBase where GO stands for GameObject. This is a MonoBehavior that can be dropped onto a GameObject as a Component. It is a thin proxy that is designed to delegate work to a non-Unity specific core data source.

A data source may expose the ability to manipulate data within Unity Editor. If this is the case, the derived class can contain all of the data source logic, or it can leverage a "stock" data source, but also add inspector fields or other means of configuring the data.

### Writing a new Data Consumer

A data consumer gets notifications when data has changed and then updates some aspect of the user experience, such as the text shown in a TextMeshPro Component.

All data consumers must provide the IDataConsumer interface. Some of the basic functionality is offered in a base class called DataConsumerGOBase, where GO stands for GameObject. 

The majority of the work of a data consumer is to accept new data and then prepare it for presentation.  This may be as simple as selecting the right prefab, or it could mean fetching more data from a cloud service such as a content management system.

### Writing a data collection item placer

A data collection item placer is responsible for managing which parts of a collection are currently visible and how to present that visible collection, whether the collection is a small static list, or a giant million record database.

All item placers must provide the IDataCollectionItemPlacer interface.  Some of the basic funcationality is offered in a base class called DataColletionItemPlacerGOBase.  All item placers should derive from this class. 

# Known Limitations and Missing Features #

- Need to verify proper cleanup of resources in all use cases, particularly lists.
- Dynamic changes to list data completely refreshes entire list instead of incrementally updating.
- Data validation (eg. number in correct range) and formatting (eg. data and currency formats)
- Predictive data fetching for faster list navigation
- Material selector is work in progress
- Font style selector is a work in progress
- Populating other UX elements such as sliders, radio buttons, and checkboxes not yet supported
- DataSourceJson nodes should implement IDataNode interface to be interoperable with DataSourceObjects
- Not yet integrated with Volumetric Layouts and Scrolling Object Collection