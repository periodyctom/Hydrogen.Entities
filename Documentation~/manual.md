# Hydrogen.Entities manual

This is the manual for the Hydrogen.Entities DOTS Utility framework.

## Introduction

* [Hydrogen.Entities overview](./index.md)

## **Getting Started**

* [Installation] (./index.md#Installation)
* [Using the ScriptableObjectConversionSystem](./scriptableobject-conversion-system.md)
  * [Implementing IConvertScriptableObjectToBlob&lt;T&gt;](./scriptableobject-conversion-system.md#IConvertScriptableObjectToBlob&lt;T&gt;)
  * [Providing custom functions for externally defined ScriptableObjects](./scriptableobject-conversion-system.md#ScriptToBlobFunc&lt;in T0, T1&gt;)
* [Using SingletonConverter&lt;T&gt; and SingletonConverterSystem&lt;T&gt;](./singleton-converter-system.md)
  * [Creating and registering SingletonConverter&lt;T&gt;](./singleton-converter-system.md#SingletonConverter&lt;T&gt;)
  * [Responding to Singleton component data being loaded and converted](./singleton-converter-system.md#SingletonConverted)
  * [Converting Blob Asset Data](./singleton-converter-system#SingletonBlobConvertSystem&lt;T&gt;)