# Changelog

## [0.2.2-preview.2] - 2020-02-17
- Minor fix for an API rename

## [0.2.2-preview.1] - 2019-11-04
- Removed unnecessary system ordering attributes from sample systems, thanks to the system group additions.

## [0.2.2-preview.0] - 2019-11-03
- Added SingletonConvertGroup and SingletonPostConvertGroup to simplify managing order for conversion systems.
- SingletonConvertSystem<T> now all by default run in SingletonConvertGroup
- All SingletonChanged* variant systems now run in SingletonPostConvertGroup
- Bumped required Entities version to 0.3.0-preview.0, since it fixes several ConvertToEntity issues. 

## [0.2.1-preview.1] - 2019-11-30
- Fixed Warnings with using manual GO conversion in tests due to API changes.

## [0.2.1-preview.0] - 2019-11-27
- Fixed issues with Entities 0.2.0 compatibility
- Updated tests to fix issues with bugs being introduced and fixed in the API.

## [0.2.0-preview.3] - 2019-11-18
- Added 3 working samples based on user feedback.
- Added more base classes to reduce boilerplate for common system patterns.
- Eliminated need to override a copy function for changing out Blob Asset Singletons.

## [0.2.0-preview.2] - 2019-11-05
- Fixed Hydrogen.Entities and Hydrogen.Entities.Hybrid asmdefs not being auto-referenced for projects not using asmdefs.
- Added Basic Converter Samples.

## [0.2.0-preview.1] - 2019-10-17
- Removed Obsolete parts of the API

## [0.2.0-preview.0] - 2019-10-17
- Obsoleted old Config and Singleton API, in favor of the new SingletonConverter<T> Framework
- Added full unit Test coverage for both ScriptableObjectConversionSystem, and the new SingletonConverter<T> Framework.
- Split things up into more assembly definitions.
- Blob Singletons loaded via a converter in a subscene will now copy instead of assign to avoid invalid memory access
 errors
- Rewrote Documentation

## [0.1.3] - 2019-09-05
- Fixed Incorrect asmdef setting
- Began work on new Conversion Framework.

## [0.1.2] - 2019-08-19
- Simplified IConvertScriptableObjectToBlob API surface.
- User can now access the GameObjectConversionSystem from ScriptableObjectConversionSystem's GoConversionSystem property.

## [0.1.1]

Minor adjustments to package naming.
Added Initial Documentation

## [0.1.0]
Initial commits.

Config and BlobAsset/ref helpers.
