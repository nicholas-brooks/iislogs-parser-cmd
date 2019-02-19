# IIS Logs Parser Cmd

Utility that reads IIS Logs from a folder, parses the UserAgent, then writes out the requests with OS and Browser information to a CSV file.  Useful to then import to Excel and analyse OS and Browser usage.

This was build for a specific need.  Clone it and make changes to Program.cs to fit your needs.

This was developed and run with .net core 2.2.


## To Run

```
cd app
dotnet run SOURCE_FOLDER DESTINATION_PATH
```


## To Build

```
cd app
dotnet build
```


## Thanks To

Thanks to the devs of the following repos (nuget packages).  They make this util easy:

* https://github.com/Kabindas/IISLogParser
* https://github.com/ua-parser/uap-csharp
