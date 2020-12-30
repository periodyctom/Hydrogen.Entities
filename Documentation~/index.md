# About Hydrogen Entities

Hydrogen.Entities is a support library that contains several utilities for working with [Unity's DOTS framework](https://unity.com/dots). 
It is designed to be as simple as possible to assist in certain types of [Entity conversions](https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.Entity.html).
 
In particular it has support for creating singleton data for configuration purposes and creating [```BlobAssetReference<T>```](https://docs.unity3d.com/Packages/com.unity.entities@0.3/api/Unity.Entities.BlobAssetReference-1.html) from ScriptableObjects.
This makes it easier to use Pure(er) ECS data and less need for overhead when accessing shared configuration data.

There are also [Unit Tests](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html) setup to ensure robustness, and to serve as examples for programmers interested in utilizing the framework.

# Installation

To install this package, you can follow the instructions for using [github packages](https://docs.unity3d.com/Manual/upm-git.html) from the [Unity Package Manager](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html).
> **Note**: The github page for the framework is https://github.com/periodyctom/Hydrogen.Entities.

# Using Hydrogen.Entities

You can read the [manual](./manual.md) for usage information.

The [table of contents](./TableOfContents.md) has a listing of the important pages.

# Technical Details

## Requirements

This version of Hydrogen.Entities has been tested with 2019.3b4+.

## Document History
| Date       | Reason                               |
| :--------- | :----------------------------------- |
| December 29, 2020 | Long overdue cleanup after getting rid of code rot. Several Unity DOTS features now go beyond what this API supports. |
| December 03, 2019 | Updated after documentation improvements and hyperlink updates. |
| October 14, 2019 | Updated after major revision to 0.2.0 |