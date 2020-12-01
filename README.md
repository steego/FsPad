# FsPad

This is prototype a fork of Gustavo Leon's FsPad project.

## Getting Started

1. Install the NuGet package [Steego.FsPad](https://www.nuget.org/packages/Steego.FsPad) in your solution
2. Create an fsx to load Steego.FsPad and configure your interactive printer


## Example Script - SteegoFsPad.fsx
```fsharp
#r "nuget: Steego.FsPad"

//  This is the default port for the web server
let browser = FsPad.Web(3000)

//  Provides a function to send values directly to the browser
let dump(value:'a) = browser.Dump(value, 5)

let autoPrint(value:'a) = 
    dump(value) |> ignore
    sprintf "%A" value

//  Configures your FSI printer to use this as a printer
//  Comment this out if you don't want values automatically printing
fsi.AddPrinter (autoPrint)
```


##  Example Script with using LINQPad's Object Dumper 
```fsharp
#I "./bin/Debug"

#r "Suave.dll"
#r "Steego.FsPad.dll"
#r @"C:\Program Files (x86)\LINQPad5\LINQPad.exe"

open FsPad

let browser = FsPad.Web(8080)

let dump(value:'a) = browser.Dump(value, 5)

let autoPrint(value:'a) = 
    //  Use LINQPad to convert the value into HTML
    let html = LINQPad.Util.ToHtmlString(3, value :> obj)
    //  Then send it to the browser
    dump(UnprotectedHtml(html)) |> ignore
    sprintf "%A" value

fsi.AddPrinter (autoPrint)
```
