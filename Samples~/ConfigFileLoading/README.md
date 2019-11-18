# Overview

This sample demonstrates a simple mapping of ini properties to a blob asset,
and usage of the conversion framework without matching Authoring/SO components.

# The MonoBehaviour Components

## IniFileExample

This script parses the provided IniContents string field, and sets the "file" name for the ini file data.
It then matches the fields from the file to the [IniFile](#inifile) described below.

# The DOTS Components

## IniFile

This struct is the Blob Asset and contains the corresponding fields.
- Name (BlobString) - the name of the ini file.
- Bar (BlobString)
- Foo (int)
- Baz (float)
- Qux (Speed) - uses the Speed Enum described below.

## Speed
An enum to demonstrate enum parsing and conversion has 3 values.
- Slow
- Medium
- Fast

# The DOTS Systems

## IniFileConvertSystem

Derives from ```SingletonBlobConvertSystem<IniFile>``` to handle the boilerplate for us. 

## IniFileChangedSystem

Derives from ```SingletonBlobChangedComponentSystem<IniFile>``` to report the converted singleton data and verify success.
Handles the boilerplate for us.