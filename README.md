# InTheHand.AppCenter.Insights
Wrapper for Visual Studio AppCenter to support the legacy Xamarin Insights API

[![NuGet version](https://badge.fury.io/nu/inthehand.appcenter.insights.svg)](https://badge.fury.io/nu/inthehand.appcenter.insights)

## Usage

- Remove the Xamarin.Insights NuGet package from your projects. 
- Add this package instead (InTheHand.AppCenter.Insights), this will also add the AppCenter dependencies.
- Create a new app on AppCenter for your chosen platform(s).
- Replace the apiKey in your Initialize call with the id from your AppCenter app.
- Rebuild, repackage and deploy

## Unsupported Methods

- PurgePendingCrashReports
- Save
- DisableCollection
- DisableCollectionTypes
- ForceDataTransmission