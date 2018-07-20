# Babel Fish

An app that shows how to use the Microsoft Translator Speech APIs to build a real-time voice translation app for the Universal Windows Platform, even for Windows 10 IoT Core.

![A screenshot of the UWP desktop version](https://raw.githubusercontent.com/marcominerva/BabelFish/master/Screenshots/App.png)

![The Windows 10 IoT Core device running the app](https://raw.githubusercontent.com/marcominerva/BabelFish/master/Screenshots/App-IoT.jpg)


**Getting started**

First of all, you need to register for a *Translator Speech* service on the [Azure Portal](https://portal.azure.com/#create/Microsoft.CognitiveServicesSpeechTranslation), in order to obtain the key that is requested by the app. Then, you can either insert the key in the [Constants.cs](https://github.com/marcominerva/BabelFish/blob/master/BabelFish/Common/Constants.cs#L11) file or create a file named *settings.babelfish* and put it in the My Documents folder:

```
{
  "speechSubscriptionKey": "your_key",
  "source":"it",
  "translation":"en",
  "voice":"en-US-Zira",
  "autoConnect": true
}
```

Parameters are self-explanatory. This file is used to automatically configure the app and is useful in particular for Windows 10 IoT Core, as in this case you haven't a UI.

**Contribute**

The project is continuously evolving. We welcome contributions. Feel free to file issues and pull requests on the repo and we'll address them as we can.