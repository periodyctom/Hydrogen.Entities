# Overview

This sample demonstrates simple usage of basic singleton converters.
It also shows demonstrates how the DontReplace flag works.

## The Sample Scenes
The Scenes are preset to demonstrate the following features, ensure they are added to your build settings to test them properly!
- SimpleConverters is the main demo scene, and is setup with examples of all authoring component types.
    - The SampleSceneLoader will also load other example scenes to demonstrate use of the DontReplace flag, and loading ECS subscene data.
- DontReplaceConverters is a scene setup to demonstrate the DontReplace flag on the Authoring Components.
    - If you open it on its own and click play, you should see the data load. If you load it after the singletons are set, those authoring components won't have their data set.
- SimpleSubSceneLoader is setup to load the SimpleSubScene that contains prepared converter data.
    - You will need to open SimpleSubSceneLoader and Rebuild the Entity cache via the SubScene component on the SimpleSubScene GameObject in order for the subscene to load correctly.

# Implementing a Simple IComponentData Singleton
The simplest singleton component type is just a normal IComponentData struct used with the singleton API.

In order to get a singleton data converter working, you need to do the following:
1. Define your Singleton Component Data, as a ```struct``` implementing ```IComponentData```. 
    - Ensure it is marked as ```Serializable``` so you can modify it in the editor.
1. Define the Authoring component that can live in a scene (and be converted to ECS sub-scenes) using ```SingletonConvertDataAuthoring<T>```.
    - With your T0 as your Singleton Component Data Type. 
    - This will show your Singleton Data as the editable "Source" field in the component inspector for your Authoring component. 
    - The class body can be empty.
1. Implement a Convert System by sub-classing ```SingletonConvertSystem<T>```, where T is your Singleton Component Data Type.
    - The class body can be empty.
1. In order for everything to work correctly, you need to add the following assembly Attribute: 
    - ```[assembly: RegisterGenericComponentType(typeof(SingletonConverter<T>))]``` to your assembly (either in the files defining your data, or in an Assembly.cs/AssemblyInfo.cs central location).
    - where T is your Singleton Component Data type. 
    - This will register the converter payload struct with Unity's ECS, as generic components need to be registered via that attribute.

That's all you need to support a basic Singleton Component Data Converter for non asset blob data.
If you simply need to read the data, you can do so via the standard ECS Singleton APIs.

The [MeaningofLife Sample](./MeaningOfLifeAuthoring.cs) demonstrates these basics.

# ScriptableObject conversion

For more complex data, the Unity ECS API provides Blob Assets. 
As these have special lifetime rules and memory must be managed, they have their own extra requirements to work with them.

The ```SingletonBlob*``` family of Systems and the ```BlobRefData<T>``` struct help with the following: 
- Simplify boilerplate code, the user only has to provide the blob conversion code and derive/declare a few sub-classes.
- Manage memory correctly when dealing with ECS sub-scenes. 
    - Since the memory used by ```BlobAssetsReference<T>``` belongs to a sub-scene (if cooked into that format), we copy it into the global ```BlobAssetReference<T>``` memory pool after scene load.
 
