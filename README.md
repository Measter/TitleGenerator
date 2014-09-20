TitleGenerator
==============

Full source code release for the Kingdoms Abound Titular Title Generator for Crusader Kings II, along with the MarkovChainDump helper program for building the name generator chains.

Requires the [CK2Utils](https://github.com/Measter/CK2Utils/) project.

# Overview

## CK2Data

All of the loading and accessing of data in this program is done through the CK2Data class. This is also where the markov text generators are called through when generating culture strings.

## DataUI

This is the main UI, where the user configures the output.

## ProgressPopup

This popup is where the tasks are executed; such as loading of game data, and generation and saving of the mod output.

## Tasks

These are where everything from the calls to load CK2 data, to generating and saving the mod are done. Note that this task system was implemented prior to moving the program to .Net 4, which introduced the Task class to the framework.

### ShareTask

Almost all tasks inherit from the SharedTask class, which implements functionality commonly used during mod generation. The exception to this is the LoadTask class, which doesn't need any of that, and so directly implements ITask. Classes inheriting this should override the Execute function.

The functions provided include color space conversion, storing the passed in options and logger, sending messages to the UI, and logging Generate class messages.

### CreateHistoryShared 

Furthermore, because there functions common to the history generation tasks, those inherit from CreateHistoryShared, which itself inherits from ShareTask. This task ensures the correct directories exist, creates ruleset characters, and saves province history.

Functionality provided are generating and writing characters, making characters for a list of titles, and writing title owners and lieges.
