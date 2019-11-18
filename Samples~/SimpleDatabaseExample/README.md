# Overview

This sample shows how one might convert an SO-backed database to work with DOTS via blobs.
The source data loads from JSON into a SO, which then uses the SO -> Blob Interface and Singleton Converter frameworks.

# The MonoBehaviour Components

## DatabaseExample
 
A MonoBehaviour script that loads the JSON text into a ScriptableObject and creates a Conversion GameObject dynamically.

## DatabaseAuthoring

A component that derives from the ```SingletonConvertBlobInterfaceAuthoring<Database, DatabaseDefinition>``` class.
The DatabaseExample script creates a GameObject manually and adds this and the ConvertToEntity script to do the loading dynamically.

## DatabaseDefinition 

This ScriptableObject contains the tables of the database. 
In this sample, there's a table named "Attacks" that contains a column of "Attack" data.

## AttackDefinition

A simple class that defines Attack rows in the database.
Defines several values that can easily be stored or converted to work in Blob format.
- Attack Name (string) - the attack name, in the default string format.
- Power (int) - The strength of the attack.
- Speed (float) - How fast the attack takes to execute.
- Debuffs Flags (byte bitmask) - Bit flags of debuff status effects the attack inflicts.

# The DOTS Components

## Database

This is the main Asset Blob structure, it mirrors the DatabaseDefinition.
- Name (BlobString) - the name of the database from the SO's name field.
- Attacks (BlobArray<AttackArray>) - the array of Attack Entries. 

### DatabaseEx

This is extension class that shows using ref-based extensions for Asset Blob types.
This provides an ```GetAttackByName(this ref Database db, string name)``` function that demonstrates safe access of Asset Blob elements. 

## AttackEntry

A smaller struct that contains Name -> ID Hashes to speed up name lookups, and a ```BlobPtr<Attack>``` to the actual attack data. 

## Attack

This struct contains the actual Blob version of the attack data.
- Name (BlobString) - name of the attack, in blob string data format.
- Power (int) - same as in [AttackDefinition](#attackdefinition)
- Speed (float) - same as in [AttackDefinition](#attackdefinition)
- Debuffs Flags (byte bitmask) - same as in [AttackDefinition](#attackdefinition)

## Debuffs

A bitmask that demonstrates debuff attack status effects.
- None = 0
- Burn = 1
- Soak = 0x2
- Shock = 0x4
- Freeze = 0x8
- Dizzy = 0x10
- Poison = 0x20
- Slow = 0x40
- Wither = 0x80

# The DOTS Systems

## DatabaseConvertSystem

This derives from the ```SingletonBlobConvertSystem<Database>``` that handles the convert boilerplate for us.

## DatabaseChangedSystem

This reacts to the Database being loaded and demonstrates main thread usage.
Derives from ```SingletonBlobChangedComponentSystem<Database>``` to handle the boilerplate for us.
