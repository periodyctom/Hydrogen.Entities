
# **Reacting to Singleton Changes**

Often, you can just use a singleton directly in a system once it's been set, using the [RequireSingletonForUpdate&lt;T&gt;](https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.ComponentSystemBase.html?q=ComponentSystemBase#Unity_Entities_ComponentSystemBase_RequireSingletonForUpdate__1) API. 
However, there are times when you want to perform an operation only when the singleton has been created or modified. 
There are several helper Component and System types associated with the Singleton Conversion framework that can simplify setting up such reactive systems by setting up the boilerplate for you.

## Handling Changes

The following systems you can derive from to respond to singleton conversion changes on the main thread.
You can access the **ChangedQuery** [EntityQuery](https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.EntityQuery.html) if you require query access.
Those that run on the main thread derive from **ComponentSystems**, those that are jobified derive from **JobComponentSystem**
Simply implement ```OnUpdate()``` or ```JobHandle OnUpdate(JobHandle inputDeps)``` as you would any other ComponentSystem. 

- **SingletonChangedComponentSystem&lt;T&gt;**
   - **SingletonBlobChangedComponentSystem&lt;T&gt;** 
-  **SingletonChangedJobComponentSystem&lt;T&gt;**
   - **SingletonBlobChangedJobComponentSystem&lt;T&gt;**

As [SingletonBlobChangedComponentSystem&lt;T&gt;](#singletonchangedjobcomponentsystemt), but allows you to schedule jobs.

## Handling Conversion Issues

You can have issues with setting a singleton from a converter if you load multiple converters of the same component type.
You can diagnose these issues with following systems:
As with the changed counterparts above, simply implement ```OnUpdate()``` or ```JobHandle OnUpdate(JobHandle inputDeps)``` as you would any other ComponentSystem.

- **SingletonUnchangedComponentSystem&lt;T&gt;**
   - **SingletonBlobUnchangedComponentSystem&lt;T&gt;** 
-  **SingletonUnchangedJobComponentSystem&lt;T&gt;**
   - **SingletonBlobUnchangedJobComponentSystem&lt;T&gt;**

# **SingletonPostConvertGroup** 

By default, all of the changed/unchanged systems update in this group. Doing so guarantees that all conversion have completed.
If you have other logic you want to run on initialization after conversion, this is the group to run it in.
