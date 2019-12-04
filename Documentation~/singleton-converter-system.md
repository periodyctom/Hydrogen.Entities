# **SingletonConvertSystem&lt;T&gt;**

The SingletonConvertSystem uses the **SingletonConverter&lt;T&gt;** struct type to act as a payload for singleton data components that can be saved and loaded in a Scene or subscene.

With pure data components, you can use it as-is (if you wish to manually install it into your Systems setup).
The recommended approach is to create a concrete sub-class that can function with the normal System bootstrapping.

For Example:
```cs
public sealed class TimeConfigConvertSystem : SingletonConvertSystem<TimeConfig> { }
``` 

## **SingletonConvertGroup**

By default, systems deriving from **SingletonConvertSystem&lt;T&gt;** will run in SingletonConvertGroup.
This system group ensures that all conversions happen after scene loading and before [modifications based on them](#singletonpost-convertgroup) occur.

## **SingletonConverter&lt;T&gt;**

The **SingletonConverter&lt;T&gt;** [IComponentData struct](https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.IComponentData.html) that solves issues with loading singleton data from subscenes.
It wraps the actual Singleton component data that the user intends to set. It also provides a simple conflict resolution mechanism for different subscenes loading or reloading a singleton's data.
Using the ```bool DontReplace;``` flag, you can specify that a SingletonConverter&lt;T&gt; loaded in a later subscene won't override data if it's already been set. This is most useful for allowing individual levels to load their own bootstrap configs if loaded without going through the normal application flow.
An example of this would be having a root/boostrap scene that normally loads all the app config data at startup, but later scenes can also load their own minimum config settings for testing in-editor.
  
> **NOTE** A Singleton Converter's concrete type must be [registered in the assembly that contains it](https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.RegisterGenericComponentTypeAttribute.html).

For SingletonConverters such as this:
```cs
SingletonConverter<BlobDataRef<Locales>>
SingletonConverter<TimeConfig>
```

They need to be registered with [assembly attributes](https://docs.microsoft.com/en-us/dotnet/standard/assembly/set-attributes) like this:
```cs
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<BlobRefData<Locales>>))]
[assembly: RegisterGenericComponentType(typeof(SingletonConverter<TimeConfig>))]
```

## **SingletonConverted**

The **SingletonConverted** tag struct is useful when you want to monitor for Singleton data changes. This can be useful to provide systems that only run when a conversion has taken place for potentially expensive or far-reaching operations.
One example is having a singleton store application-wide options that are presented on an options screen. When the options singleton has changed, we only then apply the changes.

For Example: 
```cs
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(TimeConfigConvertSystem))]
public sealed class TimeConfigRefreshSystem : ComponentSystem
{
    protected override void OnCreate()
    {
        RequireForUpdate(
            GetEntityQuery(
                ComponentType.ReadOnly<SingletonConverter<TimeConfig>>(),
                ComponentType.ReadOnly<SingletonConverted>()));

        RequireSingletonForUpdate<TimeConfig>();
    }

    protected override void OnUpdate()
    {
        var config = GetSingleton<TimeConfig>();

        Time.fixedDeltaTime = config.FixedDeltaTime;
        Application.targetFrameRate = (int) config.AppTargetFrameRate;

        Debug.Log("Updated Time Config!");
    }
}
```

# **SingletonBlobConvertSystem&lt;T&gt;**

The SingletonBlobConvertSystem uses the [SingletonConverter&lt;T&gt;](#singletonconvertert) and [BlobRefData&lt;T&gt;](#blobrefdatat) structs to simplify conversion of Singleton data that references a Blob Asset.

Unlike simpler component data, The blob asset created/loaded and assigned to the SingletonConverter&lt;T&gt; will not survive scene reload if loaded from a sub-scene.
As there's no easy way to transfer this ownership from the subscene memory block to the Entity World in general, this system provides an abstract method that must be overloaded to safely perform the copy.
The method provided for this purpose is ```protected abstract override BlobRefData<T> Prepare(BlobRefData<T> data);```.

For Example:
```cs
public sealed class LocalesConvertSystem : SingletonBlobConvertSystem<Locales>
{
    protected override BlobRefData<Locales> Prepare(BlobRefData<Locales> data)
    {
        var b = new BlobBuilder(Allocator.Persistent);
        ref Locales src = ref data.Resolve;

        ref Locales dst = ref b.ConstructRoot<Locales>();

        int availLen = src.Available.Length;
        BlobBuilderArray<NativeString64> dstAvailable = b.Allocate(ref dst.Available, availLen);

        for (int i = 0; i < availLen; i++)
        {
            ref NativeString64 srcStr = ref src.Available[i];
            ref NativeString64 dstStr = ref dstAvailable[i];
            dstStr = srcStr;
        }

        ref NativeString64 srcDefault = ref src.Default.Value;
        ref NativeString64 dstDefault = ref b.Allocate(ref dst.Default);
        dstDefault = srcDefault;

        BlobAssetReference<Locales> reference = b.CreateBlobAssetReference<Locales>(Allocator.Persistent);

        data.Value = reference;
        
        b.Dispose();

        return data;
    }
}
```

## **BlobRefData&lt;T&gt;**

The BlobRefData&lt;T&gt; struct provides a simple generic way of storing a BlobAssetReference&lt;T&gt; as an [IComponentData](https://docs.unity3d.com/Packages/com.unity.entities@0.3/manual/component_data.html) struct.
It provides a ```ref T Resolve``` getter property for direct access to the blob data, and only contains the BlobAssetReference&lt;T&gt; as it's value.

## **SingletonChanged**

The SingletonChanged component marks a converter that's value was successfully set as the singleton value.
You can use this to [react to singleton loads and changes](./reacting-to-singleton-changes.md) for doing additional setup, if needed. 

## **SingletonUnchanged**

The SingletonUnchanged component marks a converter that's value was not set as the singleton.
You can use this to [respond to/diagnose change issues](./reacting-to-singleton-changes#handling-conversion-issues).

# Working Examples
For more examples, see the samples that come with the package.
