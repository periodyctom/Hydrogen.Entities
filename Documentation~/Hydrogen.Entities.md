
# Overview
Hydrogen.Entities contains several utilities for working with Unity's ECS framework.
In particular it has support for creating singletons for configuration data and creating ```AssetBlobReferences<T>``` from ScriptableObjects.
This makes it easier to use Pure(er) ECS data and less need for overhead when accessing shared configuration data.

There are several major parts to the framework, which is split into pure ECS (Hydrogen.Entities) and the Hybrid/authoring scripts (Hydrogen.Entities.Hybrid).

# Hydrogen.Entities

## Singletons

The singleton class helps with creating/converting data into ECS Singletons. While the Singleton pattern is normally ill-advised in an OOP context, here the "order of singleton startup" issues can be avoided. This is due to the way the query system works, so you can have systems that don't run until all the needed configuration is loaded, instead of relying on unstable ordering or manual initializations.

Since Unity's ECS Singleton data is currently a bit hard to work with, the Singletons static class offers various utilities to either create singletons from supplied data, or make an existing single-component entity into a singleton as part of a complex setup script. There are functions that return or auto-dispose the query used to create the singleton, depending on your needs for it afterwards.

## Configuration

There are several utility Interfaces, structs, and Systems related to managing application configuration data, particularly for ```BlobAssetReferences<T>``` converted from ScriptableObjects.

Some parts are intended to work with the ECS Singletons concept, but some can be used to have non-singleton configuration data for objects if you don't need to keep the managed object asset around. 

### ```IResolveRef<T>```
An interface the provides a simpler reference access for structs that contain a primary ```BlobAssetReference<T>```. Note that the ```Resolve``` property should only be safe to call if the BlobAsset is correctly constructed.
- ```T must be a struct```

### ```IConfigRef<T>```
An interface that is used to define a component that holds a configuration data ```BlobAssetReference<T>``` and allows direct access to it.
- Requires implementing ```IResolveRef<T>```.

### ```ConfigReload<T0, T1>```
A generic component that when created as a singleton, can be used to trigger a reload for infrequent/singlularly set data. Such as primary simulation framework, or the data in a user-configurable options screen.
- ```T0 must be an IConfigRef<T1>```
- ```T1 must be a struct```

A ```ConfigReload<T0, T1>``` should be automatically destroyed by the related ```ConfigSystem<T0, T1>```.
- ```T0 must be an IConfigRef<T1>```
- ```T1 must be a struct```

### ```ConfigSystem<T0, T1>```
An abstract ```ComponentSystem``` class that only runs if a singleton and it's matching ```ConfigReload<T0, T1>``` are both existant.
- ```T0 must be an IConfigRef<T1>```
- ```T1 must be a struct```

Provides an overloaded method where config updates are intended to be set: ```UpdateConfig(T0 configRef)```

# Hydrogen.Entities.Hybrid

The Hybrid authoring interface contains helpers for working with Config data and converting ScriptableObjects into more pure ECS data.

# ```ScriptableObjectConversionSystem```
Similar to and piggybacking on the GameObject conversion pipeline. The ScriptableObjectConversionSystems has functions for creating ```BlobAsset<T>``` and ```BlobAssetReference<T>``` to them from ```ScriptableObjects```.

The conversion system caches created BlobAssetReferences<T> so given the same inputs, you should return the same result. This allows a converted SO to be referenced in more than one place.

The conversion flow is also given access to the GameObjectConversionSystem used in converting GOs, so you can create SO conversions that reference converted prefabs, given the originating prefab references!

There are 2 main conversion routes:

## ```IConvertScriptableObjectToBlob<T>```
An interface that should be used on a ```ScriptableObject``` that defines it's own conversion into a ```BlobAsset<T>```. This is used to create a BlobAssetReference<T> that points to the converted SO blob and can either be assigned in one place, or shared.


## ```ScriptToBlobFunc<in T0, T1>```
A delegate for manual conversions that do not utilize the ```IConvertScriptableObjectToBlob<T>``` workflow, primarily intended for scenarios where access to the original SO code is limited or non-existant. Such as Unity-developed config data, or 3rd-party assets/packages where code changes would be overwritten.

# Config Singletons

## ```ConvertSingleton<T0, T1>```
Abstract class that defines a conversion flow from a referenced scriptable object to a Singleton ECS entity. Sub-classes provide hooks for specific conversion types, although users can just use this class as a base for custom conversion flows.
- ```T0 must be a ScriptableObject```
- ```T1 must be an IComponentData struct```

## ```ConfigSingleton<T0, T1, T2>```
Derives from ```ConvertSingleton<T0, T1>``` but handles the conversion with IConvertScriptableObject<T>.
- ```T0 must be a ScriptableObject, and must implement IConvertScriptableObject<T2>```
- ```T1 must implement IConfigRef<T2>```
- ```T2 must be a struct```

### ```ConfigSingletonWithReload<T0, T1, T2>```
Derives from ```ConfigSingleton<T0, T1, T2>``` and automatically creates a ```ConfigReload<T1, T2>``` singleton to force relevant ConfigSystem(s) to update. Only useful if the data is used to set Application-wide data once or very infrequently.

## ```ManualConfigSingleton<T0, T1, T2>```
Derives from ```ConvertSingleton<T0, T1>``` but handles the conversion with ScriptToBlob delegate returned from the user-implemented ManualConversion property. This allows the user to return pre-cached delegates or even select from a set of implementations if needed.

### ManualConvertConfigSingleton
Derives from ```ManualConfigSingleton<T0, T1, T2>``` and automatically creates a ```ConfigReload<T1, T2>``` singleton to force relevant ConfigSystem(s) to update. Only useful if the data is used to set Application-wide data once or very infrequently.