If you have source code access to the SO (you defined it), you can use the [Interface SO Conversion](#interface-so-conversion) route.
If you do not have source code access to the SO (3rd party asset or Unity package), you can use the [Custom SO Conversion](#custom-so-conversion) route.

## Common Conversion Steps

The common steps for both Interface and Custom SO -> Blob conversion are as follows:
1. Define your Blob Asset ```struct``` using the Blob Asset structures, such as ```BlobPtr<T>```, ```BlobString```, and ```BlobArray<T>```. 
    - Simpler/Smaller fields can be defined directly on the blob struct.
1. Implement a Convert System by sub-classing ```SingletonBlobConvertSystem<T>```, where T is your Blob struct Type.
    - The class body can be empty.
1. Register the ```BlobRefData<T>``` type with the assembly: ```[assembly: RegisterGenericComponentType(typeof(BlobRefData<T>))]```
    - where T is your Asset Blob Type.
1. Register the ```SingletonConverter<BlobRefData<T>>``` type with the assembly: ```[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<T>>))]```
    - where T is your Asset Blob Type.
 
## Interface SO Conversion

If you have the source code access to the SO, you can simply implement the ```IConvertScriptableObjectToBlob<T0>``` on your SO.

In order to get a singleton blob interface conversion authoring working, you need to do the following:

1. Implement the ```IConvertScriptableObjectToBlob<T0>``` interface on your ScriptableObject class. 
1. Define the Authoring component that can live in a scene (and be converted to ECS sub-scenes) using ```SingletonConvertBlobInterfaceAuthoring<T0, T1>```.
    - Where T0 is the Blob Asset struct type, and T1 is the concrete ScriptableObject type.
    - The Source Parameter will be an SO object field property.
    - The class body can be empty.
    
The [NameList Sample](./NameListDefinition.cs) demonstrates this concept.

## Custom SO Conversion

For the cases where you do not have access to the source code for the SO, you can use the custom SO -> blob function pipeline instead.
Using this is largely the same as Interface SO Conversion, with the following differences:
1. Instead of Implementing the conversion interface, you implement a function whose signature matches the following delegate: 
    - ```public delegate BlobAssetReference<T1> ScriptToBlobFunc<in T0, T1>(T0 src, ScriptableObjectConversionSystem convert)```
    - where T0 is your ScriptableObject type
    - where T1 is your Asset Blob type
1. Sub-class ```SingletonConvertCustomAuthoring<T0, T1>``` instead of the interface version.
    - Where T0 is the Blob Asset struct type, and T1 is the concrete ScriptableObject type.
    - Override the ScriptToBlob property getter to return the conversion delegate.
    - The Source Parameter will be an SO object field property.
    - The class body can be empty.

The [FooBarBaz Sample](./FooBarBaz.cs) demonstrates this concept.

# Reacting to Conversion Attempts

Sometimes you want to react to the outcomes of conversion attempts. 
There are 2 main system families for handling this, [SingletonChangedSystems](#singletonchangedsystems) for handling successful changes,
and [SingletonUnchangedSystems](#singletonunchangedsystems) for when none of the converters being processed are set.

Simple examples of this are in the test files that accompany the package and for a few of the preceding sample implementations. 

## SingletonChangedSystems

You can easily react to a loaded or updated Singleton Data via the ```SingletonChanged*System``` family of Component Systems.
One example is calling various settings APIs if your singleton represents data from an options menu. 
These will automatically set up the required query boilerplate to only run when Singleton Conversion has taken place.

The following base classes are provided for this purpose:
- ```SingletonChangedComponentSystem<T>``` where T is the Singleton Component Data type.
- ```SingletonBlobChangedComponentSystem<T>``` where T is the blob struct type.
- ```SingletonChangedJobComponentSystem<T>``` where T is the Singleton Component Data type.
- ```SingletonBlobChangedJobComponentSystem<T>``` where T is the blob struct type.

Each has a ```ChangedQuery``` EntityQuery you can use to obtain specifics about the successfully converted candidate.

## SingletonUnchangedSystems  

There may be times when you want to react when SingletonConverter<T>s are processed, but none successfully converted.
Generally this could be for diagnostic purposes.
Like the [SingletonChangedSystems](#singletonchangedsystems), these will setup the required boilerplate for you.

The following base classes are provided for this purpose:
- ```SingletonUnchangedComponentSystem<T>``` where T is the Singleton Component Data type.
- ```SingletonBlobUnchangedComponentSystem<T>``` where T is the blob struct type.
- ```SingletonUnchangedJobComponentSystem<T>``` where T is the Singleton Component Data type.
- ```SingletonBlobUnchangedJobComponentSystem<T>``` where T is the blob struct type.

Each has a ```UnchangedQuery``` EntityQuery you can use to obtain specifics about the rejected candidates.