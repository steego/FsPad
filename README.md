# FsPad

This is prototype a fork of Gustavo Leon's FsPad project.

## Getting Started

1. Install the NuGet package [Steego.FsPad](https://www.nuget.org/packages/Steego.FsPad) in your solution
2. Create an fsx to load Steego.FsPad and configure your interactive printer


## Example Script - SteegoFsPad.fsx
```fsharp
#I "./bin/Debug"  //  Set this to the location where the package was installed

#r "Suave.dll"    
#r "Steego.FsPad.dll"

//  This is the default port for the web server
let browser = FsPad.Web(8080)

//  Provides a function to send values directly to the browser
let dump(value:'a) = browser.Dump(value, 5)

let autoPrint(value:'a) = 
    dump(value) |> ignore
    sprintf "%A" value

//  Configures your FSI printer to use this as a printer
//  Comment this out if you don't want values automatically printing
fsi.AddPrinter (autoPrint)
```
