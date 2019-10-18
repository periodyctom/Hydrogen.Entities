# Changelog

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