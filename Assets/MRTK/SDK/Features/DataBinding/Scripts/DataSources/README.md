# DataSources folder

Data Sources are objects that implement the IDataSource interface.  They manage a data set and can notify Data Consumers when any data has changed (or upon initial load). These can be as simple as a single number generator, and as complex as very large XML or JSON data sets retrieved from a RESTful web service, including binary data such as images.

## Key Paths

A single datum in the data set managed by the Data Source is identified by a key path. A key path is an arbitrary text string that can uniquely identify each datum of interest.  A key path or set of key paths can consist of any namespace, any data access scheme such as XPaths, or as is the case with all data sources provided here, can be data accessors consistent with javascript accessing its own data structures created  programmatically or via loading JSON.

## Key Path Mappers

So that multiple different data sources can populate the same standard view template, key path mappers can be used to map data namespaces to view namespaces and visa versa.  This mechanism allows for flexible pairings between standard data sources and standard data consumers.


